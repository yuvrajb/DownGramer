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
using InstaAPI.Auth;
using InstaAPI.Endpoints.Authenticated;
using InstaAPI.Endpoints.OptionalParameters;
using InstaAPI.Endpoints.Unauthenticated;
using InstaAPI.Entities;
using DownGramer.App_Data.Entities;

namespace DownGramer
{
    /// <summary>
    /// Interaction logic for Image.xaml
    /// </summary>
    public partial class Image : MetroWindow
    {
        private InstaConfig Config;
        private AuthUser AuthorizedUser;
        private String ImageId;
        private LogException MyException = new LogException();

        public Image()
        {
            InitializeComponent();
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.WindowState = System.Windows.WindowState.Maximized;
        }

        /// <summary>
        ///     this windows opens by this call
        /// </summary>
        /// <param name="Config"></param>
        /// <param name="AuthorizedUser"></param>
        /// <param name="ImageId"></param>
        public Image(InstaConfig Config, AuthUser AuthorizedUser, String ImageId)
        {
            // set variables
            this.Config = Config;
            this.AuthorizedUser = AuthorizedUser;
            this.ImageId = ImageId;

            // important
            InitializeComponent();

            // some changes -- important
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.WindowState = System.Windows.WindowState.Maximized;
        }

        /***************************************************************************************************/

        /// <summary>
        ///     perform tasks on window load
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindowImage_Loaded(object sender, RoutedEventArgs e)
        {
            // start a thread for loading image
            Thread LoadImageThread = new Thread(() => LoadImage(ImageId));
            LoadImageThread.SetApartmentState(ApartmentState.STA);
            LoadImageThread.Start();
        }

        /***************************************************************************************************/

        /// <summary>
        ///     captures various key down events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindowImage_KeyDown(object sender, KeyEventArgs e)
        {
            // close the image window when escape key is pressed
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
    }
}
