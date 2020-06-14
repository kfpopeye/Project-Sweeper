using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace PKHL.ProjectSweeper.LinePatternCleaner
{
    /// <summary>
    /// Interaction logic for SingleElementWindow.xaml
    /// </summary>
    public partial class SingleElementWindow : Window
    {
        public System.Collections.ObjectModel.ObservableCollection<LinePatternDefinition> TheCollection { get { return data; } }
        System.Collections.ObjectModel.ObservableCollection<LinePatternDefinition> data = null;
        public LinePatternDefinition chossenStyle = null;
        public bool DeleteSourceStyle = true;
        private LinePatternDefinition selectedStyle = null;

        public SingleElementWindow(System.Collections.ObjectModel.ObservableCollection<LinePatternDefinition> _data, LinePatternDefinition _selectedStyle)
        {
            InitializeComponent();
            _data.Add(new LinePatternDefinition() { StyleName = LocalizationProvider.GetLocalizedValue<string>("LPC_Solid"), ItsId = -1 });
            data = _data;
            selectedStyle = _selectedStyle;
            this.Title = Title + selectedStyle.StyleName;

            if (!selectedStyle.IsDeleteable)
            {
                cb_Delete.IsChecked = false;
                cb_Delete.IsEnabled = false;
                DeleteSourceStyle = false;
            }

            ssNumberUsing.Text = selectedStyle.NumberOfUses.ToString();
            ssStyleName.Text = selectedStyle.StyleName;
            ssthePattern.LinePattern = selectedStyle.thePattern;
        }

        void FilterSelectedStyle(object sender, FilterEventArgs e)
        {
            LinePatternDefinition fpd = e.Item as LinePatternDefinition;
            if (fpd.ItsId == selectedStyle.ItsId)
            {
                e.Accepted = false;
            }
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
            if (selectedStyle.NumberOfUses > 0)
            {
                ListUsersWindow luw = new ListUsersWindow(selectedStyle);
                luw.Owner = this;
                luw.ShowDialog();
            }
        }

        private void theListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            chossenStyle = theListView.SelectedItem as LinePatternDefinition;
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
            ProjectSweeper._contextualHelp.HelpTopicUrl = Constants.FPC_HELP;
            ProjectSweeper._contextualHelp.Launch();
        }
    }
}
