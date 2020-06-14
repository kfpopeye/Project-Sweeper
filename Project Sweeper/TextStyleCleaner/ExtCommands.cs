using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Collections.ObjectModel;
using log4net;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PKHL.ProjectSweeper.TextStyleCleaner
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class main_command : IExternalCommand
    {
        private readonly ILog log = LogManager.GetLogger(typeof(main_command));
        private Document TheDoc = null;
        private System.Text.StringBuilder ResultOutput = null;
        private ObservableCollection<TextStyleDefinition> allTextStyles = null;
        private IList<TextStyleDefinition> selectedStyles = null;

        //default text styles in an empty project are "Schedule Default" and "Text Note 1"

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
#if !DEBUG
                if (!ProjectSweeper.UserIsEntitled(commandData))
                    return Result.Failed;
#endif
                TheDoc = commandData.Application.ActiveUIDocument.Document;
                allTextStyles = new ObservableCollection<TextStyleDefinition>();

                //Collect all text style elements
                FilteredElementCollector collector = new FilteredElementCollector(TheDoc);
                IList<Element> textnotetypes = collector.WherePasses(new ElementClassFilter(typeof(TextNoteType))).ToElements();
                foreach (Element el in textnotetypes)
                {
                    TextNoteType tnt = el as TextNoteType;
                    TextStyleDefinition tsd = new TextStyleDefinition(tnt);
                    setDependants(ref tsd);
                    allTextStyles.Add(tsd);
                }
                Debug.WriteLine(string.Format("Found {0} text styles.", allTextStyles.Count));
                if (allTextStyles.Count == 1)
                {
                    //Revit Taskdialog TSC_DIA001
                    TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("TXT_Warning"));
                    td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("TSC_DIA001_MainInst"); // "There is only one text style in this project file."
                    td.MainContent = LocalizationProvider.GetLocalizedValue<string>("TSC_DIA001_MainCont"); // "There is no text style to convert another text style to and Revit project files must have at least one text style at all times."
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
                log.Error(LocalizationProvider.GetLocalizedValue<string>("TSC_Title_MainWindow"), err);
#if DEBUG
                Debug.WriteLine(new string('*', 100));
                Debug.WriteLine(err.ToString());
                Debug.WriteLine(new string('*', 100));
#endif
                Autodesk.Revit.UI.TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Title"));
                td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("TSC_Title_MainWindow") + LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainInst");
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
                main_win = new MainWindow(allTextStyles, selectedStyles, ref TheDoc);
                System.Windows.Interop.WindowInteropHelper x = new System.Windows.Interop.WindowInteropHelper(main_win);
                x.Owner = commandData.Application.MainWindowHandle;
                main_win.ShowDialog();
                Debug.WriteLine("-----------Exiting main window");

                //change note style and delete unwanted styles
                if ((bool)main_win.DialogResult)
                {
                    ResultOutput = new System.Text.StringBuilder();
                    allTextStyles = main_win.TheCollection;
                    trans = new Transaction(TheDoc);
                    trans.Start(LocalizationProvider.GetLocalizedValue<string>("TSC_Title_MainWindow"));
                    IList<ElementId> deleteTheseStyles = new List<ElementId>();
                    int stylesConverted = 0;
                    int totalElementsDeleted = 0;

                    //iterate through notes changing styles
                    foreach (TextStyleDefinition tsd in allTextStyles)
                    {
                        if (tsd.StyleToBeConverted || tsd.StyleToBeDeleted || tsd.DeleteElements)
                        {
                            ResultOutput.AppendLine();
                            ResultOutput.AppendLine(tsd.StyleName);
                            ResultOutput.AppendLine(new string('-', tsd.StyleName.Length));
                        }
                        if (tsd.StyleToBeConverted || tsd.DeleteElements)
                        {
                            totalElementsDeleted += convertAndOrDeleteNotes(tsd);
                            convertViewSchedules(tsd);
                            if (tsd.StyleToBeConverted)
                                stylesConverted++;
                        }
                        if (tsd.StyleToBeDeleted)
                        {
                            deleteTheseStyles.Add(new ElementId(tsd.ItsId));
                            ResultOutput.AppendLine("\t" + LocalizationProvider.GetLocalizedValue<string>("RSLT_Queue")); //Style queued to be deleted
                            Debug.WriteLine(tsd.StyleName + " is queued to be deleted");
                        }
                    }

                    int deletedStyles = 0;
                    if (deleteTheseStyles.Count > 0)
                    {
                        //and delete unwanted
                        if (deleteTheseStyles.Count == allTextStyles.Count)
                        {
														
                            //Revit Taskdialog TSC_DIA002
                            TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("TXT_Warning"));
                            td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("TSC_DIA002_MainInst"); // Removing all text styles from a Revit project is not allowed.
                            td.MainContent = string.Format(LocalizationProvider.GetLocalizedValue<string>("TSC_DIA002_MainCont"), allTextStyles[0].StyleName); // The text style \"{0}\" will not be deleted.
                            td.Show();
                            ResultOutput.AppendLine(string.Format(LocalizationProvider.GetLocalizedValue<string>("RSLT_RemoveQueue"), allTextStyles[0].StyleName)); // Style {0} removed from queue
                            deleteTheseStyles.Remove(new ElementId(allTextStyles[0].ItsId));
                        }
                        st = new SubTransaction(TheDoc);
                        st.Start();
                        foreach (ElementId eid in deleteTheseStyles) // used this loop because deleting textnotetype removes unknown dependancies as well
                        {
                            if (TheDoc.Delete(eid).Count > 0)
                                ++deletedStyles;
                        }
                        st.Commit();
                    }
                    trans.Commit();
											  
                    ResultOutput.AppendLine();
                    ResultOutput.AppendLine(LocalizationProvider.GetLocalizedValue<string>("RSLT_Summary")); // Summary
                    ResultOutput.AppendLine(new string('-', LocalizationProvider.GetLocalizedValue<string>("RSLT_Summary").Length)); // -------
                    ResultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("RSLT_Found"), allTextStyles.Count); // Found {0} style(s).
                    ResultOutput.AppendLine();
                    ResultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("RSLT_Converted"), stylesConverted); // Converted {0} style(s).
                    ResultOutput.AppendLine();
                    if (totalElementsDeleted > 0)
                    {
                        ResultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("RSLT_Deleted"), totalElementsDeleted); //Deleted {0} style(s).
                        ResultOutput.AppendLine();
                    }
                    ResultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("RSLT_Removed"), deletedStyles); // Removed {0} style(s).

                    //show result output
                    ResultWindow res_win = new ResultWindow(LocalizationProvider.GetLocalizedValue<string>("TSC_Title_MainWindow"), ResultOutput);
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
            TextStyleDefinition selectedTsd = selectedStyles.First<TextStyleDefinition>();
            foreach (TextStyleDefinition f in allTextStyles)
            {
                if (f.Equals(selectedTsd))
                {
                    selectedTsd = f;
                    break;
                }
            }
            try
            {
                Debug.WriteLine("-----------Starting single mode window");
                se_win = new SingleElementWindow(allTextStyles, selectedTsd, ref TheDoc);
                System.Windows.Interop.WindowInteropHelper wih = new System.Windows.Interop.WindowInteropHelper(se_win);
                wih.Owner = commandData.Application.MainWindowHandle;
                se_win.ShowDialog();
                Debug.WriteLine("-----------Exiting single mode window");

                //change note style and delete the old styles
                if ((bool)se_win.DialogResult)
                {
                    selectedTsd.NewStyle = se_win.chossenStyle;
                    selectedTsd.StyleToBeDeleted = se_win.DeleteSourceStyle;
                    ResultOutput = new System.Text.StringBuilder();

                    trans = new Transaction(TheDoc);
                    trans.Start(LocalizationProvider.GetLocalizedValue<string>("TSC_Title_MainWindow"));
                    if (selectedTsd.StyleToBeConverted || selectedTsd.DeleteElements)
                    {
                        ResultOutput.AppendLine(selectedTsd.StyleName);
                        ResultOutput.AppendLine(new string('-', selectedTsd.StyleName.Length));
                        st = new SubTransaction(TheDoc);
                        st.Start();
                        convertAndOrDeleteNotes(selectedTsd);
                        convertViewSchedules(selectedTsd);
                        st.Commit();
                    }

                    //and delete unwanted
                    if (selectedTsd.StyleToBeDeleted)
                    {
                        st = new SubTransaction(TheDoc);
                        st.Start();
                        TheDoc.Delete(new ElementId(selectedTsd.ItsId));
                        Debug.WriteLine("Deleted style: " + selectedTsd.StyleName);
                        st.Commit();
                        ResultOutput.AppendFormat(LocalizationProvider.GetLocalizedValue<string>("RSLT_SingleMode"), selectedTsd.StyleName); //The style {0} was removed from the project.
                    }
                    trans.Commit();

                    //show result output
                    ResultWindow res_win = new ResultWindow(LocalizationProvider.GetLocalizedValue<string>("TSC_Title_MainWindow"), ResultOutput);
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

        private int convertAndOrDeleteNotes(TextStyleDefinition _tsd)
        {
            Debug.WriteLine(string.Format("convertAndOrDeleteNotes of style: {0}-----------", _tsd.StyleName));
            int converted = 0, deleted = 0;
            IList<ElementId> groupTypesToEdit = new List<ElementId>();
            IList<Element> notesWithStyle = getNotesUsingThisStyle(_tsd);
            SubTransaction st = new SubTransaction(TheDoc);
            Dictionary<string, bool> GroupsToExplode = new Dictionary<string, bool>();

            st.Start();
            foreach (TextNote tn in notesWithStyle)
            {
                //check if notes are part of group and prompt to ungroup
                if (!tn.GroupId.Equals(ElementId.InvalidElementId))
                {
                    Group g = TheDoc.GetElement(tn.GroupId) as Group;
                    Debug.WriteLine(string.Format("Found a note belonging to group: {0}.", g.Name));
                    bool UngroupIt = false;
                    if (!GroupsToExplode.TryGetValue(g.Name, out UngroupIt))
                    {
                        //Revit Taskdialog DIA006
                        TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("DIA006_Title")); //Found group
                        td.MainInstruction = string.Format(LocalizationProvider.GetLocalizedValue<string>("DIA006_MainInst"), _tsd.StyleName, g.Name); //An element using the {0} style was found in the group {1}
                        td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, string.Format(LocalizationProvider.GetLocalizedValue<string>("DIA006_Command1"), g.Name)); //Ungroup group: {0} and change the element.
                        td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, LocalizationProvider.GetLocalizedValue<string>("DIA006_Command2")); //Leave the group unchanged. (The style will not be deleted).
                        if (td.Show() == TaskDialogResult.CommandLink1)
                            UngroupIt = true;
                        else
                        {
                            UngroupIt = false;
                            ResultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("RSLT_NotUngrouped"), g.Name, _tsd.StyleName); // Group {0} was not ungrouped. {1} will not be removed.
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
                        _tsd.StyleToBeDeleted = false;
                }

                if (tn.GroupId.Equals(ElementId.InvalidElementId))
                {
                    if (_tsd.StyleToBeConverted)
                    {
                        if (_tsd.DeleteElements)
                        {
                            TheDoc.Delete(tn.Id);
                            deleted++;
                        }
                        else
                        {
                            tn.TextNoteType = TheDoc.GetElement(new ElementId(_tsd.NewStyle.ItsId)) as TextNoteType;
                            converted++;
                        }
                    }
                }
            }
            if (_tsd.DeleteElements)
            {
                ResultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("TSC_RSLT_DeletedNotes"), deleted); // Deleted {0} notes
                ResultOutput.AppendLine();
            }
            if (_tsd.StyleToBeConverted)
            {
                ResultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("TSC_RSLT_ConvertedNotes"), converted, _tsd.NewStyle.StyleName); // Converted {0} notes to \"{1}\"
                ResultOutput.AppendLine();
            }

            Debug.WriteLine(string.Format("Converted {0} notes from \"{1}\" to \"{2}\"",
                converted, _tsd.StyleName, _tsd.NewStyle.StyleName));
            Debug.WriteLine(string.Format("Deleted this many \"{0}\" notes {1}", _tsd.StyleName, deleted));
            st.Commit();
            return deleted;
        }

        //set schedules and sched templates to new style Id or to default style id
        private void convertViewSchedules(TextStyleDefinition tsd)
        {
            if (tsd.OwnerSchedules == null || tsd.OwnerSchedules.Count == 0)
                return;
            if (!tsd.StyleToBeConverted)   //indicates tsd style is being deleted, not converted. Schedule text styles will default to system text style.
                return;

            SubTransaction st = new SubTransaction(TheDoc);
            st.Start();
            foreach (int i in tsd.OwnerSchedules.Keys.ToList<int>())
            {
                bool foundStyle = false;
                ViewSchedule vs = TheDoc.GetElement(new ElementId(i)) as ViewSchedule;
                if (vs.BodyTextTypeId.IntegerValue == tsd.ItsId)
                {
                    if(tsd.StyleToBeConverted)
                        vs.BodyTextTypeId = new ElementId(tsd.NewStyle.ItsId);
                    foundStyle = true;
                }
                if (vs.HeaderTextTypeId.IntegerValue == tsd.ItsId)
                {
                    if (tsd.StyleToBeConverted)
                        vs.HeaderTextTypeId = new ElementId(tsd.NewStyle.ItsId);
                    foundStyle = true;
                }
                if (vs.TitleTextTypeId.IntegerValue == tsd.ItsId)
                {
                    if (tsd.StyleToBeConverted)
                        vs.TitleTextTypeId = new ElementId(tsd.NewStyle.ItsId);
                    foundStyle = true;
                }
                if (foundStyle)
                {
                    if (tsd.StyleToBeConverted)
                        ResultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("TSC_RSLT_ConvSchedule"), vs.Name); //Converted style on schedule: {0}
                    else if (tsd.StyleToBeDeleted)
                        ResultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("TSC_RSLT_ConvSched2Default"), vs.Name); //Style on schedule {0} will be set to system default
                    ResultOutput.AppendLine();
                }
            }
            st.Commit();
        }

        /// <summary>
        /// sets the owner views and schedules that use a certain text style
        /// </summary>
        private void setDependants(ref TextStyleDefinition _tsd)
        {
            //collect list of views that have notes using this style
            IList<Element> noteList = getNotesUsingThisStyle(_tsd);
            if (noteList != null && noteList.Count != 0)
            {
                Dictionary<int, string> ov = new Dictionary<int, string>();
                foreach (TextNote tn in noteList)
                {
                    string view_name = TheDoc.GetElement(tn.OwnerViewId).Name;
                    if (!ov.ContainsKey(tn.OwnerViewId.IntegerValue))
                        ov.Add(tn.OwnerViewId.IntegerValue, view_name);
                }
                _tsd.OwnerViews = ov;
                Debug.WriteLine(string.Format("Found {0} views with notes using the {1} text style.", _tsd.OwnerViews.Count, _tsd.StyleName));
            }

            //collect list of schedules and sched templates using this style
            ElementClassFilter ecf = new ElementClassFilter(typeof(ViewSchedule));
            FilteredElementCollector sched_collector = new FilteredElementCollector(TheDoc);
            IList<Element> sc = sched_collector.WherePasses(ecf).ToElements();
            if (sc != null && sc.Count != 0)
            {
                Dictionary<int, string> ov = new Dictionary<int, string>();
                foreach (ViewSchedule vs in sched_collector)
                {
                    if (vs.HeaderTextTypeId.IntegerValue == _tsd.ItsId ||
                        vs.BodyTextTypeId.IntegerValue == _tsd.ItsId ||
                        vs.TitleTextTypeId.IntegerValue == _tsd.ItsId)
                        if (!ov.ContainsKey(vs.Id.IntegerValue))
                            ov.Add(vs.Id.IntegerValue, vs.Name);
                }
                _tsd.OwnerSchedules = ov;
                Debug.WriteLine(string.Format("Found {0} schedules using the {1} text style.", _tsd.OwnerSchedules.Count, _tsd.StyleName));
            }
        }

        /// <summary>
        /// Gets all text notes with a certain text style
        /// </summary>
        private IList<Element> getNotesUsingThisStyle(TextStyleDefinition _tsd)
        {
            Element tnt = TheDoc.GetElement(new ElementId(_tsd.ItsId));
            ElementClassFilter ecf = new ElementClassFilter(typeof(TextNote));
            ParameterValueProvider pvp = new ParameterValueProvider(new ElementId((int)BuiltInParameter.ELEM_FAMILY_PARAM));
            FilterNumericRuleEvaluator fnre = new FilterNumericEquals();
            FilterRule IdRule = new FilterElementIdRule(pvp, fnre, tnt.Id);
            ElementParameterFilter filter = new ElementParameterFilter(IdRule);
            LogicalAndFilter andFilter = new LogicalAndFilter(ecf, filter);

            FilteredElementCollector note_collector = new FilteredElementCollector(TheDoc);
            return note_collector.WherePasses(andFilter).ToElements();
        }

        private bool userSelectedSingleElement(ExternalCommandData commandData)
        {
            ICollection<ElementId> selection = commandData.Application.ActiveUIDocument.Selection.GetElementIds();
            if (selection == null || selection.Count == 0)
            {
                Debug.Print("Selection set was null or empty.");
                return false;
            }

            selectedStyles = new List<TextStyleDefinition>();
            foreach (ElementId eid in selection)
            {
                TextNote el = TheDoc.GetElement(commandData.Application.ActiveUIDocument.Selection.GetElementIds().First<ElementId>()) as TextNote;
                if (el != null)
                {
                    TextNoteType selectedNoteType = el.TextNoteType;
                    selectedStyles.Add(new TextStyleDefinition(selectedNoteType));
                }
            }
            Debug.WriteLine(string.Format("Found {0} patterns in user selected elements.", selectedStyles.Count));

            if (selectedStyles.Count == 0)
            {
				//DIA #004
                TaskDialog.Show(
                    LocalizationProvider.GetLocalizedValue<string>("TSC_DIA004_Title"), //"No styles found"
                    LocalizationProvider.GetLocalizedValue<string>("TSC_DIA004_MainInstr")); // "The selected element(s) was not a text node. Starting in multi-mode.");
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