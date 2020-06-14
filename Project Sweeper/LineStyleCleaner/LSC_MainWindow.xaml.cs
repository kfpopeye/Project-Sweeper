using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Rdb = Autodesk.Revit.DB;

namespace PKHL.ProjectSweeper.LineStyleCleaner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public System.Collections.ObjectModel.ObservableCollection<LineStyleDefinition> TheCollection { get { return data; } }
        private System.Collections.ObjectModel.ObservableCollection<LineStyleDefinition> data = null;
        private LineStyleDefinition deleteStyleLSD = null;
        private LineStyleDefinition theThinLineStyle = null;
        private IList<LineStyleDefinition> selectedSStyles = null;
        private Rdb.Document TheDoc = null;

        private LineStyleDefinition _defaultLineStyle;
        public LineStyleDefinition defaultLineStyle
        {
            get
            {
                return _defaultLineStyle;
            }
            set
            {
                _defaultLineStyle = value;
                MI_DefaultName.Text = _defaultLineStyle.StyleName;
            }
        }

        public MainWindow(System.Collections.ObjectModel.ObservableCollection<LineStyleDefinition> _data, IList<LineStyleDefinition> _selectedSStyles, ref Rdb.Document _theDoc)
        {
            InitializeComponent();
            TheDoc = _theDoc;
            data = _data;
            selectedSStyles = _selectedSStyles;
            if (selectedSStyles != null)
                FilterBox.IsEnabled = false;
            deleteStyleLSD = ((LineStyleDefinition)theLeftList.Resources["deleteItem"]);

            foreach (LineStyleDefinition lsd in data)
            {
                if (lsd.CategoryId == TheDoc.Settings.Categories.get_Item(Rdb.BuiltInCategory.OST_CurvesThinLines).Id.IntegerValue)
                {
                    theThinLineStyle = lsd;
                    MI_Thin.Tag = lsd;
                }
                else if (lsd.CategoryId == TheDoc.Settings.Categories.get_Item(Rdb.BuiltInCategory.OST_CurvesMediumLines).Id.IntegerValue)
                    MI_Medium.Tag = lsd;
                else if (lsd.CategoryId == TheDoc.Settings.Categories.get_Item(Rdb.BuiltInCategory.OST_CurvesWideLines).Id.IntegerValue)
                    MI_Wide.Tag = lsd;
                else if (lsd.CategoryId == TheDoc.Settings.Categories.get_Item(Rdb.BuiltInCategory.OST_GenericLines).Id.IntegerValue)
                    MI_Lines.Tag = lsd;
                else if (lsd.CategoryId == TheDoc.Settings.Categories.get_Item(Rdb.BuiltInCategory.OST_DemolishedLines).Id.IntegerValue)
                    MI_Demolished.Tag = lsd;
                else if (lsd.CategoryId == TheDoc.Settings.Categories.get_Item(Rdb.BuiltInCategory.OST_HiddenLines).Id.IntegerValue)
                    MI_Hidden.Tag = lsd;
                else if (lsd.CategoryId == TheDoc.Settings.Categories.get_Item(Rdb.BuiltInCategory.OST_OverheadLines).Id.IntegerValue)
                    MI_Overhead.Tag = lsd;
                else if (lsd.CategoryId == TheDoc.Settings.Categories.get_Item(Rdb.BuiltInCategory.OST_LinesBeyond).Id.IntegerValue)
                    MI_Beyond.Tag = lsd;
                else if (lsd.CategoryId == TheDoc.Settings.Categories.get_Item(Rdb.BuiltInCategory.OST_CenterLines).Id.IntegerValue)
                    MI_Centerline.Tag = lsd;
                else if (lsd.CategoryId == TheDoc.Settings.Categories.get_Item(Rdb.BuiltInCategory.OST_LinesHiddenLines).Id.IntegerValue)
                    MI_HiddenLines.Tag = lsd;
            }

            defaultLineStyle = theThinLineStyle;
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

            LineStyleDefinition l_item = e.Item as LineStyleDefinition;
            foreach (LineStyleDefinition lsd in selectedSStyles)
            {
                if (l_item.Equals(lsd))
                {
                    e.Accepted = true;
                    return;
                }
            }

            e.Accepted = false;
        }

        void ComboBoxDataView_Filter(object sender, FilterEventArgs e)
        {
            if (!this.IsLoaded)
            {
                e.Accepted = true;
                return;
            }

            LineStyleDefinition lsd = e.Item as LineStyleDefinition;
            if (lsd != null)
            {
                if (lsd.StyleToBeDeleted || (lsd.NewStyle != null && lsd.NewStyle.ItsId == deleteStyleLSD.ItsId))
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
            foreach (LineStyleDefinition lsd in data)
            {
                if (theLeftList.Items.Contains(lsd))
                {
                    if (lsd.IsDeleteable)
                    {
                        lsd.StyleToBeDeleted = true;
                        if (!lsd.StyleToBeConverted)
                            lsd.NewStyle = defaultLineStyle;
                    }
                }
            }
            theLeftList.EndInit();
            ((CollectionViewSource)this.Resources["theDataView"]).View.Refresh();
            ((CollectionViewSource)theLeftList.Resources["theComboBoxDataView"]).View.Refresh();
        }

        void DeleteCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            if (cb != null && cb.IsLoaded)
            {
                ListViewItem lvi = pkhCommon.WPF.Helpers.GetVisualParent<ListViewItem>(cb);
                LineStyleDefinition lvi_lsd = lvi.Content as LineStyleDefinition;
                foreach (LineStyleDefinition lsd in data)
                {
                    if (lsd.NewStyle == lvi_lsd)
                        lsd.NewStyle = defaultLineStyle;
                }
                if (!lvi_lsd.StyleToBeConverted)
                    lvi_lsd.NewStyle = defaultLineStyle;
            }
            ((CollectionViewSource)this.Resources["theDataView"]).View.Refresh();
            ((CollectionViewSource)theLeftList.Resources["theComboBoxDataView"]).View.Refresh();
        }

        void PurgeButton_Click(object sender, RoutedEventArgs e)
        {
            theLeftList.BeginInit();
            foreach (LineStyleDefinition lsd in data)
            {
                if (theLeftList.Items.Contains(lsd))
                {
                    if (lsd.DetailLinesUsingStyle == 0 && lsd.ModelLinesUsingStyle == 0 && lsd.IsDeleteable)
                    {
                        lsd.StyleToBeDeleted = true;
                        if (!lsd.StyleToBeConverted)
                            lsd.NewStyle = defaultLineStyle;
                    }
                }
            }
            theLeftList.EndInit();
            ((CollectionViewSource)this.Resources["theDataView"]).View.Refresh();
            ((CollectionViewSource)theLeftList.Resources["theComboBoxDataView"]).View.Refresh();
        }

        void MI_setDefaultStyle_Click(object sender, RoutedEventArgs e)
        {
            LineStyleDefinition lvi_lsd = theLeftList.SelectedItem as LineStyleDefinition;
            defaultLineStyle.IsDeleteable = true;
            lvi_lsd.StyleToBeDeleted = false;
            lvi_lsd.IsDeleteable = false;
            defaultLineStyle = lvi_lsd;
            ((CollectionViewSource)this.Resources["theDataView"]).View.Refresh();
            ((CollectionViewSource)theLeftList.Resources["theComboBoxDataView"]).View.Refresh();
        }

        void MI_Reset_Click(object sender, RoutedEventArgs e)
        {
            defaultLineStyle.IsDeleteable = true;
            defaultLineStyle = deleteStyleLSD;
            ((CollectionViewSource)this.Resources["theDataView"]).View.Refresh();
            ((CollectionViewSource)theLeftList.Resources["theComboBoxDataView"]).View.Refresh();
        }

        private void MI_resetStyle_Click(object sender, RoutedEventArgs e)
        {
            LineStyleDefinition lvi_lsd = theLeftList.SelectedItem as LineStyleDefinition;
            if (lvi_lsd != null)
            {
                lvi_lsd.StyleToBeDeleted = false;
                lvi_lsd.NewStyle = null;
            }
            ((CollectionViewSource)this.Resources["theDataView"]).View.Refresh();
            ((CollectionViewSource)theLeftList.Resources["theComboBoxDataView"]).View.Refresh();
        }

        /// <summary>
        /// Sets the line style to the menu item style
        /// </summary>
        private void MI_lineClick_Click(object sender, RoutedEventArgs e)
        {
            LineStyleDefinition lvi_lsd = theLeftList.SelectedItem as LineStyleDefinition;
            MenuItem menuItem = sender as MenuItem;
            if (lvi_lsd != null)
                lvi_lsd.NewStyle = menuItem.Tag as LineStyleDefinition;
            ((CollectionViewSource)this.Resources["theDataView"]).View.Refresh();
            ((CollectionViewSource)theLeftList.Resources["theComboBoxDataView"]).View.Refresh();
        }

        private void listviews_button_Click(object sender, RoutedEventArgs e)
        {
            LineStyleDefinition lvi_lsd = theLeftList.SelectedItem as LineStyleDefinition;
            if (lvi_lsd != null)
            {
                if (lvi_lsd.ModelLinesUsingStyle == 0 && lvi_lsd.DetailLinesUsingStyle == 0)
                {
                    Autodesk.Revit.UI.TaskDialog td = new Autodesk.Revit.UI.TaskDialog("No views");
                    td.MainContent = "Current line style is not used in any views.";
                    td.Show();
                }
                else
                {
                    ListViewsWindow lvw = new ListViewsWindow(lvi_lsd, TheDoc.Application);
                    lvw.Owner = this;
                    lvw.ShowDialog();
                }
            }
        }

        private void menuItem_Loaded(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            LineStyleDefinition lsd = mi.Tag as LineStyleDefinition;
            Rdb.Category cat = TheDoc.Settings.Categories.get_Item((Rdb.BuiltInCategory)lsd.CategoryId);
            mi.Header = cat.Name;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
            Title = Title + " - DEBUG BUILD";
#endif
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!theLeftList.IsLoaded)
                return;
            ComboBox cb = sender as ComboBox;
            if (cb == null || !cb.IsDropDownOpen || cb.SelectedItem == null)
                return;
            LineStyleDefinition cb_lsd = cb.SelectedItem as LineStyleDefinition;
            ListViewItem lvi = pkhCommon.WPF.Helpers.GetVisualParent<ListViewItem>(cb);
            LineStyleDefinition lvi_lsd = lvi.Content as LineStyleDefinition;

            System.Diagnostics.Debug.Print("ComboBox_SelectionChanged doing something.");
            if(cb_lsd.NewStyle != null && cb_lsd.NewStyle.Equals(lvi_lsd)) //changing to a style set to change to this style
            {
                Autodesk.Revit.UI.TaskDialog td = new Autodesk.Revit.UI.TaskDialog("Warning");
                td.MainIcon = Autodesk.Revit.UI.TaskDialogIcon.TaskDialogIconWarning;
                td.MainInstruction = string.Format("{0} is being converted to this line style.", cb_lsd.StyleName);
                td.MainContent = string.Format("The style {0} is already being converted to {1}. This could lead to unpredictable results.", cb_lsd.StyleName, lvi_lsd.StyleName);
                td.Show();
                cb.SelectedValue = lvi_lsd.NewStyle;
                return;
            }

            if (lvi_lsd.NewStyle == null || !lvi_lsd.NewStyle.Equals(cb_lsd))
                lvi_lsd.NewStyle = cb_lsd;

            if (cb_lsd.ItsId != deleteStyleLSD.ItsId) //deal with "Delete Lines" scenarios
                return;
            if (defaultLineStyle.Equals(lvi_lsd))   //is default style being set to delete lines
            {
                lvi_lsd.NewStyle = null;
                Autodesk.Revit.UI.TaskDialog td = new Autodesk.Revit.UI.TaskDialog("Warning");
                td.MainIcon = Autodesk.Revit.UI.TaskDialogIcon.TaskDialogIconWarning;
                td.MainInstruction = string.Format("{0} is the default line style.", lvi_lsd.StyleName);
                td.MainContent = "Change the default style to another style before trying to delete all lines of this style.";
                td.Show();
                return;
            }

            theLeftList.BeginInit();
            bool showWarning = true;
            System.Diagnostics.Debug.Print("Checking styles for dependencies on: {0}", lvi_lsd.StyleName);
            foreach (LineStyleDefinition lsd in data)   //are any styles being set to a style whos' lines will be deleted
            {
                System.Diagnostics.Debug.Write("\t" + lsd.StyleName + " : ");
                if (lsd.StyleToBeConverted && lsd.NewStyle.Equals(lvi_lsd))
                {
                    System.Diagnostics.Debug.WriteLine("Yes");
                    lsd.NewStyle = null;
                    lsd.StyleToBeDeleted = false;
                    if (showWarning)
                    {
                        Autodesk.Revit.UI.TaskDialog td = new Autodesk.Revit.UI.TaskDialog("Warning");
                        td.MainIcon = Autodesk.Revit.UI.TaskDialogIcon.TaskDialogIconWarning;
                        td.MainContent = string.Format("Some line styles were set to be converted to {0}. These styles have been reset.", lvi_lsd.StyleName);
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
            ProjectSweeper._contextualHelp.HelpTopicUrl = Constants.LSC_HELP;
            ProjectSweeper._contextualHelp.Launch();
        }

        private void ResetAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (LineStyleDefinition lvi_def in TheCollection)
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
            foreach (LineStyleDefinition frtd in TheCollection)
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

            TemplateWindow tw = new TemplateWindow(styleData, "LSC");
            tw.Owner = this;
            tw.ShowDialog();
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            int appliedStyles = 0;
            System.Text.StringBuilder results = new System.Text.StringBuilder();
            System.Collections.Generic.List<TemplateWindow.StyleDataContainer> styleData = null;

            TemplateWindow tw = new TemplateWindow(null, "LSC");
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
                    LineStyleDefinition srcFrtd = null;
                    foreach (LineStyleDefinition frtd in TheCollection)
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
                    LineStyleDefinition newFrtd = null;
                    if (sdc.newStyle == deleteStyleLSD.StyleName)
                    {
                        newFrtd = deleteStyleLSD;
                    }
                    else
                    {
                        foreach (LineStyleDefinition frtd in TheCollection)
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
    }
}