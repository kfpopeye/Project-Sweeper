using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Collections.ObjectModel;
using log4net;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PKHL.ProjectSweeper.LinePatternCleaner
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class MainCommand : IExternalCommand
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(MainCommand));
        private Document theDoc = null;
        private ObservableCollection<LinePatternDefinition> allLinePatterns = null;
        private IList<LinePatternDefinition> selectedPatterns = null;
        private System.Text.StringBuilder resultOutput = null;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
#if !DEBUG
                if (!ProjectSweeper.UserIsEntitled(commandData))
                    return Result.Failed;
#endif
                theDoc = commandData.Application.ActiveUIDocument.Document;
                allLinePatterns = new ObservableCollection<LinePatternDefinition>();

                //Collect all line pattern elements
                FilteredElementCollector collector = new FilteredElementCollector(theDoc);
                IList<Element> linePatternElements = collector.WherePasses(new ElementClassFilter(typeof(LinePatternElement))).ToElements();
                //Collect all graphicsStyles
                FilteredElementCollector collector2 = new FilteredElementCollector(this.theDoc);
                IList<Element> graphicStyleElements = collector2.WherePasses(new ElementClassFilter(typeof(GraphicsStyle))).ToElements();

                foreach (Element el in linePatternElements)
                {
                    LinePatternElement lpe = el as LinePatternElement;
                    allLinePatterns.Add(new LinePatternDefinition(lpe, ref graphicStyleElements));
                }
            }
            catch (Exception err)
            {
                _log.Error(LocalizationProvider.GetLocalizedValue<string>("LPC_Title_MainWindow"), err);
                Debug.WriteLine(new string('*', 100));
                Debug.WriteLine(err.ToString());
                Debug.WriteLine(new string('*', 100));

                Autodesk.Revit.UI.TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Title"));
                td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("LPC_Title_MainWindow") + LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainInst");
                td.MainContent = LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainCont");
                td.ExpandedContent = err.ToString();
                //td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Command1"));
                td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                TaskDialogResult tdr = td.Show();

                //if (tdr == TaskDialogResult.CommandLink1)
                //{
                //    pkhCommon.Email.SendErrorMessage(
                //        commandData.Application.Application.VersionName, 
                //        commandData.Application.Application.VersionBuild, 
                //        err, 
                //        this.GetType().Assembly.GetName());
                //}

                return Result.Failed;
            }

            if (UserSelectedSingleElement(commandData))
                return StartSingleSelectionMode(commandData);
            else
                return StartMainMode(commandData);
        }

        private bool UserSelectedSingleElement(ExternalCommandData commandData)
        {
            ICollection<ElementId> selection = commandData.Application.ActiveUIDocument.Selection.GetElementIds();
            if (selection == null || selection.Count == 0)
            {
                Debug.Print("Selection set was null or empty.");
                return false;
            }

            selectedPatterns = new List<LinePatternDefinition>();
            foreach (ElementId eid in selection)
            {
                Element el = theDoc.GetElement(eid);
                if (el.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Lines)
                {
                    CurveElement ce = el as CurveElement;
                    GraphicsStyle gs = ce.LineStyle as GraphicsStyle;
                    ElementId e = gs.GraphicsStyleCategory.GetLinePatternId(gs.GraphicsStyleType);
                    if (!e.Equals(LinePatternElement.GetSolidPatternId()))
                    {
                        LinePatternDefinition lpe = new LinePatternDefinition(gs);
                        selectedPatterns.Add(lpe);
                    }
                }
            }
            Debug.WriteLine(string.Format("Found {0} patterns in user selected elements.", selectedPatterns.Count));

            if (selectedPatterns.Count == 0)
            {
                //Revit Taskdialog LPC_DIA001
                TaskDialog.Show(LocalizationProvider.GetLocalizedValue<string>("LPC_DIA001_TItle"), // "No patterns found"
                    LocalizationProvider.GetLocalizedValue<string>("LPC_DIA001_MainInst")); // The line pattern of the selected element was "Solid" and cannot be changed or the selected element was not a detail or model line. Starting in multi-mode.
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
            Transaction transaction = null;
            SubTransaction subTransaction = null;
            int patternsConverted = 0;
            try
            {
                Debug.WriteLine("-----------Starting main window");
                mainWindow = new MainWindow(allLinePatterns, selectedPatterns, ref theDoc);
                System.Windows.Interop.WindowInteropHelper x = new System.Windows.Interop.WindowInteropHelper(mainWindow);
                x.Owner = commandData.Application.MainWindowHandle;
                mainWindow.ShowDialog();
                Debug.WriteLine("-----------Exiting main window");

                //change patterns and delete unwanted
                if ((bool)mainWindow.DialogResult)
                {
                    resultOutput = new System.Text.StringBuilder();
                    allLinePatterns = mainWindow.TheCollection;
                    transaction = new Transaction(theDoc);
                    transaction.Start(LocalizationProvider.GetLocalizedValue<string>("LPC_Title_MainWindow"));
                    IList<ElementId> deleteThesePatterns = new List<ElementId>();
                    subTransaction = new SubTransaction(theDoc);
                    subTransaction.Start();

                    //iterate through patterns changing as req
                    foreach (LinePatternDefinition lpd in allLinePatterns)
                    {
                        if (lpd.StyleToBeConverted || lpd.StyleToBeDeleted)
                        {
                            resultOutput.AppendLine();
                            resultOutput.AppendLine(lpd.StyleName);
                            resultOutput.AppendLine(new string('-', lpd.StyleName.Length));
                        }

                        if (lpd.StyleToBeConverted && lpd.OwnerAssets.Count != 0)
                        {
                            ConvertPattern(lpd);
                            patternsConverted++;
                        }

                        if (lpd.StyleToBeDeleted)
                        {
                            deleteThesePatterns.Add(new ElementId(lpd.ItsId));
                            resultOutput.AppendLine("\t" + LocalizationProvider.GetLocalizedValue<string>("RSLT_Queue_Pattern")); // Pattern queued to be removed
                            resultOutput.AppendLine();
                            Debug.WriteLine(lpd.StyleName + " is queued to be deleted");
                        }
                    }
                    subTransaction.Commit();

                    //and delete unwanted
                    int deleted = 0;
                    if (deleteThesePatterns.Count > 0)
                    {
                        subTransaction = new SubTransaction(theDoc);
                        subTransaction.Start();
                        deleted = theDoc.Delete(deleteThesePatterns).Count;
                        Debug.WriteLine("Deleted this many line patterns: " + deleted.ToString());
                        subTransaction.Commit();
                    }
                    transaction.Commit();

                    resultOutput.AppendLine();
                    resultOutput.AppendLine(LocalizationProvider.GetLocalizedValue<string>("RSLT_Summary"));
                    resultOutput.AppendLine(new string('-', LocalizationProvider.GetLocalizedValue<string>("RSLT_Summary").Length)); // -------
                    resultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("RSLT_Found"), allLinePatterns.Count);
                    resultOutput.AppendLine();
                    resultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("RSLT_Converted"), patternsConverted);
                    resultOutput.AppendLine();
                    resultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("RSLT_Removed"), deleted);

                    //show result output
                    ResultWindow res_win = new ResultWindow(LocalizationProvider.GetLocalizedValue<string>("LPC_Title_MainWindow"), resultOutput);
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
            SubTransaction subTransaction = null;
            SingleElementWindow singleWindow = null;
            try
            {
                Debug.WriteLine("-----------Starting single mode window");
                LinePatternDefinition selectedLpd = selectedPatterns.First();
                foreach (LinePatternDefinition f in allLinePatterns)
                {
                    if (f.Equals(selectedLpd))
                    {
                        selectedLpd = f;
                        break;
                    }
                }
                singleWindow = new SingleElementWindow(allLinePatterns, selectedLpd);
                System.Windows.Interop.WindowInteropHelper wih = new System.Windows.Interop.WindowInteropHelper(singleWindow);
                wih.Owner = commandData.Application.MainWindowHandle;
                singleWindow.ShowDialog();
                Debug.WriteLine("-----------Exiting single mode window");

                //change line style and delete the old styles
                if ((bool)singleWindow.DialogResult)
                {
                    selectedLpd.NewStyle = singleWindow.chossenStyle;
                    selectedLpd.StyleToBeDeleted = singleWindow.DeleteSourceStyle;
                    resultOutput = new System.Text.StringBuilder();

                    transaction = new Transaction(theDoc);
                    transaction.Start(LocalizationProvider.GetLocalizedValue<string>("LPC_Title_MainWindow"));
                    if (selectedLpd.StyleToBeConverted)
                    {
                        resultOutput.AppendLine(selectedLpd.StyleName);
                        resultOutput.AppendLine(new string('-', selectedLpd.StyleName.Length));
                        subTransaction = new SubTransaction(theDoc);
                        subTransaction.Start();
                        ConvertPattern(selectedLpd);
                        subTransaction.Commit();
                    }

                    //and delete unwanted
                    if (selectedLpd.StyleToBeDeleted)
                    {
                        subTransaction = new SubTransaction(theDoc);
                        subTransaction.Start();
                        theDoc.Delete(new ElementId(selectedLpd.ItsId));
                        Debug.WriteLine("Deleted style: " + selectedLpd.StyleName);
                        subTransaction.Commit();
                        resultOutput.Append(LocalizationProvider.GetLocalizedValue<string>("RSLT_SingleMode"));
                    }
                    transaction.Commit();

                    //show result output
                    ResultWindow res_win = new ResultWindow(LocalizationProvider.GetLocalizedValue<string>("LPC_Title_MainWindow"), resultOutput);
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
                if (singleWindow != null && singleWindow.IsActive)
                    singleWindow.Close();
                if (subTransaction != null && subTransaction.HasStarted())
                    subTransaction.RollBack();
                if (transaction != null && transaction.HasStarted())
                    transaction.RollBack();
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(err).Throw();
                throw;
            }
        }

        private void ConvertPattern(LinePatternDefinition lpd)
        {
            Debug.Print("Converting : {0}", lpd.StyleName);
            if (lpd.OwnerAssets.Count > 0)
            {
                resultOutput.AppendFormat("\t" + LocalizationProvider.GetLocalizedValue<string>("LPC_RSLT_ConvertedStyle"), lpd.NewStyle.StyleName); // Converted the following style(s) to pattern : {0}
                resultOutput.AppendLine();
            }
            else
                return;

            foreach (AssetDefinition ad in lpd.OwnerAssets)
            {
                GraphicsStyle gs = theDoc.GetElement(new ElementId(ad.RvtId)) as GraphicsStyle;
                if(gs != null)
                {
                    if (lpd.DeleteElements) //convert to solid line style
                        gs.GraphicsStyleCategory.SetLinePatternId(LinePatternElement.GetSolidPatternId(), gs.GraphicsStyleType);
                    else
                        gs.GraphicsStyleCategory.SetLinePatternId(new ElementId(lpd.NewStyle.ItsId), gs.GraphicsStyleType);
                    Debug.Print("Converted : {0}", gs.Name);
                    if(gs.GraphicsStyleCategory.Parent != null)
                        resultOutput.AppendFormat("\t\t{0} : {1}", gs.GraphicsStyleCategory.Parent.Name, gs.Name);
                    else
                        resultOutput.AppendFormat("\t\t{0}", gs.Name);
                    resultOutput.AppendLine();
                }
            }
        }
    }

    public class command_AvailableCheck : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication appdata, CategorySet selectCatagories)
        {
            if (appdata.Application.Documents.Size == 0)
                return false;

            if (appdata.ActiveUIDocument.Document.IsFamilyDocument)
                return false;

            return true;
        }
    }
}