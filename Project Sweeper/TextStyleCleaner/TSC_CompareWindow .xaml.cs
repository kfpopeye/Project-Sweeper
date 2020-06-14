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
    public partial class CompareWindow : Window
    {
        public System.Collections.ObjectModel.ObservableCollection<TextStyleDefinition> TheCollection { get { return data; } }
        private System.Collections.ObjectModel.ObservableCollection<TextStyleDefinition> data = null;

        public CompareWindow(System.Collections.ObjectModel.ObservableCollection<TextStyleDefinition> _data)
        {
            InitializeComponent();
            data = _data;
        }

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

        void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
            Title = Title + " - DEBUG BUILD";
#endif
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ProjectSweeper._contextualHelp.HelpTopicUrl = Constants.TSC_HELP;
            ProjectSweeper._contextualHelp.Launch();
        }
    }
}