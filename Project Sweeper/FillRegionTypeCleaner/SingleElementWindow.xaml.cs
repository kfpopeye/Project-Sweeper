using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Rdb = Autodesk.Revit.DB;

namespace PKHL.ProjectSweeper.FillRegionTypeCleaner
{
    /// <summary>
    /// Interaction logic for SingleElementWindow.xaml
    /// </summary>
    public partial class SingleElementWindow : Window
    {
        public System.Collections.ObjectModel.ObservableCollection<FillRegionTypeDefinition> TheCollection { get { return data; } }
        System.Collections.ObjectModel.ObservableCollection<FillRegionTypeDefinition> data = null;
        public FillRegionTypeDefinition chossenStyle = null;
        public bool DeleteSourceStyle = true;
        private FillRegionTypeDefinition selectedStyle = null;
        private Rdb.Document TheDoc = null;

        public SingleElementWindow(System.Collections.ObjectModel.ObservableCollection<FillRegionTypeDefinition> _data, FillRegionTypeDefinition _selectedStyle, ref Rdb.Document _theDoc)
        {
            InitializeComponent();
            _data.Add(new FillRegionTypeDefinition() { StyleName = LocalizationProvider.GetLocalizedValue<string>("FRTC_DeleteRegion"), ItsId = -1 });
            data = _data;
            selectedStyle = _selectedStyle;
            this.Title = Title + " " + selectedStyle.StyleName;
            TheDoc = _theDoc;

            if (!selectedStyle.IsDeleteable)
            {
                cb_Delete.IsChecked = false;
                cb_Delete.IsEnabled = false;
                DeleteSourceStyle = false;
            }

            ssStyleName.Text = selectedStyle.StyleName;
            ssLineweight.Text = selectedStyle.LineWeight;
            ssBackground.IsChecked = selectedStyle.IsMasking;
            ssNumberUses.Text = selectedStyle.NumberOfUses.ToString();

            ssFgPatt.FillPattern = selectedStyle.ForegroundPattern;
            ssForeName.Text = selectedStyle.ForePattName;
            ssForeType.Text = selectedStyle.FgPattType;
            ssBgPatt.FillPattern = selectedStyle.BackgroundPattern;
            ssBackName.Text = selectedStyle.BackPattName;
            ssBackType.Text = selectedStyle.BgPattType;

            var converter = new System.Windows.Media.BrushConverter();
            var Fgbrush = (System.Windows.Media.Brush)converter.ConvertFrom(selectedStyle.ForegroundFpColour); //ConvertFromString(selectedStyle.ForegroundFpColour);
            ssFgColour.Background = Fgbrush;
            var Bgbrush = (System.Windows.Media.Brush)converter.ConvertFrom(selectedStyle.BackgroundFpColour);
            ssBgColour.Background = Bgbrush;
        }

        void FilterSelectedStyle(object sender, FilterEventArgs e)
        {
            FillRegionTypeDefinition tsd = e.Item as FillRegionTypeDefinition;
            if (tsd.ItsId == selectedStyle.ItsId)
                e.Accepted = false;
            else
                e.Accepted = true;
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            DeleteSourceStyle = (bool)cb.IsChecked;
        }

        private void cb_Delete_Unchecked(object sender, RoutedEventArgs e)
        {
            DeleteSourceStyle = (bool)cb_Delete.IsChecked;
        }

        private void listviews_button_Click(object sender, RoutedEventArgs e)
        {
            if (selectedStyle != null)
            {
                foreach (FillRegionTypeDefinition tsd in data)
                {
                    if (tsd.Equals(selectedStyle))
                    {
                        ListViewsWindow lvw = new ListViewsWindow(tsd, TheDoc.Application);
                        lvw.Owner = this;
                        lvw.ShowDialog();
                        break;
                    }
                }
            }
        }

        private void theListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            chossenStyle = theListView.SelectedItem as FillRegionTypeDefinition;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
            Title = Title + " - DEBUG BUILD";
#endif
        }

        private void theListView_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            cb_Delete.IsChecked = false;
            DeleteSourceStyle = (bool)cb_Delete.IsChecked;
        }

        private void theListView_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            cb_Delete.IsChecked = true;
            DeleteSourceStyle = (bool)cb_Delete.IsChecked;
        }

        private void theListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OK_Button_Click(null, null);
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ProjectSweeper._contextualHelp.HelpTopicUrl = Constants.FRT_HELP;
            ProjectSweeper._contextualHelp.Launch();
        }

        //Revit handles localization of parameters
        private void TextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            TextBlock tb = sender as TextBlock;
            switch (tb.Text)
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
            tb.Text += ": ";
        }

        private void SourceTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                if (SrcForeTab.IsSelected)
                {
                    SrcForePanel.Visibility = Visibility.Visible;
                    SrcBackPanel.Visibility = Visibility.Collapsed;
                }
                else if (SrcBackTab.IsSelected)
                {
                    SrcForePanel.Visibility = Visibility.Collapsed;
                    SrcBackPanel.Visibility = Visibility.Visible;
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
    }
}
