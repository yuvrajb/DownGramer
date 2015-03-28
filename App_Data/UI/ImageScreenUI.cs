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

namespace DownGramer
{
    public partial class Image : MetroWindow
    {
        /// <summary>
        ///     shows image on the ImageScreen
        /// </summary>
        /// <param name="MediaInfo"></param>
        private void LoadImageOnCanvas(MediaData MediaInfo)
        {
            // get data
            String MediaOwner = "@" + MediaInfo.Feed.User.UserName;
            String MediaUrl = MediaInfo.Feed.Images.StandardResolution.url;

            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        // create Border
                        Border CanvasBorder = new Border();
                        CanvasBorder.Width = 640;
                        CanvasBorder.Height = 640;
                        CanvasBorder.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                        CanvasBorder.VerticalAlignment = System.Windows.VerticalAlignment.Center;

                        // create canvas
                        Canvas CanvasImage = new Canvas();
                        
                        ImageBrush IB = new ImageBrush();
                        IB.ImageSource = new BitmapImage(new Uri(MediaUrl));

                        // set background
                        CanvasImage.Background = IB;

                        // add to respective elements
                        CanvasBorder.BorderThickness = new Thickness(0);
                        CanvasBorder.BorderBrush = Brushes.Black;
                        CanvasBorder.Child = CanvasImage;
                        GridImage.Children.Add(CanvasBorder);
                    }), null);
        }
    }
}
