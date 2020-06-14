using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.IO;
using System.Data;

namespace PKHL.ProjectSweeper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class TemplateWindow : Window
    {
        /// <summary>
        /// A structure to hold style data. It is used to pass information between the command main windows and this one.
        /// </summary>
        public struct StyleDataContainer
        {
            public string oldStyle;
            public string newStyle;
            public bool deleteStyle;
        }
        public List<StyleDataContainer> sirDataList = null;
        public bool ignoreCase = true;

        private CollectionViewSource appView = null;
        private PSTemplates templateDataSet = null;
        private string xmlFile = null;
        private bool saveMode = true;
        private string appName = null;

        public TemplateWindow(List<StyleDataContainer> sdc, string appname)
        {
            InitializeComponent();

            appName = appname;
            templateDataSet = this.FindResource("TemplateClass") as PSTemplates;
            appView = this.FindResource("appViewSource") as CollectionViewSource;

            if (sdc != null)
            {
                sirDataList = sdc;
                saveMode = true;
            }
            else
                saveMode = false;

            if (!saveMode)
            {
                commentTextBox.IsEnabled = false;
                caseCheckBox.IsEnabled = false;
                okButton.Content = LocalizationProvider.GetLocalizedValue<string>("LoadButton"); //Load
            }

            switch (appName)
            {
                case "FPC":
                    this.Title = LocalizationProvider.GetLocalizedValue<string>("FPC_Title_MainWindow");
                    break;
                case "FRTC":
                    this.Title = LocalizationProvider.GetLocalizedValue<string>("FRTC_Title_MainWindow");
                    break;
                case "TSC":
                    this.Title = LocalizationProvider.GetLocalizedValue<string>("TSC_Title_MainWindow");
                    break;
                case "LPC":
                    this.Title = LocalizationProvider.GetLocalizedValue<string>("LPC_Title_MainWindow");
                    break;
                case "LSC":
                    this.Title = LocalizationProvider.GetLocalizedValue<string>("LSC_Title_MainWindow");
                    break;
                default:
                    this.Title = "Unknown application";
                    break;
            };
            this.Title += " " +
                        LocalizationProvider.GetLocalizedValue<string>("TEMPLATEWIN_Title_Templates");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            xmlFile = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "pkhlineworks\\ProjectSweeper\\TemplateFile.xml");
            if (!File.Exists(xmlFile))
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(xmlFile));
                PSTemplates.AppRow ar = templateDataSet.App.NewAppRow();
                ar.AName = "FPC";
                templateDataSet.App.AddAppRow(ar);
                ar = templateDataSet.App.NewAppRow();
                ar.AName = "FRTC";
                templateDataSet.App.AddAppRow(ar);
                ar = templateDataSet.App.NewAppRow();
                ar.AName = "TSC";
                templateDataSet.App.AddAppRow(ar);
                ar = templateDataSet.App.NewAppRow();
                ar.AName = "LPC";
                templateDataSet.App.AddAppRow(ar);
                ar = templateDataSet.App.NewAppRow();
                ar.AName = "LSC";
                templateDataSet.App.AddAppRow(ar);
                templateDataSet.AcceptChanges();
                templateDataSet.WriteXml(xmlFile);
            }
            else
            {
                templateDataSet.Clear();
                templateDataSet.ReadXml(xmlFile);
                templateDataSet.AcceptChanges();
            }

            if (saveMode)
            {
                templateDataSet.BeginInit();
                //create new saved template
                PSTemplates.SavedtemplateRow str = templateDataSet.Savedtemplate.NewSavedtemplateRow();
                str.TName = LocalizationProvider.GetLocalizedValue<string>("TEMPLATEWIN_NewTemplatename"); //<Create New Template
                str.Comment = LocalizationProvider.GetLocalizedValue<string>("TEMPLATEWIN_NewTemplateComment"); //Select this template to create a new saved template. You will be prompted to name it. Or select an existing template to overwrite it. Replace this comment with something meaningful to you.
                str.IgnoreCase = false;
                foreach (PSTemplates.AppRow ar in templateDataSet.App)
                {
                    if (ar.AName == appName)
                    {
                        str.App_Id = ar.App_Id;
                        break;
                    }
                }
                templateDataSet.Savedtemplate.AddSavedtemplateRow(str);

                //add saved items to saved template table
                PSTemplates.StyleItemsRow sir = templateDataSet.StyleItems.NewStyleItemsRow();
                sir.Savedtemplate_Id = str.Savedtemplate_Id;
                templateDataSet.StyleItems.AddStyleItemsRow(sir);

                //add save data to saved items table
                foreach (StyleDataContainer sdr in sirDataList)
                {
                    PSTemplates.StyleDataRow newSdr = templateDataSet.StyleData.NewStyleDataRow();
                    newSdr.OldStyle = sdr.oldStyle;
                    newSdr.NewStyle = sdr.newStyle;
                    newSdr.DeleteStyle = sdr.deleteStyle;
                    newSdr.StyleItems_Id = sir.StyleItems_Id;
                    templateDataSet.StyleData.AddStyleDataRow(newSdr);
                }
                templateDataSet.AcceptChanges();
                templateDataSet.EndInit();
            }

            //set listbox to visible app
            appView.View.MoveCurrentToFirst();
            do
            {
                DataRowView drv = appView.View.CurrentItem as DataRowView;
                System.Diagnostics.Debug.WriteLine(drv.Row.ItemArray[0].ToString());
                if (drv.Row.ItemArray[0].ToString() != appName)
                    appView.View.MoveCurrentToNext();
                else
                    break;
            }
            while (!appView.View.IsCurrentAfterLast);
            TemplateList.SelectedIndex = 0;
#if DEBUG
            this.Title += " - DEBUG MODE";
#endif
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            DataRowView drv = TemplateList.SelectedItem as DataRowView;
            PSTemplates.SavedtemplateRow str = drv.Row as PSTemplates.SavedtemplateRow;

            if (saveMode)
            {
                if (str.TName == LocalizationProvider.GetLocalizedValue<string>("TEMPLATEWIN_NewTemplatename"))
                {
                    //DIA007
                    Input_Box input_Box = new Input_Box(Constants.GROUP_NAME); //Project Sweeper
                    input_Box.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("DIA007_MainInstr"); //Template name required.
                    input_Box.MainContent = LocalizationProvider.GetLocalizedValue<string>("DIA007_MainCont"); //Please choose a name for the new template.
                    input_Box.ShowDialog();
                    if (input_Box.DialogResult == true)
                        str.TName = pkhCommon.StringHelper.SafeForXML(input_Box.UserInput);
                    else
                        return;
                }
                else
                {
                    //DIA008
                    MessageBoxResult res = MessageBox.Show(
                        LocalizationProvider.GetLocalizedValue<string>("DIA008_MsgBxTxt"), //Are you sure you want to overwrite this template.
                        LocalizationProvider.GetLocalizedValue<string>("DIA008_MsgBxCapt"), //Confirm overwrite
                        MessageBoxButton.YesNo);
                    if (res == MessageBoxResult.Yes)
                    {
                        //overwrite an existing template
                        string Tname = str.TName;
                        PSTemplates.SavedtemplateRow newTemplate = null;
                        foreach(DataRowView d in TemplateList.Items)
                        {
                            PSTemplates.SavedtemplateRow s = d.Row as PSTemplates.SavedtemplateRow;
                            if (s.TName == LocalizationProvider.GetLocalizedValue<string>("TEMPLATEWIN_NewTemplatename"))
                            {
                                newTemplate = s;
                                break;
                            }
                        }
                        templateDataSet.BeginInit();
                        newTemplate.Comment = str.Comment;
                        newTemplate.IgnoreCase = str.IgnoreCase;
                        drv.Delete();
                        newTemplate.TName = Tname;
                        templateDataSet.EndInit();
                    }
                    else
                        return;
                }
                templateDataSet.AcceptChanges();
                templateDataSet.WriteXml(xmlFile);
            }
            else
            {
                sirDataList = new List<StyleDataContainer>();
                PSTemplates.StyleItemsRow s = str.GetStyleItemsRows().First();
                foreach(PSTemplates.StyleDataRow r in s.GetStyleDataRows())
                {
                    StyleDataContainer c = new StyleDataContainer();
                    c.deleteStyle = r.DeleteStyle;
                    c.oldStyle = r.OldStyle;
                    c.newStyle = r.NewStyle;
                    sirDataList.Add(c);
                }
                ignoreCase = str.IgnoreCase;
            }

            this.DialogResult = true;
            this.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void deleteTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TemplateList.IsLoaded)
                return;
            Button butt = sender as Button;
            if (butt == null)
                return;

            System.Diagnostics.Debug.Print("Delete button pressed.");

            Autodesk.Revit.UI.TaskDialogResult res;
            //DIA009
            using (Autodesk.Revit.UI.TaskDialog tdr = new Autodesk.Revit.UI.TaskDialog(LocalizationProvider.GetLocalizedValue<string>("DIA009_Title"))) //confirm delete
            {
                tdr.CommonButtons = Autodesk.Revit.UI.TaskDialogCommonButtons.Yes | Autodesk.Revit.UI.TaskDialogCommonButtons.No;
                tdr.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("DIA009_MainInstr"); // You have selected to delete a saved template. This cannot be undone. Are you sure you want to continue?
                res = tdr.Show();
            }

            if (res == Autodesk.Revit.UI.TaskDialogResult.Yes)
            {
                ListBoxItem lbi = pkhCommon.WPF.Helpers.GetVisualParent<ListBoxItem>(butt);
                DataRowView drv = lbi.Content as DataRowView;
                PSTemplates.SavedtemplateRow str = drv.Row as PSTemplates.SavedtemplateRow;
                str.Delete();
                templateDataSet.AcceptChanges();
                templateDataSet.WriteXml(xmlFile);
            }
        }

        private void TemplateList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TemplateList == null || !TemplateList.IsLoaded)
                return;

            if (TemplateList.SelectedItem != null)
                okButton.IsEnabled = true;
            else
                okButton.IsEnabled = false;
        }
    }

    /// <summary>
    /// Returns Visible or Hidden based on the templates name
    /// </summary>
    [ValueConversion(typeof(string), typeof(Visibility))]
    public class TNameToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type type, object parameter, System.Globalization.CultureInfo culture)
        {
            string theValue = (string)value;
            if (theValue != null && theValue != LocalizationProvider.GetLocalizedValue<string>("TEMPLATEWIN_NewTemplatename"))
                return Visibility.Visible;

            return Visibility.Hidden;
        }

        public object ConvertBack(object o, Type type, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
