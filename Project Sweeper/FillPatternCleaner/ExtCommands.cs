using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading;
using log4net;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PKHL.ProjectSweeper.FillPatternCleaner
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class MainCommand : IExternalCommand
    {
        internal static EventWaitHandle _progressWindowWaitHandle;
        private readonly ILog _log = LogManager.GetLogger(typeof(MainCommand));
        //progress window runs on separate thread therefore we must localize from here	
        private readonly string _cancelText = LocalizationProvider.GetLocalizedValue<string>("CancelButton");
        private readonly string _progCancelText = LocalizationProvider.GetLocalizedValue<string>("PROG_Cancelled");

        private IList<Element> regionLib = null, familyLib = null, componentLib = null, materialLib = null;
        ElementId solidPatternId = null;
        private Document theDoc = null;
        private ObservableCollection<FillPatternDefinition> allFillPatterns = null;
        private IList<FillPatternDefinition> selectedPatterns = null;
        private System.Text.StringBuilder resultOutput = null;
        private ProgressBarWindow progWindow;
        private bool cancelledFromProgressWindow = false;
        private IntPtr RevitHandle;
        private System.Collections.ArrayList sharedFamilies = new System.Collections.ArrayList();
        private System.Collections.ArrayList rootFamilyList = new System.Collections.ArrayList();

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
#if !DEBUG
                if (!ProjectSweeper.UserIsEntitled(commandData))
                    return Result.Failed;
#endif
                RevitHandle = commandData.Application.MainWindowHandle;
                theDoc = commandData.Application.ActiveUIDocument.Document;
                InitalizeLibraries(theDoc);
                allFillPatterns = new ObservableCollection<FillPatternDefinition>();

                //Collect all fill pattern elements
                FilteredElementCollector collector = new FilteredElementCollector(theDoc);
                IList<Element> fillPatternElements = collector.WherePasses(new ElementClassFilter(typeof(FillPatternElement))).ToElements();
                foreach (FillPatternElement fpe in fillPatternElements)
                {
                    if (fpe.GetFillPattern().IsSolidFill) //store solid fill id for later used
                    {
                        solidPatternId = fpe.Id;
                    }
                    else
                    {
                        FillPatternDefinition fpd = new FillPatternDefinition(fpe, ref componentLib, ref materialLib, ref regionLib);
                        allFillPatterns.Add(fpd);
                    }
                }

                //suppress "Line slightly off axis" and other warnings
                commandData.Application.Application.FailuresProcessing += ApplicationFailuresProcessing;
                ScanFamiliesForFillPattern();
                commandData.Application.Application.FailuresProcessing -= ApplicationFailuresProcessing;

                //TODO: scan views for overrides

                if (cancelledFromProgressWindow)
                    return Result.Cancelled;
                Debug.WriteLine(string.Format("Found {0} patterns.", allFillPatterns.Count));
            }
            catch (Exception err)
            {
                _log.Error(LocalizationProvider.GetLocalizedValue<string>("FPC_Title_MainWindow"), err);
				
                Debug.WriteLine(new string('*', 100));
                Debug.WriteLine(err.ToString());
                Debug.WriteLine(new string('*', 100));

                Autodesk.Revit.UI.TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Title"));
                td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("FPC_Title_MainWindow") + LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainInst");
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

            if (UserSelectedSingleElement(commandData))
                return StartSingleSelectionMode(commandData);
            else
                return StartMainMode(commandData);
        }

        /// <summary>
        /// Creates a list of user selected fill patterns. These are used to filter the list view in multi-mode or trigger single-mode if only one is selected.
        /// </summary>
        /// <param name="commandData"></param>
        /// <returns></returns>
        private bool UserSelectedSingleElement(ExternalCommandData commandData)
        {
            ICollection<ElementId> selection = commandData.Application.ActiveUIDocument.Selection.GetElementIds();
            if (selection == null || selection.Count == 0)
            {
                Debug.Print("User selected no elements.");
                return false;
            }

            selectedPatterns = new List<FillPatternDefinition>();
            foreach (ElementId eid in selection)
            {
                Element el = theDoc.GetElement(eid);
                ElementType et = theDoc.GetElement(el.GetTypeId()) as ElementType;
                Debug.Print("Checking element: {0} of category {1}", el.Name, el.Category.Name);
                ElementId pid = null;
                switch ((BuiltInCategory)el.Category.Id.IntegerValue)
                {
                    case BuiltInCategory.OST_Walls:
                    case BuiltInCategory.OST_Floors:
                    case BuiltInCategory.OST_Ceilings:
                    case BuiltInCategory.OST_Roofs:
                        if ((pid = et.get_Parameter(BuiltInParameter.COARSE_SCALE_FILL_PATTERN_ID_PARAM).AsElementId()) != ElementId.InvalidElementId &&
                                pid != solidPatternId)
                        {
                            FillPatternElement fpe = theDoc.GetElement(pid) as FillPatternElement;
                            FillPatternDefinition fpd = new FillPatternDefinition(fpe);
                            if (!selectedPatterns.Contains(fpd))
                                selectedPatterns.Add(fpd);
                            Debug.Print("\tFound {0}", fpe.Name);
                        }
                        foreach (ElementId mid in el.GetMaterialIds(false))
                        {
                            Material m = theDoc.GetElement(mid) as Material;
                            if (m.CutForegroundPatternId != ElementId.InvalidElementId &&
                                m.CutForegroundPatternId != solidPatternId)
                            {
                                FillPatternElement fpe = theDoc.GetElement(m.CutForegroundPatternId) as FillPatternElement;
                                FillPatternDefinition fpd = new FillPatternDefinition(fpe);
                                if (!selectedPatterns.Contains(fpd))
                                    selectedPatterns.Add(fpd);
                                Debug.Print("\tFound {0}", fpe.Name);
                            }
                            if (m.CutBackgroundPatternId != ElementId.InvalidElementId &&
                                m.CutBackgroundPatternId != solidPatternId)
                            {
                                FillPatternElement fpe = theDoc.GetElement(m.CutBackgroundPatternId) as FillPatternElement;
                                FillPatternDefinition fpd = new FillPatternDefinition(fpe);
                                if (!selectedPatterns.Contains(fpd))
                                    selectedPatterns.Add(fpd);
                                Debug.Print("\tFound {0}", fpe.Name);
                            }
                            if (m.SurfaceForegroundPatternId != ElementId.InvalidElementId &&
                                m.SurfaceForegroundPatternId != solidPatternId)
                            {
                                FillPatternElement fpe = theDoc.GetElement(m.SurfaceForegroundPatternId) as FillPatternElement;
                                FillPatternDefinition fpd = new FillPatternDefinition(fpe);
                                if (!selectedPatterns.Contains(fpd))
                                    selectedPatterns.Add(fpd);
                                Debug.Print("\tFound {0}", fpe.Name);
                            }
                            if (m.SurfaceBackgroundPatternId != ElementId.InvalidElementId &&
                                m.SurfaceBackgroundPatternId != solidPatternId)
                            {
                                FillPatternElement fpe = theDoc.GetElement(m.SurfaceBackgroundPatternId) as FillPatternElement;
                                FillPatternDefinition fpd = new FillPatternDefinition(fpe);
                                if (!selectedPatterns.Contains(fpd))
                                    selectedPatterns.Add(fpd);
                                Debug.Print("\tFound {0}", fpe.Name);
                            }
                        }
                        break;
                    case BuiltInCategory.OST_DetailComponents:
                        if (el.GetType().Equals(typeof(FilledRegion)))
                        {
                            pid = (et as FilledRegionType).ForegroundPatternId;
                            if (pid != null && pid != ElementId.InvalidElementId &&
                                pid != solidPatternId)
                            {
                                FillPatternElement fpe = et.Document.GetElement(pid) as FillPatternElement;
                                if (fpe != null)
                                {
                                    FillPatternDefinition fpd = new FillPatternDefinition(fpe);
                                    if (!selectedPatterns.Contains(fpd))
                                        selectedPatterns.Add(fpd);
                                    Debug.Print("\tFound {0}", fpe.Name);
                                }
                            }
                            pid = (et as FilledRegionType).BackgroundPatternId;
                            if (pid != null && pid != ElementId.InvalidElementId &&
                                pid != solidPatternId)
                            {
                                FillPatternElement fpe = et.Document.GetElement(pid) as FillPatternElement;
                                if (fpe != null)
                                {
                                    FillPatternDefinition fpd = new FillPatternDefinition(fpe);
                                    if (!selectedPatterns.Contains(fpd))
                                        selectedPatterns.Add(fpd);
                                    Debug.Print("\tFound {0}", fpe.Name);
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            Debug.WriteLine(string.Format("Found {0} patterns in user selected elements.", selectedPatterns.Count));

            if (selectedPatterns.Count == 0)
            {
                TaskDialog.Show(
                    LocalizationProvider.GetLocalizedValue<string>("TXT_Warning"),
                    LocalizationProvider.GetLocalizedValue<string>("FPC_DIA001_NoPattern")); // The items you selected had no fill patterns that could be worked on. All the patterns will be shown for editing.
                selectedPatterns = null;
                return false;
            }

            if (selectedPatterns.Count == 1)
                return true;
            else
                return false;
        }

        private Result StartMainMode(ExternalCommandData commandData)
        {
            MainWindow mainWindow = null;
            int patternsConverted = 0;
            Transaction transaction = null;
            SubTransaction subTransaction = null;

            try
            {
                Debug.WriteLine("-----------Starting main window");
                mainWindow = new MainWindow(allFillPatterns, selectedPatterns);
                System.Windows.Interop.WindowInteropHelper x = new System.Windows.Interop.WindowInteropHelper(mainWindow);
                x.Owner = commandData.Application.MainWindowHandle;
                mainWindow.ShowDialog();
                Debug.WriteLine("-----------Exiting main window");

                //change patterns and delete unwanted
                if ((bool)mainWindow.DialogResult)
                {
                    resultOutput = new System.Text.StringBuilder();
                    allFillPatterns = mainWindow.TheCollection;
                    IList<ElementId> deleteTheseStyles = new List<ElementId>();
                    transaction = new Transaction(theDoc);
                    transaction.Start(LocalizationProvider.GetLocalizedValue<string>("FPC_Title_MainWindow"));
                    subTransaction = new SubTransaction(theDoc);
                    subTransaction.Start();

                    //iterate through patterns changing as req
                    foreach (FillPatternDefinition fpd in allFillPatterns)
                    {                        
                        if (fpd.StyleToBeConverted || fpd.StyleToBeDeleted || fpd.DeleteElements)
                        {
                            resultOutput.AppendLine();
                            resultOutput.AppendLine(fpd.StyleName);
                            resultOutput.AppendLine(new string('-', fpd.StyleName.Length));
                        }
                        if (fpd.StyleToBeConverted || fpd.DeleteElements)
                        {
                            ConvertPatterns(fpd);
                            if(fpd.StyleToBeConverted)
                                patternsConverted++;
                        }
                        if (fpd.StyleToBeDeleted)
                        {
                            deleteTheseStyles.Add(new ElementId(fpd.ItsId));
                            resultOutput.AppendLine("\t" + LocalizationProvider.GetLocalizedValue<string>("RSLT_Queue_Pattern")); //Pattern queued to be removed.
                            resultOutput.AppendLine();
                            Debug.WriteLine(fpd.StyleName + " is queued to be deleted");
                        }
                    }
                    subTransaction.Commit();
                    
                    //and delete unwanted
                    ICollection<ElementId> deletedList = new List<ElementId>();
                    if (deleteTheseStyles.Count > 0)
                    {
                        deletedList = theDoc.Delete(deleteTheseStyles);
                        Debug.WriteLine("Deleted this many fill patterns: " + deletedList.Count.ToString());
                    }
                    transaction.Commit();
                    resultOutput.AppendLine();
                    resultOutput.AppendLine(LocalizationProvider.GetLocalizedValue<string>("RSLT_Summary"));
                    resultOutput.AppendLine(new string('-', LocalizationProvider.GetLocalizedValue<string>("RSLT_Summary").Length)); // -------
                    resultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("RSLT_Found"), allFillPatterns.Count);
                    resultOutput.AppendLine();
                    resultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("RSLT_Converted"), patternsConverted);
                    resultOutput.AppendLine();
                    resultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("RSLT_Removed"), deletedList.Count);

                    //show result output
                    ResultWindow res_win = new ResultWindow(LocalizationProvider.GetLocalizedValue<string>("FPC_Title_MainWindow"), resultOutput);
                    System.Windows.Interop.WindowInteropHelper x1 = new System.Windows.Interop.WindowInteropHelper(res_win);
                    x1.Owner = commandData.Application.MainWindowHandle;
                    res_win.ShowDialog();

                    _log.Info(resultOutput.ToString());
                    _log.Info("Finished command.");

                    return Result.Succeeded;
                }
                else
                {
                    _log.Info("Finished command.");
                    return Result.Cancelled;
                }
            }
            catch (Exception err)
            {
                if (progWindow != null)
                    progWindow.Dispatcher.Invoke(new Action(progWindow.Close));
                if (mainWindow != null && mainWindow.IsActive)
                    mainWindow.Close();
                if (subTransaction != null && subTransaction.HasStarted())
                    subTransaction.RollBack();
                if (transaction != null && transaction.HasStarted())
                    transaction.RollBack();
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(err).Throw();
                throw;
            }
        }

        private Result StartSingleSelectionMode(ExternalCommandData commandData)
        {
            Transaction transaction = null;
            SubTransaction subtransaction = null;
            SingleElementWindow singleElementWindow = null;

            try
            {
                Debug.WriteLine("-----------Starting single mode window");
                FillPatternDefinition selectedFpd = selectedPatterns.First();
                foreach(FillPatternDefinition f in allFillPatterns)
                {
                    if(f.Equals(selectedFpd))
                    {
                        selectedFpd = f;
                        break;
                    }
                }
                singleElementWindow = new SingleElementWindow(allFillPatterns, selectedFpd);
                System.Windows.Interop.WindowInteropHelper wih = new System.Windows.Interop.WindowInteropHelper(singleElementWindow);
                wih.Owner = commandData.Application.MainWindowHandle;
                singleElementWindow.ShowDialog();
                Debug.WriteLine("-----------Exiting single mode window");

                //change line style and delete the old styles
                if ((bool)singleElementWindow.DialogResult)
                {
                    selectedFpd.NewStyle = singleElementWindow.chossenStyle;
                    if(selectedFpd.IsDeleteable)
                        selectedFpd.StyleToBeDeleted = singleElementWindow.DeleteSourceStyle;
                    resultOutput = new System.Text.StringBuilder();
                    transaction = new Transaction(theDoc);
                    transaction.Start(LocalizationProvider.GetLocalizedValue<string>("FPC_Title_MainWindow"));
                    subtransaction = new SubTransaction(theDoc);
                    subtransaction.Start();

                    if(selectedFpd.StyleToBeConverted || selectedFpd.StyleToBeDeleted || selectedFpd.DeleteElements)
                    {
                        resultOutput.AppendLine(selectedFpd.StyleName);
                        resultOutput.AppendLine(new string('-', selectedFpd.StyleName.Length));
                    }
                    if (selectedFpd.StyleToBeConverted)
                    {
                        ConvertPatterns(selectedFpd);
                    }
                    subtransaction.Commit();
                    //and delete unwanted
                    if (selectedFpd.StyleToBeDeleted)
                    {
                        theDoc.Delete(new ElementId(selectedFpd.ItsId));
                        Debug.WriteLine("Deleted style: " + selectedFpd.StyleName);
                        resultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("RSLT_SingleMode"), selectedFpd.StyleName); // The style was removed from the project.
                    }
                    transaction.Commit();

                    //show result output
                    ResultWindow res_win = new ResultWindow(LocalizationProvider.GetLocalizedValue<string>("FPC_Title_MainWindow"), resultOutput);
                    System.Windows.Interop.WindowInteropHelper x1 = new System.Windows.Interop.WindowInteropHelper(res_win);
                    x1.Owner = commandData.Application.MainWindowHandle;
                    res_win.ShowDialog();

                    _log.Info(resultOutput.ToString());
                    _log.Info("Finished command.");
                    return Result.Succeeded;
                }
                else
                {
                    _log.Info("Finished command.");
                    return Result.Cancelled;
                }
            }
            catch (Exception err)
            {
                if (singleElementWindow != null && singleElementWindow.IsActive)
                    singleElementWindow.Close();
                if (subtransaction != null && subtransaction.HasStarted())
                    subtransaction.RollBack();
                if (transaction != null && transaction.HasStarted())
                    transaction.RollBack();
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(err).Throw();
                throw;
            }
        }

        void ScanFamiliesForFillPattern()
        {
            string familyName = null;

            try
            {
                //Starts New Progress Window Thread
                using (_progressWindowWaitHandle = new AutoResetEvent(false))
                {
                    //Starts the progress window thread
                    Thread newprogWindowThread = new Thread(new ThreadStart(ShowProgWindow));
                    newprogWindowThread.SetApartmentState(ApartmentState.STA);
                    newprogWindowThread.IsBackground = true;
                    newprogWindowThread.Start();

                    //Wait for thread to notify that it has created the window
                    _progressWindowWaitHandle.WaitOne();
                }

                int i = 1;
                string scanTxt = LocalizationProvider.GetLocalizedValue<string>("FPC_Progress");

                foreach (Family f in familyLib)
                {
                    if (this.progWindow.IsCanceled)
                    {
                        Debug.WriteLine("---------USER CANCELED----------");
                        cancelledFromProgressWindow = true;
                        break;
                    }
                    familyName = f.Name;
                    this.progWindow.UpdateProgress(scanTxt + familyName, i, familyLib.Count);
                    _log.InfoFormat("Checking family {0}", familyName);
                    if (f.get_Parameter(BuiltInParameter.FAMILY_SHARED).AsInteger() == 1)
                        if (sharedFamilies.Contains(familyName))
                            continue;
                        else
                            sharedFamilies.Add(familyName);
                    rootFamilyList = new System.Collections.ArrayList();
                    CheckForFillPatterns(f, f);
                    System.Diagnostics.Debug.WriteLine("Scanning families for fill patterns - {0}%", Convert.ToInt32(((double)i / familyLib.Count) * 100));
                    ++i;
                }

                //closes the Progress window
                progWindow.Dispatcher.Invoke(new Action(progWindow.Close));
                progWindow = null;
            }
            catch (Exception err)
            {
                System.Diagnostics.Debug.WriteLine(err.ToString());
                _log.Error(err.ToString() + "FAMILY:" + familyName);
                err.Data.Add("Fill Pattern Cleaner", "FAMILY:" + familyName);
                if (progWindow != null)
                    progWindow.Dispatcher.Invoke(new Action(progWindow.Close));
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(err).Throw();
                throw;
            }
        }

        /// <summary>
        /// Checks through all materials, fill region types and nested families for all the fill patterns and
        /// adds project level family to assets list.
        /// </summary>
        /// <param name="familyToCheck">Family from project file or nested family to scan</param>
        /// <param name="projectFamily">Family in project file that should be added to asset list</param>
        /// <returns>True if fill pattern found, false otherwise. Used to stop scanning deeper than project level families</returns>
        private bool CheckForFillPatterns(Family familyToCheck, Family projectFamily)
        {
            bool result = false;

            if (!familyToCheck.IsEditable)
            {
                _log.InfoFormat("Family {0} : {1} was not editable.", familyToCheck.FamilyCategory.Name, familyToCheck.Name);
                System.Diagnostics.Debug.WriteLine("Family {0} : {1} was not editable.", familyToCheck.FamilyCategory.Name, familyToCheck.Name);
                return false;
            }
            else
               _log.InfoFormat("Checking family {0} : {1}", familyToCheck.FamilyCategory.Name, familyToCheck.Name);

            System.Diagnostics.Debug.WriteLine("Checking {0} : {1} for fill patterns.", familyToCheck.FamilyCategory.Name, familyToCheck.Name);
            Document ftcDocument = familyToCheck.Document.EditFamily(familyToCheck);
            rootFamilyList.Add(familyToCheck.Name);

            //check MATERIALS
            FilteredElementCollector collector = new FilteredElementCollector(ftcDocument);
            IList<Element> componentTypes = collector.WherePasses(new ElementClassFilter(typeof(Material))).ToElements();
            System.Diagnostics.Debug.WriteLine("Found {0} materials.", componentTypes.Count);
            foreach (Material mt in componentTypes)
            {
                foreach (FillPatternDefinition fpd in allFillPatterns)
                {
                    if (mt != null && 
                        mt.CutForegroundPatternId.IntegerValue == fpd.ItsId || 
                        mt.CutBackgroundPatternId.IntegerValue == fpd.ItsId ||
                        mt.SurfaceForegroundPatternId.IntegerValue == fpd.ItsId ||
                        mt.SurfaceBackgroundPatternId.IntegerValue == fpd.ItsId)
                    {
                        System.Diagnostics.Debug.WriteLine(string.Format("\tFound fill pattern {0} in material.", fpd.StyleName));
                        fpd.AddAsset(new AssetDefinition(projectFamily));
                        result = true;
                    }
                }
            }

            if (this.progWindow.IsCanceled)
                return false;

            if (!result)
            {
                //check FILLED REGION TYPES
                collector = new FilteredElementCollector(ftcDocument);
                componentTypes = collector.WherePasses(new ElementClassFilter(typeof(FilledRegionType))).ToElements();
                System.Diagnostics.Debug.WriteLine("Found {0} Filled Region Types.", componentTypes.Count);
                foreach (FilledRegionType frt in componentTypes)
                {
                    foreach (FillPatternDefinition fpd in allFillPatterns)
                    {
                        if (frt != null)
                        {
                            if (frt.ForegroundPatternId.IntegerValue == fpd.ItsId)
                            {
                                System.Diagnostics.Debug.WriteLine(string.Format("\tFound foreground fill pattern {0} in a filled region type.", fpd.StyleName));
                                fpd.AddAsset(new AssetDefinition(projectFamily));
                                result = true;
                            }
                            else if (frt.BackgroundPatternId.IntegerValue == fpd.ItsId)
                            {
                                System.Diagnostics.Debug.WriteLine(string.Format("\tFound background fill pattern {0} in a filled region type.", fpd.StyleName));
                                fpd.AddAsset(new AssetDefinition(projectFamily));
                                result = true;
                            }
                        }
                    }
                }
            }

            if (this.progWindow.IsCanceled)
                return false;

            if (result)
                _log.InfoFormat("Family {0} : {1} has fill patterns.", familyToCheck.FamilyCategory.Name, familyToCheck.Name);
            else
                _log.InfoFormat("Family {0} : {1} did not have fill patterns.", familyToCheck.FamilyCategory.Name, familyToCheck.Name);

            /// Autodesk.Revit.Exceptions.InvalidOperationException: Loaded Family Editing failed. Can happen when family is already being edited.
            /// Could this be a recursion error? A nested family with the same name attempting to be edited? Can I close the previous document
            /// before opening the nested families? Or use the families owner family to open it? use document from family instead of editfamily(doesn't
            /// work)

            //check for nested families and check them
            collector = new FilteredElementCollector(ftcDocument);
            componentTypes = collector.WherePasses(new ElementClassFilter(typeof(Family))).ToElements();
            System.Diagnostics.Debug.WriteLine("Checking {0} nested famlies...", componentTypes.Count);
            foreach (Family f in componentTypes)
            {
                if (string.IsNullOrEmpty(f.Name))
                    continue;

                if (this.progWindow.IsCanceled)
                    return false;

                _log.InfoFormat("Checking nested family: {0}", f.Name);

                //shared families only need to be checked once. They are consistent throughout the project.
                if (f.get_Parameter(BuiltInParameter.FAMILY_SHARED).AsInteger() == 1)
                    if (sharedFamilies.Contains(f.Name))
                        continue;
                    else
                        sharedFamilies.Add(f.Name);

                //root family is reset in a way that each tree of families in the project is checked for self referencing families
                if (rootFamilyList.Contains(f.Name))
                {
                    _log.InfoFormat("Temporarily renaming self referencing family: {0}", f.Name);
                    Transaction t = new Transaction(ftcDocument, "Self reference conversion");
                    t.Start();
                    f.Name = f.Name + Guid.NewGuid().ToString();
                    t.Commit();
                    break;
                }
                else
                    rootFamilyList.Add(f.Name);

                if (CheckForFillPatterns(f, projectFamily))
                {
                    result = true;
                }
            }

            ftcDocument.Close(false);
            if(result)
                _log.InfoFormat("Family {0} : {1} has fill patterns.", familyToCheck.FamilyCategory.Name, familyToCheck.Name);
            else
                _log.InfoFormat("Family {0} : {1} did not have fill patterns.", familyToCheck.FamilyCategory.Name, familyToCheck.Name);
            return result;
        }

        private void ConvertPatterns(FillPatternDefinition fpd)
        {
            Debug.Print("Converting : {0}", fpd.StyleName);

            foreach (AssetDefinition assetDef in fpd.getAssets())
            {
                Element theElement = theDoc.GetElement(new ElementId(assetDef.RvtId));

                if (assetDef.Type == "Material")
                {
                    Material mt = theElement as Material;
                    if (mt.CutForegroundPatternId.IntegerValue == fpd.ItsId)
                    {
                        Debug.Print("Changing cut pattern of material {0}", mt.Name);
                        if (fpd.SetToNone)
                        {
                            mt.CutForegroundPatternId = ElementId.InvalidElementId;
                            resultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("FPC_RSLT_RemCutFgPatt"), mt.Name); // Removed cut foreground pattern on material {0}.
                        }
                        else
                        {
                            mt.CutForegroundPatternId = (new ElementId(fpd.NewStyle.ItsId));
                            resultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("FPC_RSLT_ConvCutFgPatt"), mt.Name, fpd.NewStyle.StyleName); // Converted cut foreground pattern on material {0} to {1} pattern.
                        }
                        resultOutput.AppendLine();
                    }
                    if (mt.CutBackgroundPatternId.IntegerValue == fpd.ItsId)
                    {
                        Debug.Print("Changing cut pattern of material {0}", mt.Name);
                        if (fpd.SetToNone)
                        {
                            mt.CutBackgroundPatternId = ElementId.InvalidElementId;
                            resultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("FPC_RSLT_RemCutBgPatt"), mt.Name); // Removed cut background pattern on material {0}.
                        }
                        else
                        {
                            mt.CutBackgroundPatternId = (new ElementId(fpd.NewStyle.ItsId));
                            resultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("FPC_RSLT_ConvCutBgPatt"), mt.Name, fpd.NewStyle.StyleName); // Converted cut background pattern on material {0} to {1} pattern.
                        }
                        resultOutput.AppendLine();
                    }
                    if (mt.SurfaceForegroundPatternId.IntegerValue == fpd.ItsId)
                    {
                        Debug.Print("Changing surface pattern of material {0}", mt.Name);
                        if (fpd.SetToNone)
                        {
                            mt.SurfaceForegroundPatternId = ElementId.InvalidElementId;
                            resultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("FPC_RSLT_RemSurfFgPatt"), mt.Name); // Removed surface foreground pattern on material {0}.
                        }
                        else
                        {
                            mt.SurfaceForegroundPatternId = (new ElementId(fpd.NewStyle.ItsId));
                            resultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("FPC_RSLT_ConvSurfFgPatt"), mt.Name, fpd.NewStyle.StyleName); // Converted surface foreground pattern on material {0} to {1} pattern.
                        }
                        resultOutput.AppendLine();
                    }
                    if (mt.SurfaceBackgroundPatternId.IntegerValue == fpd.ItsId)
                    {
                        Debug.Print("Changing surface pattern of material {0}", mt.Name);
                        if (fpd.SetToNone)
                        {
                            mt.SurfaceBackgroundPatternId = ElementId.InvalidElementId;
                            resultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("FPC_RSLT_RemSurfBgPatt"), mt.Name); // Removed surface background pattern on material {0}.
                        }
                        else
                        {
                            mt.SurfaceBackgroundPatternId = (new ElementId(fpd.NewStyle.ItsId));
                            resultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("FPC_RSLT_ConvSurfBgPatt"), mt.Name, fpd.NewStyle.StyleName); // Converted surface background pattern on material {0} to {1} pattern.
                        }
                        resultOutput.AppendLine();
                    }
                }

                else if (assetDef.Type == "Region")
                {
                    FilledRegionType frt = theElement as FilledRegionType;
                    if (frt.ForegroundPatternId.IntegerValue == fpd.ItsId)
                    {
                        Debug.Print("Changing filled region type {0}", frt.Name);
                        if (fpd.SetToNone)
                        {
                            frt.ForegroundPatternId = ElementId.InvalidElementId;
                            resultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("FPC_RSLT_FillRegFg_Rem"), frt.Name); // Removed foreground pattern on filled region type {0}.
                        }
                        else
                        {
                            frt.ForegroundPatternId = new ElementId(fpd.NewStyle.ItsId);
                            resultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("FPC_RSLT_FillRegFg_Conv"), frt.Name, fpd.NewStyle.StyleName); // Set foreground pattern on filled region type {0} to {1}.
                        }
                        resultOutput.AppendLine();
                    }

                    if (frt.BackgroundPatternId.IntegerValue == fpd.ItsId)
                    {
                        Debug.Print("Changing filled region type {0}", frt.Name);
                        if (fpd.SetToNone)
                        {
                            frt.BackgroundPatternId = ElementId.InvalidElementId;
                            resultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("FPC_RSLT_FillRegBg_Rem"), frt.Name); // Removed background pattern on filled region type {0}.
                        }
                        else
                        {
                            if ((fpd.NewStyle as FillPatternDefinition).TheTarget == FillPatternTarget.Drafting) // background must have drafting pattern
                            {
                                frt.BackgroundPatternId = new ElementId(fpd.NewStyle.ItsId);
                                resultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("FPC_RSLT_FillRegBg_Conv"), frt.Name, fpd.NewStyle.StyleName); // Set background pattern on filled region type {0} to {1}.
                            }
                            else
                            {
                                //FPC DIALOG #004
                                TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("TXT_Warning"));
                                td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                                td.CommonButtons = TaskDialogCommonButtons.Ok;
                                td.MainContent = string.Format(LocalizationProvider.GetLocalizedValue<string>("FPC_DIA004_MainCont"),
                                    frt.Name,
                                    fpd.NewStyle.StyleName); //"Could not set background pattern of filled region type {0} to pattern {1}."
                                td.MainInstruction = string.Format(LocalizationProvider.GetLocalizedValue<string>("FPC_DIA004_MainInst"), fpd.StyleName); //This pattern is not a drafting pattern. The filled region type will remain unchanged and the fill pattern {0} will not be removed.
                                td.Show();
                                fpd.StyleToBeDeleted = false;
                                _log.InfoFormat("\t****" + LocalizationProvider.GetLocalizedValue<string>("FPC_RSLT_FrtcFailed"),
                                    frt.Name,
                                    fpd.NewStyle.StyleName);
                                resultOutput.AppendFormat("\t****" + LocalizationProvider.GetLocalizedValue<string>("FPC_RSLT_FrtcFailed"),
                                    frt.Name,
                                    fpd.NewStyle.StyleName); // Could not set background of filled region type {0} to pattern {1}. This pattern is not a drafting pattern.
                            }
                        }
                        resultOutput.AppendLine();
                    }
                }

                //walls, roofs and compund ceilings
                else if (assetDef.Type == "Component")
                {
                    Parameter p = theElement.get_Parameter(BuiltInParameter.COARSE_SCALE_FILL_PATTERN_ID_PARAM);
                    if (p.AsElementId().IntegerValue == fpd.ItsId)
                    {
                        Debug.Print("Found {0} called {1} using pattern.", theElement.Name, theElement.Category.Name);
                        if (fpd.SetToNone)
                        {
                            p.Set(ElementId.InvalidElementId);
                            resultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("FPC_RSLT_RemCourseFP"), theElement.Name); // Removed "Course Scale Fill Pattern" on {0}.
                        }
                        else
                        {
                            p.Set(new ElementId(fpd.NewStyle.ItsId));
                            resultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("FPC_RSLT_ConvCourseFP"), theElement.Name, fpd.NewStyle.StyleName); // Converted "Course Scale Fill Pattern" on {0} to {1} pattern.
                        }
                        resultOutput.AppendLine();
                    }
                }
            }
        }

        /// <summary>
        /// Scan all wall types, roof types, ceiling types, floor types, materials and filled region types and store
        /// the ones that use a fill pattern into libraries. Also stoes all families into a library for scanning later.
        /// Only materials and filled region types have double fill patterns.
        /// </summary>
        /// <param name="theDoc"></param>
        private void InitalizeLibraries(Document theDoc)
        {
            //families (detail components, annotations, model components, profiles)
            familyLib = new List<Element>();
            FilteredElementCollector collector = new FilteredElementCollector(theDoc);
            familyLib = collector.WherePasses(new ElementClassFilter(typeof(Family))).ToElements();
            Debug.WriteLine(string.Format("Found {0} families.", familyLib.Count));

            componentLib = new List<Element>();
            //walls
            collector = new FilteredElementCollector(theDoc);
            IList<Element> filteredTypes = collector.WherePasses(new ElementClassFilter(typeof(WallType))).ToElements();
            foreach (WallType wt in filteredTypes)
            {
                if (wt != null && wt.get_Parameter(BuiltInParameter.COARSE_SCALE_FILL_PATTERN_ID_PARAM).AsElementId() != ElementId.InvalidElementId)
                {
                    componentLib.Add(wt);
                }
            }
            //roofs
            collector = new FilteredElementCollector(theDoc);
            filteredTypes = collector.WherePasses(new ElementClassFilter(typeof(RoofType))).ToElements();
            foreach (RoofType rt in filteredTypes)
            {
                if (rt != null && rt.get_Parameter(BuiltInParameter.COARSE_SCALE_FILL_PATTERN_ID_PARAM).AsElementId() != ElementId.InvalidElementId)
                {
                    componentLib.Add(rt);
                }
            }
            //ceilings
            collector = new FilteredElementCollector(theDoc);
            filteredTypes = collector.WherePasses(new ElementClassFilter(typeof(CeilingType))).ToElements();
            foreach (CeilingType ct in filteredTypes)
            {
                if (ct != null && ct.get_Parameter(BuiltInParameter.COARSE_SCALE_FILL_PATTERN_ID_PARAM).AsElementId() != ElementId.InvalidElementId)
                {
                    componentLib.Add(ct);
                }
            }
            //floors
            collector = new FilteredElementCollector(theDoc);
            filteredTypes = collector.WherePasses(new ElementClassFilter(typeof(FloorType))).ToElements();
            foreach (FloorType ft in filteredTypes)
            {
                if (ft != null && ft.get_Parameter(BuiltInParameter.COARSE_SCALE_FILL_PATTERN_ID_PARAM).AsElementId() != ElementId.InvalidElementId)
                {
                    componentLib.Add(ft);
                }
            }
            Debug.WriteLine(string.Format("Found {0} component types with course fill patterns.", componentLib.Count));

            //MaterialLib
            materialLib = new List<Element>();
            collector = new FilteredElementCollector(theDoc);
            filteredTypes = collector.WherePasses(new ElementClassFilter(typeof(Material))).ToElements();
            foreach (Material mt in filteredTypes)
            {
                if (mt != null && 
                    mt.CutForegroundPatternId != ElementId.InvalidElementId || 
                    mt.CutBackgroundPatternId != ElementId.InvalidElementId ||
                    mt.SurfaceForegroundPatternId != ElementId.InvalidElementId || 
                    mt.SurfaceBackgroundPatternId !=  ElementId.InvalidElementId)
                {
                    materialLib.Add(mt);
                }
            }
            Debug.WriteLine(string.Format("Found {0} materials.", materialLib.Count));

            //RegionLib
            regionLib = new List<Element>();
            collector = new FilteredElementCollector(theDoc);
            filteredTypes = collector.WherePasses(new ElementClassFilter(typeof(FilledRegionType))).ToElements();
            foreach (FilledRegionType frt in filteredTypes)
            {
                if (frt != null)
                {
                    if (frt.ForegroundPatternId != ElementId.InvalidElementId ||
                        frt.BackgroundPatternId != ElementId.InvalidElementId)
                    {
                        regionLib.Add(frt);
                    }
                }
            }
            Debug.WriteLine(string.Format("Found {0} region types.", regionLib.Count));
        }

        private void ShowProgWindow()
        {
            //creates and shows the progress window
            progWindow = new ProgressBarWindow(_cancelText, _progCancelText);
            //makes sure dispatcher is shut down when the window is closed
            progWindow.Closed += new EventHandler(ProgWindowClosed);
            System.Windows.Interop.WindowInteropHelper x = new System.Windows.Interop.WindowInteropHelper(progWindow);
            x.Owner = RevitHandle;
            progWindow.Show();
            //Notifies command thread the window has been created
            _progressWindowWaitHandle.Set();
            //Starts window dispatcher
            System.Windows.Threading.Dispatcher.Run();
        }

        private void ProgWindowClosed(object sender, EventArgs e)
        {
            System.Windows.Threading.Dispatcher.ExitAllFrames();
        }

        void ApplicationFailuresProcessing(object sender, Autodesk.Revit.DB.Events.FailuresProcessingEventArgs e)
        {
            FailuresAccessor fa = e.GetFailuresAccessor();
            IList<FailureMessageAccessor> failList = new List<FailureMessageAccessor>();
            failList = fa.GetFailureMessages(); // Inside event handler, get all warnings
            foreach (FailureMessageAccessor failure in failList)
            {
                FailureDefinitionId id = failure.GetFailureDefinitionId();
                FailureSeverity failureSeverity = fa.GetSeverity();
                if (failureSeverity == FailureSeverity.Warning)   //simply catch all warnings, so you don't have to find out what warning is causing the message to pop up
                {
                    fa.DeleteWarning(failure);
                }
            }
        }
    }

    public class CommandAvailableCheck : IExternalCommandAvailability
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

    class FamilyOption : IFamilyLoadOptions
    {
        public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
        {
            overwriteParameterValues = true;
            return true;
        }

        public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues)
        {
            source = FamilySource.Family;
            overwriteParameterValues = true;
            return true;
        }
    }
}