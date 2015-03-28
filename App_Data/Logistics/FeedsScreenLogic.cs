using System;
using System.Collections;
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
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;

using MahApps.Metro.Controls;
using InstaAPI.Auth;
using InstaAPI.Endpoints.Authenticated;
using InstaAPI.Endpoints.OptionalParameters;
using InstaAPI.Endpoints.Unauthenticated;
using InstaAPI.Entities;
using DownGramer.App_Data.Entities;
using DownGramer.Download;

namespace DownGramer
{
    public partial class FeedsScreen : MetroWindow
    {
        private Users UsersObj;
        private Media MediaObj;

        private Boolean FlagLoadingFeeds = false;
        private Boolean FlagLoadingUploads = false;
        private Boolean FlagLoadingFavorites = false;
        private Boolean FlagLoadingPopular = false;
        private Boolean FlagNoMoreLoads = false;
        private Boolean FlagShowingFavsList = false;

        private Boolean RescanFavorites = true;
        internal Boolean LoadMoreOnScroll = false;

        private String StringNextFeedId = String.Empty;
        private String StringNextUploadId = String.Empty;

        private List<String> ListGlobalPopular = new List<String>();
        private List<String> SelectedFeeds = new List<String>(); // store feed media ids
        private List<String> SelectedUploads = new List<String>(); // store uploaded media ids
        private List<String> SelectedPopular = new List<String>(); // store popular media ids
        private List<String> SelectedFavorites = new List<String>(); // store fav media ids
        private List<String> ListFavorites = new List<String>(); // store fav user ids
        private List<String> ListPagination = new List<String>(); // store fav user pagination

        internal static Hashtable DownloadData = new Hashtable();
        internal WrapPanel WPDownloads;
        internal int MediaDownloaded = 0;
        internal int TotalDownload = 0;

        private String FavoritesFilePathBase = String.Empty;
        internal String DownloadedImagePathBase = String.Empty;
        internal String DownloadedVideosPathBase = String.Empty;

        private String UpdateCheckUrl = @"http://downgramer.netai.net/updates/update.xml";
        private String AppSiteUrl = @"http://downgramer.netai.net/";

        /// <summary>
        ///     check for update
        /// </summary>
        private void UpdateCheck()
        {
            ThreadStart ThrS = new ThreadStart(
                delegate()
                {
                    try
                    {
                        WebClient Client = new WebClient();
                        Byte[] ResponseData = Client.DownloadData(@"http://downgramer.netai.net/updates/update.xml");

                        MemoryStream Stream = new MemoryStream(ResponseData);
                        XmlSerializer Ser = new XmlSerializer(typeof(AppVersion));
                        AppVersion Version = (AppVersion)Ser.Deserialize(Stream);

                        String CurrVersion = System.Configuration.ConfigurationManager.AppSettings["Version"];
                        if (!CurrVersion.Equals(Version.Version))
                        {
                            new Thread(() => ShowNewVersionAvailable(Version.Version, Version.Description, Version.Published)).Start();
                        }
                    }
                    catch (Exception Ex)
                    {
                        MyException.EnterLog(Ex);
                        //Console.WriteLine(Ex.StackTrace);
                    }
                });
            new Thread(ThrS).Start();
        }

        /***************************************************************************************************/

        /// <summary>
        ///     set up file paths
        /// </summary>
        private void SetUpApp()
        {
            try
            {
                String PFFolder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                PFFolder += @"\DownGramer\";

                // set favorites data
                FavoritesFilePathBase = PFFolder + @"Data\fav.bin";

                // set photo and video file destination
                FileStream Settings = File.Open(PFFolder + @"Settings\settings.xml", FileMode.Open, FileAccess.Read);
                XmlSerializer Ser = new XmlSerializer(typeof(AppSettings));
                AppSettings AppSett = (AppSettings)Ser.Deserialize(Settings);

                DownloadedImagePathBase = AppSett.DefaultImagePath;
                PhotoDestination.ToolTip = DownloadedImagePathBase;
                DownloadedVideosPathBase = AppSett.DefaultVideoPath;
                VideoDestination.ToolTip = DownloadedVideosPathBase;
                LoadMoreOnScroll = AppSett.ScrollOnLoad;

                ChangeLoadMoreOnScrollIcon(LoadMoreOnScroll);

                Settings.Close();
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     performs deep copy of the list without any references
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="SourceList"></param>
        /// <returns></returns>
        private List<T> DeepCopy<T>(List<T> SourceList)
        {
            List<T> CopiedList = null;
            try
            {
                MemoryStream Memory = new MemoryStream();
                BinaryFormatter BFormatter = new BinaryFormatter();
                BFormatter.Serialize(Memory, SourceList);

                Memory.Position = 0;
                CopiedList = (List<T>)BFormatter.Deserialize(Memory);
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
                //Console.WriteLine(Ex.StackTrace);
            }

            return CopiedList;
        }

        /***************************************************************************************************/

        /// <summary>
        ///     scans favorites list
        /// </summary>
        private void ScanFavorites()
        {
            try
            {
                // add data in the table if fav file exists
                if (File.Exists(FavoritesFilePathBase))
                {
                    int PreviousCount = ListFavorites.Count;
                    ListFavorites.Clear();

                    FileStream FStream = File.Open(FavoritesFilePathBase, FileMode.Open, FileAccess.Read);
                    BinaryFormatter BFormatter = new BinaryFormatter();

                    Favorites MyFavorites = (Favorites)BFormatter.Deserialize(FStream);

                    int Index = 0;
                    PreviousCount--;
                    foreach (var FavUser in MyFavorites.FavoriteList)
                    {
                        ListFavorites.Add(FavUser.Id);
                        if (Index > PreviousCount)
                        {
                            ListPagination.Add(String.Empty);
                        }
                        Index++;
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
        ///     loads authorized user's feed
        /// </summary>
        private void LoadUserFeed(String NextMaxId)
        {
            try
            {
                // set flag
                FlagLoadingFeeds = true;

                // show loading tile
                if (!ProgressRingFeeds.IsVisible)
                {
                    RemoveLoadMoreTile(WrapPanelFeeds);
                    AddLoadingTile(127, WrapPanelFeeds);
                }

                // fetch feed
                UsersObj = new Users(Config, AuthorizedUser);
                Feeds UserFeeds = UsersObj.GetUserFeeds(new GetUserFeedsParameters() { Count = 50, MaxId = NextMaxId });

                if (UserFeeds.Meta.Code == 200)
                {
                    // collapse progress ring
                    CollapseProgressRingFeeds();

                    // hide loading tile
                    RemoveLoadingTile(WrapPanelFeeds);

                    // load tiles
                    foreach (var Feed in UserFeeds.Data)
                    {
                        try
                        {
                            LoadTile(Feed.Id, Feed.Images.Thumbnail.url, 127, WrapPanelFeeds);
                        }
                        catch (Exception) { }
                    }

                    // check pagination
                    if (UserFeeds.Pagination.NextMaxId == null || UserFeeds.Pagination.NextMaxId.Length == 0)
                    {
                        StringNextFeedId = String.Empty;
                    }
                    else
                    {
                        StringNextFeedId = UserFeeds.Pagination.NextMaxId;

                        // show load more tile
                        AddLoadMoreTile(127, WrapPanelFeeds);
                    }

                    // reset flag
                    FlagLoadingFeeds = false;
                }
                else
                {
                    // collapse progress ring
                    CollapseProgressRingFeeds();

                    // collapse loading tile
                    if (!ProgressRingFeeds.IsVisible)
                    {
                        RemoveLoadingTile(WrapPanelFeeds);
                    }

                    // show load more tile
                    AddLoadMoreTile(127, WrapPanelFeeds);
                }
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);

                // collapse progress ring
                CollapseProgressRingFeeds();

                // collapse loading tile
                if (!ProgressRingFeeds.IsVisible)
                {
                    RemoveLoadingTile(WrapPanelFeeds);
                }

                // show load more tile
                AddLoadMoreTile(127, WrapPanelFeeds);
            }
            finally
            {
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     loads user's uploads
        ///     users identified by UserId
        /// </summary>
        /// <param name="NextMaxId"></param>
        private void LoadUploads(String UserId, String NextMaxId)
        {
            try
            {
                // set flag
                FlagLoadingUploads = true;

                // show loading tile
                if (!ProgressRingFeeds.IsVisible)
                {
                    RemoveLoadMoreTile(WrapPanelUploads);
                    AddLoadingTile(127, WrapPanelUploads);
                }

                // fetch authorized users uploads
                UsersObj = new Users(Config, AuthorizedUser);
                Feeds UserPosts = UsersObj.GetUserPosts(UserId, new GetUserPostsParameters() { Count = 50, MaxId = NextMaxId });

                if (UserPosts.Meta.Code == 200)
                {
                    // collapse progree ring
                    CollapseProgressRingUploads();

                    // hide loading tile
                    RemoveLoadingTile(WrapPanelUploads);

                    // load tiles
                    foreach (var Post in UserPosts.Data)
                    {
                        try
                        {
                            LoadTile(Post.Id, Post.Images.Thumbnail.url, 127, WrapPanelUploads);
                        }
                        catch (Exception) { }
                    }

                    // check pagination
                    if (UserPosts.Pagination.NextMaxId == null || UserPosts.Pagination.NextMaxId.Length == 0)
                    {
                        StringNextUploadId = String.Empty;
                    }
                    else
                    {
                        StringNextUploadId = UserPosts.Pagination.NextMaxId;
                        
                        // show load more tile
                        AddLoadMoreTile(127, WrapPanelUploads);
                    }

                    // reset flag 
                    FlagLoadingUploads = false;
                }
                else
                {
                    // collapse progress ring
                    CollapseProgressRingUploads();

                    // show loading tile
                    if (!ProgressRingFeeds.IsVisible)
                    {
                        RemoveLoadingTile(WrapPanelUploads);
                    }
                    
                    // show load more tile
                    AddLoadMoreTile(127, WrapPanelUploads);
                }
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
                //Console.WriteLine(Ex.StackTrace);

                // collapse progress ring
                CollapseProgressRingUploads();

                // show loading tile
                if (!ProgressRingFeeds.IsVisible)
                {
                    RemoveLoadingTile(WrapPanelUploads);
                }

                // show load more tile
                AddLoadMoreTile(127, WrapPanelUploads);
            }
            finally
            {
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     loads popular media at the moment
        /// </summary>
        private void LoadPopular()
        {
            try
            {
                // set flag
                FlagLoadingPopular = true;

                // show loading tile
                if (!ProgressRingFeeds.IsVisible)
                {
                    RemoveLoadMoreTile(WrapPanelPopular);
                    AddLoadingTile(127, WrapPanelPopular);
                }

                // fetch popular media
                MediaObj = new Media(Config, AuthorizedUser);
                MediaPopular PopularFeeds = MediaObj.GetMediaPopular();

                if (PopularFeeds.Meta.Code == 200)
                {
                    // collapse progress ring
                    CollapseProgressRingPopular();

                    // hiding loading tile
                    RemoveLoadingTile(WrapPanelPopular);

                    // load tiles
                    foreach (var Media in PopularFeeds.Data)
                    {
                        try
                        {
                            if (!ListGlobalPopular.Contains(Media.Id))
                            {
                                ListGlobalPopular.Add(Media.Id);
                                LoadTile(Media.Id, Media.Images.Thumbnail.url, 127, WrapPanelPopular);
                            }
                        }
                        catch (Exception) { }
                    }


                    // show load more tile
                    AddLoadMoreTile(127, WrapPanelPopular);

                    // reset flag
                    FlagLoadingPopular = false;
                }
                else
                {
                    // collapse progress ring
                    CollapseProgressRingPopular();

                    // hide loading tile
                    if (!ProgressRingFeeds.IsVisible)
                    {
                        RemoveLoadingTile(WrapPanelPopular);
                    }

                    // show load more tile
                    AddLoadMoreTile(127, WrapPanelPopular);
                }
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
                //Console.WriteLine(Ex.StackTrace);

                // collapse progress ring
                CollapseProgressRingPopular();

                // hide loading tile
                if (!ProgressRingFeeds.IsVisible)
                {
                    RemoveLoadingTile(WrapPanelPopular);
                }

                // show load more tile
                AddLoadMoreTile(127, WrapPanelPopular);
            }
            finally
            {
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     loads feeds per user in the favorite list
        /// </summary>
        /// <param name="UserId"></param>
        /// <param name="NextMaxId"></param>
        /// <param name="DestinationPanel"></param>
        private void LoadFeedsForFavorites(int Index, String UserId, String NextMaxId, WrapPanel DestinationPanel)
        {
            try
            {
                // show loading tile
                if (!ProgressRingFeeds.IsVisible)
                {
                    RemoveLoadMoreTile(WrapPanelFavorites);
                    AddLoadingTile(127, WrapPanelFavorites);
                }

                // fetch feed
                UsersObj = new Users(Config, AuthorizedUser);
                Feeds UserPosts = UsersObj.GetUserPosts(UserId, new GetUserPostsParameters() {Count = 50, MaxId = NextMaxId });

                if (UserPosts.Meta.Code == 200)
                {
                    // collapse progress ring
                    CollapseProgressRingFavorites();

                    // hide loading tile
                    RemoveLoadMoreTile(WrapPanelFavorites);
                    RemoveLoadingTile(WrapPanelFavorites);

                    // load tiles
                    foreach (var Feed in UserPosts.Data)
                    {
                        try
                        {
                            LoadTile(Feed.Id, Feed.Images.Thumbnail.url, 127, DestinationPanel);
                        }
                        catch (Exception) { }
                    }

                    // check pagination
                    if (UserPosts.Pagination.NextMaxId == null || UserPosts.Pagination.NextMaxId.Length == 0)
                    {
                        ListPagination[Index] = "-1";
                    }
                    else
                    {
                        ListPagination[Index] = UserPosts.Pagination.NextMaxId;
                    }
                }
                else
                {
                    // collpase progress ring
                    CollapseProgressRingFavorites();

                    // hide loading tile
                    if (!ProgressRingFeeds.IsVisible)
                    {
                        RemoveLoadingTile(WrapPanelFavorites);
                    }
                }
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
                //ListPagination[Index] = "-2";
                //RemoveLoadingTile(WrapPanelFavorites);
                //CollapseProgressRingFavorites();

                // collpase progress ring
                CollapseProgressRingFavorites();

                // hide loading tile
                if (!ProgressRingFeeds.IsVisible)
                {
                    RemoveLoadingTile(WrapPanelFavorites);
                }
            }
            finally
            {
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     load feeds from user added to favorites
        /// </summary>
        private void LoadFavorites()
        {
            try
            {
                // if being loaded first time than scan the file for favs
                if (RescanFavorites)
                {
                    ScanFavorites();
                    RescanFavorites = false;
                }

                // set flag
                FlagLoadingFavorites = true;

                Boolean HasMore = false;

                // load feeds
                for (int i = 0; i < ListFavorites.Count; i++)
                {
                    LoadFeedsForFavorites(i, ListFavorites[i], ListPagination[i], WrapPanelFavorites);
                    if (!ListPagination[i].Equals("-1") && !ListPagination[i].Equals("-2"))
                    {
                        HasMore = true;
                    }
                }

                if (HasMore)
                {
                    AddLoadMoreTile(127, WrapPanelFavorites);
                }

                if (ListFavorites.Count == 0)
                {
                    CollapseProgressRingFavorites();
                    AddLoadMoreTile(127, WrapPanelFavorites);
                }

                // raise flag
                if (!HasMore && ListFavorites.Count != 0)
                {
                    FlagNoMoreLoads = true;
                }

                // reset flag
                FlagLoadingFavorites = false;
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
                //Console.WriteLine(Ex.StackTrace);
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     searches for people from instagram
        /// </summary>
        /// <param name="Query"></param>
        private void SearchPeople(String Query)
        {
            try
            {
                // no search on null string
                if (Query.Length == 0 || Query == String.Empty || Query == null)
                {
                    return;
                }

                // disable text box
                DisableTextBoxSearch();

                // remove previous search result
                RemovePreviousSearchResult();

                // show progress ring
                ShowProgressRingSearch();

                // fethch results
                Users Search = new Users(Config, AuthorizedUser);
                UserSearch SearchResult = Search.GetUserSearchResult(new GetUserSearchResultParameters() {Query = Query, Count = 100 });

                if (SearchResult.Meta.Code == 200)
                {
                    // collapse progress ring
                    CollapseProgressRingSearch();

                    // load tiles
                    foreach (var User in SearchResult.Data)
                    {
                        try
                        {
                            LoadSearchTile(User.ProfilePicture, User.Id, User.UserName, 127, WrapPanelSearch);
                        }
                        catch (Exception Ex)
                        {
                            MyException.EnterLog(Ex);
                        }
                    }

                    // reenable the text box
                    EnableTextBoxSearch();
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
        ///     processes the tile select request
        /// </summary>
        /// <param name="Type"></param>
        /// <param name="MediaId"></param>
        private void ProcessTileSelectRequest(String MediaId, Tile Sender)
        {
            if (CanvasFeeds.IsVisible) // feeds screen active
            {
                if (!SelectedFeeds.Contains(MediaId)) // new entry
                {
                    SelectTile(Sender);
                    SelectedFeeds.Add(MediaId);
                }
                else // remove entry
                {
                    DeselectTile(Sender);
                    SelectedFeeds.Remove(MediaId);
                }
            }
            if (CanvasUploads.IsVisible) // upload screen active
            {
                if (!SelectedUploads.Contains(MediaId)) // new entry
                {
                    SelectTile(Sender);
                    SelectedUploads.Add(MediaId);
                }
                else // remove entry
                {
                    DeselectTile(Sender);
                    SelectedUploads.Remove(MediaId);
                }
            }
            if (CanvasPopular.IsVisible) // popular screen active
            {
                if (!SelectedPopular.Contains(MediaId)) // new entry
                {
                    SelectTile(Sender);
                    SelectedPopular.Add(MediaId);
                }
                else // remove entry
                {
                    DeselectTile(Sender);
                    SelectedPopular.Remove(MediaId);
                }
            }
            if (CanvasFavorites.IsVisible) // fav screen active
            {
                if (!SelectedFavorites.Contains(MediaId)) // new entry
                {
                    SelectTile(Sender);
                    SelectedFavorites.Add(MediaId);
                }
                else // remove entry
                {
                    DeselectTile(Sender);
                    SelectedFavorites.Remove(MediaId);
                }
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     selects all the media on the screen and akss for confirmation
        /// </summary>
        /// <param name="Sender"></param>
        private void ConfirmAllDownload(WrapPanel Sender)
        {
            List<String> MediaIds = new List<String>();

            try
            {
                Dispatcher.Invoke(
                    new Action(
                        delegate()
                        {
                            // auto select tiles
                            IEnumerable<DependencyObject> Medias = Sender.GetChildObjects();
                            foreach (var EachMedia in Medias)
                            {
                                if (EachMedia.GetType() == typeof(Tile))
                                {
                                    if (((Tile)EachMedia).Name.Equals("LoadMore"))
                                    {
                                        continue;
                                    }
                                    SelectTile((Tile)EachMedia);
                                    MediaIds.Add(((Tile)EachMedia).Name.Substring(1));
                                }
                            }

                            // add to appropriate list
                            if (Sender == WrapPanelFeeds)
                            {
                                SelectedFeeds = MediaIds;
                            }
                            else if (Sender == WrapPanelUploads)
                            {
                                SelectedUploads = MediaIds;
                            }
                            else if (Sender == WrapPanelPopular)
                            {
                                SelectedPopular = MediaIds;
                            }
                            else if (Sender == WrapPanelFavorites)
                            {
                                SelectedFavorites = MediaIds;
                            }

                            // show confirmation msg
                            if (MediaIds.Count > 0)
                            {
                                // start a thread to show confirmation dialog for starting a download for selected media.
                                Thread ConfirmDownloadThread = new Thread(() => ConfirmSelectedDownload(DeepCopy(MediaIds), (Canvas)Sender.Parent.GetParentObject()));
                                ConfirmDownloadThread.SetApartmentState(ApartmentState.STA);
                                ConfirmDownloadThread.Start();
                            }
                            else
                            {
                                // start a thread to show error msg
                                Thread ShowNoMediaSelectedErrorThread = new Thread(() => ShowNoMediaSelectedError());
                                ShowNoMediaSelectedErrorThread.SetApartmentState(ApartmentState.STA);
                                ShowNoMediaSelectedErrorThread.Start();
                            }
                        }), null);

            }
            catch (Exception Ex)
            {
                Console.Write(Ex.Message);
                //Console.WriteLine(Ex.StackTrace);
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     check whether user already exists in the favorite list or not
        /// </summary>
        /// <param name="UserId"></param>
        /// <returns></returns>
        private Boolean CheckExistingFavorite(String UserId)
        {
            Boolean Exists = false;
            Stream FStream = null;

            try
            {
                if (File.Exists(FavoritesFilePathBase))
                {
                    FStream = File.Open(FavoritesFilePathBase, FileMode.Open, FileAccess.Read);
                    BinaryFormatter BFormatter = new BinaryFormatter();

                    Favorites CurrentFavs = (Favorites)BFormatter.Deserialize(FStream);
                    foreach (var Fav in CurrentFavs.FavoriteList)
                    {
                        if (Fav.Id.Equals(UserId))
                        {
                            Exists = true;
                            break;
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
                //Console.WriteLine(Ex.StackTrace);
            }
            finally
            {
                if (FStream != null)
                {
                    FStream.Close();
                }
            }

            return Exists;
        }

        /***************************************************************************************************/

        /// <summary>
        ///     add the specified user in the list
        /// </summary>
        /// <param name="UserId"></param>
        private void AddUserToFavoriteList(String UserId)
        {
            Stream FStream = null;

            try
            {
                Favorites CurrentFavs = new Favorites();
                if (File.Exists(FavoritesFilePathBase))
                {
                    FStream = File.Open(FavoritesFilePathBase, FileMode.Open, FileAccess.ReadWrite);
                    BinaryFormatter BFormatter = new BinaryFormatter();

                    CurrentFavs = (Favorites)BFormatter.Deserialize(FStream);
                    FStream.Close();
                }
                else
                {
                    CurrentFavs.FavoriteList = new List<User>();
                }

                // check relationship with logged in user
                Relationships Relation = new Relationships(Config, AuthorizedUser);
                UserRelationship MyRelation = Relation.GetUserRelationship(UserId);

                if (MyRelation.Meta.Code == 200)
                {
                    if (MyRelation.Data.TargetUserIsPrivate && !MyRelation.Data.OutgoingStatus.Contains("follows"))
                    {
                        ShowPrivateUserMsg();
                    }
                    else
                    {
                        Users UsersObj = new Users(Config, AuthorizedUser);
                        User SpecificUser = UsersObj.GetUserInformation(UserId);

                        if (SpecificUser.Meta.Code == 200)
                        {
                            SpecificUser.Id = UserId;
                            CurrentFavs.FavoriteList.Add(SpecificUser);
                            RescanFavorites = true; // so that next time the file is rescaned
                        }
                    }
                }

                // store in fav.bin
                FStream = File.Open(FavoritesFilePathBase, FileMode.Create, FileAccess.Write);
                BinaryFormatter BinFormatter = new BinaryFormatter();
                BinFormatter.Serialize(FStream, CurrentFavs);
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
                //Console.WriteLine(Ex.StackTrace);
            }
            finally
            {
                if (FStream != null)
                {
                    FStream.Close();
                }
            }
        }

        /***************************************************************************************************/
        
        /// <summary>
        ///     start download process
        /// </summary>
        /// <param name="Config"></param>
        /// <param name="AuthorizedUser"></param>
        /// <param name="TileName"></param>
        internal void InitiateDownload(InstaConfig Config, AuthUser AuthorizedUser, List<String> FeedsToDownload, String TileName, WrapPanel Source)
        {
            try
            {
                DownloadManager Manager = new DownloadManager(Config, AuthorizedUser, FeedsToDownload, TileName, Source, this);
                Thread StartDownloadThread = new Thread(() => Manager.StartDownload());

                // add tile
                DownloadData.Add(TileName, true);
                
                // update download stats
                UpdateDownloadStats(0, FeedsToDownload.Count, 0);

                // start download
                StartDownloadThread.Start();
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
                //Console.WriteLine(Ex.StackTrace);
            }
        }

        /// <summary>
        ///     updates download stats in the title bar
        /// </summary>
        /// <param name="Downloaded"></param>
        /// <param name="ToBeDownloaded"></param>
        internal void UpdateDownloadStats(int Downloaded, int ToBeDownloaded, int ops)
        {
            if (ops == 0) // new download
            {
                MediaDownloaded += 0;
                TotalDownload += ToBeDownloaded;
            }
            else if (ops == 1) // complete
            {
                MediaDownloaded ++;
                MediaDownloaded -= ToBeDownloaded;
                TotalDownload -= ToBeDownloaded;
            }
            else if (ops == 2) // kill download
            {
                MediaDownloaded -= Downloaded;
                TotalDownload -= ToBeDownloaded;
            }
            else // normal
            {
                MediaDownloaded += Downloaded;
            }

            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        float DownloadCompleted;
                        if (TotalDownload != 0)
                        {
                            DownloadCompleted = ((float)MediaDownloaded / TotalDownload) * 100;
                        }
                        else
                        {
                            DownloadCompleted = 100;
                        }
                        DownloadStatus.Content = String.Format("{0:0.00}", DownloadCompleted.ToString()) + "% Complete";
                    }), null);
        }

        /***************************************************************************************************/

        /// <summary>
        ///     scans the fav file for loading list
        /// </summary>
        private void ScanFavoritesAndDisplayonFavoritesList()
        {
            FileStream FavFile = null;

            try
            {
                if (File.Exists(FavoritesFilePathBase))
                {
                    FavFile = File.Open(FavoritesFilePathBase, FileMode.Open, FileAccess.Read);
                    BinaryFormatter BFormatter = new BinaryFormatter();
                    Favorites MyFavorites = (Favorites)BFormatter.Deserialize(FavFile);

                    // start a thread to load favorite list tiles
                    
                    new Thread(() => LoadFavoritesListTiles(MyFavorites.FavoriteList)).Start();
                }
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
                //Console.WriteLine(Ex.StackTrace);
            }
            finally
            {
                if (FavFile != null)
                {
                    FavFile.Close();
                }
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     makes request in the ui file to make changes
        /// </summary>
        /// <param name="Favs"></param>
        private void LoadFavoritesListTiles(List<User> Favs)
        {
            // remove previous list
            RemoveAllTiles(WrapPanelFavoritesList);

            // check count in the file
            if (Favs.Count != 0)
            {
                CollapseFavoriteListLonelyText();
            }
            else
            {
                ShowFavoriteListLonelyText();
            }

            // add tiles
            int Index = 0;
            foreach (var User in Favs)
            {
                try
                {

                    Thread LoadFavoriteListTileThread = new Thread(() => LoadFavoriteListTile(User.ProfilePicture, User.Id, Index, User.UserName, 77, WrapPanelFavoritesList));
                    LoadFavoriteListTileThread.Start();
                    LoadFavoriteListTileThread.Join();
                }
                catch (Exception) { }
                Index++;
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     removes from favorite list
        /// </summary>
        /// <param name="UserId"></param>
        private void RemoveFromFavorites(String UserId)
        {
            FileStream FavFile = null;

            try
            {
                if (File.Exists(FavoritesFilePathBase))
                {
                    FavFile = File.Open(FavoritesFilePathBase, FileMode.Open, FileAccess.Read);
                    BinaryFormatter BFormatter = new BinaryFormatter();
                    Favorites MyFavs = (Favorites)BFormatter.Deserialize(FavFile);

                    FavFile.Close();


                    String ActualUserId = UserId.Substring(1, UserId.LastIndexOf("_") - 1);

                    int Index = 0;
                    foreach (User Usr in DeepCopy<User>(MyFavs.FavoriteList))
                    {
                        if (Usr.Id.Equals(ActualUserId))
                        {
                            MyFavs.FavoriteList.RemoveAt(Index);
                        }
                        Index++;
                    }

                    if (MyFavs.FavoriteList.Count == 0)
                    {
                        ShowFavoriteListLonelyText();
                    }

                    FavFile = File.Open(FavoritesFilePathBase, FileMode.Create, FileAccess.Write);
                    BFormatter.Serialize(FavFile, MyFavs);

                    FavFile.Close();
                }
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
            }
            finally
            {
                if (FavFile != null)
                {
                    FavFile.Close();
                }
            }
        }
                
    }
}
