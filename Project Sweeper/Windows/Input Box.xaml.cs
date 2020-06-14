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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;

namespace PKHL.ProjectSweeper
{
    /// <summary>
    /// Interaction logic for Input_Box.xaml
    /// </summary>
    public partial class Input_Box : Window
    {
        private string Input = string.Empty;
        private Regex invalidCharRegex = null;
        private bool isPassword = false;

        public Input_Box(string title)
        {
            InitializeComponent();
            this.Title = title;
            ResponseBox.Focus();
        }

        public Input_Box(string title, string main_instruction, string main_content)
        {
            InitializeComponent();
            this.Title = title;
            MainInstruction = main_instruction;
            MainContent = main_content;
            ResponseBox.Focus();
        }

        public Input_Box(string title, string main_instruction, string main_content, Regex invalid_chars)
        {
            InitializeComponent();
            this.Title = title;
            MainInstruction = main_instruction;
            MainContent = main_content;
            InvalidCharRegex = invalid_chars;
            ResponseBox.Focus();
        }

        public Regex InvalidCharRegex
        {
            set
            {
                invalidCharRegex = value;
            }
        }

        public bool IsPassword
        {
            get
            {
                return isPassword;
            }

            set
            {
                if (value)
                {
                    ResponseBox.Visibility = System.Windows.Visibility.Collapsed;
                    ResponseBox.IsTabStop = false;
                    boxPassword.Visibility = System.Windows.Visibility.Visible;
                    boxPassword.TabIndex = 1;
                    boxPassword.IsTabStop = true;
                    boxPassword.Focus();
                    isPassword = true;
                }
            }
        }

        public string MainInstruction
        {
            set
            {
                tb_MainInstruction.Text = value;
            }
        }

        public string MainContent
        {
            set
            {
                tbMainContent.Text = value;
            }
        }

        public string UserInput
        {
            get
            {
                return Input;
            }
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            string user_text = null;
            if (IsPassword)
                user_text = boxPassword.Password;
            else
                user_text = ResponseBox.Text;

            if (invalidCharRegex != null)
            {
                MatchCollection mc = invalidCharRegex.Matches(user_text);
                if (mc.Count == 0)
                {
                    Input = user_text;
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    MessageBox.Show(LocalizationProvider.GetLocalizedValue<string>("INPUTBOX_InvalidMsg")); //Your input has some invalid characters. Please try again.
                }
            }
            else
            {
                Input = user_text;
                this.DialogResult = true;
                this.Close();
            }
        }
    }
}
