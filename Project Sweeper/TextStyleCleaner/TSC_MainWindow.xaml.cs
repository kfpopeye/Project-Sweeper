using System;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using Rdb = Autodesk.Revit.DB;

namespace PKHL.ProjectSweeper.TextStyleCleaner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public System.Collections.ObjectModel.ObservableCollection<TextStyleDefinition> TheCollection { get { return data; } }
        private System.Collections.ObjectModel.ObservableCollection<TextStyleDefinition> data = null;
        private TextStyleDefinition deleteTextStyle = null;
        private Rdb.Document TheDoc = null;
        private IList<TextStyleDefinition> selectedSStyles = null;

        private TextStyleDefinition _defaultTextStyle;
        public TextStyleDefinition defaultTextStyle
        {
            get
            {
                return _defaultTextStyle;
            }
            set
            {
                _defaultTextStyle = value;
                MI_DefaultName.Text = _defaultTextStyle.StyleName;
            }
        }

        public MainWindow(System.Collections.ObjectModel.ObservableCollection<TextStyleDefinition> _data, IList<TextStyleDefinition> _selectedSStyles, ref Rdb.Document _theDoc)
        {
            InitializeComponent();
            data = _data;
            deleteTextStyle = ((TextStyleDefinition)theLeftList.Resources["deleteItem"]);   //ItsId = -1
            defaultTextStyle = deleteTextStyle;
            TheDoc = _theDoc;
            selectedSStyles = _selectedSStyles;
            if (selectedSStyles != null)
                FilterBox.IsEnabled = false;
        }

        /// <summary>
        /// Shows only user selected line styles, if any
        /// </summary>
        void theDataView_Filter(object sender, FilterEventArgs e)
        {
            if (selectedSStyles == null)
            {
                e.Accepted = true;
                return;
            }

            TextStyleDefinition l_item = e.Item as TextStyleDefinition;
            foreach (TextStyleDefinition lsd in selectedSStyles)
            {
                if (l_item.Equals(lsd))
                {
                    e.Accepted = true;
                    return;
                }
            }

            e.Accepted = false;
        }

        /// <summary>
        /// Filters out styles that are to be deleted
        /// </summary>
        void ComboBoxDataView_Filter(object sender, FilterEventArgs e)
        {
            if (!this.IsLoaded)
            {
                e.Accepted = true;
                return;
            }

            TextStyleDefinition tsd = e.Item as TextStyleDefinition;
            if (tsd != null)
            {
                if (tsd.StyleToBeDeleted || tsd.DeleteElements)
                    e.Accepted = false;
                else
                    e.Accepted = true;
            }
            else
                e.Accepted = true;
        }

        void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        void DeleteAllButton_Click(object sender, RoutedEventArgs e)
        {
            theLeftList.BeginInit();
            foreach (TextStyleDefinition tsd in data)
            {
                if (theLeftList.Items.Contains(tsd))
                {
                    if (tsd.IsDeleteable && !tsd.StyleToBeDeleted)
                    {
                        tsd.StyleToBeDeleted = true;
                        if (!tsd.StyleToBeConverted)
                            tsd.NewStyle = defaultTextStyle;
                    }
                }
            }
            theLeftList.EndInit();
            ((CollectionViewSource)this.Resources["theDataView"]).View.Refresh();
            ((CollectionViewSource)theLeftList.Resources["theComboBoxDataView"]).View.Refresh();
        }

        void PurgeButton_Click(object sender, RoutedEventArgs e)
        {
            theLeftList.BeginInit();
            foreach (TextStyleDefinition tsd in data)
            {
                if (theLeftList.Items.Contains(tsd))
                {
                    if (tsd.NumberOfUses == 0 && tsd.IsDeleteable)
                    {
                        tsd.StyleToBeDeleted = true;
                        if (!tsd.StyleToBeConverted)
                            tsd.NewStyle = defaultTextStyle;
                    }
                }
            }
            theLeftList.EndInit();
            ((CollectionViewSource)this.Resources["theDataView"]).View.Refresh();
            ((CollectionViewSource)theLeftList.Resources["theComboBoxDataView"]).View.Refresh();
        }

        void DeleteCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox cxb = sender as CheckBox;
            if (cxb != null && cxb.IsLoaded)
            {
                ListViewItem lvi = pkhCommon.WPF.Helpers.GetVisualParent<ListViewItem>(cxb);
                TextStyleDefinition lvi_tsd = lvi.Content as TextStyleDefinition;
                foreach (TextStyleDefinition tsd in data)   //check if any styles are set to be changed to this one
                {
                    if (tsd.NewStyle == lvi_tsd)
                        tsd.NewStyle = defaultTextStyle;
                }
                if (!lvi_tsd.StyleToBeConverted)
                    lvi_tsd.NewStyle = defaultTextStyle;
            }
            ((CollectionViewSource)this.Resources["theDataView"]).View.Refresh();
            ((CollectionViewSource)theLeftList.Resources["theComboBoxDataView"]).View.Refresh();
        }

        /// <summary>
        /// Uses Revit to handle localization of paramter names
        /// </summary>
        private void TextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            TextBlock tb = sender as TextBlock;
            switch (tb.Text)
            {
                case "LINE_COLOR":
                    tb.Text = Rdb.LabelUtils.GetLabelFor(Rdb.BuiltInParameter.LINE_COLOR);
                    break;
                case "LINE_PEN":
                    tb.Text = Rdb.LabelUtils.GetLabelFor(Rdb.BuiltInParameter.LINE_PEN);
                    break;
                case "TEXT_BACKGROUND":
                    tb.Text = Rdb.LabelUtils.GetLabelFor(Rdb.BuiltInParameter.TEXT_BACKGROUND);
                    break;
                case "TEXT_BOX_VISIBILITY":
                    tb.Text = Rdb.LabelUtils.GetLabelFor(Rdb.BuiltInParameter.TEXT_BOX_VISIBILITY);
                    break;
                case "LEADER_OFFSET_SHEET":
                    tb.Text = Rdb.LabelUtils.GetLabelFor(Rdb.BuiltInParameter.LEADER_OFFSET_SHEET);
                    break;
                case "LEADER_ARROWHEAD":
                    tb.Text = Rdb.LabelUtils.GetLabelFor(Rdb.BuiltInParameter.LEADER_ARROWHEAD);
                    break;
                case "TEXT_FONT":
                    tb.Text = Rdb.LabelUtils.GetLabelFor(Rdb.BuiltInParameter.TEXT_FONT);
                    break;
                case "TEXT_SIZE":
                    tb.Text = Rdb.LabelUtils.GetLabelFor(Rdb.BuiltInParameter.TEXT_SIZE);
                    break;
                case "TEXT_TAB_SIZE":
                    tb.Text = Rdb.LabelUtils.GetLabelFor(Rdb.BuiltInParameter.TEXT_TAB_SIZE);
                    break;
                case "TEXT_WIDTH_SCALE":
                    tb.Text = Rdb.LabelUtils.GetLabelFor(Rdb.BuiltInParameter.TEXT_WIDTH_SCALE);
                    break;
                case "TEXT_STYLE_UNDERLINE":
                    tb.Inlines.Clear();
                    tb.Inlines.Add(new Underline(new Run(Rdb.LabelUtils.GetLabelFor(Rdb.BuiltInParameter.TEXT_STYLE_UNDERLINE))));
                    break;
                case "TEXT_STYLE_ITALIC":
                    tb.Text = Rdb.LabelUtils.GetLabelFor(Rdb.BuiltInParameter.TEXT_STYLE_ITALIC);
                    break;
                case "TEXT_STYLE_BOLD":
                    tb.Text = Rdb.LabelUtils.GetLabelFor(Rdb.BuiltInParameter.TEXT_STYLE_BOLD);
                    break;
                case "SCHEDULE_FORMAT_PARAM":
                    tb.Text = Rdb.LabelUtils.GetLabelFor(Rdb.BuiltInParameter.SCHEDULE_FORMAT_PARAM);
                    //not the proper parameter but hopefully works the same in all languages
                    break;
                default:
                    tb.Text = "ERROR****";
                    break;
            }
        }

        void MI_setDefaultStyle_Click(object sender, RoutedEventArgs e)
        {
            TextStyleDefinition lvi_tsd = theLeftList.SelectedItem as TextStyleDefinition;
            if (defaultTextStyle != null)
                defaultTextStyle.IsDeleteable = true;
            lvi_tsd.StyleToBeDeleted = false;
            lvi_tsd.IsDeleteable = false;
            defaultTextStyle = lvi_tsd;
            ((CollectionViewSource)this.Resources["theDataView"]).View.Refresh();
            ((CollectionViewSource)theLeftList.Resources["theComboBoxDataView"]).View.Refresh();
        }

        void MI_Reset_Click(object sender, RoutedEventArgs e)
        {
            if (defaultTextStyle != null)
                defaultTextStyle.IsDeleteable = true;
            defaultTextStyle = deleteTextStyle;
            MI_DefaultName.Text = LocalizationProvider.GetLocalizedValue<string>("TSC_DeleteNotes");
            ((CollectionViewSource)this.Resources["theDataView"]).View.Refresh();
            ((CollectionViewSource)theLeftList.Resources["theComboBoxDataView"]).View.Refresh();
        }

        private void MI_resetStyle_Click(object sender, RoutedEventArgs e)
        {
            TextStyleDefinition tsd = theLeftList.SelectedItem as TextStyleDefinition;
            if (tsd != null)
            {
                tsd.StyleToBeDeleted = false;
                tsd.NewStyle = null;
            }
            ((CollectionViewSource)this.Resources["theDataView"]).View.Refresh();
            ((CollectionViewSource)theLeftList.Resources["theComboBoxDataView"]).View.Refresh();
        }

        private void listviews_button_Click(object sender, RoutedEventArgs e)
        {
            TextStyleDefinition lvi_tsd = theLeftList.SelectedItem as TextStyleDefinition;
            if (lvi_tsd != null)
            {
                if (lvi_tsd.NumberOfUses > 0)
                {
                    if (lvi_tsd.OwnerViews != null)
                    {
                        ListViewsWindow lvw = new ListViewsWindow(lvi_tsd, TheDoc.Application);
                        lvw.Owner = this;
                        lvw.ShowDialog();
                    }
                    else
                    {
                        ListUsersWindow luw = new ListUsersWindow(lvi_tsd);
                        luw.Owner = this;
                        luw.ShowDialog();
                    }
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
            Title = Title + " - DEBUG BUILD";
#endif
        }

        private void changeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!theLeftList.IsLoaded)
                return;
            ComboBox cb = sender as ComboBox;
            if (cb == null || !cb.IsDropDownOpen || cb.SelectedItem == null)
                return;
            TextStyleDefinition cb_tsd = cb.SelectedItem as TextStyleDefinition;
            ListViewItem lvi = pkhCommon.WPF.Helpers.GetVisualParent<ListViewItem>(cb);
            TextStyleDefinition lvi_tsd = lvi.Content as TextStyleDefinition;

            System.Diagnostics.Debug.Print("ComboBox_SelectionChanged doing something.");
            if (cb_tsd.NewStyle != null && cb_tsd.NewStyle.Equals(lvi_tsd)) //changing to a style set to change to this style
            {
                // Revit Taskdialog DIA001
                Autodesk.Revit.UI.TaskDialog td = new Autodesk.Revit.UI.TaskDialog(LocalizationProvider.GetLocalizedValue<string>("TXT_Warning"));
                td.MainIcon = Autodesk.Revit.UI.TaskDialogIcon.TaskDialogIconWarning;
                td.MainInstruction = string.Format(LocalizationProvider.GetLocalizedValue<string>("DIA001_MainInst"), cb_tsd.StyleName);
                td.MainContent = string.Format(LocalizationProvider.GetLocalizedValue<string>("DIA001_MainCont"), cb_tsd.StyleName, lvi_tsd.StyleName);
                td.Show();
                cb.SelectedValue = lvi_tsd.NewStyle;
                return;
            }

            if (lvi_tsd.NewStyle == null || !lvi_tsd.NewStyle.Equals(cb_tsd))
                lvi_tsd.NewStyle = cb_tsd;

            if (cb_tsd.ItsId != deleteTextStyle.ItsId) //deal with "Delete Lines" scenarios
                return;
            if (defaultTextStyle.Equals(lvi_tsd))   //is default style being set to delete lines
            {
                lvi_tsd.NewStyle = null;
                // Revit Taskdialog DIA002
                Autodesk.Revit.UI.TaskDialog td = new Autodesk.Revit.UI.TaskDialog(LocalizationProvider.GetLocalizedValue<string>("TXT_Warning"));
                td.MainIcon = Autodesk.Revit.UI.TaskDialogIcon.TaskDialogIconWarning;
                td.MainInstruction = string.Format(LocalizationProvider.GetLocalizedValue<string>("DIA002_MainInst"), lvi_tsd.StyleName);
                td.MainContent = LocalizationProvider.GetLocalizedValue<string>("DIA002_MainCont");
                td.Show();
                return;
            }

            theLeftList.BeginInit();
            bool showWarning = true;
            System.Diagnostics.Debug.Print("Checking styles for dependencies on: {0}", lvi_tsd.StyleName);
            foreach (TextStyleDefinition tsd in data)   //are any styles being set to a style whos' lines will be deleted
            {
                System.Diagnostics.Debug.Write("\t" + tsd.StyleName + " : ");
                if (tsd.StyleToBeConverted && tsd.NewStyle.Equals(lvi_tsd))
                {
                    System.Diagnostics.Debug.WriteLine("Yes");
                    tsd.NewStyle = null;
                    tsd.StyleToBeDeleted = false;
                    if (showWarning)
                    {
                        Autodesk.Revit.UI.TaskDialog td = new Autodesk.Revit.UI.TaskDialog(LocalizationProvider.GetLocalizedValue<string>("TXT_Warning"));
                        td.MainIcon = Autodesk.Revit.UI.TaskDialogIcon.TaskDialogIconWarning;
                        td.MainContent = string.Format(LocalizationProvider.GetLocalizedValue<string>("DIA003_MainCont"), lvi_tsd.StyleName);
                        td.Show();
                        showWarning = false;
                    }
                }
                else
                    System.Diagnostics.Debug.WriteLine("No");
            }
            theLeftList.EndInit();
            ((CollectionViewSource)theLeftList.Resources["theComboBoxDataView"]).View.Refresh();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ProjectSweeper._contextualHelp.HelpTopicUrl = Constants.TSC_HELP;
            ProjectSweeper._contextualHelp.Launch();
        }

        private void ResetAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (TextStyleDefinition lvi_def in TheCollection)
            {
                if (lvi_def != null)
                {
                    if (lvi_def.IsDeleteable)
                        lvi_def.StyleToBeDeleted = false;
                    lvi_def.NewStyle = null;
                }
            }
            ((CollectionViewSource)this.Resources["theDataView"]).View.Refresh();
            ((CollectionViewSource)theLeftList.Resources["theComboBoxDataView"]).View.Refresh();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            System.Collections.Generic.List<TemplateWindow.StyleDataContainer> styleData = new List<TemplateWindow.StyleDataContainer>();
            foreach (TextStyleDefinition frtd in TheCollection)
            {
                if (frtd.StyleToBeConverted || frtd.StyleToBeDeleted)
                {
                    System.Diagnostics.Debug.WriteLine(frtd.StyleName);
                    TemplateWindow.StyleDataContainer sdr = new TemplateWindow.StyleDataContainer();
                    sdr.oldStyle = frtd.StyleName;
                    sdr.newStyle = frtd.NewStyle.StyleName;
                    sdr.deleteStyle = frtd.StyleToBeDeleted;
                    styleData.Add(sdr);
                }
            }

            TemplateWindow tw = new TemplateWindow(styleData, "TSC");
            tw.Owner = this;
            tw.ShowDialog();
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            int appliedStyles = 0;
            System.Text.StringBuilder results = new System.Text.StringBuilder();
            System.Collections.Generic.List<TemplateWindow.StyleDataContainer> styleData = null;

            TemplateWindow tw = new TemplateWindow(null, "TSC");
            tw.Owner = this;
            bool? res = tw.ShowDialog();

            if (res != null && (bool)res)
            {
                if ((styleData = tw.sirDataList) == null)
                {
                    System.Diagnostics.Debug.WriteLine("Style data was null");
                    return;
                }
                StringComparison compareType = StringComparison.CurrentCulture;
                if (tw.ignoreCase)
                    compareType = StringComparison.CurrentCultureIgnoreCase;

                foreach (TemplateWindow.StyleDataContainer sdc in styleData)
                {
                    TextStyleDefinition srcFrtd = null;
                    foreach (TextStyleDefinition frtd in TheCollection)
                    {

                        if (frtd.StyleName.Equals(sdc.oldStyle, compareType))
                        {
                            srcFrtd = frtd;
                            break;
                        }
                    }
                    if (srcFrtd == null)
                    {
                        results.AppendFormat(
                            LocalizationProvider.GetLocalizedValue<string>("FRTC_LOAD_RSLT_NoType"),  //Could not find type "{0}". Skipping it.
                            sdc.oldStyle);
                        results.AppendLine();
                        continue;
                    }
                    TextStyleDefinition newFrtd = null;
                    if (sdc.newStyle == deleteTextStyle.StyleName)
                    {
                        newFrtd = deleteTextStyle;
                    }
                    else
                    {
                        foreach (TextStyleDefinition frtd in TheCollection)
                        {
                            if (frtd.StyleName.Equals(sdc.newStyle, compareType))
                            {
                                newFrtd = frtd;
                                break;
                            }
                        }
                    }
                    if (newFrtd == null)
                    {
                        results.AppendFormat(
                            LocalizationProvider.GetLocalizedValue<string>("FRTC_LOAD_RSLT_NoType2Conv"),  //Could not find type "{0}" to convert to. Skipping it.
                            sdc.newStyle);
                        results.AppendLine();
                        continue;
                    }

                    if (newFrtd.NewStyle != null && newFrtd.NewStyle.Equals(srcFrtd)) //changing to a style set to change to this style
                    {
                        results.AppendFormat(
                            LocalizationProvider.GetLocalizedValue<string>("FRTC_LOAD_RSLT_AlreadyConv"), //Type "{0}" is already being converted to "{1}". Skipping it.
                            sdc.newStyle,
                            sdc.oldStyle);
                        continue;
                    }

                    srcFrtd.NewStyle = newFrtd;
                    ++appliedStyles;
                    if (srcFrtd.IsDeleteable)
                    {
                        srcFrtd.StyleToBeDeleted = sdc.deleteStyle;
                        results.AppendFormat(
                            LocalizationProvider.GetLocalizedValue<string>("FRTC_LOAD_RSLT_ChangeAndDel"),  //Set type "{0}" to be changed to "{1}" and then delete the type.
                            sdc.oldStyle,
                            sdc.newStyle);
                    }
                    else
                        results.AppendFormat(
                            LocalizationProvider.GetLocalizedValue<string>("FRTC_LOAD_RSLT_Change"),  //Set type "{0}" to be changed to "{1}".
                            sdc.oldStyle,
                            sdc.newStyle);
                    results.AppendLine();
                }
                results.AppendLine();
                results.AppendFormat(
                    LocalizationProvider.GetLocalizedValue<string>("FPC_LOAD_RSLT_Summary"),  //Template had {0} style items. {1} of them were successfully applied.", 
                    styleData.Count,
                    appliedStyles);
                ((CollectionViewSource)this.Resources["theDataView"]).View.Refresh();
                ((CollectionViewSource)theLeftList.Resources["theComboBoxDataView"]).View.Refresh();

                ResultWindow rw = new ResultWindow(
                    LocalizationProvider.GetLocalizedValue<string>("FPC_LOAD_RSLTWIN_Title"), //Template results
                    results);
                rw.Owner = this;
                rw.ShowDialog();
            }
        }

        /// <summary>
        /// Converts integer value to Visibility
        /// 1 = visible, 0 = hidden unless parameter!=null then collapsed
        /// </summary>
        [ValueConversion(typeof(int), typeof(Visibility))]
        public class VisibleConverter : IValueConverter
        {
            public object Convert(object value, Type type, object parameter, System.Globalization.CultureInfo culture)
            {
                if ((int)value == 1)
                    return Visibility.Visible;

                if (parameter != null)
                {
                    return Visibility.Collapsed;
                }

                return Visibility.Hidden;
            }

            public object ConvertBack(object o, Type type, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotSupportedException();
            }
        }
    }

    /// <summary>
    /// Converts integer value to Visibility
    /// 1 = visible, 0 = hidden unless parameter!=null then collapsed
    /// </summary>
    [ValueConversion(typeof(int), typeof(Visibility))]
    public class VisibleConverter : IValueConverter
    {
        public object Convert(object value, Type type, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((int)value == 1)
                return Visibility.Visible;

            if (parameter != null)
            {
                return Visibility.Collapsed;
            }

            return Visibility.Hidden;
        }

        public object ConvertBack(object o, Type type, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}