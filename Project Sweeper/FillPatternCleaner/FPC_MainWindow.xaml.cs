using System.Windows;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Data;
using System;

namespace PKHL.ProjectSweeper.FillPatternCleaner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
	{
		public System.Collections.ObjectModel.ObservableCollection<FillPatternDefinition> TheCollection { get { return data; } }
		private System.Collections.ObjectModel.ObservableCollection<FillPatternDefinition> data = null;
        private FillPatternDefinition nonePattern;
        private IList<FillPatternDefinition> selectedSStyles = null;

        private FillPatternDefinition _defaultPattern;
        public FillPatternDefinition defaultPattern
        {
            get
            {
                return _defaultPattern;
            }
            set
            {
                _defaultPattern = value;
                MI_DefaultName.Text = _defaultPattern.StyleName;
            }
        }

        public MainWindow(System.Collections.ObjectModel.ObservableCollection<FillPatternDefinition> _data, IList<FillPatternDefinition> _selectedSStyles)
		{
			InitializeComponent();
			data = _data;
            nonePattern = ((FillPatternDefinition)theLeftList.Resources["NoneItem"]);
            defaultPattern = nonePattern;
            selectedSStyles = _selectedSStyles;
        }

        /// <summary>
        /// Shows only user selected fill pattern, if any
        /// </summary>
        void theDataView_Filter(object sender, FilterEventArgs e)
        {
            if (selectedSStyles == null)
            {
                e.Accepted = true;
                return;
            }

            FillPatternDefinition l_item = e.Item as FillPatternDefinition;
            foreach (FillPatternDefinition lsd in selectedSStyles)
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
			if(!this.IsLoaded)
			{
				e.Accepted = true;
				return;
			}

            FillPatternDefinition fpd = e.Item as FillPatternDefinition;
			if(fpd != null)
				e.Accepted = !fpd.StyleToBeDeleted;
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
			foreach(FillPatternDefinition fpd in theLeftList.Items)
			{
				if(fpd.IsDeleteable && !fpd.StyleToBeDeleted)
				{
					fpd.StyleToBeDeleted = true;
                    if(!fpd.StyleToBeConverted)
					    fpd.NewStyle = defaultPattern;
				}
			}
			((CollectionViewSource)this.Resources["theDataView"]).View.Refresh();
			((CollectionViewSource)theLeftList.Resources["theComboBoxDataView"]).View.Refresh();
		}
		
		void DeleteCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			CheckBox cb = sender as CheckBox;
			if (cb != null && cb.IsLoaded)
			{
				ListViewItem lvi = pkhCommon.WPF.Helpers.GetVisualParent<ListViewItem>(cb);
                FillPatternDefinition lvi_fpd = lvi.Content as FillPatternDefinition;
				foreach(FillPatternDefinition fpd in data)
				{
					if(fpd.NewStyle == lvi_fpd)
						fpd.NewStyle = defaultPattern;
				}
                if(!lvi_fpd.StyleToBeConverted)
				    lvi_fpd.NewStyle = defaultPattern;
			}
			((CollectionViewSource)this.Resources["theDataView"]).View.Refresh();
			((CollectionViewSource)theLeftList.Resources["theComboBoxDataView"]).View.Refresh();
		}
		
		void PurgeButton_Click(object sender, RoutedEventArgs e)
		{
			foreach(FillPatternDefinition fpd in theLeftList.Items)
			{
				if(fpd.NumberOfUses == 0 && fpd.IsDeleteable)
				{
					fpd.StyleToBeDeleted = true;
                    if(!fpd.StyleToBeConverted)
                        fpd.NewStyle = defaultPattern;
				}
            }
            ((CollectionViewSource)this.Resources["theDataView"]).View.Refresh();
            ((CollectionViewSource)theLeftList.Resources["theComboBoxDataView"]).View.Refresh();
		}
		
		void MI_setDefaultStyle_Click(object sender, RoutedEventArgs e)
		{
            FillPatternDefinition lvi_fpd = theLeftList.SelectedItem as FillPatternDefinition;
			if(defaultPattern.ItsId != -1)
                defaultPattern.IsDeleteable = true;
            lvi_fpd.StyleToBeDeleted = false;
            lvi_fpd.IsDeleteable = false;
            lvi_fpd.NewStyle = null;
            defaultPattern = lvi_fpd;
            ((CollectionViewSource)this.Resources["theDataView"]).View.Refresh();
			((CollectionViewSource)theLeftList.Resources["theComboBoxDataView"]).View.Refresh();
		}
		
		void MI_Reset_Click(object sender, RoutedEventArgs e)
		{
            defaultPattern.IsDeleteable = true;
            defaultPattern = nonePattern;
            ((CollectionViewSource)this.Resources["theDataView"]).View.Refresh();
			((CollectionViewSource)theLeftList.Resources["theComboBoxDataView"]).View.Refresh();
        }

        private void MI_resetStyle_Click(object sender, RoutedEventArgs e)
        {
            FillPatternDefinition lvi_fpd = theLeftList.SelectedItem as FillPatternDefinition;
            if (lvi_fpd != null)
            {
                if(lvi_fpd.IsDeleteable)
                    lvi_fpd.StyleToBeDeleted = false;
                lvi_fpd.NewStyle = null;
            }
            ((CollectionViewSource)this.Resources["theDataView"]).View.Refresh();
            ((CollectionViewSource)theLeftList.Resources["theComboBoxDataView"]).View.Refresh();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
            Title = Title + " - DEBUG BUILD";
#endif
        }

        private void listusers_button_Click(object sender, RoutedEventArgs e)
        {
            FillPatternDefinition lvi_fpd = theLeftList.SelectedItem as FillPatternDefinition;
            if (lvi_fpd != null)
            {
                if (lvi_fpd.NumberOfUses > 0)
                {
                    ListUsersWindow luw = new ListUsersWindow(lvi_fpd);
                    luw.Owner = this;
                    luw.ShowDialog();
                }
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!theLeftList.IsLoaded)
                return;
            ComboBox cb = sender as ComboBox;
            if (cb == null || !cb.IsDropDownOpen || cb.SelectedItem == null)
                return;

            System.Diagnostics.Debug.Print("ComboBox_SelectionChanged doing something.");
            FillPatternDefinition cb_fpd = cb.SelectedItem as FillPatternDefinition;
            ListViewItem lvi = pkhCommon.WPF.Helpers.GetVisualParent<ListViewItem>(cb);
            FillPatternDefinition lvi_fpd = lvi.Content as FillPatternDefinition;

            if (cb_fpd.NewStyle != null && cb_fpd.NewStyle.Equals(lvi_fpd)) //changing to a style set to change to this style
            {
                // Revit Taskdialog DIA001
                Autodesk.Revit.UI.TaskDialog td = new Autodesk.Revit.UI.TaskDialog(LocalizationProvider.GetLocalizedValue<string>("TXT_Warning"));
                td.MainIcon = Autodesk.Revit.UI.TaskDialogIcon.TaskDialogIconWarning;
                td.MainInstruction = string.Format(LocalizationProvider.GetLocalizedValue<string>("DIA001_MainInst"), cb_fpd.StyleName); // {0} is being converted to this pattern.
                td.MainContent = string.Format(LocalizationProvider.GetLocalizedValue<string>("DIA001_MainCont"), cb_fpd.StyleName, lvi_fpd.StyleName); // The pattern {0} is already being converted to {1}. This could lead to unpredictable results.
                td.Show();
                cb.SelectedValue = lvi_fpd.NewStyle;
                return;
            }

            if (lvi_fpd.NewStyle == null || !lvi_fpd.NewStyle.Equals(cb_fpd))
                lvi_fpd.NewStyle = cb_fpd;

            if (cb_fpd.ItsId != nonePattern.ItsId) //deal with "Delete" scenarios
                return;

            if (defaultPattern.Equals(lvi_fpd))   //is default style being set to delete elements
            {
                lvi_fpd.NewStyle = null;
                Autodesk.Revit.UI.TaskDialog td = new Autodesk.Revit.UI.TaskDialog(LocalizationProvider.GetLocalizedValue<string>("TXT_Warning"));
                td.MainIcon = Autodesk.Revit.UI.TaskDialogIcon.TaskDialogIconWarning;
                td.MainInstruction = string.Format(LocalizationProvider.GetLocalizedValue<string>("DIA002_MainInst"), lvi_fpd.StyleName); // {0} is the default pattern.
                td.MainContent = LocalizationProvider.GetLocalizedValue<string>("DIA002_MainCont"); // Change the default pattern to another pattern before trying to delete all patterns of this type.
                td.Show();
                return;
            }

            theLeftList.BeginInit();
            bool showWarning = true;
            System.Diagnostics.Debug.Print("Checking styles for dependencies on: {0}", lvi_fpd.StyleName);
            foreach (FillPatternDefinition fpd in data)   //are any styles being set to a style who will be deleted
            {
                System.Diagnostics.Debug.Write("\t" + fpd.StyleName + " : ");
                if (fpd.StyleToBeConverted && fpd.NewStyle.Equals(lvi_fpd))
                {
                    System.Diagnostics.Debug.WriteLine("Yes");
                    fpd.NewStyle = null;
                    fpd.StyleToBeDeleted = false;
                    if (showWarning)
                    {
                        Autodesk.Revit.UI.TaskDialog td = new Autodesk.Revit.UI.TaskDialog(LocalizationProvider.GetLocalizedValue<string>("TXT_Warning"));
                        td.MainIcon = Autodesk.Revit.UI.TaskDialogIcon.TaskDialogIconWarning;
                        td.MainContent = string.Format(LocalizationProvider.GetLocalizedValue<string>("DIA003_MainCont"), lvi_fpd.StyleName); // Some patterns were set to be converted to {0}. These patterns have been reset.
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
            ProjectSweeper._contextualHelp.HelpTopicUrl = Constants.FPC_HELP;
            ProjectSweeper._contextualHelp.Launch();
        }

        private void ResetAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (FillPatternDefinition lvi_fpd in TheCollection)
            {
                if (lvi_fpd != null)
                {
                    if (lvi_fpd.IsDeleteable)
                        lvi_fpd.StyleToBeDeleted = false;
                    lvi_fpd.NewStyle = null;
                }
            }
            ((CollectionViewSource)this.Resources["theDataView"]).View.Refresh();
            ((CollectionViewSource)theLeftList.Resources["theComboBoxDataView"]).View.Refresh();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            System.Collections.Generic.List<TemplateWindow.StyleDataContainer> styleData = new List<TemplateWindow.StyleDataContainer>();
            foreach (FillPatternDefinition fpd in TheCollection)
            {
                if (fpd.StyleToBeConverted || fpd.StyleToBeDeleted)
                {
                    System.Diagnostics.Debug.WriteLine(fpd.StyleName);
                    TemplateWindow.StyleDataContainer sdr = new TemplateWindow.StyleDataContainer();
                    sdr.oldStyle = fpd.StyleName;
                    sdr.newStyle = fpd.NewStyle.StyleName;
                    sdr.deleteStyle = fpd.StyleToBeDeleted;
                    styleData.Add(sdr);
                }
            }

            TemplateWindow tw = new TemplateWindow(styleData, "FPC");
            tw.Owner = this;
            tw.ShowDialog();
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            int appliedStyles = 0;
            System.Text.StringBuilder results = new System.Text.StringBuilder();
            System.Collections.Generic.List<TemplateWindow.StyleDataContainer> styleData = null;

            TemplateWindow tw = new TemplateWindow(null, "FPC");
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
                    FillPatternDefinition srcFpd = null;
                    foreach (FillPatternDefinition fpd in TheCollection)
                    {

                        if (fpd.StyleName.Equals(sdc.oldStyle, compareType))
                        {
                            srcFpd = fpd;
                            break;
                        }
                    }
                    if (srcFpd == null)
                    {
                        results.AppendFormat(
                            LocalizationProvider.GetLocalizedValue<string>("FPC_LOAD_RSLT_NoPatt"),  //Could not find pattern \"{0}\". Skipping it.
                            sdc.oldStyle);
                        results.AppendLine();
                        continue;
                    }
                    FillPatternDefinition newFpd = null;
                    if (sdc.newStyle == nonePattern.StyleName)
                    {
                        newFpd = nonePattern;
                    }
                    else
                    {
                        foreach (FillPatternDefinition fpd in TheCollection)
                        {
                            if (fpd.StyleName.Equals(sdc.newStyle, compareType))
                            {
                                newFpd = fpd;
                                break;
                            }
                        }
                    }
                    if (newFpd == null)
                    {
                        results.AppendFormat(
                            LocalizationProvider.GetLocalizedValue<string>("FPC_LOAD_RSLT_NoPatt2Conv"),  //Could not find pattern "{0}" to convert to. Skipping it.
                            sdc.newStyle);
                        results.AppendLine();
                        continue;
                    }

                    if (newFpd.NewStyle != null && newFpd.NewStyle.Equals(srcFpd)) //changing to a style set to change to this style
                    {
                        results.AppendFormat(
                            LocalizationProvider.GetLocalizedValue<string>("FPC_LOAD_RSLT_AlreadyConv"), //Pattern "{0}" is already being convert to "{1}". Skipping it.
                            sdc.newStyle,
                            sdc.oldStyle);
                        continue;
                    }

                    srcFpd.NewStyle = newFpd;
                    ++appliedStyles;
                    if (srcFpd.IsDeleteable)
                    {
                        srcFpd.StyleToBeDeleted = sdc.deleteStyle;
                        results.AppendFormat(
                            LocalizationProvider.GetLocalizedValue<string>("FPC_LOAD_RSLT_ChangeAndDel"),  //Set pattern "{0}" to be changed to "{1}" and then delete the style. 
                            sdc.oldStyle,
                            sdc.newStyle);
                    }
                    else
                        results.AppendFormat(
                            LocalizationProvider.GetLocalizedValue<string>("FPC_LOAD_RSLT_Change"),  //Set pattern "{0}" to be changed to "{1}".
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