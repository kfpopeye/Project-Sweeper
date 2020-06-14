using System.Windows;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Data;
using Rdb = Autodesk.Revit.DB;

namespace PKHL.ProjectSweeper.FillRegionTypeCleaner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
	{
		public System.Collections.ObjectModel.ObservableCollection<FillRegionTypeDefinition> TheCollection { get { return data; } }
		private System.Collections.ObjectModel.ObservableCollection<FillRegionTypeDefinition> data = null;
		private FillRegionTypeDefinition deleteStyle = null;
        private Rdb.Document TheDoc = null;
        private IList<FillRegionTypeDefinition> selectedSStyles = null;

        private FillRegionTypeDefinition _defaultStyle;
        public FillRegionTypeDefinition defaultStyle
        {
            get
            {
                return _defaultStyle;
            }
            set
            {
                _defaultStyle = value;
                MI_DefaultName.Text = _defaultStyle.StyleName;
            }
        }

        public MainWindow(System.Collections.ObjectModel.ObservableCollection<FillRegionTypeDefinition> _data, IList<FillRegionTypeDefinition> _selectedSStyles, ref Rdb.Document _theDoc)
		{
			InitializeComponent();
			data = _data;
			deleteStyle = ((FillRegionTypeDefinition)theLeftList.Resources["deleteItem"]);
            defaultStyle = deleteStyle;
            TheDoc = _theDoc;
            selectedSStyles = _selectedSStyles;
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

            FillRegionTypeDefinition l_item = e.Item as FillRegionTypeDefinition;
            foreach (FillRegionTypeDefinition lsd in selectedSStyles)
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

			FillRegionTypeDefinition fpd = e.Item as FillRegionTypeDefinition;
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
			foreach(FillRegionTypeDefinition fpd in theLeftList.Items)
			{
				if(fpd.IsDeleteable)
				{
					fpd.StyleToBeDeleted = true;
                    if(!fpd.StyleToBeConverted)
					    fpd.NewStyle = defaultStyle;
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
				FillRegionTypeDefinition lvi_fpd = lvi.Content as FillRegionTypeDefinition;
				foreach(FillRegionTypeDefinition fpd in data)
				{
					if(fpd.NewStyle == lvi_fpd)
						fpd.NewStyle = defaultStyle;
				}
                if(!lvi_fpd.StyleToBeConverted)
				    lvi_fpd.NewStyle = defaultStyle;				
			}
			((CollectionViewSource)this.Resources["theDataView"]).View.Refresh();
			((CollectionViewSource)theLeftList.Resources["theComboBoxDataView"]).View.Refresh();
		}
		
		void PurgeButton_Click(object sender, RoutedEventArgs e)
        {
            theLeftList.BeginInit();
            foreach (FillRegionTypeDefinition fpd in theLeftList.Items)
			{
				if(fpd.NumberOfUses == 0 && fpd.IsDeleteable)
				{
					fpd.StyleToBeDeleted = true;
                    if (!fpd.StyleToBeConverted)
                        fpd.NewStyle = defaultStyle;
				}
            }
            theLeftList.EndInit();
            ((CollectionViewSource)this.Resources["theDataView"]).View.Refresh();
            ((CollectionViewSource)theLeftList.Resources["theComboBoxDataView"]).View.Refresh();
		}

        //Revit handles localization of parameters
		private void TextBlock_Loaded(object sender, RoutedEventArgs e)
		{
			TextBlock tb = sender as TextBlock;
			switch(tb.Text)
			{
				case "FILLED_REGION_MASKING":
					tb.Text = Rdb.LabelUtils.GetLabelFor(Rdb.BuiltInParameter.FILLED_REGION_MASKING);
					break;
				case "LINE_PEN":
					tb.Text = Rdb.LabelUtils.GetLabelFor(Rdb.BuiltInParameter.LINE_PEN);
					break;
				case "ANY_PATTERN_ID_PARAM_NO_NO":
					tb.Text = Rdb.LabelUtils.GetLabelFor(Rdb.BuiltInParameter.ANY_PATTERN_ID_PARAM_NO_NO);
					break;
				case "LINE_COLOR":
					tb.Text = Rdb.LabelUtils.GetLabelFor(Rdb.BuiltInParameter.LINE_COLOR);
					break;
				case "ELEM_TYPE_PARAM":
					tb.Text = Rdb.LabelUtils.GetLabelFor(Rdb.BuiltInParameter.ELEM_TYPE_PARAM);
					break;
			}
		}
		
		void MI_setDefaultStyle_Click(object sender, RoutedEventArgs e)
		{
			FillRegionTypeDefinition lvi_fpd = theLeftList.SelectedItem as FillRegionTypeDefinition;
			if(defaultStyle.ItsId != -1)
				defaultStyle.IsDeleteable = true;
            lvi_fpd.StyleToBeDeleted = false;
            lvi_fpd.IsDeleteable = false;
            lvi_fpd.NewStyle = null;
			defaultStyle = lvi_fpd;
            ((CollectionViewSource)this.Resources["theDataView"]).View.Refresh();
			((CollectionViewSource)theLeftList.Resources["theComboBoxDataView"]).View.Refresh();
		}
		
		void MI_Reset_Click(object sender, RoutedEventArgs e)
		{
			defaultStyle.IsDeleteable = true;
			defaultStyle = deleteStyle;
			((CollectionViewSource)this.Resources["theDataView"]).View.Refresh();
			((CollectionViewSource)theLeftList.Resources["theComboBoxDataView"]).View.Refresh();
        }

        private void MI_resetStyle_Click(object sender, RoutedEventArgs e)
        {
            FillRegionTypeDefinition lvi_frtd = theLeftList.SelectedItem as FillRegionTypeDefinition;
            if (lvi_frtd != null)
            {
                lvi_frtd.StyleToBeDeleted = false;
                lvi_frtd.NewStyle = null;
            }
            ((CollectionViewSource)this.Resources["theDataView"]).View.Refresh();
            ((CollectionViewSource)theLeftList.Resources["theComboBoxDataView"]).View.Refresh();
        }

        private void listviews_button_Click(object sender, RoutedEventArgs e)
        {
            FillRegionTypeDefinition lvi_lsd = theLeftList.SelectedItem as FillRegionTypeDefinition;
            if (lvi_lsd != null)
            {
                if (lvi_lsd.NumberOfUses > 0)
                {
                    ListViewsWindow lvw = new ListViewsWindow(lvi_lsd, TheDoc.Application);
                    lvw.Owner = this;
                    lvw.ShowDialog();
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
            FillRegionTypeDefinition cb_frtd = cb.SelectedItem as FillRegionTypeDefinition;
            ListViewItem lvi = pkhCommon.WPF.Helpers.GetVisualParent<ListViewItem>(cb);
            FillRegionTypeDefinition lvi_frtd = lvi.Content as FillRegionTypeDefinition;

            System.Diagnostics.Debug.Print("ComboBox_SelectionChanged doing something.");
            if (cb_frtd.NewStyle != null && cb_frtd.NewStyle.Equals(lvi_frtd)) //changing to a style set to change to this style
            {
                //Revit TaskDialog DIA001
                Autodesk.Revit.UI.TaskDialog td = new Autodesk.Revit.UI.TaskDialog(LocalizationProvider.GetLocalizedValue<string>("TXT_Warning"));
                td.MainIcon = Autodesk.Revit.UI.TaskDialogIcon.TaskDialogIconWarning;
                td.MainInstruction = string.Format(LocalizationProvider.GetLocalizedValue<string>("DIA001_MainInst"), cb_frtd.StyleName); //{0} is being converted to this style.
                td.MainContent = string.Format(LocalizationProvider.GetLocalizedValue<string>("DIA001_MainCont"), cb_frtd.StyleName, lvi_frtd.StyleName); //The style {0} is already being converted to {1}. This could lead to unpredictable results.
                td.Show();
                cb.SelectedValue = lvi_frtd.NewStyle;
                return;
            }

            if (lvi_frtd.NewStyle == null || !lvi_frtd.NewStyle.Equals(cb_frtd))
                lvi_frtd.NewStyle = cb_frtd;

            if (cb_frtd.ItsId != deleteStyle.ItsId) //deal with "Delete" scenarios
                return;
            if (defaultStyle.Equals(lvi_frtd))   //is default style being set to delete elements
            {
                lvi_frtd.NewStyle = null;
                //Revit TaskDialog DIA002
                Autodesk.Revit.UI.TaskDialog td = new Autodesk.Revit.UI.TaskDialog(LocalizationProvider.GetLocalizedValue<string>("TXT_Warning"));
                td.MainIcon = Autodesk.Revit.UI.TaskDialogIcon.TaskDialogIconWarning;
                td.MainInstruction = string.Format(LocalizationProvider.GetLocalizedValue<string>("DIA002_MainInst"), lvi_frtd.StyleName); // {0} is the default style.
                td.MainContent = LocalizationProvider.GetLocalizedValue<string>("DIA002_MainCont"); //Change the default style to another style before trying to delete all elements of this style.
                td.Show();
                return;
            }

            theLeftList.BeginInit();
            bool showWarning = true;
            System.Diagnostics.Debug.Print("Checking styles for dependencies on: {0}", lvi_frtd.StyleName);
            foreach (FillRegionTypeDefinition frtd in data)   //are any styles being set to a style whos' lines will be deleted
            {
                System.Diagnostics.Debug.Write("\t" + frtd.StyleName + " : ");
                if (frtd.StyleToBeConverted && frtd.NewStyle.Equals(lvi_frtd))
                {
                    System.Diagnostics.Debug.WriteLine("Yes");
                    frtd.NewStyle = null;
                    frtd.StyleToBeDeleted = false;
                    if (showWarning)
                    {
                        //Revit TaskDialog DIA003
                        Autodesk.Revit.UI.TaskDialog td = new Autodesk.Revit.UI.TaskDialog(LocalizationProvider.GetLocalizedValue<string>("TXT_Warning"));
                        td.MainIcon = Autodesk.Revit.UI.TaskDialogIcon.TaskDialogIconWarning;
                        td.MainContent = string.Format(LocalizationProvider.GetLocalizedValue<string>("DIA003_MainCont"), lvi_frtd.StyleName); // Some styles were set to be converted to {0}. These styles have been reset.
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
            Title = Title + " - DEBUG BUILD";
#endif
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ProjectSweeper._contextualHelp.HelpTopicUrl = Constants.FRT_HELP;
            ProjectSweeper._contextualHelp.Launch();
        }

        private void SourceTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                if (SrcForeTab.IsSelected)
                {
                    ForePanel.Visibility = Visibility.Visible;
                    BackPanel.Visibility = Visibility.Collapsed;
                }
                else if (SrcBackTab.IsSelected)
                {
                    ForePanel.Visibility = Visibility.Collapsed;
                    BackPanel.Visibility = Visibility.Visible;
                }
            }
        }

        private void TargetTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                if (TrgtForeTab.IsSelected)
                {
                    TrgtForePanel.Visibility = Visibility.Visible;
                    TrgtBackPanel.Visibility = Visibility.Collapsed;
                }
                else if (TrgtBackTab.IsSelected)
                {
                    TrgtForePanel.Visibility = Visibility.Collapsed;
                    TrgtBackPanel.Visibility = Visibility.Visible;
                }
            }
        }

        private void ResetAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (FillRegionTypeDefinition lvi_def in TheCollection)
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
            foreach (FillRegionTypeDefinition frtd in TheCollection)
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

            TemplateWindow tw = new TemplateWindow(styleData, "FRTC");
            tw.Owner = this;
            tw.ShowDialog();
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            int appliedStyles = 0;
            System.Text.StringBuilder results = new System.Text.StringBuilder();
            System.Collections.Generic.List<TemplateWindow.StyleDataContainer> styleData = null;

            TemplateWindow tw = new TemplateWindow(null, "FRTC");
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
                    FillRegionTypeDefinition srcFrtd = null;
                    foreach (FillRegionTypeDefinition frtd in TheCollection)
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
                    FillRegionTypeDefinition newFrtd = null;
                    if (sdc.newStyle == deleteStyle.StyleName)
                    {
                        newFrtd = deleteStyle;
                    }
                    else
                    {
                        foreach (FillRegionTypeDefinition frtd in TheCollection)
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