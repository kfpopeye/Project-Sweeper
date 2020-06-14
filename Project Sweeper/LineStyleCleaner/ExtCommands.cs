using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Collections.ObjectModel;
using log4net;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PKHL.ProjectSweeper.LineStyleCleaner
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    public class main_command : IExternalCommand
    {
        private readonly ILog log = LogManager.GetLogger(typeof(main_command));
        private Document TheDoc = null;
        private UIDocument TheUIDoc = null;
        private const double TOLERANCE = 0.0000000000001;
        private Dictionary<string, ElementId> ModelGroupTypeDictionary = null;
        private ElementId ThinLineStyleId = null;
        private System.Text.StringBuilder ResultOutput = null;
        private ObservableCollection<LineStyleDefinition> allLineStyles = null;
        private IList<LineStyleDefinition> selectedStyles = null;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
#if !DEBUG
            if (!PKHL.ProjectSweeper.ProjectSweeper.UserIsEntitled(commandData))
                return Result.Failed;
#endif

                //no api access to lines created with linework tool
                TheUIDoc = commandData.Application.ActiveUIDocument;
                TheDoc = commandData.Application.ActiveUIDocument.Document;
                allLineStyles = new ObservableCollection<LineStyleDefinition>();

                //Collect all graphicsStyles and filter lines
                FilteredElementCollector collector = new FilteredElementCollector(TheDoc);
                IList<Element> linestyleselements = collector.WherePasses(new ElementClassFilter(typeof(GraphicsStyle))).ToElements();
                foreach (Element el in linestyleselements)
                {
                    GraphicsStyle gs = el as GraphicsStyle;
                    if (gs != null && gs.GraphicsStyleCategory != null && gs.GraphicsStyleCategory.Parent != null)
                    {
                        if (gs.GraphicsStyleCategory.Parent.Id == TheDoc.Settings.Categories.get_Item(BuiltInCategory.OST_Lines).Id &&
                            graphicStyleIsValidLineStyle(gs))
                        {
                            Dictionary<int, string> OwnerViews = new Dictionary<int, string>();
                            LineStyleDefinition lsd = new LineStyleDefinition(gs);
                            //collect curve elements using the graphicStyle                           
                            IList<Element> ceList = getLinesUsingGraphicStyle(gs);
                            //count model & detail lines using style
                            foreach (CurveElement ce in ceList)
                            {
                                if (ce.get_Parameter(BuiltInParameter.CURVE_IS_DETAIL).AsInteger() == 1)
								{
                                    ++lsd.DetailLinesUsingStyle;
                                    if (ce.OwnerViewId != ElementId.InvalidElementId)
									{
										string view_name = TheDoc.GetElement(ce.OwnerViewId).Name;
										if (!OwnerViews.ContainsKey(ce.OwnerViewId.IntegerValue))
											OwnerViews.Add(ce.OwnerViewId.IntegerValue, view_name);
									}
								}
									else
										++lsd.ModelLinesUsingStyle;
                            }
                            lsd.OwnerViews = OwnerViews;
                            if (lsd.CategoryId == TheDoc.Settings.Categories.get_Item(BuiltInCategory.OST_CurvesThinLines).Id.IntegerValue)
                                ThinLineStyleId = gs.Id;
                            //add style to collection
                            allLineStyles.Add(lsd);
                        }
                    }
                }
                Debug.WriteLine("Found " + allLineStyles.Count + " line styles.");
                log.InfoFormat("Found {0} line styles in the project.", allLineStyles.Count.ToString());

                if (userSelectedSingleElement(commandData))
                    return startSingleSelectionMode(commandData);
                else
                    return startMainMode(commandData);
            }
            catch (Exception err)
            {
                log.Error(LocalizationProvider.GetLocalizedValue<string>("LSC_Title_MainWindow"), err);
                Debug.WriteLine(new string('*', 100));
                Debug.WriteLine(err.ToString());
                Debug.WriteLine(new string('*', 100));

                Autodesk.Revit.UI.TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Title"));
                td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("LSC_Title_MainWindow") + LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainInst");
                td.MainContent = LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainCont");
                td.ExpandedContent = err.ToString();
                //td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Command1"));
                td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                TaskDialogResult tdr = td.Show();

                //if (tdr == TaskDialogResult.CommandLink1)
                //{
                //    pkhCommon.Email.SendErrorMessage(commandData.Application.Application.VersionName, commandData.Application.Application.VersionBuild, err, this.GetType().Assembly.GetName());
                //}
                return Result.Failed;
            }
        }

        private Result startMainMode(ExternalCommandData commandData)
        {
            MainWindow main_win = null;
            Transaction trans = null;
            SubTransaction st = null;
            try
            {
                Debug.WriteLine("-----------Starting main window");
                main_win = new MainWindow(allLineStyles, selectedStyles, ref TheDoc);
                System.Windows.Interop.WindowInteropHelper x = new System.Windows.Interop.WindowInteropHelper(main_win);
                x.Owner = commandData.Application.MainWindowHandle;
                main_win.ShowDialog();
                Debug.WriteLine("-----------Exiting main window");
                /*  
                    pinning only prevents object from being moved, graphicsstyle can be changed
                    linked files and families do not expose their curve elements, therefore ignored
                */
                /* Result of changes to Revit project (line style is changed and original style removed)
                   - groups: if more than 1 group Revit will prompt to ungroup or cancel, if only 1 group Revit will change group
                   - lines are converted normally
                   - importinstances remain unchanged until exploded then change to thin lines
                   - filled and masking regions remain unchanged until boundary is edited then it changes to new style
                */
                /* Result of changes to Revit project (line style is removed and lines are deleted)
                  - groups: same as above
                  - lines are removed
                  - importinstances remain unchanged until exploded then change to thin lines
                  - filled and masking regions removed. If region is left open Revit will prompt to delete it.
               */

                //change line style and delete unwanted styles
                if ((bool)main_win.DialogResult)
                {
                    ResultOutput = new System.Text.StringBuilder();
                    allLineStyles = main_win.TheCollection;
                    trans = new Transaction(TheDoc);
                    trans.Start(LocalizationProvider.GetLocalizedValue<string>("LSC_Title_MainWindow"));
                    IList<ElementId> deleteTheseStyles = new List<ElementId>();
                    int stylesConverted = 0;
                    int totalElementsDeleted = 0;

                    //iterate through curveElements changing line styles
                    foreach (LineStyleDefinition lsd in allLineStyles)
                    {
                        if (lsd.StyleToBeConverted || lsd.StyleToBeDeleted || lsd.DeleteElements)
                        {
                            ResultOutput.AppendLine();
                            ResultOutput.AppendLine(lsd.StyleName);
                            ResultOutput.AppendLine(new string('-', lsd.StyleName.Length));
                        }
                        if (lsd.StyleToBeConverted || lsd.DeleteElements)
                        {
                            totalElementsDeleted += convertAndOrDeleteLines(lsd);
                            if (lsd.StyleToBeConverted) //was DeleteElements
                                stylesConverted++;
                        }
                        if (lsd.StyleToBeDeleted)
                        {
                            ResultOutput.AppendLine("\t" + LocalizationProvider.GetLocalizedValue<string>("RSLT_Queue")); // Style queued to be removed.
                            deleteTheseStyles.Add(new ElementId(lsd.ItsId));
                            deleteTheseStyles.Add(new ElementId(lsd.CategoryId));
                            Debug.WriteLine(lsd.StyleName + " is queued to be removed");
                        }
                    }

                    //and delete unwanted styles
                    int deleted = 0;
                    if (deleteTheseStyles.Count > 0)
                    {
                        st = new SubTransaction(TheDoc);
                        st.Start();
                        foreach (ElementId eid in deleteTheseStyles)
                        {
                            if (TheDoc.Delete(eid).Count > 0)
                                ++deleted;
                        }							
                        deleted /= 2; //each linestyle has a corresponding graphic style that must be deleted.
                        st.Commit();
                    }
                    trans.Commit();

                    ResultOutput.AppendLine();
                    ResultOutput.AppendLine(LocalizationProvider.GetLocalizedValue<string>("RSLT_Summary")); //summary report
                    ResultOutput.AppendLine(new string('-', LocalizationProvider.GetLocalizedValue<string>("RSLT_Summary").Length)); // -----------
                    ResultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("RSLT_Found"), allLineStyles.Count); // Found {0} style(s).
                    ResultOutput.AppendLine();
                    ResultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("RSLT_Converted"), stylesConverted); // Converted {0} style(s).
                    ResultOutput.AppendLine();
                    ResultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("RSLT_Removed"), deleted); // Removed {0} line style(s).
                    Debug.WriteLine("Removed this many line styles: " + (deleted).ToString());

                    //show result output
                    ResultWindow res_win = new ResultWindow(LocalizationProvider.GetLocalizedValue<string>("LSC_Title_MainWindow"), ResultOutput);
                    System.Windows.Interop.WindowInteropHelper x1 = new System.Windows.Interop.WindowInteropHelper(res_win);
                    x1.Owner = commandData.Application.MainWindowHandle;
                    res_win.ShowDialog();

                    return Result.Succeeded;
                }
                else
                    return Result.Cancelled;
            }
            catch (Exception err)
            {
                if (main_win != null && main_win.IsActive)
                    main_win.Close();
                if (st != null && st.HasStarted())
                    st.RollBack();
                if (trans != null && trans.HasStarted())
                    trans.RollBack();
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(err).Throw();
                throw;
            }
        }

        private Result startSingleSelectionMode(ExternalCommandData commandData)
        {
            Transaction trans = null;
            SubTransaction st = null;
            SingleElementWindow se_win = null;
            try
            {
                LineStyleDefinition selectedLsd = selectedStyles.First<LineStyleDefinition>();
                foreach (LineStyleDefinition f in allLineStyles)
                {
                    if (f.Equals(selectedLsd))
                    {
                        selectedLsd = f;
                        break;
                    }
                }

                Debug.WriteLine("-----------Starting single mode window");
                se_win = new SingleElementWindow(allLineStyles, selectedLsd, ref TheDoc);
                System.Windows.Interop.WindowInteropHelper wih = new System.Windows.Interop.WindowInteropHelper(se_win);
                wih.Owner = commandData.Application.MainWindowHandle;
                se_win.ShowDialog();
                Debug.WriteLine("-----------Exiting single mode window");

                //change line style and delete the old styles
                if ((bool)se_win.DialogResult)
                {
                    ResultOutput = new System.Text.StringBuilder();
                    selectedLsd.NewStyle = se_win.chossenStyle;
                    selectedLsd.StyleToBeDeleted = se_win.DeleteSourceStyle;

                    trans = new Transaction(TheDoc);
                    trans.Start(LocalizationProvider.GetLocalizedValue<string>("LSC_Title_MainWindow"));
                    if (selectedLsd.StyleToBeConverted || selectedLsd.StyleToBeDeleted || selectedLsd.DeleteElements)
                    {
                        ResultOutput.AppendLine();
                        ResultOutput.AppendLine(selectedLsd.StyleName);
                        ResultOutput.AppendLine(new string('-', selectedLsd.StyleName.Length));
                    }
                    if (selectedLsd.StyleToBeConverted || selectedLsd.DeleteElements)
                    {
                        st = new SubTransaction(TheDoc);
                        st.Start();
                        convertAndOrDeleteLines(selectedLsd);
                        st.Commit();
                    }

                    //and delete unwanted
                    if (selectedLsd.StyleToBeDeleted)
                    {
                        st = new SubTransaction(TheDoc);
                        st.Start();
                        TheDoc.Delete(new ElementId(selectedLsd.ItsId));
                        TheDoc.Delete(new ElementId(selectedLsd.CategoryId));
                        Debug.WriteLine("Deleted style: " + selectedLsd.StyleName);
                        st.Commit();
                        ResultOutput.AppendFormat(LocalizationProvider.GetLocalizedValue<string>("RSLT_SingleMode"), selectedLsd.StyleName); //The style {0} was removed from the project.
                    }
                    trans.Commit();

                    //show result output
                    ResultWindow res_win = new ResultWindow(LocalizationProvider.GetLocalizedValue<string>("LSC_Title_MainWindow"), ResultOutput);
                    System.Windows.Interop.WindowInteropHelper x1 = new System.Windows.Interop.WindowInteropHelper(res_win);
                    x1.Owner = commandData.Application.MainWindowHandle;
                    res_win.ShowDialog();

                    return Result.Succeeded;
                }
                else
                    return Result.Cancelled;

            }
            catch (Exception err)
            {
                if (se_win != null && se_win.IsActive)
                    se_win.Close();
                if (st != null && st.HasStarted())
                    st.RollBack();
                if (trans != null && trans.HasStarted())
                    trans.RollBack();
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(err).Throw();
                throw;
            }
        }

        /// <summary>
        /// Collects all Lines using a certain graphic style
        /// Checks if they are part of a group
        /// </summary>
        /// <param name="_lsd"></param>				   
        private int convertAndOrDeleteLines(LineStyleDefinition _lsd)
        {
            Debug.WriteLine(string.Format("convertAndOrDeleteLines of style: {0}-----------", _lsd.StyleName));
            int converted = 0, deleted = 0, thin_convert = 0;
            IList<ElementId> groupTypesToEdit = new List<ElementId>();
            IList<Curve> curvesInRegions = new List<Curve>();
            IList<Element> linesWithStyle = getLinesUsingGraphicStyle(TheDoc.GetElement(new ElementId(_lsd.ItsId)) as GraphicsStyle);
            ResultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("LSC_RSLT_FoundLines"), linesWithStyle.Count, _lsd.StyleName); // Found {0} lines using the {1} style.
            ResultOutput.AppendLine();
            Dictionary<string, bool> GroupsToExplode = new Dictionary<string, bool>();
            bool promptOncePerStyle = true;
            SubTransaction st = new SubTransaction(TheDoc);

            st.Start();
            foreach (CurveElement ce in linesWithStyle)
            {
                //check if lines are part of group and prompt to ungroup
                if (!ce.GroupId.Equals(ElementId.InvalidElementId))
                {
                    Group g = TheDoc.GetElement(ce.GroupId) as Group;
                    Debug.WriteLine(string.Format("Found a line belonging to group: {0}.", g.Name));
                    bool UngroupIt = false;
                    if (!GroupsToExplode.TryGetValue(g.Name, out UngroupIt))
                    {
                        //Revit Taskdialog DIA006
                        TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("DIA006_Title")); //Found group
                        td.MainInstruction = string.Format(LocalizationProvider.GetLocalizedValue<string>("DIA006_MainInst"), _lsd.StyleName, g.Name); //An element using the {0} style was found in the group {1}
                        td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, string.Format(LocalizationProvider.GetLocalizedValue<string>("DIA006_Command1"), g.Name)); //Ungroup group: {0} and change the element.
                        td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, LocalizationProvider.GetLocalizedValue<string>("DIA006_Command2")); //Leave the group unchanged. (The style will not be deleted).
                        if (td.Show() == TaskDialogResult.CommandLink1)
                            UngroupIt = true;
                        else
                        {
                            UngroupIt = false;
                            ResultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("RSLT_NotUngrouped"), g.Name, _lsd.StyleName); // Group {0} was not ungrouped. {1} will not be removed.
                            ResultOutput.AppendLine();
                        }
                        GroupsToExplode.Add(g.Name, UngroupIt);
                    }
                    if (UngroupIt)
                    {
                        SubTransaction stg = new SubTransaction(TheDoc);
                        stg.Start();
                        Debug.WriteLine(string.Format("Ungrouping: {0}.", g.Name));
                        ResultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("RSLT_Ungrouped"), g.Name); // Ungrouping {0} group
                        ResultOutput.AppendLine();
                        g.UngroupMembers();
                        stg.Commit();
                    }
                    else
                    {
                        _lsd.StyleToBeDeleted = false;
                    }
                    //TODO: work out group edits
                    //if (!groupTypesToEdit.Contains(g.GroupType.Id))
                    //    groupTypesToEdit.Add(g.GroupType.Id);
                }

                if (ce.GroupId.Equals(ElementId.InvalidElementId))
                {
                    if (ce.Category.Id.IntegerValue == (int)BuiltInCategory.OST_SketchLines) //is line part of a region
                    {
                        Debug.WriteLine(string.Format("Found {0} line in region.", _lsd.StyleName));
                        if (!_lsd.DeleteElements)
                        {
                            curvesInRegions.Add(ce.GeometryCurve); //so we can regenerate regions later
                            ce.LineStyle = TheDoc.GetElement(new ElementId(_lsd.NewStyle.ItsId));
                            converted++;
                        }
                        else
                        {
                            //prompt to use thin lines style or keep style
                            //TODO: add option to delete region
                            if (promptOncePerStyle)
                            {
                                promptOncePerStyle = false;
                                //Revit Taskdialog DIA007
                                TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("LSC_DIA007_Title")); //Region found
                                td.MainInstruction = string.Format(LocalizationProvider.GetLocalizedValue<string>("LSC_DIA007_MainInst"), _lsd.StyleName); //A line using the {0} style was found in a filled\\masking region.
                                td.MainContent = LocalizationProvider.GetLocalizedValue<string>("LSC_DIA007_MainCont"); //Deleting this line will leave the filled\\masking region open and therefore invalid. What would you like to do?
                                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, string.Format(LocalizationProvider.GetLocalizedValue<string>("LSC_DIA007_Command1"), _lsd.StyleName)); //Convert line to Thin Line style so style {0} can be removed.
                                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, string.Format(LocalizationProvider.GetLocalizedValue<string>("LSC_DIA007_Command2"), _lsd.StyleName)); //Do not remove style {0} and keep line as is.					 

                                if (td.Show() == TaskDialogResult.CommandLink2)
                                {
                                    _lsd.StyleToBeDeleted = false;
                                    ResultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("LSC_RSLT_NoDelete")); // {0} will not be deleted to maintain filled\\masking regions.
                                    ResultOutput.AppendLine();
                                }
                            }

                            if (_lsd.StyleToBeDeleted)
                            {
                                ce.LineStyle = TheDoc.GetElement(ThinLineStyleId);
                                curvesInRegions.Add(ce.GeometryCurve); //so we can regenerate regions later
                                thin_convert++;
                            }
                        }
                    }
                    else
                    {
                        if (_lsd.DeleteElements)
                        {
                            if (TheDoc.Delete(ce.Id).Count > 0) //delete line
                                deleted++;
                            else
                                Debug.WriteLine("Failed to delete line.");
                        }
                        else
                        {
                            ce.LineStyle = TheDoc.GetElement(new ElementId(_lsd.NewStyle.ItsId));
                            converted++;
                        }
                    }
                }
            }
            st.Commit();	
            if (thin_convert > 0)
            {
                ResultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("LSC_RSLT_Convert2Thin"), thin_convert); // Converted {0} lines to Thin Lines to maintain filled\\masking regions
                ResultOutput.AppendLine();
            }
            if (_lsd.DeleteElements)
            {
                ResultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("LSC_RSLT_DeletedLines"), deleted); // Deleted {0} lines
                ResultOutput.AppendLine();
            }
            if (_lsd.StyleToBeConverted)
            {
                ResultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("LSC_RSLT_ConvertedLines"), converted, _lsd.NewStyle.StyleName); // Converted {0} lines to \"{1}\"
                ResultOutput.AppendLine();
            }
            log.InfoFormat("Converted {0} lines from \"{1}\" to \"{2}\"",
                converted, _lsd.StyleName, _lsd.NewStyle.StyleName);
            log.InfoFormat("Deleted this many \"{0}\" lines {1}", _lsd.StyleName, deleted);

            Debug.WriteLine(string.Format("Converted {0} lines from \"{1}\" to Thin Lines",
                thin_convert, _lsd.StyleName, _lsd.NewStyle.StyleName));
            Debug.WriteLine(string.Format("Converted {0} lines from \"{1}\" to \"{2}\"",
                converted, _lsd.StyleName, _lsd.NewStyle.StyleName));
            Debug.WriteLine(string.Format("Deleted this many \"{0}\" lines {1}", _lsd.StyleName, deleted));
            //foreach (ElementId eid in groupTypesToEdit) //edit groups
            //{
            //    if (!convertGroupType(eid, ref _lsd))
            //        _lsd.StyleToBeDeleted = false;
            //}
            forceRegionsToRegen(curvesInRegions);
            return deleted;   //for totalling all lines deleted in summary
            //TODO: convertImports();
        }

        /// <summary>
        /// This is a workaround. Nudging a region back and forth cause Revit to regenerate the region. Changes to the linestyles won't be visible otherwise.
        /// </summary>
        private void forceRegionsToRegen(IList<Curve> _curvesList)
        {
            IList<ElementId> regionList = new List<ElementId>();
            FilteredElementCollector collector = new FilteredElementCollector(TheDoc);
            IList<Element> fr_list = collector.WherePasses(new ElementClassFilter(typeof(FilledRegion))).ToElements();
            foreach (FilledRegion fr in fr_list)
            {
                foreach (CurveLoop cl in fr.GetBoundaries())
                {
                    foreach (Curve c in cl)
                    {
                        if (removeFromList(ref _curvesList, c))
                            if (!regionList.Contains(fr.Id))
                                regionList.Add(fr.Id);
                        if (_curvesList.Count == 0)
                            break;
                    }
                    if (_curvesList.Count == 0)
                        break;
                }
                if (_curvesList.Count == 0)
                    break;
            }
            Debug.WriteLine(string.Format("Found {0} regions to nudge.", regionList.Count));
            if (regionList.Count > 0)
            {
                List<ElementId> pinnedList = new List<ElementId>();
                Element fr = null;
                try
                {
                    foreach (ElementId eid in regionList) //pinned objects will cause Revit to stop command. Constraints are ignored within transactions.
                    {
                        fr = TheDoc.GetElement(eid);
                        if (fr.Pinned)
                        {
                            pinnedList.Add(eid);
                            fr.Pinned = false;
                        }
                    }
                    Debug.WriteLine("Nudging regions.");
                    ElementTransformUtils.MoveElements(TheDoc, regionList, XYZ.BasisX); //move all regions at once to reduce regenerating time
                    ElementTransformUtils.MoveElements(TheDoc, regionList, XYZ.BasisX.Negate());
                    Debug.WriteLine("Finished moving regions.");
                    fr = null;
                    foreach (ElementId eid in pinnedList)
                    {
                        fr = TheDoc.GetElement(eid);
                        fr.Pinned = true;
                    }
                }
                catch (Autodesk.Revit.Exceptions.InvalidOperationException err)
                {
                    FilledRegionType frt = TheDoc.GetElement(fr.GetTypeId()) as FilledRegionType;
                    err.Data[LocalizationProvider.GetLocalizedValue<string>("LSC_Title_MainWindow")] += "An error occurred processing the filled region : " + frt.Name + " : " + fr.Name;
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(err).Throw();
                    throw;
                }
                Debug.WriteLine(string.Format("Changed {0} out of {1} regions.", regionList.Count, fr_list.Count));
            }
        }

        /// <summary>
        /// checks to see if a curve is stored in a list and removes it. Returns true if removed, false otherwise.
        /// This helps reduce time spent searching for filled regions that need regenerating.
        /// </summary>
        private bool removeFromList(ref IList<Curve> _theList, Curve _theItem)
        {
            foreach (Curve c in _theList)
            {
                //bug fix (5/19/2016) using true param to evaluate causes exception (cannot normalize unbound curves)
                if (c.Evaluate(0, false).IsAlmostEqualTo(_theItem.Evaluate(0, false), TOLERANCE) &&
                    c.Evaluate(1, false).IsAlmostEqualTo(_theItem.Evaluate(1, false), TOLERANCE))
                {
                    _theList.Remove(c);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// converts the line styles in a group. Returns true if successful otherwise false.
        /// </summary>
        private bool convertGroupType(ElementId _groupTypeId, ref LineStyleDefinition _lsd)
        {
            GroupType groupTypeToModify = TheDoc.GetElement(_groupTypeId) as GroupType;
            string originalName = groupTypeToModify.Name;
            Element newStyle = TheDoc.GetElement(new ElementId(_lsd.NewStyle.ItsId));

            if (groupTypeToModify.Category.Id.IntegerValue == (int)BuiltInCategory.OST_IOSAttachedDetailGroups)
            {
                Debug.WriteLine("Found attached detail group type: " + originalName + " for line style: " + _lsd.StyleName);
                _lsd.StyleToBeDeleted = false;
                //Revit Taskdialog DIA008
                TaskDialog.Show(LocalizationProvider.GetLocalizedValue<string>("LSC_DIA008_Title"), //Unsupported detail group
                    string.Format(LocalizationProvider.GetLocalizedValue<string>("LSC_DIA008_MainInst"), //Cannot modify detail group \"{0}\" because it is attached to a model group. Line style \"{1}\" will not be deleted.
                    originalName, _lsd.StyleName));
                return false;
                //GroupSetIterator gsItr = grt.Groups.ForwardIterator();
                //gsItr.Reset();
                //gsItr.MoveNext();
                //Group atdetailGroup = gsItr.Current as Group;
                //Parameter p = atdetailGroup.get_Parameter(BuiltInParameter.GROUP_ATTACHED_PARENT_NAME);
                //string mgName = p.AsString();
                //Group modelGroup = getModelGroupByName(mgName);

                //List<ElementId> eidList = atdetailGroup.UngroupMembers().ToList();
                //eidList.AddRange(modelGroup.UngroupMembers().ToList());

                //int converted = 0; int deleted = 0;
                //SubTransaction st = new SubTransaction(TheDoc);
                //st.Start();
                //for (int i = eidList.Count - 1; i >= 0; i--)
                //{
                //    CurveElement ce = TheDoc.GetElement(eidList[i]) as CurveElement;
                //    if (ce != null && ce.LineStyle.Id.IntegerValue == _lsd.ItsId)
                //    {
                //        if (_lsd.NewStyle.ItsId != -1)
                //        {
                //            ce.LineStyle = newStyle; //convert lines
                //            converted++;
                //        }
                //        else
                //        {
                //            TheDoc.Delete(ce.Id); //delete lines
                //            eidList.RemoveAt(i);
                //            deleted++;
                //        }
                //    }
                //}
                //if (converted > 0) Debug.WriteLine(string.Format("Converted {0} lines from \"{1}\" to \"{2}\"", converted.ToString(), _lsd.StyleName, _lsd.NewStyle.StyleName));
                //if (deleted > 0) Debug.WriteLine(string.Format("Deleted this many \"{0}\" lines {1}", _lsd.StyleName, deleted.ToString()));

                ////create new group
                //int modified = 0;
                //Group newGroup = TheDoc.Create.NewGroup(eidList);
                //st.Commit();
                //st = new SubTransaction(TheDoc);
                //st.Start();
                //foreach (Group g in modelGroup.GroupType.Groups)
                //{
                //    g.GroupType = newGroup.GroupType;   //TODO: attached detail group is not shown by default????????
                //    modified++;
                //}

                //newGroup.GroupType.Name = mgName; //rename group to existing name
                ////TheDoc.Delete(newGroup.Id);
                //st.Commit();
                //Debug.WriteLine(string.Format("Finished modifying group type {0} : {1}. Modified {2} groups.", mgName, originalName, modified));
                //return true;
            }
            else
            {

                if (groupTypeToModify.Category.Id.IntegerValue == (int)BuiltInCategory.OST_IOSDetailGroups)
                    Debug.WriteLine("Modifying detail group type: " + originalName + " for line style: " + _lsd.StyleName);
                else if (groupTypeToModify.Category.Id.IntegerValue == (int)BuiltInCategory.OST_IOSModelGroups)
                    Debug.WriteLine("Modifying model group type: " + originalName + " for line style: " + _lsd.StyleName);

                //get a group to modify
                GroupSetIterator gsitr = groupTypeToModify.Groups.ForwardIterator();
                gsitr.Reset();
                gsitr.MoveNext();
                Group editGroup = gsitr.Current as Group;

                int converted = 0; int deleted = 0;
                //ungroup members and collect members
                SubTransaction st = new SubTransaction(TheDoc);
                st.Start();
                List<ElementId> eidList = editGroup.UngroupMembers().ToList();
                for (int i = eidList.Count - 1; i >= 0; i--)
                {
                    CurveElement ce = TheDoc.GetElement(eidList[i]) as CurveElement;
                    if (ce != null && ce.LineStyle.Id.IntegerValue == _lsd.ItsId)
                    {
                        if (_lsd.NewStyle.ItsId != -1)
                        {
                            ce.LineStyle = newStyle; //convert lines
                            converted++;
                        }
                        else
                        {
                            TheDoc.Delete(ce.Id); //delete lines
                            eidList.RemoveAt(i);
                            deleted++;
                        }
                    }
                }
                if (converted > 0) Debug.WriteLine(string.Format("Converted {0} lines from \"{1}\" to \"{2}\"", converted.ToString(), _lsd.StyleName, _lsd.NewStyle.StyleName));
                if (deleted > 0) Debug.WriteLine(string.Format("Deleted this many \"{0}\" lines {1}", _lsd.StyleName, deleted.ToString()));
                st.Commit();
                st = new SubTransaction(TheDoc);
                st.Start();
                //create new group
                Group newGroup = TheDoc.Create.NewGroup(eidList);
                int modified = 0;
                foreach (Group g in groupTypeToModify.Groups)
                {
                    g.GroupType = newGroup.GroupType;
                    modified++;
                }

                newGroup.GroupType.Name = originalName; //rename groupType to existing name
                TheDoc.Delete(newGroup.Id);
                Debug.WriteLine("Finished modifying group type: " + originalName + ". Modified groups = " + modified.ToString());
                st.Commit();
                //TODO: regroup arrays
                //TODO: array groups seem to act differently than groups that have been arrayed
                return true;
            }
        }

        /// <summary>
        /// Creates a dictionary of model group names and their elementId's because attached detail groups only have the name of the model group, not elementId
        /// </summary>
        private Group getModelGroupByName(string mgName)
        {
            if (ModelGroupTypeDictionary == null)
            {
                ModelGroupTypeDictionary = new Dictionary<string, ElementId>();
                ElementClassFilter ecf = new ElementClassFilter(typeof(GroupType));
                ElementCategoryFilter catFilter = new ElementCategoryFilter(BuiltInCategory.OST_IOSModelGroups);
                LogicalAndFilter andFilter = new LogicalAndFilter(ecf, catFilter);

                FilteredElementCollector mgt_collector = new FilteredElementCollector(TheDoc);
                IList<Element> mgList = mgt_collector.WherePasses(andFilter).ToElements();
                foreach (GroupType gt in mgList)
                {
                    ModelGroupTypeDictionary.Add(gt.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_NAME).AsString(), gt.Id);
                }
            }

            ElementId eid = null;
            if (ModelGroupTypeDictionary.TryGetValue(mgName, out eid))
            {
                GroupType mgType = TheDoc.GetElement(eid) as GroupType;
                GroupSetIterator gsItr = mgType.Groups.ForwardIterator();
                gsItr.Reset();
                gsItr.MoveNext();
                Group modelGroup = gsItr.Current as Group;
                return modelGroup;
            }
            else
                return null;
        }

        /// <summary>
        /// These builtin categories are NOT styles that can be converted or deleted (these cannot be detail or model lines)
        /// </summary>
        private bool graphicStyleIsValidLineStyle(GraphicsStyle gs)
        {
            if (gs.GraphicsStyleCategory.Id.IntegerValue == (int)BuiltInCategory.OST_RoomSeparationLines)
                return false;
            if (gs.GraphicsStyleCategory.Id.IntegerValue == (int)BuiltInCategory.OST_MEPSpaceSeparationLines)
                return false;
            if (gs.GraphicsStyleCategory.Id.IntegerValue == (int)BuiltInCategory.OST_InsulationLines)
                return false;
            if (gs.GraphicsStyleCategory.Id.IntegerValue == (int)BuiltInCategory.OST_SketchLines)
                return false;
            if (gs.GraphicsStyleCategory.Id.IntegerValue == (int)BuiltInCategory.OST_AreaSchemeLines)
                return false;
            if (gs.GraphicsStyleCategory.Id.IntegerValue == (int)BuiltInCategory.OST_StairsSketchLandingCenterLines)
                return false;
            if (gs.GraphicsStyleCategory.Id.IntegerValue == (int)BuiltInCategory.OST_StairsSketchRunLines)
                return false;
            if (gs.GraphicsStyleCategory.Id.IntegerValue == (int)BuiltInCategory.OST_StairsSketchBoundaryLines)
                return false;
            if (gs.GraphicsStyleCategory.Id.IntegerValue == (int)BuiltInCategory.OST_StairsSketchRiserLines)
                return false;
            if (gs.GraphicsStyleCategory.Id.IntegerValue == (int)BuiltInCategory.OST_StairsSketchPathLines)
                return false;
            if (gs.GraphicsStyleCategory.Id.IntegerValue == (int)BuiltInCategory.OST_RailingRailPathLines)
                return false;
            if (gs.GraphicsStyleCategory.Id.IntegerValue == (int)BuiltInCategory.OST_RailingRailPathExtensionLines)
                return false;
            if (gs.GraphicsStyleCategory.Id.IntegerValue == (int)BuiltInCategory.OST_AxisOfRotation)
                return false;
            if (gs.GraphicsStyleCategory.Id.IntegerValue == (int)BuiltInCategory.OST_FabricAreaSketchEnvelopeLines)
                return false;
            if (gs.GraphicsStyleCategory.Id.IntegerValue == (int)BuiltInCategory.OST_FabricAreaSketchSheetsLines)
                return false;
            if (gs.GraphicsStyleCategory.Id.IntegerValue == (int)BuiltInCategory.OST_BasePointAxisX) //-2001273
                return false;
            if (gs.GraphicsStyleCategory.Id.IntegerValue == (int)BuiltInCategory.OST_BasePointAxisY) //-2001274
                return false;
            if (gs.GraphicsStyleCategory.Id.IntegerValue == (int)BuiltInCategory.OST_BasePointAxisZ) //-2001275
                return false;

            return true;
        }

        /// <summary>
        /// Gets all curve elements with a certain graphic style
        /// </summary>
        private IList<Element> getLinesUsingGraphicStyle(GraphicsStyle gs)
        {
            ElementClassFilter ecf = new ElementClassFilter(typeof(CurveElement));
            ParameterValueProvider pvp = new ParameterValueProvider(new ElementId((int)BuiltInParameter.BUILDING_CURVE_GSTYLE));
            FilterNumericRuleEvaluator fnre = new FilterNumericEquals();
            FilterRule IdRule = new FilterElementIdRule(pvp, fnre, gs.Id);
            ElementParameterFilter filter = new ElementParameterFilter(IdRule);
            LogicalAndFilter andFilter = new LogicalAndFilter(ecf, filter);

            FilteredElementCollector line_collector = new FilteredElementCollector(TheDoc);
            return line_collector.WherePasses(andFilter).ToElements();
        }

        private bool userSelectedSingleElement(ExternalCommandData commandData)
        {
            ICollection<ElementId> selection = commandData.Application.ActiveUIDocument.Selection.GetElementIds();
            if (selection == null || selection.Count == 0)
            {
                Debug.Print("Selection set was null or empty.");
                return false;
            }

            selectedStyles = new List<LineStyleDefinition>();
            foreach (ElementId eid in selection)
            {
                Element el = TheDoc.GetElement(eid);
                CurveElement ce = el as CurveElement;
                if (ce != null)
                {
                    GraphicsStyle gs = ce.LineStyle as GraphicsStyle;
                    if (gs != null && gs.GraphicsStyleCategory != null && gs.GraphicsStyleCategory.Parent != null)
                    {
                        if (gs.GraphicsStyleCategory.Parent.Id == TheDoc.Settings.Categories.get_Item(BuiltInCategory.OST_Lines).Id &&
                            graphicStyleIsValidLineStyle(gs))
                        {
                            LineStyleDefinition lpe = new LineStyleDefinition(gs);
                            selectedStyles.Add(lpe);
                        }
                    }
                }
            }
            Debug.WriteLine(string.Format("Found {0} patterns in user selected elements.", selectedStyles.Count));

            if (selectedStyles.Count == 0)
            {
                ///DIA #009
                TaskDialog.Show(
                    LocalizationProvider.GetLocalizedValue<string>("LSC_DIA009_Title"), // "No styles found", 
                    LocalizationProvider.GetLocalizedValue<string>("LSC_DIA009_MainInstr")); //"The selected element was not a detail or model line. Starting in multi-mode."
                selectedStyles = null;
                return false;
            }

            if (selectedStyles.Count == 1)
                return true;
            else
                return false;
        }
    }

    public class command_AvailableCheck : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication appdata, CategorySet selectCatagories)
        {
            if (appdata.Application.Documents.Size == 0)
                return false;

            if (!appdata.ActiveUIDocument.Document.IsFamilyDocument)
                return true;

            return false;
        }
    }
}