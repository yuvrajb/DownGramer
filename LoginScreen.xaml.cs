using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Net;
using System.IO;
using System.Threading;
using System.Drawing;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.Xml.Serialization;

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using DownGramer.App_Data.Instagram;
using InstaAPI.Auth;
using InstaAPI.Entities;
using DownGramer.App_Data.Entities;

namespace DownGramer
{
    /// <summary>
    /// Interaction logic for LoginScreen.xaml
    /// </summary>
    public partial class LoginScreen : MetroWindow
    {
        private InstaConfig Config;
        private OAuth Auth;
        private AuthUser AuthorizedUser;
        private String FilePathBase = String.Empty;
        private LogException MyException = new LogException();

        /// <summary>
        ///     default constructor
        /// </summary>
        public LoginScreen()
        {
            InitializeComponent();  
        }

        /***************************************************************************************************/

        /// <summary>
        ///     most popular media at the moment to be fetched
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CheckForAppFolders(); // create all folders
            CheckForAppFiles(); // check for files

            // set the app version
            this.ApplicationVersion.Content = System.Configuration.ConfigurationManager.AppSettings["Version"];

            // start a thread for loading popular feeds
            Thread LoadPopularFeedsThread = new Thread(() => LoadPopularFeeds());
            LoadPopularFeedsThread.SetApartmentState(ApartmentState.STA);
            LoadPopularFeedsThread.Start();

            // start a thread for checking existing auth key
            Thread SearchForExistingTokensThread = new Thread(() => SearchForExistingTokens());
            SearchForExistingTokensThread.SetApartmentState(ApartmentState.STA);
            SearchForExistingTokensThread.Start();
        }

        /***************************************************************************************************/

        /// <summary>
        ///     makes sure all the folders are in place
        /// </summary>
        private void CheckForAppFolders()
        {
            try
            {
                String PFFolder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                PFFolder += @"\DownGramer";

                if (!Directory.Exists(PFFolder))
                {
                    // create dir
                    Directory.CreateDirectory(PFFolder);

                    // change dir
                    Directory.SetCurrentDirectory(PFFolder);

                    // create dirs
                    Directory.CreateDirectory("Config");
                    Directory.CreateDirectory("Data");
                    Directory.CreateDirectory("Downloads");
                    Directory.CreateDirectory("Settings");

                    // change dir
                    Directory.SetCurrentDirectory(PFFolder + @"\Downloads");

                    // create dirs
                    Directory.CreateDirectory("Images");
                    Directory.CreateDirectory("Videos");
                }
                else
                {
                    // change dir
                    Directory.SetCurrentDirectory(PFFolder);

                    if (!Directory.Exists(PFFolder + @"\Config"))
                    {
                        Directory.CreateDirectory("Config"); // create dir
                    }
                    if (!Directory.Exists(PFFolder + @"\Data"))
                    {
                        Directory.CreateDirectory("Data"); // create dir
                    }
                    if (!Directory.Exists(PFFolder + @"\Downloads"))
                    {
                        Directory.CreateDirectory("Downloads"); // create dir
                    }
                    if (!Directory.Exists(PFFolder + @"\Settings"))
                    {
                        Directory.CreateDirectory("Settings"); // create dir
                    }

                    if (Directory.Exists(PFFolder + @"\Downloads"))
                    {
                        Directory.SetCurrentDirectory(PFFolder + @"\Downloads");

                        if (!Directory.Exists("Images"))
                        {
                            Directory.CreateDirectory("Images");
                        }
                        if (!Directory.Exists("Videos"))
                        {
                            Directory.CreateDirectory("Videos");
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
                //Console.WriteLine(Ex.StackTrace);
            }
        }

        /// <summary>
        ///     makes sure all the files are in place
        /// </summary>
        private void CheckForAppFiles()
        {
            CheckAuthFile(); // check auth.bin
            CheckSettingsFile(); // check app settings
        }

        /// <summary>
        ///     checks for auth file
        ///     if exists then checks for integrity
        /// </summary>
        private void CheckAuthFile()
        {
            FileStream AuthFile = null;
            String AuthFilePath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            AuthFilePath += @"\DownGramer\Config\";
            try
            {
                if (File.Exists(AuthFilePath + "auth.bin"))
                {
                    AuthFile = File.Open(AuthFilePath + "auth.bin", FileMode.Open, FileAccess.Read);
                    BinaryFormatter BFormatter = new BinaryFormatter();
                    AuthUser AuthUser = (AuthUser)BFormatter.Deserialize(AuthFile);
                }
            }
            catch (SerializationException)
            {
                // deletes the file
                AuthFile.Close();
                File.Delete(AuthFilePath + "auth.bin");
            }
            finally
            {
                FilePathBase = AuthFilePath + "auth.bin";
                if (AuthFile != null) 
                {
                    AuthFile.Close();
                }
            }
        }

        /// <summary>
        ///     check for settings file
        /// </summary>
        private void CheckSettingsFile()
        {
            try
            {
                FileStream SettingsFile = null;
                String SettingsFilePath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                SettingsFilePath += @"\DownGramer\Settings\";
                try
                {
                    if (File.Exists(SettingsFilePath + "settings.xml"))
                    {
                        SettingsFile = File.Open(SettingsFilePath + "settings.xml", FileMode.Open, FileAccess.Read);

                        XmlSerializer Ser = new XmlSerializer(typeof(AppSettings));
                        AppSettings Settings = (AppSettings)Ser.Deserialize(SettingsFile);

                        Boolean NotExists = false;
                        if (!Directory.Exists(Settings.DefaultImagePath))
                        {
                            Settings.DefaultImagePath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\DownGramer\Downloads\Images\";
                            NotExists = true;
                        }
                        if (!Directory.Exists(Settings.DefaultVideoPath))
                        {
                            Settings.DefaultVideoPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\DownGramer\Downloads\Videos\";
                            NotExists = true;
                        }

                        if (NotExists)
                        {
                            SettingsFile.Close();
                            SettingsFile = File.Open(SettingsFilePath + "settings.xml", FileMode.Create, FileAccess.Write);
                            Ser.Serialize(SettingsFile, Settings);
                        }
                    }
                    else // create new settings file
                    {
                        SettingsFile = File.Open(SettingsFilePath + "settings.xml", FileMode.Create, FileAccess.Write);
                        XmlSerializer Ser = new XmlSerializer(typeof(AppSettings));
                        Ser.Serialize(SettingsFile, new AppSettings());
                    }
                }
                catch (SerializationException)
                {
                    SettingsFile.Close();
                    File.Delete(SettingsFilePath + "settings.xml");
                    SettingsFile = File.Open(SettingsFilePath + "settings.xml", FileMode.Create, FileAccess.Write);
                    XmlSerializer Ser = new XmlSerializer(typeof(AppSettings));
                    Ser.Serialize(SettingsFile, new AppSettings());
                }
                catch (Exception Ex)
                {
                    MyException.EnterLog(Ex);
                }
                finally
                {
                    if (SettingsFile != null)
                    {
                        SettingsFile.Close();
                    }
                }
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
                //Console.WriteLine(Ex.StackTrace);
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     processes and loads the feed in the panel
        /// </summary>
        private void LoadPopularFeeds()
        {
            try
            {
                DownGramer.App_Data.Instagram.Application App = new DownGramer.App_Data.Instagram.Application();
                Config = App.GetInstaConfig();

                List<String> PopFeeds = App.GetPopularFeed();
                
                // hide the progress ring
                ThreadStart th = new ThreadStart(
                        delegate()
                        {
                            Dispatcher.Invoke(
                                new Action(
                                    delegate()
                                    {
                                        PopularFeedsContainer.Children.Remove(PRing);
                                    }), null);
                        });
                new Thread(th).Start();

                // parse each url string to fetch images
                foreach (var x in PopFeeds)
                {
                    try
                    {
                        // use of dispatcher is important for ui changes
                        ThreadStart TStart = new ThreadStart(
                            delegate()
                            {
                                Dispatcher.Invoke(
                                    new Action(
                                        delegate()
                                        {
                                            Tile Feed = new Tile();
                                            Feed.Width = 120;
                                            Feed.Height = 120;
                                            Feed.IsEnabled = false;

                                            ImageBrush IB = new ImageBrush();
                                            IB.ImageSource = new BitmapImage(new Uri(x));
                                            Feed.Background = IB;

                                            PopularFeedsContainer.Children.Add(Feed);
                                        }), null);
                            });
                        Thread Thr = new Thread(TStart);
                        Thr.Start();
                        Thr.Join();
                    }
                    catch (Exception Ex)
                    {
                        MyException.EnterLog(Ex);
                        //Console.WriteLine(Ex.StackTrace);
                    }
                }
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
                //Console.WriteLine(Ex.StackTrace);
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     searches for existing saved tokens in the system
        /// </summary>
        private void SearchForExistingTokens()
        {
            try
            {
                // check for existing file and act accordingly
                if(File.Exists(FilePathBase))
                {
                    // validate the serialized file
                    FileStream FStream = File.Open(FilePathBase, FileMode.Open, FileAccess.Read);
                    BinaryFormatter BFormatter = new BinaryFormatter();
                    try
                    {
                        // serialize
                        AuthorizedUser = (AuthUser)BFormatter.Deserialize(FStream);
                        FStream.Close();
                        String UserHandle = AuthorizedUser.UserName;

                        // update ui
                        ThreadStart ThrS = new ThreadStart(
                            delegate()
                            {
                                Dispatcher.Invoke(
                                    new Action(
                                    delegate()
                                    {
                                        LabelStatus.Content = "Access Token found for @" + UserHandle + ". You can continue with this or re-authorize the app with new user!";
                                        ButtonAuthenticated.Visibility = System.Windows.Visibility.Visible;
                                    }), null);
                            });
                        new Thread(ThrS).Start();
                    }
                    catch (Exception )
                    {
                        // delete file
                        //File.Delete(FilePathBase);
                        
                        // update ui
                        ThreadStart ThrS = new ThreadStart(
                            delegate()
                            {
                                Dispatcher.Invoke(
                                    new Action(
                                        delegate()
                                        {
                                            LabelStatus.Content = "Access Token not found. You will need to authorize the application to continue!";
                                        }), null);
                            });
                        new Thread(ThrS).Start();
                    }
                    
                }
                else
                {
                     // update ui
                    ThreadStart ThrS = new ThreadStart(
                        delegate(){
                            Dispatcher.Invoke(
                                new Action(
                                    delegate()
                                    {
                                        LabelStatus.Content = "Access Token not found. You will need to authorize the application to continue!";
                                    }), null);
                        });
                    new Thread(ThrS).Start();
                }
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
                //Console.WriteLine(Ex.StackTrace);
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     verifies the code entered by the user
        /// </summary>
        /// <param name="Code"></param>
        /// <param name="ProgressBarControl"></param>
        private async void ValidateCodeAsync(String Code, ProgressDialogController ProgressBarControl)
        {
            int ResponseCode = 0;

            try
            {
                Auth = new OAuth(Config, Code);
                AuthorizedUser = Auth.GetAuhtorisedUser();
                ResponseCode = AuthorizedUser.Meta.Code;
                
                if (ResponseCode != 200)
                {
                    // hide progress dialog
                    await ProgressBarControl.CloseAsync();

                    // show message dialog
                    ThreadStart Thrs = new ThreadStart(
                        delegate()
                        {
                            Dispatcher.Invoke(
                                new Action(
                                    async delegate()
                                    {
                                        var DialogResponse = await this.ShowMessageAsync("Error", "code verification failed. re-authorize the app and try again!", MessageDialogStyle.Affirmative, null);
                                    }), null);
                        });
                    new Thread(Thrs).Start();
                }
                else if (ResponseCode == 200)
                {
                    // hide progress dialog
                    await ProgressBarControl.CloseAsync();
                    
                    // show message dialog
                    ThreadStart ThrS = new ThreadStart(
                        delegate()
                        {
                            Dispatcher.Invoke(
                                new Action(
                                    async delegate()
                                    {
                                        var DialogResponse = await this.ShowMessageAsync("Success", "let's get started; b/w do you want to save the tokens?", MessageDialogStyle.AffirmativeAndNegative, null);
                                        if (DialogResponse == MessageDialogResult.Affirmative)
                                        {
                                            try
                                            {
                                                // serialize authorized user object and save as auth.bin
                                                FileStream FStream = File.Open(FilePathBase, FileMode.Create, FileAccess.Write);
                                                BinaryFormatter BFormatter = new BinaryFormatter();
                                                BFormatter.Serialize(FStream, AuthorizedUser);
                                                FStream.Close();

                                                // redirect to feeds window
                                                this.Hide();
                                                new FeedsScreen(Config, AuthorizedUser).Visibility = System.Windows.Visibility.Visible;
                                            }
                                            catch (Exception Ex)
                                            {
                                                MyException.EnterLog(Ex);
                                                //Console.WriteLine(Ex.StackTrace);
                                            }
                                        }
                                        else
                                        {
                                            try
                                            {
                                                // delete previous tokens if any
                                                if (File.Exists(FilePathBase))
                                                {
                                                    File.Delete(FilePathBase);
                                                }

                                                // redirect to feeds window
                                            }
                                            catch (Exception Ex)
                                            {
                                                MyException.EnterLog(Ex);
                                                //Console.WriteLine(Ex.StackTrace);
                                            }
                                        }
                                    }), null);
                        });
                    new Thread(ThrS).Start();
                }
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     various proceses on authorize button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButtonAuthorize_Click(object sender, RoutedEventArgs e)
        {
            // fetch the authentication url and open browser
            String AuthenticationUriString = Config.GetAuthenticationUriString();
            System.Diagnostics.Process.Start(AuthenticationUriString);

            // open input dialog box
            var Code = await this.ShowInputAsync("Authentication Code", "enter the code that you received!", null);
            // user did not cancel or did not give empty input
            if (Code != null) 
            {
                var ProgressBarControl = await this.ShowProgressAsync("Verifying", "sit tight! running some checks with instagram!", false, null);
                new Thread(() => ValidateCodeAsync(Code, ProgressBarControl)).Start(); 
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     open LoginScreen if tokens are found
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonAuthenticated_Click(object sender, RoutedEventArgs e)
        {
            // redirect to feeds windows
            this.Hide();
            new FeedsScreen(Config, AuthorizedUser).Visibility = System.Windows.Visibility.Visible;
        }

        /// <summary>
        ///     visit developer site
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BUttonPromo_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("http://yuvrajbabrah.host22.com");
        }

        /// <summary>
        ///     visit help
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonHelp_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("http://downgramer.netai.net/#two");
        }

        /***************************************************************************************************/
    }
}
