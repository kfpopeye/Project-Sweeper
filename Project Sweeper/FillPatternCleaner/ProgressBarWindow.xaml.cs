using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Autodesk.Revit.DB;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace PKHL.ProjectSweeper.FillPatternCleaner
{
    /// <summary>
    /// Interaction logic for ProgressBarWindow.xaml
    /// </summary>
    public partial class ProgressBarWindow : Window
    {
        public bool IsCanceled { get; set; }
        private string CancelledText = null;

        public ProgressBarWindow(string cncl, string cncld)
        {
            InitializeComponent();
            IsCanceled = false;
            this.CancelButton.Content = cncl;
            CancelledText = cncld;
        }

        public void UpdateProgress(string comment, int current, int total)
        {
            this.Dispatcher.Invoke(new Action<string, int, int>(

            delegate(string s, int v, int t)
            {
                this._message.Text = s;
                this._bar.Maximum = System.Convert.ToDouble(t);
                this._bar.Value = System.Convert.ToDouble(v);
            }),
            System.Windows.Threading.DispatcherPriority.Background, comment, current, total);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            IsCanceled = true;
            UpdateProgress(CancelledText, 1, 1);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
            Title = Title + " - DEBUG BUILD";
#endif
        }
    }
}