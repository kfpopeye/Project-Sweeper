using System;
using System.IO;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using log4net;
using log4net.Config;

namespace PKHL.ProjectSweeper
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Journaling(Autodesk.Revit.Attributes.JournalingMode.NoCommandData)]
    public partial class ProjectSweeper : IExternalApplication
    {
        internal static ContextualHelp _contextualHelp = null;
        private static ILog _log = null;

        private void CreateRibbonPanel(UIControlledApplication application)
        {
            // This method is used to create the ribbon panel.
            // which contains the controlled application.

            string addinPath = Properties.Settings.Default.AddinPath;
            string dllPath = addinPath + @"\Project Sweeper.dll";
            RibbonPanel pkhlPanel = application.CreateRibbonPanel(Constants.GROUP_NAME);

            PushButton lscButton = pkhlPanel.AddItem(
                new PushButtonData("lscButton", 
                LocalizationProvider.GetLocalizedValue<string>("LSC_Title_Ribbon"), 
                dllPath, 
                "PKHL.ProjectSweeper.LineStyleCleaner.main_command")) as PushButton;
            lscButton.Image = NewBitmapImage("lsc16x16.png");
            lscButton.LargeImage = NewBitmapImage("lsc.png");
            lscButton.ToolTip = LocalizationProvider.GetLocalizedValue<string>("LSC_IconTip");
            lscButton.Visible = true;
            lscButton.AvailabilityClassName = "PKHL.ProjectSweeper.LineStyleCleaner.command_AvailableCheck";
            lscButton.SetContextualHelp(_contextualHelp);

            PushButton lpcButton = pkhlPanel.AddItem(
                new PushButtonData("lpcButton", 
                LocalizationProvider.GetLocalizedValue<string>("LPC_Title_Ribbon"), 
                dllPath,
                "PKHL.ProjectSweeper.LinePatternCleaner.MainCommand")) as PushButton;
            lpcButton.Image = NewBitmapImage("lpc16x16.png");
            lpcButton.LargeImage = NewBitmapImage("lpc.png");
            lpcButton.ToolTip = LocalizationProvider.GetLocalizedValue<string>("LPC_IconTip");
            lpcButton.Visible = true;
            lpcButton.AvailabilityClassName = "PKHL.ProjectSweeper.LinePatternCleaner.command_AvailableCheck";
            lpcButton.SetContextualHelp(_contextualHelp);

            PushButton tscButton = pkhlPanel.AddItem(
                new PushButtonData("tscButton", 
                LocalizationProvider.GetLocalizedValue<string>("TSC_Title_Ribbon"), 
                dllPath, 
                "PKHL.ProjectSweeper.TextStyleCleaner.main_command")) as PushButton;
            tscButton.Image = NewBitmapImage("tsc16x16.png");
            tscButton.LargeImage = NewBitmapImage("tsc.png");
            tscButton.ToolTip = LocalizationProvider.GetLocalizedValue<string>("TSC_IconTip"); //Cleans redundant text styles from a project file.
            tscButton.Visible = true;
            tscButton.AvailabilityClassName = "PKHL.ProjectSweeper.TextStyleCleaner.command_AvailableCheck";
            tscButton.SetContextualHelp(_contextualHelp);

            PushButton frtcButton = pkhlPanel.AddItem(
                new PushButtonData("frtcButton", 
                LocalizationProvider.GetLocalizedValue<string>("FRTC_Title_Ribbon"), 
                dllPath, 
                "PKHL.ProjectSweeper.FillRegionTypeCleaner.main_command")) as PushButton;
            frtcButton.Image = NewBitmapImage("frt16x16.png");
            frtcButton.LargeImage = NewBitmapImage("frt.png");
            frtcButton.ToolTip = LocalizationProvider.GetLocalizedValue<string>("FRTC_IconTip");
            frtcButton.Visible = true;
            frtcButton.AvailabilityClassName = "PKHL.ProjectSweeper.FillRegionTypeCleaner.command_AvailableCheck";
            frtcButton.SetContextualHelp(_contextualHelp);

            PushButton fpcButton = pkhlPanel.AddItem(
                new PushButtonData("fpcButton", 
                LocalizationProvider.GetLocalizedValue<string>("FPC_Title_Ribbon"), 
                dllPath,
                "PKHL.ProjectSweeper.FillPatternCleaner.MainCommand")) as PushButton;
            fpcButton.Image = NewBitmapImage("fpc16x16.png");
            fpcButton.LargeImage = NewBitmapImage("fpc.png");
            fpcButton.ToolTip = LocalizationProvider.GetLocalizedValue<string>("FPC_IconTip"); //Cleans redundant fill patterns from a project file.
            fpcButton.Visible = true;
            fpcButton.AvailabilityClassName = "PKHL.ProjectSweeper.FillPatternCleaner.CommandAvailableCheck";
            fpcButton.SetContextualHelp(_contextualHelp);

            // Create a slide out
            pkhlPanel.AddSlideOut();

            PushButton aboutButton = pkhlPanel.AddItem(
                new PushButtonData("aboutButton",
                LocalizationProvider.GetLocalizedValue<string>("ABOUT_Title"), 
                dllPath,
                "PKHL.ProjectSweeper.AboutBoxCommand")) as PushButton;
            aboutButton.Image = NewBitmapImage("about16x16.png");
            aboutButton.LargeImage = NewBitmapImage("about.png");
            aboutButton.ToolTip = LocalizationProvider.GetLocalizedValue<string>("ABOUT_IconTip");
            aboutButton.AvailabilityClassName = "PKHL.ProjectSweeper.AlwaysAvailableCheck";

            PushButton helpButton = pkhlPanel.AddItem(
                new PushButtonData("helpButton",
                LocalizationProvider.GetLocalizedValue<string>("HelpButton"), 
                dllPath,
                "PKHL.ProjectSweeper.ApplicationHelp")) as PushButton;
            helpButton.Image = NewBitmapImage("help16x16.png");
            helpButton.LargeImage = NewBitmapImage("help.png");
            helpButton.ToolTip = LocalizationProvider.GetLocalizedValue<string>("HELP_IconTip"); 
            helpButton.AvailabilityClassName = "PKHL.ProjectSweeper.AlwaysAvailableCheck";

#if MULTILICENSE
            PushButton eulaButton = pkhlPanel.AddItem(
                new PushButtonData("eulaButton",
                LocalizationProvider.GetLocalizedValue<string>("EULA_Title"), 
                dllPath, 
                "PKHL.ProjectSweeper.ApplicationEula")) as PushButton;
            eulaButton.Image = NewBitmapImage("about16x16.png");
            eulaButton.LargeImage = NewBitmapImage("about.png");
            eulaButton.ToolTip = LocalizationProvider.GetLocalizedValue<string>("EULA_IconTip");
            eulaButton.AvailabilityClassName = "PKHL.ProjectSweeper.AlwaysAvailableCheck";
#endif

#if DEBUG
            PushButton test_Button = pkhlPanel.AddItem(new PushButtonData("TestButton", "Test Entitlement", dllPath, "PKHL.ProjectSweeper.test_Entitlement")) as PushButton;
            test_Button.Image = NewBitmapImage("help16x16.png");
            test_Button.LargeImage = NewBitmapImage("help.png");
            test_Button.ToolTip = "Test the entitlement system. Debug mode only.";
            test_Button.Visible = true;
            test_Button.AvailabilityClassName = "PKHL.ProjectSweeper.AlwaysAvailableCheck";
#endif
        }

        /// <summary>
        /// Load a new icon bitmap from embedded resources.
        /// For the BitmapImage, make sure you reference WindowsBase and PresentationCore, and import the System.Windows.Media.Imaging namespace.
        /// Drag images into Resources folder in solution explorer and set build action to "Embedded Resource"
        /// </summary>
        private BitmapImage NewBitmapImage(string imageName)
        {
            Stream s = this.GetType().Assembly.GetManifestResourceStream("PKHL.ProjectSweeper.Resources.RibbonImages." + imageName);
            BitmapImage img = new BitmapImage();

            img.BeginInit();
            img.StreamSource = s;
            img.EndInit();

            return img;
        }

        #region Event Handlers
        public Autodesk.Revit.UI.Result OnShutdown(UIControlledApplication application)
        {
            return Autodesk.Revit.UI.Result.Succeeded;
        }

        public Autodesk.Revit.UI.Result OnStartup(UIControlledApplication application)
        {
            string s = this.GetType().Assembly.Location;
            Properties.Settings.Default.AddinPath = System.IO.Path.GetDirectoryName(s);
            Properties.Settings.Default.Save();
            System.Diagnostics.Debug.WriteLine("Addin path = " + Properties.Settings.Default.AddinPath);

            string logConfig = Path.Combine(Properties.Settings.Default.AddinPath, "projectsweeper.log4net.config");
            FileInfo configStream = new FileInfo(logConfig);
            XmlConfigurator.Configure(configStream);
            _log = LogManager.GetLogger(typeof(ProjectSweeper));
            _log.InfoFormat("Running version: {0}", this.GetType().Assembly.GetName().Version.ToString());
            _log.InfoFormat("Found myself at: {0}", Properties.Settings.Default.AddinPath);

            _contextualHelp = new ContextualHelp(
                ContextualHelpType.ChmFile,
                Path.Combine(
                    Directory.GetParent(Properties.Settings.Default.AddinPath).ToString(), //contents directory
                    LocalizationProvider.GetLocalizedValue<string>("HelpFile")));

            CreateRibbonPanel(application);

            try
            {
                pkhCommon.StringHelper.RemoveNewLines("This causes the common library to load so the FRTC, LSC and FPC commands don't throw a file not found error.");
            }
            catch(Exception)
            {                
            }

            return Autodesk.Revit.UI.Result.Succeeded;
        }
        #endregion
    }
}