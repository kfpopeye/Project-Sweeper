using System;
using System.Diagnostics;
using log4net;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PKHL.ProjectSweeper
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class AboutBoxCommand : IExternalCommand
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(AboutBoxCommand));

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            PKHL.ProjectSweeper.AboutBox aboutBox = null;
            try
            {
#if MULTILICENSE

                //if (ProjectSweeper.UserIsEntitled(commandData))
                //    aboutBox = new AboutBox(System.Reflection.Assembly.GetExecutingAssembly(),
                //        PKHL.ProjectSweeper.ProjectSweeper.License.AuthenticationData,
                //        PKHL.ProjectSweeper.ProjectSweeper.License.ComputerID,
                //        ((DateTime)ProjectSweeper.License.ExpiryDate).ToString("MMMM dd, yyyy")
                //        );
                //else if(ProjectSweeper.License != null)
                //{
                //    aboutBox = new AboutBox(System.Reflection.Assembly.GetExecutingAssembly(), null, ProjectSweeper.License.ComputerID);
                //}
                //else
                //{
                //    aboutBox = new AboutBox(System.Reflection.Assembly.GetExecutingAssembly(), null, null);
                //}
#else
                //aboutBox = new AboutBox(System.Reflection.Assembly.GetExecutingAssembly());
#endif

                aboutBox = new AboutBox(System.Reflection.Assembly.GetExecutingAssembly());
                System.Windows.Interop.WindowInteropHelper x = new System.Windows.Interop.WindowInteropHelper(aboutBox);
                x.Owner = commandData.Application.MainWindowHandle;
                aboutBox.ShowDialog();
            }
            catch (Exception err)
            {
                _log.Error("About dialog", err);
                if (aboutBox != null)
                    aboutBox.Close();
                TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Title"));
                td.MainInstruction = Constants.GROUP_NAME + LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainInst");
                td.MainContent = LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainCont");
                td.ExpandedContent = err.ToString();
                //td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Command1"));
                td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                TaskDialogResult tdr = td.Show();

                //if (tdr == TaskDialogResult.CommandLink1)
                //{
                //    pkhCommon.Email.SendErrorMessage(commandData.Application.Application.VersionName, 
                //        commandData.Application.Application.VersionBuild, 
                //        err, 
                //        this.GetType().Assembly.GetName());
                //}
            }

            return Result.Succeeded;
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class ApplicationHelp : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            ProjectSweeper._contextualHelp.HelpTopicUrl = @"Welcome.htm";
            ProjectSweeper._contextualHelp.Launch();
            return Result.Succeeded;
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class ApplicationEula : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
            myProcess.StartInfo.FileName = Properties.Settings.Default.AddinPath + @"\EULA.TXT";
            myProcess.StartInfo.UseShellExecute = true;
            myProcess.StartInfo.RedirectStandardOutput = false;
            myProcess.Start();
            myProcess.Dispose();
            return Result.Succeeded;
        }
    }

#if DEBUG
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class test_Entitlement : IExternalCommand
    {
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Properties.Settings.Default.EntCheck = System.DateTime.Today.Subtract(TimeSpan.FromDays(7));
            Properties.Settings.Default.Save();
            if (ProjectSweeper.UserIsEntitled(commandData))
                TaskDialog.Show("Entitlement Test", "You are entitled.");
            else
                TaskDialog.Show("Entitlement Test", "You are NOT entitled.");
            return Result.Succeeded;
        }
    }
#endif

    public class AlwaysAvailableCheck : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication appdata, CategorySet selectCatagories)
        {
            return true;
        }
    }
}