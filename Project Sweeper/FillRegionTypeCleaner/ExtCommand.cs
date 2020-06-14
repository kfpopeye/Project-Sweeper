using System;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Diagnostics;
using log4net;

namespace PKHL.ProjectSweeper.FillRegionTypeCleaner
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class main_command : IExternalCommand
    {
        private readonly ILog log = LogManager.GetLogger(typeof(main_command));
        private Document TheDoc = null;
        private System.Text.StringBuilder ResultOutput = null;
        private ObservableCollection<FillRegionTypeDefinition> allFillRegionTypes = null;
        private IList<FillRegionTypeDefinition> selectedRegions = null;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
#if !DEBUG
                if (!ProjectSweeper.UserIsEntitled(commandData))
                    return Result.Failed;
#endif

                TheDoc = commandData.Application.ActiveUIDocument.Document;
                allFillRegionTypes = new ObservableCollection<FillRegionTypeDefinition>();

                //Collect all FillRegionTypes and filter lines
                FilteredElementCollector collector = new FilteredElementCollector(TheDoc);
                IList<Element> FRTselements = collector.WherePasses(new ElementClassFilter(typeof(FilledRegionType))).ToElements();
                foreach (FilledRegionType frt in FRTselements)
                {
                    Dictionary<int, string> OwnerViews = new Dictionary<int, string>();
                    FillRegionTypeDefinition fpd = new FillRegionTypeDefinition(frt);
                    IList<Element> RList = getRegionsUsingThisStyle(frt);
                    fpd.SetOwnerViews(RList);
                    allFillRegionTypes.Add(fpd);
                }
                Debug.Print("Found {0} region types.", allFillRegionTypes.Count);
                if (allFillRegionTypes.Count == 1)
                {
                    //Revit Taskdialog DIA001
                    TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("TXT_Warning"));
                    td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("FRTC_DIA001_MainInst"); // There is only one fill region type in this project file.
                    td.MainContent = LocalizationProvider.GetLocalizedValue<string>("FRTC_DIA001_MainCont"); // There is no fill region type to convert another fill region type to and Revit project files must have at least one fill region type at all times.
                    td.Show();
                    return Result.Cancelled;
                }

            if (userSelectedSingleElement(commandData))
                return startSingleSelectionMode(commandData);
            else
                return startMainMode(commandData);
            }
            catch (Exception err)
            {
                log.Error(LocalizationProvider.GetLocalizedValue<string>("FRTC_Title_MainWindow"), err);
#if DEBUG
                Debug.WriteLine(new string('*', 100));
                Debug.WriteLine(err.ToString());
                Debug.WriteLine(new string('*', 100));
#endif
                Autodesk.Revit.UI.TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Title"));
                td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("FRTC_Title_MainWindow") + LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainInst");
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
                main_win = new MainWindow(allFillRegionTypes, selectedRegions, ref TheDoc);
                System.Windows.Interop.WindowInteropHelper x = new System.Windows.Interop.WindowInteropHelper(main_win);
                x.Owner = commandData.Application.MainWindowHandle;
                main_win.ShowDialog();
                Debug.WriteLine("-----------Exiting main window");

                //change line style and delete unwanted styles
                if ((bool)main_win.DialogResult)
                {
                    ResultOutput = new System.Text.StringBuilder();
                    allFillRegionTypes = main_win.TheCollection;
                    trans = new Transaction(TheDoc);
                    trans.Start(LocalizationProvider.GetLocalizedValue<string>("FRTC_Title_MainWindow"));
                    IList<ElementId> deleteTheseStyles = new List<ElementId>();
                    int convertedStyles = 0;
                    int totalElementsDeleted = 0;

                    //iterate through curveElements changing line styles
                    foreach (FillRegionTypeDefinition frtd in allFillRegionTypes)
                    {
                        if (frtd.StyleToBeConverted || frtd.StyleToBeDeleted || frtd.DeleteElements)
                        {
                            ResultOutput.AppendLine();
                            ResultOutput.AppendLine(frtd.StyleName);
                            ResultOutput.AppendLine(new string('-', frtd.StyleName.Length));
                        }

                        if (frtd.StyleToBeConverted || frtd.DeleteElements)
                        {
                            totalElementsDeleted += convertAndOrDeleteRegions(frtd);
                            if(frtd.StyleToBeConverted)													   
								convertedStyles++;
                        }

                        if (frtd.StyleToBeDeleted)
                        {
                            deleteTheseStyles.Add(new ElementId(frtd.ItsId));
                            ResultOutput.AppendLine("\t" + LocalizationProvider.GetLocalizedValue<string>("RSLT_Queue")); // Style queued to be deleted
                            ResultOutput.AppendLine();
                            Debug.WriteLine(frtd.StyleName + " is queued to be deleted");
                        }
                    }

                    //and delete unwanted
                    int deletedStyles = 0;
                    if (deleteTheseStyles.Count > 0)
                    {
                        if (deleteTheseStyles.Count == allFillRegionTypes.Count)
                        {
                            //Revit Taskdialog FRTC_DIA002
                            TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("FRTC_Title_MainWindow"));
                            td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("FRTC_DIA002_MainInst"); //Removing all filled region types from a Revit project is not allowed.
                            td.MainContent = string.Format(LocalizationProvider.GetLocalizedValue<string>("FRTC_DIA002_MainCont"), allFillRegionTypes[0].StyleName); //The filled region type \"{0}\" will not be deleted.
                            td.Show();
                            ResultOutput.AppendLine(string.Format("\t" + LocalizationProvider.GetLocalizedValue<string>("RSLT_RemoveQueue"), allFillRegionTypes[0].StyleName)); // Style {0} removed from queue
                            deleteTheseStyles.Remove(new ElementId(allFillRegionTypes[0].ItsId));
                        }
                        st = new SubTransaction(TheDoc);
                        st.Start();
                        foreach (ElementId eid in deleteTheseStyles)
                        {
                            if (TheDoc.Delete(eid).Count > 0)
                                ++deletedStyles;
                        }
                        Debug.WriteLine("Deleted this many styles: " + deletedStyles.ToString());
                        st.Commit();
                    }
                    trans.Commit();
                    ResultOutput.AppendLine();
                    ResultOutput.AppendLine(LocalizationProvider.GetLocalizedValue<string>("RSLT_Summary")); // Summary
                    ResultOutput.AppendLine(new string('-', LocalizationProvider.GetLocalizedValue<string>("RSLT_Summary").Length)); // -------
                    ResultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("RSLT_Found"), allFillRegionTypes.Count); // Found {0} style(s).
                    ResultOutput.AppendLine();
                    ResultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("RSLT_Converted"), convertedStyles); // Converted {0} style(s).
                    ResultOutput.AppendLine();
                    if (totalElementsDeleted > 0)
                    {
                        ResultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("FRTC_DeletedXRegions"), totalElementsDeleted); //Deleted {0} region(s).
                        ResultOutput.AppendLine();
                    }
                    ResultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("RSLT_Removed"), deletedStyles); // Removed {0} style(s).

                    //show result output
                    ResultWindow res_win = new ResultWindow(LocalizationProvider.GetLocalizedValue<string>("FRTC_Title_MainWindow"), ResultOutput);
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
                FillRegionTypeDefinition selectedFrtd = selectedRegions.First<FillRegionTypeDefinition>();
                foreach (FillRegionTypeDefinition f in allFillRegionTypes)
                {
                    if (f.Equals(selectedFrtd))
                    {
                        selectedFrtd = f;
                        break;
                    }
                }

                Debug.WriteLine("-----------Starting single mode window");
                se_win = new SingleElementWindow(allFillRegionTypes, selectedFrtd, ref TheDoc);
                System.Windows.Interop.WindowInteropHelper wih = new System.Windows.Interop.WindowInteropHelper(se_win);
                wih.Owner = commandData.Application.MainWindowHandle;
                se_win.ShowDialog();
                Debug.WriteLine("-----------Exiting single mode window");

                //change line style and delete the old styles
                if ((bool)se_win.DialogResult)
                {
                    selectedFrtd.NewStyle = se_win.chossenStyle;
                    selectedFrtd.StyleToBeDeleted = se_win.DeleteSourceStyle;
                    ResultOutput = new System.Text.StringBuilder();

                    trans = new Transaction(TheDoc);
                    trans.Start(LocalizationProvider.GetLocalizedValue<string>("FRTC_Title_MainWindow"));
                    if (selectedFrtd.StyleToBeConverted || selectedFrtd.DeleteElements)
                    {
                        ResultOutput.AppendLine(selectedFrtd.StyleName);
                        ResultOutput.AppendLine(new string('-', selectedFrtd.StyleName.Length));
                        st = new SubTransaction(TheDoc);
                        st.Start();
                        convertAndOrDeleteRegions(selectedFrtd);
                        st.Commit();
                    }

                    //and delete unwanted
                    if (selectedFrtd.StyleToBeDeleted)
                    {
                        st = new SubTransaction(TheDoc);
                        st.Start();
                        TheDoc.Delete(new ElementId(selectedFrtd.ItsId));
                        Debug.WriteLine("Deleted style: " + selectedFrtd.StyleName);
                        st.Commit();
                        ResultOutput.AppendFormat(LocalizationProvider.GetLocalizedValue<string>("RSLT_SingleMode"), selectedFrtd.StyleName); //The style {0} was removed from the project.
                    }
                    trans.Commit();

                    //show result output
                    ResultWindow res_win = new ResultWindow(LocalizationProvider.GetLocalizedValue<string>("FRTC_Title_MainWindow"), ResultOutput);
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

        private int convertAndOrDeleteRegions(FillRegionTypeDefinition _frtd)
        {
            Debug.WriteLine(string.Format("convertAndOrDeleteRegions of style: {0}-----------", _frtd.StyleName));
            int converted = 0, deleted = 0;
            IList<ElementId> groupTypesToEdit = new List<ElementId>();
            IList<Element> regionsWithStyle = getRegionsUsingThisStyle(TheDoc.GetElement(new ElementId(_frtd.ItsId)) as FilledRegionType);
            SubTransaction st = new SubTransaction(TheDoc);
            Dictionary<string, bool> GroupsToExplode = new Dictionary<string, bool>();

            st.Start();
            foreach (FilledRegion fr in regionsWithStyle)
            {
                //check if lines are part of group and prompt to ungroup
                if (!fr.GroupId.Equals(ElementId.InvalidElementId))
                {
                    Group g = TheDoc.GetElement(fr.GroupId) as Group;
                    Debug.WriteLine(string.Format("Found a region belonging to group: {0}.", g.Name));
                    bool UngroupIt = false;
                    if (!GroupsToExplode.TryGetValue(g.Name, out UngroupIt))
                    {
                        //Revit Taskdialog DIA006
                        TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("DIA006_Title")); //Found group
                        td.MainInstruction = string.Format(LocalizationProvider.GetLocalizedValue<string>("DIA006_MainInst"), _frtd.StyleName, g.Name); //An element using the {0} style was found in the group {1}
                        td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, string.Format(LocalizationProvider.GetLocalizedValue<string>("DIA006_Command1"), g.Name)); //Ungroup group: {0} and change the element.
                        td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, LocalizationProvider.GetLocalizedValue<string>("DIA006_Command2")); //Leave the group unchanged. (The style will not be deleted).
						
                        if (td.Show() == TaskDialogResult.CommandLink1)
                            UngroupIt = true;
                        else
                        {
                            UngroupIt = false;
                            ResultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("RSLT_NotUngrouped"), g.Name, _frtd.StyleName); // Group {0} was not ungrouped. {1} will not be removed.
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
                        _frtd.StyleToBeDeleted = false;
                }

                if (fr.GroupId.Equals(ElementId.InvalidElementId))
                {
                    if (_frtd.StyleToBeConverted)
                    {
                        Parameter p = fr.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM);
                        p.Set(new ElementId(_frtd.NewStyle.ItsId));
                        converted++;
                    }
                    if (_frtd.DeleteElements)
                    {
                        TheDoc.Delete(fr.Id);
                        deleted++;
                    }
                }
            }
            st.Commit();

            if (_frtd.DeleteElements)
            {
                ResultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("FRTC_RSLT_DeletedRegionTypes"), deleted); // Deleted {0} filled region types
                ResultOutput.AppendLine();
            }
            if (_frtd.StyleToBeConverted)
            {
                ResultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("FRTC_RSLT_ConvertedRegionTypes"), converted, _frtd.NewStyle.StyleName); // Converted {0} filled regions to \"{1}\"
                ResultOutput.AppendLine();
            }

            Debug.WriteLine(string.Format("Converted {0} filled regions from \"{1}\" to \"{2}\"",
                converted, _frtd.StyleName, _frtd.NewStyle.StyleName));
            Debug.WriteLine(string.Format("Deleted this many \"{0}\" filled regions types: {1}", _frtd.StyleName, deleted));
            return deleted;
        }

        /// <summary>
        /// Gets all text notes with a certain text style
        /// </summary>
        private IList<Element> getRegionsUsingThisStyle(FilledRegionType frt)
        {
            ElementClassFilter ecf = new ElementClassFilter(typeof(FilledRegion));
            ParameterValueProvider pvp = new ParameterValueProvider(new ElementId((int)BuiltInParameter.ELEM_FAMILY_PARAM));
            FilterNumericRuleEvaluator fnre = new FilterNumericEquals();
            FilterRule IdRule = new FilterElementIdRule(pvp, fnre, frt.Id);
            ElementParameterFilter filter = new ElementParameterFilter(IdRule);
            LogicalAndFilter andFilter = new LogicalAndFilter(ecf, filter);
            FilteredElementCollector collector = new FilteredElementCollector(TheDoc);
            return collector.WherePasses(andFilter).ToElements();
        }

        /// <summary>
        /// Creates a list of user selected fill regions. These are used to filter the list view in multi-mode or trigger single-mode if only one is selected.
        /// </summary>
        private bool userSelectedSingleElement(ExternalCommandData commandData)
        {
            ICollection<ElementId> selection = commandData.Application.ActiveUIDocument.Selection.GetElementIds();
            if (selection == null || selection.Count == 0)
            {
                Debug.Print("User selected no elements.");
                return false;
            }

            selectedRegions = new List<FillRegionTypeDefinition>();
            foreach (ElementId eid in selection)
            {
                Element el = TheDoc.GetElement(eid);
                if (el == null) continue;
                Debug.Print("Checking element: {0} of category {1}", el.Name, el.Category.Name);
                if ((BuiltInCategory)el.Category.Id.IntegerValue == BuiltInCategory.OST_DetailComponents)
                {
                    if (el.GetType().Equals(typeof(FilledRegion)))
                    {
                        FilledRegionType frt = TheDoc.GetElement((el as FilledRegion).GetTypeId()) as FilledRegionType;
                        selectedRegions.Add(new FillRegionTypeDefinition(frt));
                        Debug.Print("\tFound {0}", frt.Name);
                    }											   
                }
            }
            Debug.WriteLine(string.Format("Found {0} patterns in user selected elements.", selectedRegions.Count));

            if (selectedRegions.Count == 0)
            {
                ///DIA #004
                TaskDialog.Show(
                    LocalizationProvider.GetLocalizedValue<string>("FRTC_DIA004_Title"), //No regions found
                    LocalizationProvider.GetLocalizedValue<string>("FRTC_DIA004_MainInst"));//The items you selected were not filled regions. All the styles will be shown for editing.
                selectedRegions = null;
                return false;
            }

            if (selectedRegions.Count == 1)
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