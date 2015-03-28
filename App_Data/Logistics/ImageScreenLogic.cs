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
using System.Net;

using MahApps.Metro.Controls;
using InstaAPI.Auth;
using InstaAPI.Endpoints.Authenticated;
using InstaAPI.Endpoints.OptionalParameters;
using InstaAPI.Endpoints.Unauthenticated;
using InstaAPI.Entities;
using DownGramer.App_Data.Entities;

namespace DownGramer
{
    public partial class Image : MetroWindow
    {
        private void LoadImage(String ImageId)
        {
            try
            {
                Media Image = new Media(Config, AuthorizedUser);
                MediaData MediaInfo = Image.GetMediaInformation(ImageId);

                if (MediaInfo.Meta.Code == 200)
                {
                    // start a thread to load the image
                    Thread LoadImageOnCanvasThread = new Thread(() => LoadImageOnCanvas(MediaInfo));
                    LoadImageOnCanvasThread.SetApartmentState(ApartmentState.STA);
                    LoadImageOnCanvasThread.Start();
                }
                else
                {
                    Dispatcher.Invoke(
                        new Action(
                            delegate()
                            {
                                ImageScreen.Close();
                            }), null);
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
