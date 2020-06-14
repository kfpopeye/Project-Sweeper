using System.IO;
using Autodesk.Revit.UI;
//using Infralution.Licensing.WPF;

namespace PKHL.ProjectSweeper
{
    public partial class ProjectSweeper : IExternalApplication
    {
#if MULTILICENSE
//        /// <summary>
//        /// License Validation Parameters copied from License Tracker 
//        /// </summary>
//        const string LICENSE_PARAMETERS =
//    @"<AuthenticatedLicenseParameters>
//	  <EncryptedLicenseParameters>
//	    <ProductName>Project Sweeper</ProductName>
//	    <RSAKeyValue>
//	      <Modulus>se+rT33EYAv7EId8otJZhpcAU8HtX+nbV5QnyvGorFWifO5Y//B4deABgIyYt3PS4rCu9Z7/h7h0bRluq3XJSgzB6RZdho/IGUmB1ua/rr+obdcI2ekdijR/5qP1gocDqKmJwhAaO7fkLYMvQMPDlB3GWdQHUTmtH2AXQRpAVnU=</Modulus>
//	      <Exponent>AQAB</Exponent>
//	    </RSAKeyValue>
//	    <DesignSignature>bocD+o6Iuug3A9EfKcJ2BxtGpgdbxqYUfAr4S2D6KUv8++DoIAFIP/jYFiFe+oG94uhF8hLOrx7U/jRwlUGvaTYQaA1Cda0ncF0j02kH+U51xDr+ZDtfphxewH+cWAFrcu4om5ASb85shfkrL80wTOg5l17PcffSQlRc4pD1D6I=</DesignSignature>
//	    <RuntimeSignature>UZ8qgHE4MfHOAfA8HYziOv1dqOSqqW7nFpaI9IgGnbsCo3+HMVSs+YOIPS72/8ney2reoQ0bcWs28NSqA/ke6SAQfvAEV0CQyKRp/L0xCgojmdWaG99m1ppDk5Eqq+p/OQ7VJ3CZNlbsnoamME1pjhb9DIKldeDZ/LbvtQJAZT4=</RuntimeSignature>
//	    <KeyStrength>7</KeyStrength>
//	    <TextEncoding>Base32</TextEncoding>
//	    <ShortSerialNo>False</ShortSerialNo>
//	  </EncryptedLicenseParameters>
//	  <BlockTerminalServices>True</BlockTerminalServices>
//	  <DisableExpiredLicenses>True</DisableExpiredLicenses>
//	  <AuthenticationServerURL>http://www.pkhlineworks.ca/authenticate/AuthenticationService.asmx</AuthenticationServerURL>
//	  <ServerRSAKeyValue>
//	    <Modulus>xJyXwFmA9K4ZPiAzNlgobbwMPuOfmvwzVox6TSlm6+HXTbC6bMap3mR2LKXlUxZPZVNWIrL4UjAA5wpKKyvw4OGXeOS0e2zL3t7GxdJ2QztLR7+1mnUc0hR/rlPtN6s/cdQaM2bhCG3UGoO0ns/bYB7TqsBCkX/UMvJa54pcQ/U=</Modulus>
//	    <Exponent>AQAB</Exponent>
//	  </ServerRSAKeyValue>
//	</AuthenticatedLicenseParameters>";


//        /// <summary>
//        /// The name of the file to store the license key in - the sub directory is created
//        /// automatically by ILS
//        /// </summary>
//        private static string _licenseFile;

//        /// <summary>
//        /// The license provider to use to validate licenses
//        /// </summary>
//        private static AuthenticatedLicenseProvider _licenseProvider;

//        /// <summary>
//        /// The installed license if any
//        /// </summary>
//        private static AuthenticatedLicense _license;

//        /// <summary>
//        /// Return the currently installed license (if any)
//        /// </summary>
//        public static AuthenticatedLicense License
//        {
//            get { return _license; }
//        }

//        private static void InstallLicense(ref string badLicMess)
//        {
//            System.Diagnostics.Debug.WriteLine("Installing license.");
//            string authKey = null;
//            _licenseProvider = new AuthenticatedLicenseProvider(LICENSE_PARAMETERS, _licenseFile);

//            string userdata = Path.GetDirectoryName(_licenseFile) + @"\user.dat";
//            System.Diagnostics.Debug.WriteLine("Looking for auth: " + userdata);
//            if (File.Exists(userdata))
//            {
//#if DEBUG
//                TaskDialog.Show("Debug mode", "Authentication code is not read from user.dat file in debug mode.");
//                authKey = "R7PK-HFSK-8XQH-6WRM-EURD-JFDR-86";
//#else
//                authKey = File.ReadAllText(userdata);
//#endif
//                try
//                {
//                    _licenseProvider.AtomicAuthenticateAndInstallKeyWithData(
//                        authKey,
//                        System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
//                    _log.Info("Installed license.");
//                }
//                catch (Infralution.Licensing.WPF.AuthenticationsExceededException)
//                {
//                    badLicMess = LocalizationProvider.GetLocalizedValue<string>("LIC_MSG_MaxLic"); //The number of purchased licenses has already been used up. More licenses will need to be purchased before Project Sweeper can be installed on more computers.
//                    _log.Info("Ran out of licenses");
//                }
//                catch (System.Net.WebException err)
//                {
//                    // Revit Taskdialog LIC_DIA001
//                    TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("LIC_DIA001_TItle")); // Licensing error
//                    td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("LIC_DIA001_MainInst"); // Unable to retrieve a license from the server.
//                    td.MainContent = LocalizationProvider.GetLocalizedValue<string>("LIC_DIA001_MainCont"); // The license server could not be reached at this time. Are you connected to the internet?
//                    td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
//                    td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, LocalizationProvider.GetLocalizedValue<string>("LIC_DIA001_Command1")); // Yes, I am connected to the internet. Let me submit a bug report.
//                    td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, LocalizationProvider.GetLocalizedValue<string>("LIC_DIA001_Command2")); // No, I am NOT connected to the internet. Let me connect and try again later.
//                    TaskDialogResult tr = td.Show();
//                    if (tr == TaskDialogResult.CommandLink1)
//                    {
//                        _log.Error("Authorization error", err);
//                        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(err).Throw();
//                        throw;
//                    }
//                    badLicMess = LocalizationProvider.GetLocalizedValue<string>("LIC_MSG_NoSrvr"); //Unable to contact license server.
//                    _log.Info(badLicMess);
//                }
//            }
//            else
//            {
//                badLicMess = LocalizationProvider.GetLocalizedValue<string>("LIC_MSG_NoUserDat"); //Unable to find user.dat file.
//                _log.InfoFormat("Could not find {0}", userdata);
//            }
//        }
#endif

        /// <summary>
        /// Checks to see if user is authorized to access this tool
        /// </summary>
        /// 
        internal static bool UserIsEntitled(ExternalCommandData commandData)
        {
            return true;
        }

//        internal static bool UserIsEntitled(ExternalCommandData commandData)
//        {
//            System.Diagnostics.Debug.WriteLine("Checking entitlement......");
//            bool isAuthorized = false;
//#if MULTILICENSE
//            //multilicense checking here
//            string badLicenseMessage = null;

//            string path = Properties.Settings.Default.AddinPath;
//            int i = path.LastIndexOf('\\');
//            _licenseFile = path.Substring(0, i) + @"\license.lic";
//            System.Diagnostics.Debug.WriteLine("Set license location to: " + _licenseFile);

//            if (!File.Exists(_licenseFile))
//                InstallLicense(ref badLicenseMessage);
//            else
//                _log.InfoFormat("Found license at {0}", _licenseFile);

//            if (_licenseProvider == null)
//                _licenseProvider = new AuthenticatedLicenseProvider(LICENSE_PARAMETERS, _licenseFile);

//            _license = _licenseProvider.GetLicense();
//            isAuthorized = _licenseProvider.IsValid(_license);

//            if (isAuthorized)
//            {
//                try
//                {
//                    System.Diagnostics.Debug.WriteLine("Last authentication: " + _license.LastAuthenticationDate.ToString());
//                    if (_license.LastAuthenticationDate.Value.AddDays(2) < System.DateTime.Today)
//                    {
//                        _license = _licenseProvider.ReauthenticateWithData(
//                            _license,
//                            System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(),
//                            false);
//                        _log.Info("Check license authentication.");
//                    }
//                    isAuthorized = _licenseProvider.IsValid(_license);
//                }
//                catch (LicenseRevokedException)
//                {
//                    isAuthorized = false;
//                    badLicenseMessage = LocalizationProvider.GetLocalizedValue<string>("LIC_MSG_Revoked"); //This license has been revoked.
//                }
//                catch (System.Net.WebException)
//                {
//                    isAuthorized = false;
//                    badLicenseMessage = LocalizationProvider.GetLocalizedValue<string>("LIC_MSG_NoSrvr"); //The license server could not be contacted.
//                }
//            }

//            if (!isAuthorized)
//            {
//                if (badLicenseMessage == null) //will not be null if something went wrong trying to install a license
//                {
//                    if (_license == null)
//                        badLicenseMessage = LocalizationProvider.GetLocalizedValue<string>("LIC_MSG_NotFound"); //No license found.
//                    else
//                    {
//                        _licenseProvider.ValidateLicense(_license);
//                        badLicenseMessage = _licenseProvider.GetLicenseStatusText(_license); //hopefully this return a localized message so I don't need the switch below
//                            //switch (_licenseProvider.ValidateLicense(_license))
//                            //{
//                            //    case AuthenticatedLicenseStatus.Unvalidated:
//                            //        badLicenseMessage = LocalizationProvider.GetLocalizedValue<string>("LIC_MSG_NoUserDat"); //License has not been validated.
//                            //        break;
//                            //    case AuthenticatedLicenseStatus.Unauthenticated:
//                            //        badLicenseMessage = LocalizationProvider.GetLocalizedValue<string>("LIC_MSG_NoUserDat"); //The license has not been authenticated .
//                            //        break;
//                            //    case AuthenticatedLicenseStatus.Valid:
//                            //        badLicenseMessage = LocalizationProvider.GetLocalizedValue<string>("LIC_MSG_NoUserDat"); //The license is valid.
//                            //        break;
//                            //    case AuthenticatedLicenseStatus.Expired:
//                            //        badLicenseMessage = LocalizationProvider.GetLocalizedValue<string>("LIC_MSG_NoUserDat"); //The license has expired.";
//                            //        break;
//                            //    case AuthenticatedLicenseStatus.InvalidComputer:
//                            //        badLicenseMessage = LocalizationProvider.GetLocalizedValue<string>("LIC_MSG_NoUserDat"); //The license is not valid for this computer.";
//                            //        break;
//                            //    case AuthenticatedLicenseStatus.TerminalServicesNotAllowed:
//                            //        badLicenseMessage = LocalizationProvider.GetLocalizedValue<string>("LIC_MSG_NoUserDat"); //The license is not valid for use within Terminal Services.";
//                            //        break;
//                            //    case AuthenticatedLicenseStatus.InvalidProduct:
//                            //        badLicenseMessage = LocalizationProvider.GetLocalizedValue<string>("LIC_MSG_NoUserDat"); //The license key is not for this product.";
//                            //        break;
//                            //    case AuthenticatedLicenseStatus.InvalidKey:
//                            //        badLicenseMessage = LocalizationProvider.GetLocalizedValue<string>("LIC_MSG_NoUserDat"); //The license key does not match the license key parameters.";
//                            //        break;
//                            //    case AuthenticatedLicenseStatus.InvalidSignature:
//                            //        badLicenseMessage = LocalizationProvider.GetLocalizedValue<string>("LIC_MSG_NoUserDat"); //The license contents do not match the signature, indicating possible tampering.";
//                            //        break;
//                            //}
//                    }
//                }
//                _log.Info(badLicenseMessage);
//                System.Diagnostics.Debug.WriteLine(badLicenseMessage);
//            }

//            if (!isAuthorized)
//            {
//                // Revit Taskdialog LIC_DIA002
//                TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("LIC_DIA002_TItle")); // Not authorized
//                td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("LIC_DIA002_MainInst"); //You are not authorized to use this app.
//                td.MainContent = LocalizationProvider.GetLocalizedValue<string>("LIC_DIA002_MainCont"); // Please contact your IT department to ask for access.
//                td.ExpandedContent = badLicenseMessage;
//                td.Show();
//            }

//            return isAuthorized;
//#else
//            if (Properties.Settings.Default.EntCheck.AddDays(7) > System.DateTime.Today)
//                return true;

//            //Check to see if the user is logged in, must be connected to internet.
//            if (!Autodesk.Revit.ApplicationServices.Application.IsLoggedIn)
//            {
//                // Revit Taskdialog LIC_DIA003
//                TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("LIC_DIA003_Title")); // Login
//                td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("LIC_DIA003_MainInst"); // Please login to Autodesk 360 first.
//                td.MainContent = LocalizationProvider.GetLocalizedValue<string>("LIC_DIA003_MainCont");
//                //This application must check if you are authorized to use it. Login to Autodesk 360 using the same account you used to purchase this app. An internet connection is required.
//                td.Show();
//                return false;
//            }

//            string _baseApiUrl = @"https://apps.exchange.autodesk.com/";
//            string _appId = Constants.APP_STORE_ID;
//            UIApplication uiApp = commandData.Application;
//            Autodesk.Revit.ApplicationServices.Application rvtApp = commandData.Application.Application;

//            //Get the user id, and check entitlement
//            string userId = rvtApp.LoginUserId;
//            isAuthorized = pkhCommon.EntitlementHelper.Entitlement(_appId, userId, _baseApiUrl);

//            if (!isAuthorized)
//            {
//                // Revit Taskdialog LIC_DIA004
//                TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("LIC_DIA002_Title")); // Not authorized
//                td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("LIC_DIA002_MainInst"); // You are not authorized to use this app.
//                td.MainContent = LocalizationProvider.GetLocalizedValue<string>("LIC_DIA004_MainCont"); // Make sure you login into Autodesk 360 with the same account you used to buy this app. If the app was purchased under a company account, contact your IT department to allow you access.
//                td.Show();
//                return false;
//            }
//            else
//            {
//                Properties.Settings.Default.EntCheck = System.DateTime.Today;
//                Properties.Settings.Default.Save();
//            }

//            return isAuthorized;
//#endif
//        }
    }
}