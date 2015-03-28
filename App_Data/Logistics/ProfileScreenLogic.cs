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


namespace DownGramer
{
    public partial class ProfileScreen : MetroWindow
    {
        private InstaConfig Config;
        private AuthUser AuthorizedUser;
        private FeedsScreen Source;

        private Boolean FlagLoadingFeeds = false;

        private String UserId = String.Empty;
        private String UserName = String.Empty;
        private String StringNextFeedId = String.Empty;

        private int Count = 0;

        private List<String> SelectedFeeds = new List<String>();

        /***************************************************************************************************/

        /// <summary>
        ///     loads the user feeds
        /// </summary>
        private void LoadUserPosts(String NextMaxId)
        {
            try
            {
                // set the flag
                FlagLoadingFeeds = true;

                // add loading tile
                if (!ProgressRingFeeds.IsVisible)
                {
                    RemoveLoadMoreTile(WrapPanelFeeds);
                    AddLoadingTile(142, WrapPanelFeeds);
                }

                // fetch feed
                Users UsersObj = new Users(Config, AuthorizedUser);
                Feeds UserPosts = UsersObj.GetUserPosts(this.UserId, new GetUserPostsParameters() { Count = 50, MaxId = NextMaxId });

                if (UserPosts.Meta.Code == 200)
                {
                    // collapse progress ring
                    CollapseProgressRingFeeds();

                    // hide loading tile
                    RemoveLoadingTile(WrapPanelFeeds);

                    // load tiles
                    foreach (var Feed in UserPosts.Data)
                    {
                        try
                        {
                            LoadTile(Feed.Id, Feed.Images.Thumbnail.url, 142, WrapPanelFeeds, Count);
                            LoadFlipViewTiles(Feed.Id, Feed.Images.StandardResolution.url, 640, ProfileFlipView);
                            Count++;
                        }
                        catch (Exception) { }
                    }

                    // check pagination
                    if (UserPosts.Pagination.NextMaxId == null || UserPosts.Pagination.NextMaxId.Length == 0)
                    {
                        StringNextFeedId = String.Empty;
                    }
                    else
                    {
                        StringNextFeedId = UserPosts.Pagination.NextMaxId;

                        // add a load more tile
                        AddLoadMoreTile(142, WrapPanelFeeds);
                    }

                    // reset flag
                    FlagLoadingFeeds = false;
                }
                else
                {
                    if (!ProgressRingFeeds.IsVisible)
                    {
                        RemoveLoadingTile(WrapPanelFeeds);
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
        ///     selects or deselects a tile
        /// </summary>
        /// <param name="MediaId"></param>
        /// <param name="Sender"></param>
        private void ProcessTileSelectRequest(String MediaId, Tile Sender)
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
                                    SelectTile((Tile)EachMedia);
                                    MediaIds.Add(((Tile)EachMedia).Name.Substring(1));
                                }
                            }

                            // add to appropriate list
                            if (Sender == WrapPanelFeeds)
                            {
                                SelectedFeeds = MediaIds;
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
        ///     confirmation for download selected media
        /// </summary>
        /// <param name="FeedsToDownload"></param>
        /// <param name="Sender"></param>
        private void ConfirmSelectedDownload(List<String> FeedsToDownload, Canvas Sender)
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
                                    Source.AddDownloadingTile(Sender, FeedsToDownload.Count, TileName, this.UserName);

                                    // this start the download process
                                    Source.InitiateDownload(Config, AuthorizedUser, FeedsToDownload, TileName, Source.WPDownloads);
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
        ///     check for relationship
        /// </summary>
        private void CheckRelationShip()
        {
            try
            {
                Relationships Relation = new Relationships(Config, AuthorizedUser);
                UserRelationship MyRelation = Relation.GetUserRelationship(this.UserId);

                if (MyRelation.Meta.Code == 200)
                {                    
                    if (MyRelation.Data.TargetUserIsPrivate && !MyRelation.Data.OutgoingStatus.Contains("follows"))
                    {
                        ShowPrivateUserMsg();
                    }
                }
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
                //Console.WriteLine(Ex.StackTrace);
            }
        }
    }
}
