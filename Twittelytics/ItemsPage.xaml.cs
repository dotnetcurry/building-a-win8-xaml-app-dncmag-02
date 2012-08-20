using Twittelytics.Data;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using LinqToTwitter;
using Twittelytics.Common;
using Twittelytics.DataModel;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.UI;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;

// The Items Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234233

namespace Twittelytics
{
    /// <summary>
    /// A page that displays a collection of item previews.  In the Split Application this page
    /// is used to display and select one of the available groups.
    /// </summary>
    public sealed partial class ItemsPage : Twittelytics.Common.LayoutAwarePage
    {
        PinAuthorizer auth;

        public ItemsPage()
        {
            this.InitializeComponent();
        }


        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            // TODO: Create an appropriate data model for your problem domain to replace the sample data
            BindUserData((string)navigationParameter);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            BindUserData("AllGroups");
        }


        async private void CreateCollage()
        {
            var sampleDataGroups = TwitterDataSource.GetGroups("AllGroups");
            if (sampleDataGroups.Count() > 0 && sampleDataGroups.ToList()[0].TopItems.Count() == 0) return;

            TwitterList currentList = sampleDataGroups.ToList()[0];

            IEnumerable<TweetItem> topItems = sampleDataGroups.ToList()[0].TopItems;

            List<Uri> uris = (from tweetItem in topItems
                                      select ((BitmapImage)tweetItem.Image).UriSource).ToList<Uri>();

            int number = (int)Math.Ceiling(Math.Sqrt((double)uris.Count));
            WriteableBitmap destination = new WriteableBitmap(48 * number, 48 * number);
            int col = 0;
            int row = 0;
            destination.Clear(Colors.Transparent);
            WriteableBitmap bitmap;
            foreach (var uri1 in uris)
            {
                RandomAccessStreamReference streamRef = RandomAccessStreamReference.CreateFromUri(uri1);
                int wid = 0;
                int hgt = 0;
                byte[] srcPixels;
                using (IRandomAccessStreamWithContentType fileStream = await streamRef.OpenReadAsync())
                {
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);
                    BitmapFrame frame = await decoder.GetFrameAsync(0);

                    // I know the parameterless version of GetPixelDataAsync works for this image
                    PixelDataProvider pixelProvider = await frame.GetPixelDataAsync();
                    srcPixels = pixelProvider.DetachPixelData();

                    // Create the WriteableBitmap
                    wid = (int)frame.PixelWidth;
                    hgt = (int)frame.PixelHeight;
                    bitmap = new WriteableBitmap(wid, hgt);
                }

                //// Set the bitmap to the Image element
                Stream pixelStream1 = bitmap.PixelBuffer.AsStream();

                pixelStream1.Seek(0, SeekOrigin.Begin);
                pixelStream1.Write(srcPixels, 0, (int)srcPixels.Length);
                bitmap.Invalidate();

                if (row < number)
                {
                    destination.Blit(new Rect(col * wid, row * hgt, wid, hgt), bitmap, new Rect(0, 0, wid, hgt));
                    col++;
                    if (col >= number)
                    {
                        row++;
                        col = 0;
                    }
                }
            }
            currentList.Collage = destination;
            ((WriteableBitmap)currentList.Collage).Invalidate();
        }
        /// <summary>
        /// Invoked when an item is clicked.
        /// </summary>
        /// <param name="sender">The GridView (or ListView when the application is snapped)
        /// displaying the item clicked.</param>
        /// <param name="e">Event data that describes the item clicked.</param>
        void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Navigate to the appropriate destination page, configuring the new page
            // by passing required information as a navigation parameter
            var groupId = ((TwitterList)e.ClickedItem).UniqueId;
            this.Frame.Navigate(typeof(SplitPage), groupId);
        }

        private void Image_Tapped_1(object sender, TappedRoutedEventArgs e)
        {
            NavigateToLogin();
        }

        private void BindUserData(string navigationParameter)
        {
            try
            {
                if (auth == null)
                {
                    Authenticate();
                }
                if (auth != null)
                {
                    var twitterCtx = new TwitterContext(auth);
                    string screenName = twitterCtx.AuthorizedClient.ScreenName;

                    var users = (from usr in twitterCtx.User
                                 where usr.Type == UserType.Lookup &&
                                 usr.ScreenName == screenName
                                 select usr).ToList();

                    userIdText.Text = users[0].ScreenName;
                    userName.Text = users[0].Name;
                    var sampleDataGroups = TwitterDataSource.GetGroups(navigationParameter);
                    userImage.Source = TwitterDataSource.GetCurrentUser().Image;
                    this.DefaultViewModel["Items"] = sampleDataGroups;
                    CreateCollage();
                }
                else
                {
                    TwitterDataSource.Clear(navigationParameter);
                    var sampleDataGroups = TwitterDataSource.GetGroups(navigationParameter);
                    userImage.Source = null;
                    userIdText.Text = "";
                    userName.Text = "Log in";
                    this.DefaultViewModel["Items"] = sampleDataGroups;
                    CreateCollage();
                }
            }
            catch (NullReferenceException ex)
            {
                //((IsolatedStorageCredentials)SuspensionManager.SessionState["SavedAuthorizer"]).Clear();
            }

        }

        private void NavigateToLogin()
        {
            if (auth == null &&
                (!SuspensionManager.SessionState.ContainsKey("SavedAuthorizer") ||
                SuspensionManager.SessionState["SavedAuthorizer"] == null))
            {
                this.Frame.Navigate(typeof(Login));
            }
            else
            {
                if (!SuspensionManager.SessionState.ContainsKey("Authorizer") ||
                    SuspensionManager.SessionState["Authorizer"] == null)
                {
                    Authenticate();
                    BindUserData("AllGroups");
                }
                else
                {
                    MessageDialog dialog = new MessageDialog("Are you sure you want to Log Out?", "Logout");
                    // Add commands and set their callbacks
                    dialog.Commands.Add(new UICommand("Logout", (command) =>
                    {
                        Logout();
                    }));

                    dialog.Commands.Add(new UICommand("Cancel", (command) =>
                    {

                    }));
                    dialog.DefaultCommandIndex = 1;
                    dialog.ShowAsync();
                }

            }
        }

        private void Logout()
        {
            IsolatedStorageCredentials cred = (IsolatedStorageCredentials)SuspensionManager.SessionState["SavedAuthorizer"];
            cred.Clear();
            cred = null;
            SuspensionManager.SessionState["SavedAuthorizer"] = null;
            SuspensionManager.SessionState.Remove("SavedAuthorizer");
            SuspensionManager.SessionState["Authorizer"] = null;
            SuspensionManager.SessionState.Remove("Authorizer");
            auth = null;
            BindUserData("AllGroups");
        }

        private void Authenticate()
        {
            if (SuspensionManager.SessionState.ContainsKey("Authorizer"))
            {
                auth = SuspensionManager.SessionState["Authorizer"] as PinAuthorizer;
            }
            else if (SuspensionManager.SessionState.ContainsKey("SavedAuthorizer") &&
                SuspensionManager.SessionState["SavedAuthorizer"] != null)
            {
                IsolatedStorageCredentials cred = (IsolatedStorageCredentials)SuspensionManager.SessionState["SavedAuthorizer"];
                auth = new PinAuthorizer
                {
                    Credentials = cred,
                    UseCompression = true
                };
                SuspensionManager.SessionState["Authorizer"] = auth;
                auth.ScreenName = cred.ScreenName;
                auth.UserId = cred.UserId;
            }
        }

        private void userIdText_Tapped(object sender, TappedRoutedEventArgs e)
        {
            NavigateToLogin();
        }

        private void userName_Tapped(object sender, TappedRoutedEventArgs e)
        {
            NavigateToLogin();
        }
    }
}
