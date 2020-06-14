using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Rdb = Autodesk.Revit.DB;

namespace PKHL.ProjectSweeper.TextStyleCleaner
{
    /// <summary>
    /// Interaction logic for SingleElementWindow.xaml
    /// </summary>
    public partial class SingleElementWindow : Window
    {
        public System.Collections.ObjectModel.ObservableCollection<TextStyleDefinition> TheCollection { get { return data; } }
        System.Collections.ObjectModel.ObservableCollection<TextStyleDefinition> data = null;
        public TextStyleDefinition chossenStyle = null;
        public bool DeleteSourceStyle = true;
        private TextStyleDefinition selectedStyle = null;
        private Rdb.Document TheDoc = null;
        private readonly TextStyleDefinition _deleteNotes = new TextStyleDefinition() { StyleName = LocalizationProvider.GetLocalizedValue<string>("TSC_DeleteNotes"), ItsId = -1 };

        public SingleElementWindow(System.Collections.ObjectModel.ObservableCollection<TextStyleDefinition> _data, TextStyleDefinition _selectedStyle, ref Rdb.Document _theDoc)
        {
            InitializeComponent();
            _data.Add(_deleteNotes);
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
            ssStyleSize.Text = selectedStyle.TextSize;
            ssStyleFont.Text = selectedStyle.TextFontName;
            ssStyleBox.Text = selectedStyle.GraphicShowBorder;
            ssStyleBackground.Text = selectedStyle.GraphicBackground;
            ssNumberUses.Text = selectedStyle.NumberOfUses.ToString();
            string s = string.Empty;
            TextBlock t = new TextBlock();
            if (selectedStyle.TextBold != 0)
            {
                t.Text = "TEXT_STYLE_BOLD";
                TextBlock_Loaded(t, null);
                s += t.Text;
            }
            if (selectedStyle.TextItalic != 0)
            {
                t.Text = "TEXT_STYLE_ITALIC";
                TextBlock_Loaded(t, null);
                s += t.Text;
            }
            if (selectedStyle.TextUnderline != 0)
            {
                t.Text = "TEXT_STYLE_UNDERLINE";
                TextBlock_Loaded(t, null);
                s += t.Text;
            }
            ssStyleFormat.Text = s;
            var converter = new System.Windows.Media.BrushConverter();
            var brush = (System.Windows.Media.Brush)converter.ConvertFromString(selectedStyle.GraphicColour);
            ssStyleColour.Background = brush;
        }

        void FilterSelectedStyle(object sender, FilterEventArgs e)
        {
            TextStyleDefinition tsd = e.Item as TextStyleDefinition;
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
                foreach (TextStyleDefinition tsd in data)
                {
                    if (tsd.Equals(selectedStyle))
                    {
                        //text styles are used in views and schedules\templates (which are also view types) 
                         if (tsd.OwnerViews != null)
                        {
                            ListViewsWindow lvw = new ListViewsWindow(tsd, TheDoc.Application);
                            lvw.Owner = this;
                            lvw.ShowDialog();
                        }
                        else
                        {
                            ListUsersWindow luw = new ListUsersWindow(tsd);
                            luw.Owner = this;
                            luw.ShowDialog();
                        }
                        break;
                    }
                }
            }
        }

        private void theListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            chossenStyle = theListView.SelectedItem as TextStyleDefinition;

            if (theListView.SelectedItems.Count != 1)
                OkButton.IsEnabled = false;
            else
                OkButton.IsEnabled = true;
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
            ProjectSweeper._contextualHelp.HelpTopicUrl = Constants.TSC_HELP;
            ProjectSweeper._contextualHelp.Launch();
        }

        private void compare_button_Click(object sender, RoutedEventArgs e)
        {
            if (theListView.SelectedItems.Count < 1)
                return;

            System.Collections.ObjectModel.ObservableCollection<TextStyleDefinition> c_data = new System.Collections.ObjectModel.ObservableCollection<TextStyleDefinition>();
            c_data.Add(selectedStyle);
            foreach (TextStyleDefinition lbi in theListView.SelectedItems)
            {
                if(lbi.ItsId != -1)
                    c_data.Add(lbi);
            }
            if (c_data.Count > 1)
            {
                CompareWindow cw = new CompareWindow(c_data);
                cw.Owner = this;
                cw.ShowDialog();
            }
        }

        /// <summary>
        /// Uses Revit to handle localization of paramter names and sets text to the first char of string (B,I or U)
        /// </summary>
        private void TextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            TextBlock tb = sender as TextBlock;
            switch (tb.Text)										  
            {
                case "TEXT_STYLE_UNDERLINE":
                    tb.Inlines.Clear();
                    tb.Inlines.Add(new System.Windows.Documents.Underline(new System.Windows.Documents.Run(Rdb.LabelUtils.GetLabelFor(Rdb.BuiltInParameter.TEXT_STYLE_UNDERLINE).Substring(0, 1))));
                    break;
                case "TEXT_STYLE_ITALIC":
                    tb.Text = Rdb.LabelUtils.GetLabelFor(Rdb.BuiltInParameter.TEXT_STYLE_ITALIC).Substring(0,1);
                    break;
                case "TEXT_STYLE_BOLD":
                    tb.Text = Rdb.LabelUtils.GetLabelFor(Rdb.BuiltInParameter.TEXT_STYLE_BOLD).Substring(0, 1);
                    break;
                default:
                    tb.Text = "ERROR****";
                    break;
            }
		}
    }
}
