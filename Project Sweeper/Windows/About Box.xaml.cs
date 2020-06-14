using System;
using System.Windows;
using System.ComponentModel;
using System.Reflection;
using System.Net;
using System.IO;
using System.Xml;

namespace PKHL.ProjectSweeper
{
    /// <summary>
    /// Interaction logic for AboutBox.xaml
    /// </summary>
    public partial class AboutBox : Window
    {
        private string AppVersion = null;
        private Assembly TheAssembly = null;
        private readonly string NotListed = LocalizationProvider.GetLocalizedValue<string>("ABOUT_RSLT_Txt1");
        private readonly string NewVersion = LocalizationProvider.GetLocalizedValue<string>("ABOUT_RSLT_Txt2");
        private readonly string Up2Date = LocalizationProvider.GetLocalizedValue<string>("ABOUT_RSLT_Txt3");
        private readonly string NoServer = LocalizationProvider.GetLocalizedValue<string>("ABOUT_RSLT_Txt4");

        public AboutBox(Assembly a)
        {
            InitializeComponent();

            TheAssembly = a;
            this.Title = LocalizationProvider.GetLocalizedValue<string>("ABOUT_Title");
            this.labelProductName.Text = AssemblyProduct;
            this.labelVersion.Text = String.Format("Version {0}.{1}.{2}.{3}", MajorVersion, MinorVersion, Build, Revision);
            this.labelCopyright.Text = AssemblyCopyright;
            this.labelCompanyName.Text = AssemblyCompany;
            AppVersion = Build;
            this.textBoxDescription.Text = LocalizationProvider.GetLocalizedValue<string>("ABOUT_Desc_Txt1"); //Checking for updates....
            setLogo();
        }

        /// <summary>
        /// Used for unlicensed multilicense apps only.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="companyName"></param>
        //public AboutBox(Assembly a, string companyName, string hardwareId)
        //{
        //    InitializeComponent();

        //    TheAssembly = a;
        //    setLogo();
        //    this.Title = LocalizationProvider.GetLocalizedValue<string>("ABOUT_Title") + " ML";
        //    this.labelProductName.Text = AssemblyProduct + " ML";
        //    this.labelVersion.Text = String.Format("Version {0}.{1}.{2}.{3}", MajorVersion, MinorVersion, Build, Revision);
        //    this.labelCopyright.Text = AssemblyCopyright;
        //    this.labelCompanyName.Text = AssemblyCompany;
        //    AppVersion = Build;
        //    if(companyName != null)
        //        this.textBoxDescription.Text = LocalizationProvider.GetLocalizedValue<string>("ABOUT_Desc_Txt2") + " " + companyName + "\n"; //This software is licensed to
        //    else
        //        this.textBoxDescription.Text = LocalizationProvider.GetLocalizedValue<string>("ABOUT_Desc_Txt3") + "\n"; //This software is not licensed.
        //    this.textBoxDescription.AppendText(string.Format("Computer: {0} ({1})", Environment.MachineName, hardwareId));
        //    this.textBoxDescription.AppendText("\n\n" + LocalizationProvider.GetLocalizedValue<string>("ABOUT_Desc_Txt1")); //Checking for updates....
        //}

        /// <summary>
        /// Used for Infralution multilicense apps only.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="companyName"></param>
        //public AboutBox(Assembly a, string companyName, string hardwareId, string expires)
        //{
        //    InitializeComponent();

        //    TheAssembly = a;
        //    setLogo();
        //    this.Title = LocalizationProvider.GetLocalizedValue<string>("ABOUT_Title") + " ML";
        //    this.labelProductName.Text = AssemblyProduct + " ML";
        //    this.labelVersion.Text = String.Format("Version {0}.{1}.{2}.{3}", MajorVersion, MinorVersion, Build, Revision);
        //    this.labelCopyright.Text = AssemblyCopyright;
        //    this.labelCompanyName.Text = AssemblyCompany;
        //    AppVersion = Build;
        //    if (companyName != null)
        //        this.textBoxDescription.Text = LocalizationProvider.GetLocalizedValue<string>("ABOUT_Desc_Txt2") + " " + companyName; //This software is licensed to
        //    else
        //        this.textBoxDescription.Text = LocalizationProvider.GetLocalizedValue<string>("ABOUT_Desc_Txt3"); //This software is not licensed.
        //    this.textBoxDescription.AppendText(string.Format("\nComputer: {0}", hardwareId));
        //    this.textBoxDescription.AppendText(string.Format("\nLicense expires: {0}", expires));
        //    this.textBoxDescription.AppendText("\n\n" + LocalizationProvider.GetLocalizedValue<string>("ABOUT_Desc_Txt1")); //Checking for updates....
        //}

        #region Assembly Attribute Accessors

        public string AssemblyTitle
        {
            get
            {
                object[] attributes = TheAssembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
                    {
                        return titleAttribute.Title;
                    }
                }
                return System.IO.Path.GetFileNameWithoutExtension(TheAssembly.CodeBase);
            }
        }

        public string AssemblyVersion
        {
            get
            {
                return TheAssembly.GetName().Version.ToString();
            }
        }

        public string MajorVersion
        {
            get
            {
                return TheAssembly.GetName().Version.Major.ToString();
            }
        }

        public string MinorVersion
        {
            get
            {
                return TheAssembly.GetName().Version.Minor.ToString();
            }
        }

        public string Build
        {
            get
            {
                return TheAssembly.GetName().Version.Build.ToString();
            }
        }

        public string Revision
        {
            get
            {
                return TheAssembly.GetName().Version.Revision.ToString();
            }
        }

        public string AssemblyDescription
        {
            get
            {
                object[] attributes = TheAssembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        public string AssemblyProduct
        {
            get
            {
                object[] attributes = TheAssembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        public string AssemblyCopyright
        {
            get
            {
                object[] attributes = TheAssembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        public string AssemblyCompany
        {
            get
            {
                object[] attributes = TheAssembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }
        #endregion

        private void setLogo()
        {
            Stream s = TheAssembly.GetManifestResourceStream("PKHL.ProjectSweeper.Resources.pkh logo vertical.jpg");
            System.Windows.Media.Imaging.BitmapImage img = new System.Windows.Media.Imaging.BitmapImage();
            img.BeginInit();
            img.StreamSource = s;
            img.EndInit();
            logoImage.Source = img;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
            TestButton.Visibility = Visibility.Visible;
            this.Title += " - Debug Mode";
#endif
            //BackgroundWorker worker = new BackgroundWorker();
            //worker.DoWork += worker_DoWork;
            //worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            //worker.RunWorkerAsync();
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create("http://www.pkhlineworks.ca/softwareversions.xml");
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                // Gets the stream associated with the response.
                Stream receiveStream = myHttpWebResponse.GetResponseStream();

                //Major-Revit version   Minor-App version   MajorRevision-App release version    MinorRevision-not used
                //AssemblyTitle must be as it appears in softwareversions.xml
                XmlReader Xread = XmlReader.Create(receiveStream);
                string appSymbol = null;
                appSymbol = (AssemblyTitle + MajorVersion + "_V" + MinorVersion).Replace(" ","_");
                if (Xread.ReadToFollowing(appSymbol))
                    System.Diagnostics.Debug.WriteLine(string.Format("Found symbol: {0}", appSymbol));
                else
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("Did not find symbol: {0}", appSymbol));
                    e.Result = NotListed; //Could not find this software listed in updates.
                    myHttpWebResponse.Close();
                    receiveStream.Close();
                    return;
                }

                int ver = Xread.ReadElementContentAsInt();
                int thisVer = Convert.ToInt32(AppVersion);

                // Releases the resources of the response.
                myHttpWebResponse.Close();
                // Releases the resources of the Stream.
                receiveStream.Close();

                if (ver > thisVer)
                    e.Result = (string.Format(NewVersion, AssemblyTitle, ver.ToString())); //A newer version of {0} is available. Version {1} is available at www.pkhlineworks.ca.
                else
                    e.Result = Up2Date; //Your product is up to date.
            }
            catch (Exception)
            {
                e.Result = NoServer; //Could not contact server to check for updates.
            }
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.textBoxDescription.AppendText("\n");
            this.textBoxDescription.AppendText(e.Result.ToString());
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Exception err = new Exception("Test exception created.");
            err.Data.Add("test data", "my test data in exception");
            throw err;
        }
    }
}
