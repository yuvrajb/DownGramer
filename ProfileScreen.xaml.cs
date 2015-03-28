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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using InstaAPI.Auth;
using InstaAPI.Endpoints.Authenticated;
using InstaAPI.Endpoints.OptionalParameters;
using InstaAPI.Endpoints.Unauthenticated;
using InstaAPI.Entities;
using DownGramer.App_Data.Entities;

namespace DownGramer
{
    /// <summary>
    /// Interaction logic for ProfileScreen.xaml
    /// </summary>
    public partial class ProfileScreen : MetroWindow
    {
        private LogException MyException = new LogException();

        /// <summary>
        ///     default constructor -- wont beusing in t he final product
        /// </summary>
        public ProfileScreen()
        {
            InitializeComponent();
        }

        /// <summary>
        ///     this constructor called by feeds screen
        /// </summary>
        /// <param name="Config"></param>
        /// <param name="AuthorizedUser"></param>
        /// <param name="UserId"></param>
        public ProfileScreen(InstaConfig Config, AuthUser AuthorisedUser, String UserId, String UserName, FeedsScreen Source)
        {
            this.Config = Config;
            this.AuthorizedUser = AuthorisedUser;
            this.UserId = UserId;
            this.Source = Source;
            this.UserName = UserName;

            InitializeComponent();
            this.Title = "@" + UserName;
            this.LoadOnScrollButton.PreviewMouseLeftButtonDown += Source.LoadOnScrollButton_PreviewMouseDown;
            this.LoadOnScrollButton.PreviewMouseLeftButtonDown += LoadOnScrollButton_PreviewMouseDown;
        }

        /***************************************************************************************************/

        /// <summary>
        ///     change load on scroll
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void LoadOnScrollButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (Source.LoadMoreOnScroll)
                {
                    ChangeLoadMoreOnScrollIcon(Source.LoadMoreOnScroll);
                }
                else
                {
                    ChangeLoadMoreOnScrollIcon(Source.LoadMoreOnScroll);
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
        ///     does taks when loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PScreen_Loaded(object sender, RoutedEventArgs e)
        {
            // set the app version
            this.ApplicationVersion.Content = System.Configuration.ConfigurationManager.AppSettings["Version"];

            // start thread to check for relationship with target user
            Thread CheckRelationShipThread = new Thread(() => CheckRelationShip());
            CheckRelationShipThread.Start();

            // start thread to load user feeds
            Thread LoadUserFeedsThread = new Thread(() => LoadUserPosts(StringNextFeedId));
            LoadUserFeedsThread.SetApartmentState(ApartmentState.STA);
            LoadUserFeedsThread.Start();
        }

        /***************************************************************************************************/

        /// <summary>
        ///     scrollbar event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrollViewerFeeds_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            try
            {
                if (!this.Source.LoadMoreOnScroll)
                {
                    return;
                }
                if ((ScrollViewerFeeds.ScrollableHeight - ScrollViewerFeeds.VerticalOffset <= 50) && !FlagLoadingFeeds && StringNextFeedId != String.Empty)
                {
                    // start thread to load user feeds
                    Thread LoadUserFeedsThread = new Thread(() => LoadUserPosts(StringNextFeedId));
                    LoadUserFeedsThread.SetApartmentState(ApartmentState.STA);
                    LoadUserFeedsThread.Start();
                }
            }
            catch(Exception Ex)
            {
                MyException.EnterLog(Ex);
                //Console.WriteLine(Ex.StackTrace);
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     select for download
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FeedTile_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Tile ClickedTile = (Tile)sender;
                String MediaId = ClickedTile.Name.Substring(1);

                // start a thread to process tile select request
                Thread ProcessTileSelectRequestThread = new Thread(() => ProcessTileSelectRequest(MediaId, ClickedTile));
                ProcessTileSelectRequestThread.SetApartmentState(ApartmentState.STA);
                ProcessTileSelectRequestThread.Start();
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
                //Console.WriteLine(Ex.StackTrace);
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     load more media 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadMoreTile_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // start thread to load user feeds
                Thread LoadUserFeedsThread = new Thread(() => LoadUserPosts(StringNextFeedId));
                LoadUserFeedsThread.SetApartmentState(ApartmentState.STA);
                LoadUserFeedsThread.Start();
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
                //Console.WriteLine(Ex.StackTrace);
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     show in preview
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void FeedTile_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Tile ClickedTile = (Tile)sender;
                String Index = ClickedTile.Title;

                ProfileFlipView.SelectedIndex = Int32.Parse(Index);
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
                //Console.WriteLine(Ex.StackTrace);
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     hit on download button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DownloadButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (SelectedFeeds.Count == 0) // option to download all feeds being diplayed
                {
                    // start a thread to select all the feeds on the screen
                    Thread ConfirmAllDownloadThread = new Thread(() => ConfirmAllDownload(WrapPanelFeeds));
                    ConfirmAllDownloadThread.SetApartmentState(ApartmentState.STA);
                    ConfirmAllDownloadThread.Start();
                }
                else // download selected feeds
                {
                    // start a thread to show confirmation dialog for starting a download for selected feeds.
                    Thread ConfirmDownloadThread = new Thread(() => ConfirmSelectedDownload(DeepCopy<String>(SelectedFeeds), CanvasFeeds));
                    ConfirmDownloadThread.SetApartmentState(ApartmentState.STA);
                    ConfirmDownloadThread.Start();
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
        ///     manage size
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PScreen_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                var ParentWidth = 1329.00;

                if (!PScreen.IsLoaded)
                {
                    //ParentWidth -= 133;
                }
                else
                {
                    ParentWidth = PScreen.ActualWidth;
                }

                if (ParentWidth <= 640 + 142)
                {
                    return;
                }

                var ComputedCanvasWidth = ParentWidth;
                var TilesCount = (double)Math.Floor((ComputedCanvasWidth - 640) / 154);
                var WrapPanelWidth = TilesCount * 154;
                var CanvasWidth = WrapPanelWidth + 640;
                var WindowWidth = CanvasWidth + 146;

                Dispatcher.Invoke(
                    new Action(
                        delegate()
                        {
                            // from
                            PScreen.Width = WindowWidth;

                            // canvas
                            CanvasCurtain.Width = CanvasWidth;
                            CanvasFeeds.Width = CanvasCurtain.Width - 640;
                            WrapPanelFeeds.Width = WrapPanelWidth;
                            ProgressRingFeeds.Margin = new Thickness((CanvasFeeds.Width - 60) / 2, (ScrollViewerFeeds.Height - 60) / 2, 0, 0);
                        }), null);
            }
            catch (Exception Ex)
            {
                //Console.WriteLine(Ex.Message);
            }
        }
    }
}
