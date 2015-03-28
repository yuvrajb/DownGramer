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

using MahApps.Metro.Controls;
using InstaAPI.Auth;
using InstaAPI.Endpoints.Authenticated;
using InstaAPI.Entities;
using DownGramer.App_Data.Entities;

namespace DownGramer.Download
{
    internal class DownloadManager
    {
        private InstaConfig Config;
        private AuthUser AuthorizedUser;
        private List<String> MediaIds;
        private String TileName;
        private int Count;
        private int Downloaded;
        private WrapPanel Source;
        private FeedsScreen WindowSource;
        internal String DownloadedImagePathBase;
        internal String DownloadedVideosPathBase;
        private LogException MyException = null;

        /// <summary>
        ///     constructor with required initialisers
        /// </summary>
        /// <param name="Config"></param>
        /// <param name="AuthorizedUser"></param>
        /// <param name="MediaIds"></param>
        public DownloadManager(InstaConfig Config, AuthUser AuthorizedUser, List<String> MediaIds, String TileName, WrapPanel Source, FeedsScreen WindowSource)
        {
            this.Config = Config;
            this.AuthorizedUser = AuthorizedUser;
            this.MediaIds = MediaIds;
            this.TileName = TileName;
            this.Count = this.MediaIds.Count;
            this.Downloaded = 0;
            this.Source = Source;
            this.WindowSource = WindowSource;
            this.DownloadedImagePathBase = WindowSource.DownloadedImagePathBase;
            this.DownloadedVideosPathBase = WindowSource.DownloadedVideosPathBase;
            MyException = new LogException();
        }

        /***************************************************************************************************/
        
        /// <summary>
        ///     gets the tilename of the current object
        /// </summary>
        /// <returns></returns>
        internal String GetTileName()
        {
            return this.TileName;
        }

        /***************************************************************************************************/

        /// <summary>
        ///     increments count on the download tile
        /// </summary>
        internal void IncrementCount()
        {
            lock(this)
            {
                this.Downloaded ++;
                Source.Dispatcher.Invoke(
                    new Action(
                        delegate()
                        {
                            Tile TileAffected = Source.FindChild<Tile>(this.TileName);
                            TileAffected.Content = Downloaded;

                            if (Downloaded < Count / 3)
                            {
                                TileAffected.Background = Brushes.LightPink;
                            }
                            else if (Downloaded < (2 * Count) / 3)
                            {
                                TileAffected.Background = Brushes.LightBlue;
                            }
                            else
                            {
                                TileAffected.Background = Brushes.LightGreen;
                            }

                            if (Downloaded >= Count)
                            {
                                Source.Children.Remove(TileAffected);

                                if (Source.Children.Count == 1)
                                {
                                    Tile DefaultTile = Source.FindChild<Tile>("DefaultDownloadTile");
                                    DefaultTile.Visibility = System.Windows.Visibility.Visible;
                                }

                                if ((Boolean)FeedsScreen.DownloadData[this.TileName])
                                {
                                    WindowSource.UpdateDownloadStats(1, Count, 1);
                                }
                            }
                            else
                            {
                                if ((Boolean)FeedsScreen.DownloadData[this.TileName])
                                {
                                    WindowSource.UpdateDownloadStats(1, Count, 3);
                                }
                            }
                        }), null);
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     starts the download process
        /// </summary>
        internal void StartDownload()
        {
            try
            {
                foreach (var Id in MediaIds)
                {
                    try
                    {
                        Thread Thr = new Thread(() => DownloadMedia.FetchMediaDetailsAndDownload(Config, AuthorizedUser, Id, this));
                        Thr.Start();
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
    }
}
