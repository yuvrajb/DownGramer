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

        /// <summary>
        ///     loads small tile with image
        /// </summary>
        /// <param name="ImageId"></param>
        /// <param name="ImageUrl"></param>
        /// <param name="Side"></param>
        /// <param name="Destination"></param>
        private void LoadTile(String ImageId, String ImageUrl, int Side, WrapPanel Destination, int Count)
        {
            Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        Tile FeedTile = new Tile();
                        FeedTile.Width = Side;
                        FeedTile.Height = Side;
                        FeedTile.Name = "_" + ImageId;
                        FeedTile.Title = Count.ToString();
                        FeedTile.TitleFontSize = 0;
                        FeedTile.Foreground = Brushes.Transparent;

                        // load image
                        ImageBrush IB = new ImageBrush();
                        IB.ImageSource = new BitmapImage(new Uri(ImageUrl));
                        FeedTile.Background = IB;

                        // add event
                        FeedTile.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(FeedTile_PreviewMouseLeftButtonDown);
                        FeedTile.PreviewMouseRightButtonDown += new MouseButtonEventHandler(FeedTile_PreviewMouseRightButtonDown);
                        //FeedTile.MouseDoubleClick += new MouseButtonEventHandler(FeedTile_MouseDoubleClick); // show individual photo
                        //FeedTile.MouseEnter += new MouseEventHandler(FeedTile_MouseEnter);

                        Destination.Children.Add(FeedTile);
                    }), null);
        }

        /***************************************************************************************************/

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

                        FeedTile.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(LoadMoreTile_PreviewMouseLeftButtonDown);

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
        ///     adds tile to the flip view
        /// </summary>
        /// <param name="ImageId"></param>
        /// <param name="ImageUrl"></param>
        /// <param name="Side"></param>
        /// <param name="Destination"></param>
        private void LoadFlipViewTiles(String ImageId, String ImageUrl, int Side, FlipView Destination)
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
                       //FeedTile.PreviewMouseDown += new MouseButtonEventHandler(FeedTile_MouseLeftButtonDown); // select / deselect the photo
                       //FeedTile.MouseDoubleClick += new MouseButtonEventHandler(FeedTile_MouseDoubleClick); // show individual photo
                       //FeedTile.MouseEnter += new MouseEventHandler(FeedTile_MouseEnter);

                       Destination.Items.Add(FeedTile);
                   }), null);
        }

        /***************************************************************************************************/

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
        ///     collapses progress ring
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
        ///     simple error dialog
        /// </summary>
        private void ShowNoMediaSelectedError()
        {
            ThreadStart Thrs = new ThreadStart(
                delegate()
                {
                    Dispatcher.Invoke(
                        new Action(
                            async delegate()
                            {
                                await this.ShowMessageAsync("Nothing to select", "don't be in such a hurry! i'm fetching the media for you :)", MessageDialogStyle.Affirmative, null);
                            }), null);
                });
            new Thread(Thrs).Start();
        }

        /***************************************************************************************************/

        /// <summary>
        ///     de selects all the selected tiles
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="TileNames"></param>
        private void DeselectAllTiles(Canvas Sender, List<String> TileNames)
        {
            foreach (String Name in TileNames)
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
                                                this.Close();
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
    }
}
