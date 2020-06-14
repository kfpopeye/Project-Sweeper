using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Rdb = Autodesk.Revit.DB;

namespace PKHL.ProjectSweeper.LineStyleCleaner
{
    /// <summary>
    /// Interaction logic for SingleElementWindow.xaml
    /// </summary>
    public partial class SingleElementWindow : Window
    {
        public System.Collections.ObjectModel.ObservableCollection<LineStyleDefinition> TheCollection { get { return data; } }
        System.Collections.ObjectModel.ObservableCollection<LineStyleDefinition> data = null;
        public LineStyleDefinition chossenStyle = null;
        public bool DeleteSourceStyle = true;
        private Rdb.Document TheDoc = null;
        public LineStyleDefinition selectedStyle { get; set; }

        public SingleElementWindow(System.Collections.ObjectModel.ObservableCollection<LineStyleDefinition> _data, 
            LineStyleDefinition _selectedStyle, 
            ref Rdb.Document _theDoc)
        {
            InitializeComponent();
            _data.Add(new LineStyleDefinition() { StyleName = LocalizationProvider.GetLocalizedValue<string>("LSC_DeleteLines"), ItsId = -1 });
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
            ssStylePattern.Text = selectedStyle.StylePattern;
            ssStyleWeight.Text = selectedStyle.StyleWeight;
            ssthePattern.LinePattern = selectedStyle.thePattern;
            ssModelLinesUsingStyle.Text = selectedStyle.ModelLinesUsingStyle.ToString();
            ssDetailLinesUsingStyle.Text = selectedStyle.DetailLinesUsingStyle.ToString();
            if (selectedStyle.DetailLinesUsingStyle == 0)
                listviews_button.IsEnabled = false;
            var converter = new System.Windows.Media.BrushConverter();
            var brush = (System.Windows.Media.Brush)converter.ConvertFromString(selectedStyle.StyleColour);
            ssStyleColour.Background = brush;
        }

        void FilterSelectedStyle(object sender, FilterEventArgs e)
        {
            LineStyleDefinition lsd = e.Item as LineStyleDefinition;
            if (lsd.ItsId == selectedStyle.ItsId)
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
            DeleteSourceStyle = (bool)cb_Delete.IsChecked;
        }

        private void cb_Delete_Unchecked(object sender, RoutedEventArgs e)
        {
            DeleteSourceStyle = (bool)cb_Delete.IsChecked;
        }

        private void listviews_button_Click(object sender, RoutedEventArgs e)
        {
            if (selectedStyle != null)
            {
                foreach (LineStyleDefinition lsd in data)
                {
                    if (lsd.Equals(selectedStyle))
                    {
                        ListViewsWindow lvw = new ListViewsWindow(lsd, TheDoc.Application);
                        lvw.Owner = this;
                        lvw.ShowDialog();
                        break;
                    }
                }
            }
        }

        private void theListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            chossenStyle = theListView.SelectedItem as LineStyleDefinition;
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
            ProjectSweeper._contextualHelp.HelpTopicUrl = Constants.LSC_HELP;
            ProjectSweeper._contextualHelp.Launch();
        }
    }
}
