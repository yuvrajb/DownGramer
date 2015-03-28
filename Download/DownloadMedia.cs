using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Drawing;

using InstaAPI.Auth;
using InstaAPI.Endpoints.Authenticated;
using InstaAPI.Entities;

namespace DownGramer.Download
{
    internal class DownloadMedia
    {
        //private static String DownloadedImagePathBase = @"DownGramer\Downloads\Images\";
        //private static String DownloadedVideosPathBase = @"DownGramer\Downloads\Videos\";

        /// <summary>
        ///     this method actually performs download task
        /// </summary>
        /// <param name="Config"></param>
        /// <param name="AuthorizedUser"></param>
        /// <param name="MediaId"></param>
        /// <param name="Obj"></param>
        private static void ActualDownload(InstaConfig Config, AuthUser AuthorizedUser, String MediaId, DownloadManager Obj)
        {
            Media Md = new Media(Config, AuthorizedUser);
            MediaData Data = Md.GetMediaInformation(MediaId);
            if (Data.Meta.Code == 200)
            {
                String MediaUrlPath = Data.Feed.Images.StandardResolution.url;

                // cancel download issued
                if (!(Boolean)FeedsScreen.DownloadData[Obj.GetTileName()])
                {
                    return;
                }

                // start downlod
                WebClient Client = new WebClient();
                byte[] ResponseData = Client.DownloadData(MediaUrlPath);

                // cancael download issued
                if (!(Boolean)FeedsScreen.DownloadData[Obj.GetTileName()])
                {
                    return;
                }

                // save image
                MemoryStream Memory = new MemoryStream(ResponseData);
                var Img = System.Drawing.Image.FromStream(Memory);
                Img.Save(Obj.DownloadedImagePathBase + MediaId + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                Memory.Close();

                // save video if applicable
                if (Data.Feed.Type.Equals("video"))
                {
                    WebClient Clientc = new WebClient();
                    byte[] ResponseDatac = Clientc.DownloadData(Data.Feed.Videos.StandardResolution.url);
                    MemoryStream Memoryc = new MemoryStream(ResponseDatac);

                    FileStream VFile = File.Open(Obj.DownloadedVideosPathBase + MediaId + ".mp4", FileMode.Create, FileAccess.Write);
                    Memoryc.WriteTo(VFile);
                    VFile.Close();
                    Memoryc.Close();
                }
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     this method is called by downloadmanager
        /// </summary>
        /// <param name="Config"></param>
        /// <param name="AuthorizedUser"></param>
        /// <param name="MediaId"></param>
        /// <param name="Obj"></param>
        internal static void FetchMediaDetailsAndDownload(InstaConfig Config, AuthUser AuthorizedUser, String MediaId, DownloadManager Obj)
        {
            try
            {
                ActualDownload(Config, AuthorizedUser, MediaId, Obj);
            }
            catch (Exception)
            {
                ReAttempt(Config, AuthorizedUser, MediaId, Obj);
            }
            finally
            {
                Obj.IncrementCount();
            }
        }

        /***************************************************************************************************/

        /// <summary>
        ///     if download fails for one time, another attempt is made
        /// </summary>
        /// <param name="Config"></param>
        /// <param name="AuthorizedUser"></param>
        /// <param name="MediaId"></param>
        /// <param name="Obj"></param>
        internal static void ReAttempt(InstaConfig Config, AuthUser AuthorizedUser, String MediaId, DownloadManager Obj)
        {
            try
            {
                ActualDownload(Config, AuthorizedUser, MediaId, Obj);
            }
            catch (Exception) { }
        }
    }
}
