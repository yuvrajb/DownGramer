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
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using InstaAPI.Auth;
using InstaAPI.Endpoints.Authenticated;
using InstaAPI.Endpoints.OptionalParameters;
using InstaAPI.Endpoints.Unauthenticated;
using InstaAPI.Entities;
using DownGramer.Download;

namespace DownGramer
{
    public partial class FeedsScreen : MetroWindow
    {
        /// <summary>
        ///     add loading tile to the panel
        /// </summary>
        /// <param name="Side"></param>
        /// <param name="Destination"></param>
        private void AddLoadingTile(int Side, WrapPanel Destination)
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        Tile FeedTile = new Tile();
                        FeedTile.Width = Side;
                        FeedTile.Height = Side;
                        FeedTile.Foreground = Brushes.Transparent;
                        FeedTile.Name = "LoadingFeeds";
                        
                        ProgressRing PRing = new ProgressRing();
                        PRing.IsActive = true;
                        FeedTile.Content = PRing;
                        FeedTile.Background = Brushes.Transparent;

                        Destination.Children.Add(FeedTile);
                    }), null);
        }

        /// <summary>
        ///     adds a load more tile on the panel
        /// </summary>
        /// <param name="Side"></param>
        /// <param name="Destination"></param>
        private void AddLoadMoreTile(int Side, WrapPanel Destination)
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        Tile FeedTile = new Tile();
                        FeedTile.Width = Side;
                        FeedTile.Height = Side;
                        FeedTile.Background = Brushes.Cyan;
                        FeedTile.Foreground = Brushes.Transparent;
                        FeedTile.Name = "LoadMore";

                        ImageBrush IB = new ImageBrush();
                        IB.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/LoadMore.png", UriKind.Absolute));
                        FeedTile.OpacityMask = IB;

                        FeedTile.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(FeedTile_PreviewMouseLeftButtonDown);

                        Destination.Children.Add(FeedTile);
                    }), null);
        }

        /***************************************************************************************************/

        /// <summary>
        ///     removes loadmoretile from the panel
        /// </summary>
        /// <param name="Source"></param>
        private void RemoveLoadMoreTile(WrapPanel Source)
        {
            Dispatcher.Invoke(
               new Action(
                   delegate()
                   {
                       Tile LoadMoreTile = Source.FindChild<Tile>("LoadMore");
                       Source.Children.Remove(LoadMoreTile);
                   }), null);
        }

        /***************************************************************************************************/

        /// <summary>
        ///     removes loading tile from the panel
        /// </summary>
        /// <param name="Source"></param>
        private void RemoveLoadingTile(WrapPanel Source)
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        Tile LoadTile = Source.FindChild<Tile>("LoadingFeeds");
                        Source.Children.Remove(LoadTile);
                    }), null);
        }

        /***************************************************************************************************/
        
        /// <summary>
        ///     loads tile with the image
        /// </summary>
        /// <param name="ImageUrl"></param>
        private void LoadTile(String ImageId, String ImageUrl, int Side, WrapPanel Destination)
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        Tile FeedTile = new Tile();
                        FeedTile.Width = Side;
                        FeedTile.Height = Side;
                        FeedTile.Name = "_" + ImageId;

                        // load image
                        ImageBrush IB = new ImageBrush();
                        IB.ImageSource = new BitmapImage(new Uri(ImageUrl));
                        FeedTile.Background = IB;

                        // add event
                        FeedTile.PreviewMouseDown += new MouseButtonEventHandler(FeedTile_MouseLeftButtonDown); // select / deselect the photo
                        FeedTile.MouseDoubleClick += new MouseButtonEventHandler(FeedTile_MouseDoubleClick); // show individual photo
                        FeedTile.MouseEnter += new MouseEventHandler(FeedTile_MouseEnter);

                        Destination.Children.Add(FeedTile);
                    }), null);
        }

        /***************************************************************************************************/

        /// <summary>
        ///     loads tile respective to search result
        /// </summary>
        /// <param name="ImageUrl"></param>
        /// <param name="UserId"></param>
        /// <param name="UserName"></param>
        /// <param name="Side"></param>
        /// <param name="Destination"></param>
        private void LoadSearchTile(String ImageUrl, String UserId, String UserName, int Side, WrapPanel Destination)
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        Tile FeedSearchTile = new Tile();
                        FeedSearchTile.Width = Side;
                        FeedSearchTile.Height = Side;
                        FeedSearchTile.Name = "_" + UserId;
                        FeedSearchTile.Title = UserName;
                        FeedSearchTile.TitleFontSize = 12;
                        FeedSearchTile.FontStyle = FontStyles.Italic;

                        // load image
                        ImageBrush IB = new ImageBrush();
                        IB.ImageSource = new BitmapImage(new Uri(ImageUrl));
                        FeedSearchTile.Background = IB;

                        // add event
                        FeedSearchTile.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(FeedSearchTile_MouseLeftButtonDown);
                        FeedSearchTile.PreviewMouseRightButtonDown += new MouseButtonEventHandler(FeedSearchTile_PreviewMouseRightButtonDown);
                        //FeedTile.PreviewMouseDown += new MouseButtonEventHandler(FeedTile_MouseLeftButtonDown); // select / deselect the photo
                        //FeedTile.MouseDoubleClick += new MouseButtonEventHandler(FeedTile_MouseDoubleClick); // show individual photo
                        //FeedTile.MouseEnter += new MouseEventHandler(FeedTile_MouseEnter);

                        Destination.Children.Add(FeedSearchTile);
                    }), null);
        }

        /***************************************************************************************************/

        /// <summary>
        ///     removes previous search tiles from the screen
        /// </summary>
        private void RemovePreviousSearchResult()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        try
                        {
                            List<String> SearchedTileNames = new List<String>();
                            IEnumerable<DependencyObject> Objects = WrapPanelSearch.GetChildObjects();

                            foreach (var Obj in Objects)
                            {
                                if (Obj.GetType() == typeof(Tile))
                                {
                                    SearchedTileNames.Add(((Tile)Obj).Name);
                                }
                            }

                            foreach (var Obj in DeepCopy<String>(SearchedTileNames))
                            {
                                WrapPanelSearch.Children.Remove(WrapPanelSearch.FindChild<Tile>(Obj));
                            }
                        }
                        catch (Exception Ex)
                        {
                            MyException.EnterLog(Ex);
                            //Console.WriteLine(Ex.StackTrace);
                        }
                    }), null);
        }

        /***************************************************************************************************/

        private void LoadFavoriteListTile(String ImageUrl, String UserId, int Index, String UserName, int Side, WrapPanel Destination)
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        Tile FeedSearchTile = new Tile();
                        FeedSearchTile.Width = Side;
                        FeedSearchTile.Height = Side;
                        FeedSearchTile.Name = "_" + UserId + "_" + Index.ToString();
                        FeedSearchTile.Title = UserName;
                        FeedSearchTile.TitleFontSize = 8;
                        FeedSearchTile.FontStyle = FontStyles.Italic;

                        // load image
                        ImageBrush IB = new ImageBrush();
                        IB.ImageSource = new BitmapImage(new Uri(ImageUrl));
                        FeedSearchTile.Background = IB;

                        // add event
                        FeedSearchTile.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(FavsListTile_PreviewMouseLeftButtonDown);
                        FeedSearchTile.PreviewMouseRightButtonDown += new MouseButtonEventHandler(FavsListTile_PreviewMouseRightButtonDown);

                        Destination.Children.Add(FeedSearchTile);
                    }), null);
        }

        /***************************************************************************************************/

        /// <summary>
        ///      makes label bold
        /// </summary>
        /// <param name="SourceLabel"></param>
        private void MakeLabelBold(Label SourceLabel)
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        SourceLabel.Foreground = Brushes.Cyan;
                    }), null);
        }

        /// <summary>
        ///     makes all label in menu normal
        /// </summary>
        private void MakeMenuLabelNormal()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        IEnumerable<DependencyObject> Objects = StackMenu.GetChildObjects();
                        foreach (var Obj in Objects)
                        {
                            if (Obj.GetType() == typeof(Label))
                            {
                                ((Label)Obj).FontWeight = FontWeights.Normal;
                                ((Label)Obj).Background = Brushes.Transparent;
                                ((Label)Obj).Foreground = Brushes.White;
                            }
                        }
                    }), null);
        }


        /***************************************************************************************************/

        /// <summary>
        ///     puts tick
        /// </summary>
        /// <param name="Sender"></param>
        private void SelectTile(Tile Sender)
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        ImageBrush IB = new ImageBrush();
                        IB.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/Tick_127.png", UriKind.Absolute));
                        Sender.OpacityMask = IB;
                    }), null);
        }

        /// <summary>
        ///     removes tick
        /// </summary>
        /// <param name="Sender"></param>
        private void DeselectTile(Tile Sender)
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        try
                        {
                            Sender.OpacityMask = Brushes.Black;
                        }
                        catch (Exception) { }
                    }), null);
        }

        /***************************************************************************************************/

        /// <summary>
        ///     de selects all the selected tiles
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="TileNames"></param>
        private void DeselectAllTiles(Canvas Sender, List<String> TileNames)
        {
            foreach(String Name in TileNames)
            {
                try
                {
                    Dispatcher.Invoke(
                        new Action(
                            delegate()
                            {
                                Tile EachTile = Sender.FindChild<Tile>("_" + Name);
                                DeselectTile(EachTile);

                                if (Sender == CanvasFeeds)
                                {
                                    SelectedFeeds.Remove(Name);
                                }
                                else if (Sender == CanvasUploads)
                                {
                                    SelectedUploads.Remove(Name);
                                }
                                else if (Sender == CanvasPopular)
                                {
                                    SelectedPopular.Remove(Name);
                                }
                                else if (Sender == CanvasFavorites)
                                {
                                    SelectedFavorites.Remove(Name);
                                }
                            }), null);
                }
                catch (Exception Ex)
                {
                    MyException.EnterLog(Ex);
                    //Console.WriteLine(Ex.StackTrace);
                }
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     removes tile from the wrappanel
        /// </summary>
        /// <param name="Sender"></param>
        private void RemoveAllTiles(WrapPanel Sender) 
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        IEnumerable<DependencyObject> Objs = Sender.GetChildObjects();
                        foreach (var Obj in Objs)
                        {
                            if (Obj.GetType() == typeof(Tile))
                            {
                                ((Tile)Obj).Visibility = System.Windows.Visibility.Collapsed;
                            }
                        }
                    }), null);
        }

        /***************************************************************************************************/

        /// <summary>
        ///     disables ButtonLoadMore
        /// </summary>
        private void DisableLoadMoreButton()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        ButtonLoadMore.IsEnabled = false;
                    }), null);
        }

        /// <summary>
        ///     enables ButtonLoadMore
        /// </summary>
        private void EnableLoadMoreButton()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        ButtonLoadMore.IsEnabled = true;
                    }), null);
        }

        /***************************************************************************************************/

        /// <summary>
        ///     collapses lonely text fav
        /// </summary>
        private void CollapseFavoriteListLonelyText()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        FavoriteListLonelyText.Visibility = System.Windows.Visibility.Collapsed;
                    }), null);
        }

        /// <summary>
        ///     shows lonely text fav
        /// </summary>
        private void ShowFavoriteListLonelyText()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        FavoriteListLonelyText.Visibility = System.Windows.Visibility.Visible;
                    }), null);
        }

        /***************************************************************************************************/

        /// <summary>
        ///     collapses ProgressRingFeeds
        /// </summary>
        private void CollapseProgressRingFeeds()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        ProgressRingFeeds.Visibility = System.Windows.Visibility.Collapsed;
                    }), null);
        }

        /***************************************************************************************************/

        /// <summary>
        ///     collapses ProgressRingPopular
        /// </summary>
        private void CollapseProgressRingPopular()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        ProgressRingPopular.Visibility = System.Windows.Visibility.Collapsed;
                    }), null);
        }

        /***************************************************************************************************/

        /// <summary>
        ///     collapses ProgressRingUploads
        /// </summary>
        private void CollapseProgressRingUploads()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        ProgressRingUploads.Visibility = System.Windows.Visibility.Collapsed;
                    }), null);
        }

        /***************************************************************************************************/

        /// <summary>
        ///     collapses ProgressRingFavorites
        /// </summary>
        private void CollapseProgressRingFavorites()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        ProgressRingFavorites.Visibility = System.Windows.Visibility.Collapsed;
                    }), null);
        }

        /***************************************************************************************************/

        /// <summary>
        ///     collapses ProgressRingSearch
        /// </summary>
        private void CollapseProgressRingSearch()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        ProgressRingSearch.Visibility = System.Windows.Visibility.Collapsed;
                    }), null);
        }

        /// <summary>
        ///     shows ProgressRingSearch
        /// </summary>
        private void ShowProgressRingSearch()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        ProgressRingSearch.Visibility = System.Windows.Visibility.Visible;
                    }), null);
        }

        /***************************************************************************************************/

        /// <summary>
        ///     collapses CanvasFeeds
        /// </summary>
        private void CollapseCanvasFeeds()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        CanvasFeeds.Visibility = System.Windows.Visibility.Collapsed;
                    }), null);
        }

        /// <summary>
        ///     shows CanvasFeeds
        /// </summary>
        private void ShowCanvasFeeds()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        CanvasFeeds.Visibility = System.Windows.Visibility.Visible;
                    }), null);
        }
        
        /***************************************************************************************************/

        /// <summary>
        ///     collapses CanvasPopular
        /// </summary>
        private void CollapseCanvasPopular()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        CanvasPopular.Visibility = System.Windows.Visibility.Collapsed;
                    }), null);
        }

        /// <summary>
        ///     shows CanvasPopular
        /// </summary>
        private void ShowCanvasPopular()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        CanvasPopular.Visibility = System.Windows.Visibility.Visible;
                    }), null);
        }

        /***************************************************************************************************/

        /// <summary>
        ///     collapses CanvasUploads
        /// </summary>
        private void CollapseCanvasUploads()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        CanvasUploads.Visibility = System.Windows.Visibility.Collapsed;
                    }), null);
        }

        /// <summary>
        ///     shows CanvasUploads
        /// </summary>
        private void ShowCanvasUploads()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        CanvasUploads.Visibility = System.Windows.Visibility.Visible;
                    }), null);
        }

        /***************************************************************************************************/

        /// <summary>
        ///     shows CanvasSearch
        /// </summary>
        private void CollapseCanvasSearch()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        CanvasSearch.Visibility = System.Windows.Visibility.Collapsed;
                    }), null);
        }

        /// <summary>
        ///     collapses CanvasSearch
        /// </summary>
        private void ShowCanvasSearch()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        CanvasSearch.Visibility = System.Windows.Visibility.Visible;
                        TextBoxSearch.Focus();
                    }), null);
        }

        /***************************************************************************************************/

        /// <summary>
        ///     shows CanvasFavorites
        /// </summary>
        private void ShowCanvasFavorites()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        CanvasFavorites.Visibility = System.Windows.Visibility.Visible;
                    }), null);
        }

        /// <summary>
        ///     collapses CanvasFavorites
        /// </summary>
        private void CollapseCanvasFavorites()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        CanvasFavorites.Visibility = System.Windows.Visibility.Collapsed;
                    }), null);
        }

        /***************************************************************************************************/
            
        /// <summary>
        ///     collapses CanvasDownloads
        /// </summary>
        private void CollapseCanvasDownloads()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        CanvasDownloads.Visibility = System.Windows.Visibility.Collapsed;
                    }), null);
        }

        /// <summary>
        ///     shows CanvasDownloads
        /// </summary>
        private void ShowCanvasDownloads()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        CanvasDownloads.Visibility = System.Windows.Visibility.Visible;
                    }), null);
        }

        /***************************************************************************************************/

        /// <summary>
        ///     collapses ScrollViewerFavoritesList
        /// </summary>
        private void CollapseCanvasFavoritesList()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        CanvasFavoritesList.Visibility = System.Windows.Visibility.Collapsed;
                        CanvasFavsBar.Visibility = System.Windows.Visibility.Collapsed;
                    }), null);
        }

        /// <summary>
        ///     shows ScrollViewerFavoritesList
        /// </summary>
        private void ShowCanvasFavoritesList()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        CanvasFavoritesList.Visibility = System.Windows.Visibility.Visible;
                        CanvasFavsBar.Visibility = System.Windows.Visibility.Visible;
                    }), null);

        }

        /// <summary>
        ///     collapses LabelShowFavs
        /// </summary>
        private void CollapseLabelShowFavs()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        LabelShowFavs.Visibility = System.Windows.Visibility.Collapsed;
                    }), null);
        }

        /// <summary>
        ///     shows LabelShowFavs
        /// </summary>
        private void ShowLabelShowFavs()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        LabelShowFavs.Visibility = System.Windows.Visibility.Visible;
                    }), null);
        }

        /***************************************************************************************************/

        /// <summary>
        ///     disable textbox search
        /// </summary>
        private void DisableTextBoxSearch()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        //TextBoxSearch.IsEnabled = false;
                    }), null);
        }

        /// <summary>
        ///     enable textbox search
        /// </summary>
        private void EnableTextBoxSearch()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        TextBoxSearch.IsEnabled = true;
                    }), null);
        }

        /// <summary>
        ///     makes load more button's text black
        /// </summary>
        private void ChangeLoadMoreButtonText(Boolean In)
        {
            if (In)
            {
                Dispatcher.Invoke(
                    new Action(
                        delegate()
                        {
                            ButtonLoadMore.Foreground = Brushes.Black;
                        }), null);
            }
            else
            {
                Dispatcher.Invoke(
                    new Action(
                        delegate()
                        {
                            Color Col = (Color)ColorConverter.ConvertFromString("#FFCCCCCC");
                            ButtonLoadMore.Foreground = new SolidColorBrush(Col);
                        }), null);
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     adds download status tile to WrapPanelDownloads
        ///     this method is called from ProfileScreenLogic.cs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="Count"></param>
        /// <param name="DistinctName"></param>
        /// <param name="UserName"></param>
        internal void AddDownloadingTile(Canvas sender, int Count, String DistinctName, String UserName)
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        // hide default tile
                        DefaultDownloadTile.Visibility = System.Windows.Visibility.Collapsed;

                        // add status tile
                        Tile DownloadTile = new Tile();
                        DownloadTile.Width = 392;
                        DownloadTile.Height = 150;
                        DownloadTile.FontSize = 60;
                        DownloadTile.FontFamily = new FontFamily("Segoe UI Symbol");
                        DownloadTile.TitleFontSize = 12;
                        DownloadTile.Name = DistinctName;
                        DownloadTile.PreviewMouseDown += new MouseButtonEventHandler(DownloadTile_PreviewMouseDown);

                        DownloadTile.Title = UserName + " - " + Count + " MEDIAS";
                        DownloadTile.Content = "0";

                        WrapPanelDownloads.Children.Add(DownloadTile);
                    }), null);
        }

        /// <summary>
        ///     add download status tile to WrapPanelDownloads
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="Count"></param>
        internal void AddDownloadingTile(Canvas Sender, int Count, String DistinctName)
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        // hide default tile
                        DefaultDownloadTile.Visibility = System.Windows.Visibility.Collapsed;

                        // add status tile
                        Tile DownloadTile = new Tile();
                        DownloadTile.Width = 392;
                        DownloadTile.Height = 150;
                        DownloadTile.FontSize = 60;
                        DownloadTile.FontFamily = new FontFamily("Segoe UI Symbol");
                        DownloadTile.TitleFontSize = 12;
                        DownloadTile.Name = DistinctName;
                        DownloadTile.PreviewMouseDown += new MouseButtonEventHandler(DownloadTile_PreviewMouseDown);

                        if (CanvasFeeds.IsVisible) 
                        {
                            DownloadTile.Title = "Feeds - " + Count + " MEDIAS";
                        }
                        else if (CanvasUploads.IsVisible)
                        {
                            DownloadTile.Title = "Uploads - " + Count + " MEDIAS";
                        }
                        else if (CanvasPopular.IsVisible)
                        {
                            DownloadTile.Title = "Popular - " + Count + " MEDIAS";
                        }
                        else if (CanvasFavorites.IsVisible)
                        {
                            DownloadTile.Title = "Favorites - " + Count + " MEDIAS";
                        }
                        else
                        {
                            DownloadTile.Title = "Profile - " + Count + " MEDIAS";
                        }
                        DownloadTile.Content = "0";

                        WrapPanelDownloads.Children.Add(DownloadTile);
                    }), null);
        }

        /***************************************************************************************************/

        /// <summary>
        ///     shows confirmation dialog for selected media
        /// </summary>
        /// <param name="FeedsToDownload"></param>
        /// <param name="Sender"></param>
        internal void ConfirmSelectedDownload(List<String> FeedsToDownload, Canvas Sender)
        {
            int Count = FeedsToDownload.Count;

            ThreadStart ThrS = new ThreadStart(
                delegate()
                {
                    Dispatcher.Invoke(
                        new Action(
                            async delegate()
                            {
                                var DialogResponse = await this.ShowMessageAsync("Download all " + Count.ToString() + " photos?", "click 'yes' to download; cancel otherwise", MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, new MetroDialogSettings() { AffirmativeButtonText = "Download", NegativeButtonText = "Cancel & Deselect", FirstAuxiliaryButtonText = "Cancel" });
                                if (DialogResponse == MessageDialogResult.Affirmative) // send a download request
                                {
                                    // start a thread to de-select all selected tiles
                                    Thread Thr = new Thread(() => DeselectAllTiles(Sender, FeedsToDownload));
                                    Thr.Start();

                                    // show tile on download panel
                                    String TileName = "dn_tile_" + DateTime.Now.Ticks.ToString();
                                    AddDownloadingTile(Sender, FeedsToDownload.Count, TileName);

                                    // this start the download process
                                    InitiateDownload(Config, AuthorizedUser, FeedsToDownload, TileName, WrapPanelDownloads);

                                    // hide all the wrap layouts in ScrollViewer
                                    CollapseCanvasFeeds();
                                    CollapseCanvasPopular();
                                    CollapseCanvasUploads();
                                    CollapseCanvasFavorites();

                                    // show downloads tab
                                    ShowCanvasDownloads();
                                }
                                else if (DialogResponse == MessageDialogResult.Negative) // deselect all tiles
                                {
                                    // start a thread to de-select all selected tiles
                                    Thread Thr = new Thread(() => DeselectAllTiles(Sender, FeedsToDownload));
                                    Thr.Start();
                                }
                            }), null);
                });
            new Thread(ThrS).Start();
        }

        /***************************************************************************************************/

        /// <summary>
        ///     simple error dialog
        /// </summary>
        private void ShowNoMediaSelectedError()
        {
            ThreadStart Thrs = new ThreadStart(
                delegate(){
                    Dispatcher.Invoke(
                        new Action(
                            async delegate(){
                                await this.ShowMessageAsync("Nothing to select", "don't be in such a hurry! i'm fetching the media for you :)", MessageDialogStyle.Affirmative, null);
                            }), null);
                });
            new Thread(Thrs).Start();
        }

        /***************************************************************************************************/

        /// <summary>
        ///     shows confirmation dialog for adding to fav
        /// </summary>
        /// <param name="UserId"></param>
        /// <param name="UserName"></param>
        private void ConfirmAddToFavorites(String UserId, String UserName)
        {
            ThreadStart Thrs = new ThreadStart(
                delegate()
                {
                    Dispatcher.Invoke(
                        new Action(
                            async delegate()
                            {
                                var DialogResponse = await this.ShowMessageAsync("Add @" + UserName + " to favorites?", "adding to favorites won't follow this user on Instagram. only public profiles can be added to favorites. if the user is private than you need to follow that user on instagram first!", MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, new MetroDialogSettings() { AffirmativeButtonText = "Add to Favorites", FirstAuxiliaryButtonText="View Profile", NegativeButtonText = "Cancel"});
                                if (DialogResponse == MessageDialogResult.Affirmative) // send a add to fav request
                                {
                                    Boolean CheckAlreayFavorited = CheckExistingFavorite(UserId);
                                    if (CheckAlreayFavorited)
                                    {
                                        await this.ShowMessageAsync("Already in your favorites :)", "seems like you've already added this user to your favorites!", MessageDialogStyle.Affirmative, null);
                                    }
                                    else
                                    {
                                        // start a thread to add the user in the favorites list
                                        new Thread(()=>AddUserToFavoriteList(UserId)).Start();
                                    }
                                    CanvasSearch.Focus();
                                }
                                else if (DialogResponse == MessageDialogResult.FirstAuxiliary)
                                {
                                    new ProfileScreen(Config, AuthorizedUser, UserId, UserName, this).Visibility = System.Windows.Visibility.Visible;
                                    CanvasSearch.Focus();
                                }
                                CanvasSearch.Focus();
                            }), null);
                });
            new Thread(Thrs).Start();
        }

        /***************************************************************************************************/

        /// <summary>
        ///     confirm concellation of download
        /// </summary>
        /// <param name="DownloadId"></param>
        private void DownloadCancelConfirmation(String DownloadId) 
        {
            ThreadStart Thrs = new ThreadStart(
                delegate()
                {
                    Dispatcher.Invoke(
                        new Action(
                            async delegate()
                            {
                                var DialogResponse = await this.ShowMessageAsync("are you sure?", "you sure about cancelling this download?", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
                                if (DialogResponse == MessageDialogResult.Affirmative)
                                {
                                    try
                                    {
                                        DownloadData[DownloadId] = false;
                                        Tile DownloadTile = CanvasDownloads.FindChild<Tile>(DownloadId);
                                        DownloadTile.Visibility = System.Windows.Visibility.Collapsed;

                                        String TileTitle = DownloadTile.Title;

                                        var Match = Regex.Match(TileTitle, @"\d+");
                                        String Den = Match.Groups[0].ToString();
                                        String Num = DownloadTile.Content.ToString();

                                        UpdateDownloadStats(Int32.Parse(Num.Trim()), Int32.Parse(Den.Trim()), 2);

                                        ScanDownloads();
                                    }
                                    catch (Exception Ex)
                                    {
                                        MyException.EnterLog(Ex);
                                        //Console.WriteLine(Ex.StackTrace);
                                    }
                                }
                            }), null);
                });
            new Thread(Thrs).Start();
        }

        /***************************************************************************************************/
        
        /// <summary>
        ///     scan for active downloads
        /// </summary>
        private void ScanDownloads()
        {
            Boolean NoDownloads = true;

            foreach (var Key in DownloadData.Keys)
            {
                if ((Boolean)DownloadData[Key] == true)
                {
                    NoDownloads = false;
                }
            }

            if (NoDownloads)
            {
                DefaultDownloadTile.Visibility = System.Windows.Visibility.Visible;
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     confirm removal from favorites
        /// </summary>
        /// <param name="UserId"></param>
        /// <param name="UserName"></param>
        /// <param name="Sender"></param>
        private void ConfirmRemoveFromFavorites(String UserId, String UserName, Tile Sender)
        {
            ThreadStart Thrs = new ThreadStart(
                delegate()
                {
                    Dispatcher.Invoke(
                        new Action(
                            async delegate()
                            {
                                var DialogResponse = await this.ShowMessageAsync("are you sure?", "you sure you want to remove @" + UserName + " from your favorites?", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
                                if (DialogResponse == MessageDialogResult.Affirmative)
                                {
                                    RemoveFromFavorites(UserId);
                                    Sender.Visibility = System.Windows.Visibility.Collapsed;
                                    RescanFavorites = true;
                                }
                            }), null);
                });
            new Thread(Thrs).Start();
        }

        /***************************************************************************************************/

        /// <summary>
        ///     show private profile msg
        /// </summary>
        private void ShowPrivateUserMsg()
        {
            ThreadStart ThrS = new ThreadStart(
                            delegate()
                            {
                                Dispatcher.Invoke(
                                    new Action(
                                        async delegate()
                                        {
                                            var DialogResponse = await this.ShowMessageAsync("oops!! private user", "unfortunately, this user is private and you haven't followed this profile yet. only followed or public profiles can be added to favorites.", MessageDialogStyle.Affirmative, null);
                                            if (DialogResponse == MessageDialogResult.Affirmative)
                                            {
                                                //this.Close();
                                            }
                                        }), null);
                            });
            new Thread(ThrS).Start();
        }

        /***************************************************************************************************/

        /// <summary>
        ///     changes icon on the title bar
        /// </summary>
        /// <param name="Scroll"></param>
        private void ChangeLoadMoreOnScrollIcon(Boolean Scroll)
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        if (Scroll)
                        {
                            VisualBrush Visual = new VisualBrush() { Visual = (Visual)Resources["appbar_thumbs_up"] };
                            RectangleLoadOnScroll.OpacityMask = Visual;
                        }
                        else
                        {
                            VisualBrush Visual = new VisualBrush() { Visual = (Visual)Resources["appbar_thumbs_down"] };
                            RectangleLoadOnScroll.OpacityMask = Visual;
                        }
                    }), null);
        }

        /***************************************************************************************************/

        /// <summary>
        ///     show new version available
        /// </summary>
        /// <param name="NewVersion"></param>
        /// <param name="Description"></param>
        private void ShowNewVersionAvailable(String NewVersion, String Description, String ReleaseDate)
        {
            ThreadStart Thrs = new ThreadStart(
                delegate()
                {
                    Dispatcher.Invoke(
                        new Action(
                            async delegate()
                            {
                                var DialogResponse = await this.ShowMessageAsync("Yipee! New Update Available", "a new version '" + NewVersion + "' released on " + ReleaseDate + " is available for download. this one has " + Description, MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText="Download Now", NegativeButtonText="Not Now"});
                                if (DialogResponse == MessageDialogResult.Affirmative)
                                {
                                    System.Diagnostics.Process.Start(AppSiteUrl);
                                }
                            }), null);
                });
            new Thread(Thrs).Start();
        }
    }
}
