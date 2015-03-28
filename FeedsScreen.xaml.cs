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
using System.Xml.Serialization;

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using InstaAPI.Auth;
using InstaAPI.Endpoints.Authenticated;
using InstaAPI.Endpoints.OptionalParameters;
using InstaAPI.Endpoints.Unauthenticated;
using InstaAPI.Entities;
using DownGramer.Download;
using DownGramer.App_Data.Entities;
using System.ComponentModel;

namespace DownGramer
{
    /// <summary>
    /// Interaction logic for FeedsScreen.xaml
    /// </summary>
    public partial class FeedsScreen : MetroWindow
    {
        private InstaConfig Config;
        private AuthUser AuthorizedUser;
        private LogException MyException = new LogException();

        /// <summary>
        ///     default constructor
        ///     won't be using it in the final product
        /// </summary>
        public FeedsScreen()
        {
            InitializeComponent();
        }

        /***************************************************************************************************/

        /// <summary>
        ///     this constructor called by LoginScreen
        /// </summary>
        /// <param name="Config"></param>
        /// <param name="AuthorizedUser"></param>
        public FeedsScreen(InstaConfig Config, AuthUser AuthorizedUser)
        {
            this.Config = Config;
            this.AuthorizedUser = AuthorizedUser;

            InitializeComponent();

            // set title
            this.Title = AuthorizedUser.UserName;
            this.WPDownloads = WrapPanelDownloads;
        }

        /***************************************************************************************************/

        /// <summary>
        ///     load necessary items
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FScreen_Loaded(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow = this;

            // set the app version
            this.ApplicationVersion.Content = System.Configuration.ConfigurationManager.AppSettings["Version"];

            // sets up app according to the settings file
            SetUpApp();

            // start a thread for checking new update
            Thread UpdateCheckThread = new Thread(() => UpdateCheck());
            UpdateCheckThread.SetApartmentState(ApartmentState.STA);
            UpdateCheckThread.Start();

            // start a thread for loading user's feed
            Thread LoadUserFeedThread = new Thread(() => LoadUserFeed(String.Empty));
            LoadUserFeedThread.SetApartmentState(ApartmentState.STA);
            LoadUserFeedThread.Start();

            // start a thread for loading popular media
            Thread LoadPopularThread = new Thread(() => LoadPopular());
            LoadPopularThread.SetApartmentState(ApartmentState.STA);
            LoadPopularThread.Start();

            // start a thread for loading authorized user uploads
            Thread LoadUploadsThread = new Thread(() => LoadUploads(AuthorizedUser.UserId, String.Empty));
            LoadUploadsThread.SetApartmentState(ApartmentState.STA);
            LoadUploadsThread.Start();

            // start a thread for loading favorite user medias
            Thread LoadFavoritesThread = new Thread(() => LoadFavorites());
            LoadFavoritesThread.SetApartmentState(ApartmentState.STA);
            LoadFavoritesThread.Start();
        }

        /***************************************************************************************************/

        /// <summary>
        ///     handle click events on menu labels
        ///     hides wrap layouts
        ///     enables/disables ButtonLoadMore
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LabelMenu_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                String LabelName = ((Label)sender).Name;
                // override hiding of screen if search has been selected
                if (LabelName.Equals("LabelSearch"))
                {
                    ShowCanvasSearch();
                    return;
                }
                if (LabelName.Equals("LabelShowFavs"))
                {
                    if (FlagShowingFavsList)
                    {
                        FlagShowingFavsList = false;
                        Dispatcher.Invoke(
                            new Action(
                                delegate()
                                {
                                    ((Label)sender).Content = "SHOW FAVS";
                                }), null);
                        CollapseCanvasFavoritesList();
                    }
                    else
                    {
                        FlagShowingFavsList = true;
                        Dispatcher.Invoke(
                            new Action(
                                delegate()
                                {
                                    ((Label)sender).Content = "HIDE FAVS";
                                }), null);
                        ShowCanvasFavoritesList();
                        new Thread(ScanFavoritesAndDisplayonFavoritesList).Start();
                    }
                    return;
                }

                // hide all the wrap layouts in ScrollViewer
                CollapseCanvasFeeds();
                CollapseCanvasPopular();
                CollapseCanvasUploads();
                CollapseCanvasFavorites();
                CollapseCanvasDownloads();
                CollapseLabelShowFavs();
                CollapseCanvasFavoritesList();

                // make menu label light
                MakeMenuLabelNormal();

                if (LabelName.Equals("LabelFeed"))
                {
                    ShowCanvasFeeds();
                    if (!FlagLoadingFeeds && StringNextFeedId != String.Empty)
                    {
                        EnableLoadMoreButton();
                    }
                    else
                    {
                        DisableLoadMoreButton();
                    }
                    MakeLabelBold((Label)sender);
                }
                if (LabelName.Equals("LabelPopular"))
                {
                    ShowCanvasPopular();
                    if (!FlagLoadingPopular)
                    {
                        EnableLoadMoreButton();
                    }
                    else
                    {
                        DisableLoadMoreButton();
                    }
                    MakeLabelBold((Label)sender);
                }
                if (LabelName.Equals("LabelUploads"))
                {
                    ShowCanvasUploads();
                    if (!FlagLoadingUploads && StringNextUploadId != String.Empty)
                    {
                        EnableLoadMoreButton();
                    }
                    else
                    {
                        DisableLoadMoreButton();
                    }
                    MakeLabelBold((Label)sender);
                }
                if (LabelName.Equals("LabelFavorites"))
                {
                    ShowCanvasFavorites();
                    ShowLabelShowFavs();

                    if (LabelShowFavs.Content.ToString().Equals("HIDE FAVS"))
                    {
                        ShowCanvasFavoritesList();
                    }
                    if (FlagNoMoreLoads)
                    {
                        DisableLoadMoreButton();
                    }
                    else
                    {
                        EnableLoadMoreButton();
                    }
                    MakeLabelBold((Label)sender);
                }
                if (LabelName.Equals("LabelDownloads"))
                {
                    ShowCanvasDownloads();
                    DisableLoadMoreButton();
                    MakeLabelBold((Label)sender);
                }
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     perform appropiate tasks when load more button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonLoadMore_Click(object sender, RoutedEventArgs e)
        {
            // load feeds
            if (CanvasFeeds.IsVisible)
            {
                if (!FlagLoadingFeeds && StringNextFeedId != String.Empty)
                {
                    // start thread
                    Thread LoadUserFeedThread = new Thread(() => LoadUserFeed(StringNextFeedId));
                    LoadUserFeedThread.SetApartmentState(ApartmentState.STA);
                    LoadUserFeedThread.Start();
                }
            }
            // load popular
            if (CanvasPopular.IsVisible)
            {
                if (!FlagLoadingPopular)
                {
                    // start thread
                    Thread LoadPopularThread = new Thread(() => LoadPopular());
                    LoadPopularThread.SetApartmentState(ApartmentState.STA);
                    LoadPopularThread.Start();
                }
            }
            // load uploads
            if (CanvasUploads.IsVisible)
            {
                if (!FlagLoadingUploads)
                {
                    // start thread
                    Thread LoadUploadsThread = new Thread(() => LoadUploads(AuthorizedUser.UserId, StringNextUploadId));
                    LoadUploadsThread.SetApartmentState(ApartmentState.STA);
                    LoadUploadsThread.Start();
                }
            }
            // load favs
            if (CanvasFavorites.IsVisible)
            {
                if (!FlagLoadingFavorites)
                {
                    // start thread
                    Thread LoadFavoritesThread = new Thread(() => LoadFavorites());
                    LoadFavoritesThread.SetApartmentState(ApartmentState.STA);
                    LoadFavoritesThread.Start();
                }
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     shuts down the application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FScreen_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // shut down the application
            Environment.Exit(0);
        }

        /*************************************************** TILE EVENTS ************************************************************/

        /// <summary>
        ///     double click on tile
        ///     open standard resolution photos
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FeedTile_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // fetch image id form control name
                String ImageId = ((Tile)sender).Name;
                ImageId = ImageId.Substring(1);

                // open image viewer
                new Image(Config, AuthorizedUser, ImageId).Visibility = System.Windows.Visibility.Visible;
            }
            catch (Exception Ex)
            {
                //Console.WriteLine(Ex.StackTrace);
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     select or deselect the feed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void FeedTile_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Tile TileClicked = (Tile)sender;
                String TileName = TileClicked.Name;
                String MediaId = TileName.Substring(1);

                // start a thread to process the request
                Thread ProcessTileSelectRequestThread = new Thread(() => ProcessTileSelectRequest(MediaId, TileClicked));
                ProcessTileSelectRequestThread.SetApartmentState(ApartmentState.STA);
                ProcessTileSelectRequestThread.Start();
            }
            catch (Exception Ex)
            {
                //Console.WriteLine(Ex.StackTrace);
            }
        }

        /***************************************************************************************************/

        private void FeedTile_MouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                //Storyboard Story = new Storyboard();

                //DoubleAnimation ReduceOpacity = new DoubleAnimation(1.0, 0.5, new Duration(TimeSpan.FromSeconds(0.5)));
                //Story.Children.Add(ReduceOpacity);
                //Storyboard.SetTargetName(ReduceOpacity, ((Tile)sender).Name);
                //Storyboard.SetTargetProperty(ReduceOpacity, new PropertyPath(Tile.OpacityProperty));

                //this.CanvasFeeds.RegisterName(((Tile)sender).Name, sender);

                //Story.Begin(this.CanvasFeeds);
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
                //Console.WriteLine(Ex.StackTrace);
            }
        }

        /*************************************************** DOWNLOAD EVENTS ************************************************************/

        /// <summary>
        ///     handle query for download confirmation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DownloadButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (CanvasFeeds.IsVisible) // download feeds
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
                if (CanvasUploads.IsVisible) // download uploads
                {
                    if (SelectedUploads.Count == 0) // option to download all uploads being diplayed
                    {
                        // start a thread to select all the feeds on the screen
                        Thread ConfirmAllDownloadThread = new Thread(() => ConfirmAllDownload(WrapPanelUploads));
                        ConfirmAllDownloadThread.SetApartmentState(ApartmentState.STA);
                        ConfirmAllDownloadThread.Start();
                    }
                    else // download selected feeds
                    {
                        // start a thread to show confirmation dialog for starting a download for selected uploads.
                        Thread ConfirmDownloadThread = new Thread(() => ConfirmSelectedDownload(DeepCopy<String>(SelectedUploads), CanvasUploads));
                        ConfirmDownloadThread.SetApartmentState(ApartmentState.STA);
                        ConfirmDownloadThread.Start();
                    }
                }
                if (CanvasPopular.IsVisible) // download popular
                {
                    if (SelectedPopular.Count == 0) // option to download all popular media being diplayed
                    {
                        // start a thread to select all the feeds on the screen
                        Thread ConfirmAllDownloadThread = new Thread(() => ConfirmAllDownload(WrapPanelPopular));
                        ConfirmAllDownloadThread.SetApartmentState(ApartmentState.STA);
                        ConfirmAllDownloadThread.Start();
                    }
                    else // download selected feeds
                    {
                        // start a thread to show confirmation dialog for starting a download for selected popular media.
                        Thread ConfirmDownloadThread = new Thread(() => ConfirmSelectedDownload(DeepCopy<String>(SelectedPopular), CanvasPopular));
                        ConfirmDownloadThread.SetApartmentState(ApartmentState.STA);
                        ConfirmDownloadThread.Start();
                    }
                }
                if (CanvasFavorites.IsVisible) // download favorites
                {
                    if (SelectedFavorites.Count == 0) // option to download all popular media being diplayed
                    {
                        // start a thread to select all the feeds on the screen
                        Thread ConfirmAllDownloadThread = new Thread(() => ConfirmAllDownload(WrapPanelFavorites));
                        ConfirmAllDownloadThread.SetApartmentState(ApartmentState.STA);
                        ConfirmAllDownloadThread.Start();
                    }
                    else // download selected feeds
                    {
                        Thread ConfirmDownloadThread = new Thread(() => ConfirmSelectedDownload(DeepCopy<String>(SelectedFavorites), CanvasFavorites));
                        ConfirmDownloadThread.SetApartmentState(ApartmentState.STA);
                        ConfirmDownloadThread.Start();
                    }
                }
            }
            catch (Exception Ex)
            {
                //Console.WriteLine(Ex.StackTrace);
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     hides the search when escape is pressed!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CanvasSearch_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Escape)
                {
                    CollapseCanvasSearch();
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
        ///     initiate search
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBoxSearch_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter)
                {
                    String Query = TextBoxSearch.Text;

                    // start a thread to search for people on instagram
                    Thread SearchPeopleThread = new Thread(() => SearchPeople(Query));
                    SearchPeopleThread.SetApartmentState(ApartmentState.STA);
                    SearchPeopleThread.Start();
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
        ///     scroll reaches end of feeds
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrollViewerFeeds_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            try
            {
                if (!LoadMoreOnScroll)
                {
                    return;
                }
                ScrollViewer Sender = (ScrollViewer)sender;
                if (Sender.ScrollableHeight - Sender.VerticalOffset <= 50)
                {
                    if (!FlagLoadingFeeds && StringNextFeedId != String.Empty)
                    {
                        // start thread
                        Thread LoadUserFeedThread = new Thread(() => LoadUserFeed(StringNextFeedId));
                        LoadUserFeedThread.SetApartmentState(ApartmentState.STA);
                        LoadUserFeedThread.Start();
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
        ///     scroll reaches end of user uploads
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrollViewerUploads_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            try
            {
                if (!LoadMoreOnScroll)
                {
                    return;
                }
                ScrollViewer Sender = (ScrollViewer)sender;
                if (Sender.ScrollableHeight - Sender.VerticalOffset <= 50)
                {
                    if (!FlagLoadingUploads && StringNextUploadId != String.Empty)
                    {
                        // start a thread for loading authorized user uploads
                        Thread LoadUploadsThread = new Thread(() => LoadUploads(AuthorizedUser.UserId, String.Empty));
                        LoadUploadsThread.SetApartmentState(ApartmentState.STA);
                        LoadUploadsThread.Start();
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
        ///     scroll reaches end of popular media
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrollViewerPopular_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            try
            {
                if (!LoadMoreOnScroll)
                {
                    return;
                }
                ScrollViewer Sender = (ScrollViewer)sender;
                if (Sender.ScrollableHeight - Sender.VerticalOffset <= 50)
                {
                    if (!FlagLoadingPopular)
                    {
                        // start a thread for loading popular media
                        Thread LoadPopularThread = new Thread(() => LoadPopular());
                        LoadPopularThread.SetApartmentState(ApartmentState.STA);
                        LoadPopularThread.Start();
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
        ///     scroll reches end of favorites
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrollViewerFavorites_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            try
            {
                if (!LoadMoreOnScroll)
                {
                    return;
                }
                ScrollViewer Sender = (ScrollViewer)sender;
                if (Sender.ScrollableHeight - Sender.VerticalOffset <= 50)
                {
                    if (!FlagLoadingFavorites)
                    {
                        // start a thread for loading popular media
                        Thread LoadPopularThread = new Thread(() => LoadFavorites());
                        LoadPopularThread.SetApartmentState(ApartmentState.STA);
                        LoadPopularThread.Start();
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
        ///     left click on feed search tile
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void FeedSearchTile_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Tile ClickedTile = (Tile)sender;
                String UserName = ClickedTile.Title; // gets the username
                String UserId = ClickedTile.Name.Substring(1); // gets the user id set as the name of the tile object

                // start a thread to show confirmation
                Thread ConfirmAddToFavoritesThread = new Thread(() => ConfirmAddToFavorites(UserId, UserName));
                ConfirmAddToFavoritesThread.SetApartmentState(ApartmentState.STA);
                ConfirmAddToFavoritesThread.Start();

                CanvasSearch.Focus();
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
                //Console.WriteLine(Ex.StackTrace);
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     mouse enters load more button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonLoadMore_MouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                ChangeLoadMoreButtonText(true);
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
                //Console.WriteLine(Ex.StackTrace);
            }
        }

        /// <summary>
        ///      mouse leave load more button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonLoadMore_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                ChangeLoadMoreButtonText(false);
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
                //Console.WriteLine(Ex.StackTrace);
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     cancel download
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DownloadTile_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            try{
                Tile ClickedTile = (Tile)sender;
                String TileName = ClickedTile.Name;

                // start a thread to await confirmation
                Thread DownloadCancelConfirmationThread = new Thread(() => DownloadCancelConfirmation(TileName));
                DownloadCancelConfirmationThread.SetApartmentState(ApartmentState.STA);
                DownloadCancelConfirmationThread.Start();

            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
                //Console.WriteLine(Ex.StackTrace);
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     remove from favs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void FavsListTile_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Tile ClickedTile = (Tile)sender;
                String UserId = ClickedTile.Name;
                String UserName = ClickedTile.Title;

                // start a thread to get confirmation about deletion
                Thread ConfirmRemoveFromFavoritesThread = new Thread(() => ConfirmRemoveFromFavorites(UserId, UserName, ClickedTile));
                ConfirmRemoveFromFavoritesThread.SetApartmentState(ApartmentState.STA);
                ConfirmRemoveFromFavoritesThread.Start();
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
                //Console.WriteLine(Ex.StackTrace);
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     pops open the user profile
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void FavsListTile_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Tile ClickedTile = (Tile)sender;
                String UserId = ClickedTile.Name.Substring(1);
                String UserName = ClickedTile.Title;
                UserId = UserId.Substring(0, UserId.IndexOf('_'));

                // pops up the user profile
                new ProfileScreen(Config, AuthorizedUser, UserId, UserName, this).Visibility = System.Windows.Visibility.Visible;
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
                //Console.WriteLine(Ex.StackTrace);
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     pops open user profile
        ///     when tile is from search results
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void FeedSearchTile_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Tile ClickedTile = (Tile)sender;
                String UserId = ClickedTile.Name.Substring(1);
                String UserName = ClickedTile.Title;

                // pops up the user profile
                new ProfileScreen(Config, AuthorizedUser, UserId, UserName, this).Visibility = System.Windows.Visibility.Visible;
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
                //Console.WriteLine(Ex.StackTrace);
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     collapses searh screen on label click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LabelHideMe_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                CollapseCanvasSearch();
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
                //Console.WriteLine(Ex.StackTrace);
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     changes cursor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LabelHideMe_MouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Hand;
            }
            catch(Exception) { }
        }

        /// <summary>
        ///     changes cursor back
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LabelHideMe_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = null;
            }
            catch (Exception) { }
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
                if (LoadMoreOnScroll)
                {
                    LoadMoreOnScroll = false;
                    ChangeLoadMoreOnScrollIcon(LoadMoreOnScroll);
                    UpdateSettings();
                }
                else
                {
                    LoadMoreOnScroll = true;
                    ChangeLoadMoreOnScrollIcon(LoadMoreOnScroll);
                    UpdateSettings();
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
        ///     loads more data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FeedTile_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // load feeds
                if (CanvasFeeds.IsVisible)
                {
                    if (!FlagLoadingFeeds && StringNextFeedId != String.Empty)
                    {
                        // start thread
                        Thread LoadUserFeedThread = new Thread(() => LoadUserFeed(StringNextFeedId));
                        LoadUserFeedThread.SetApartmentState(ApartmentState.STA);
                        LoadUserFeedThread.Start();
                    }
                }
                // load popular
                if (CanvasPopular.IsVisible)
                {
                    if (!FlagLoadingPopular)
                    {
                        // start thread
                        Thread LoadPopularThread = new Thread(() => LoadPopular());
                        LoadPopularThread.SetApartmentState(ApartmentState.STA);
                        LoadPopularThread.Start();
                    }
                }
                // load uploads
                if (CanvasUploads.IsVisible)
                {
                    if (!FlagLoadingUploads)
                    {
                        // start thread
                        Thread LoadUploadsThread = new Thread(() => LoadUploads(AuthorizedUser.UserId, StringNextUploadId));
                        LoadUploadsThread.SetApartmentState(ApartmentState.STA);
                        LoadUploadsThread.Start();
                    }
                }
                // load favs
                if (CanvasFavorites.IsVisible)
                {
                    if (!FlagLoadingFavorites)
                    {
                        // start thread
                        Thread LoadFavoritesThread = new Thread(() => LoadFavorites());
                        LoadFavoritesThread.SetApartmentState(ApartmentState.STA);
                        LoadFavoritesThread.Start();
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
        ///     shows downloads panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DownloadStatus_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                CollapseCanvasFeeds();
                CollapseCanvasPopular();
                CollapseCanvasUploads();
                CollapseCanvasFavorites();
                CollapseCanvasDownloads();
                CollapseLabelShowFavs();
                CollapseCanvasFavoritesList();

                ShowCanvasDownloads();
            }
            catch (Exception) { }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     open file selector dialog box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PhotoDestination_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var Dialog = new System.Windows.Forms.FolderBrowserDialog();
                Dialog.Description = "Select folder for your photo downloads";
                var Result = Dialog.ShowDialog();
                if (Result == System.Windows.Forms.DialogResult.OK)
                {
                    DownloadedImagePathBase = Dialog.SelectedPath + @"\";
                    PhotoDestination.ToolTip = DownloadedImagePathBase;
                    UpdateSettings();
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
        ///     open file selector dialog box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VideoDestination_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var Dialog = new System.Windows.Forms.FolderBrowserDialog();
                Dialog.Description = "Select folder for your video downloads";
                var Result = Dialog.ShowDialog();
                if (Result == System.Windows.Forms.DialogResult.OK)
                {
                    DownloadedVideosPathBase = Dialog.SelectedPath + @"\";
                    VideoDestination.ToolTip = DownloadedVideosPathBase;
                    UpdateSettings();
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
        ///     updates settings file
        /// </summary>
        private void UpdateSettings()
        {
            FileStream Settings = null;
            try
            {
                Settings = File.Open(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\DownGramer\Settings\settings.xml", FileMode.Create, FileAccess.Write);
                AppSettings Sett = new AppSettings();
                Sett.DefaultImagePath = DownloadedImagePathBase;
                Sett.DefaultVideoPath = DownloadedVideosPathBase;
                Sett.ScrollOnLoad = LoadMoreOnScroll;

                XmlSerializer Ser = new XmlSerializer(typeof(AppSettings));
                Ser.Serialize(Settings, Sett);

                Settings.Close();
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
            }
        }

        /// <summary>
        ///     window size changed events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        /***************************************************************************************************/

        /// <summary>
        ///     manage size
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FScreen_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                var ParentWidth = 1280.00;
                var ParentHeight = 768.00;

                // get current width of the window
                if (!FScreen.IsLoaded)
                {
                    ParentWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
                    ParentHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
                    ParentWidth -= 133;
                }
                else
                {
                    ParentWidth = FScreen.ActualWidth;
                    ParentHeight = FScreen.ActualHeight;
                }

                // do computations
                var ComputedCanvasWidth = ParentWidth;
                var TilesCount = (double)Math.Floor((ComputedCanvasWidth - 37) / 133);
                var WrapPanelWidth = TilesCount * 133;
                var CanvasWidth = WrapPanelWidth + 37;
                var WindowWidth = CanvasWidth + 46;

                // set the values
                Dispatcher.Invoke(
                    new Action(
                        delegate()
                        {
                            // frame
                            FScreen.Width = CanvasWidth + 26;

                            // canvas curtain
                            CanvasCurtain.Width = CanvasWidth;
                            CanvasCurtain.Height = ParentHeight - 50;

                            // menu
                            CanvasMenu.Width = CanvasWidth;
                            Canvas.SetLeft(StackMenu, (CanvasWidth - StackMenu.Width) / 2);

                            // feeds
                            CanvasFeeds.Width = CanvasWidth;
                            CanvasFeeds.Height = CanvasCurtain.Height - 50;
                            ScrollViewerFeeds.Height = CanvasCurtain.Height - 70;
                            WrapPanelFeeds.Width = WrapPanelWidth;
                            ProgressRingFeeds.Margin = new Thickness((CanvasFeeds.Width - 60) / 2, (ScrollViewerFeeds.Height - 60) / 2, 0, 0);

                            // uploads
                            CanvasUploads.Width = CanvasWidth;
                            CanvasUploads.Height = CanvasCurtain.Height - 50;
                            ScrollViewerUploads.Height = CanvasCurtain.Height - 70;
                            WrapPanelUploads.Width = WrapPanelWidth;
                            ProgressRingUploads.Margin = new Thickness((CanvasUploads.Width - 60) / 2, (ScrollViewerUploads.Height - 60) / 2, 0, 0);

                            // popular
                            CanvasPopular.Width = CanvasWidth;
                            CanvasPopular.Height = CanvasCurtain.Height - 50;
                            ScrollViewerPopular.Height = CanvasCurtain.Height - 70;
                            WrapPanelPopular.Width = WrapPanelWidth;
                            ProgressRingPopular.Margin = new Thickness((CanvasPopular.Width - 60) / 2, (ScrollViewerPopular.Height - 60) / 2, 0, 0);

                            // favs
                            CanvasFavorites.Width = CanvasWidth;
                            CanvasFavorites.Height = CanvasCurtain.Height - 50;
                            ScrollViewerFavorites.Height = CanvasCurtain.Height - 70;
                            WrapPanelFavorites.Width = WrapPanelWidth;
                            ProgressRingFavorites.Margin = new Thickness((CanvasFavorites.Width - 60) / 2, (ScrollViewerFavorites.Height - 60) / 2, 0, 0);

                            // downloads
                            CanvasDownloads.Width = CanvasWidth;
                            CanvasDownloads.Height = CanvasCurtain.Height - 50;
                            ScrollViewerDownloads.Height = CanvasCurtain.Height - 70;
                            WrapPanelDownloads.Width = WrapPanelWidth;

                            // favs list
                            CanvasFavsBar.Height = CanvasFavorites.Height;
                            CanvasFavoritesList.Height = CanvasFavorites.Height;
                            ScrollViewerFavoritesList.Height = CanvasFavorites.Height - 70;
                            Canvas.SetLeft(CanvasFavoritesList, 8);
                            Canvas.SetLeft(CanvasFavsBar, 0);

                            // search layout
                            CanvasSearch.Width = FScreen.Width;
                            StackSearch.Width = FScreen.Width;
                            CanvasSearch.Height = FScreen.ActualHeight;
                            ScrollViewerSearch.Height = FScreen.Height - 233;
                            WrapPanelSearch.Width = WrapPanelWidth;
                            Canvas.SetLeft(CanvasSearch, 0);
                            ProgressRingSearch.Margin = new Thickness((CanvasSearch.Width - 60) / 2, (ScrollViewerSearch.Height - 60) / 2, 0, 0);
                        }), null);
            }
            catch (Exception Ex)
            {
                //Console.WriteLine(Ex.Message);
            }
        }
    }
}
