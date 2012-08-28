using System;
using System.Collections.Generic;
using LinqToTwitter;
using Twittelytics.Common;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Twittelytics
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class Login : Twittelytics.Common.LayoutAwarePage
    {
        PinAuthorizer auth;
        public Login()
        {
            this.InitializeComponent();
            this.Loaded += Login_Loaded;
            OAuthWebBrowser.LoadCompleted += OAuthWebBrowser_LoadCompleted;
        }

        private void OAuthWebBrowser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            if (e.Content != null)
            {
                string html = e.Content.ToString();
            }
            PinTextBox.IsEnabled = true;
            AuthenticatePinButton.IsEnabled = true;
        }

        void Login_Loaded(object sender, RoutedEventArgs e)
        {
            auth = new PinAuthorizer
            {
                Credentials = new InMemoryCredentials
                {
                    ConsumerKey = "",
                    ConsumerSecret = ""
                },
                UseCompression = true, 
                GoToTwitterAuthorization = pageLink =>
                    Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        () => OAuthWebBrowser.Navigate(
                            new Uri(pageLink, UriKind.Absolute))).AsTask().Wait()
            };

            auth.BeginAuthorize(resp =>
                Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    switch (resp.Status)
                    {
                        case TwitterErrorStatus.Success:
                            break;
                        case TwitterErrorStatus.RequestProcessingException:
                        case TwitterErrorStatus.TwitterApiError:
                            new MessageDialog(resp.Error.ToString(), 
                                resp.Message).ShowAsync().AsTask().Wait();
                            break;
                    }
                }).AsTask().Wait());
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
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
        }

        private void AuthenticatePinButton_Click(object sender, RoutedEventArgs e)
        {
            auth.CompleteAuthorize(
                PinTextBox.Text,
                completeResp => Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    switch (completeResp.Status)
                    {
                        case TwitterErrorStatus.Success:
                            SaveAuthInfo(completeResp);
                            Frame.Navigate(typeof(ItemsPage), "AllGroups");
                            break;
                        case TwitterErrorStatus.RequestProcessingException:
                        case TwitterErrorStatus.TwitterApiError:
                            new MessageDialog(completeResp.Error.ToString(), 
                                completeResp.Message).ShowAsync().AsTask().Wait();
                            break;
                    }
                }).AsTask().Wait());
        }

        private void SaveAuthInfo(TwitterAsyncResponse<UserIdentifier> completeResp)
        {
            auth.UserId = completeResp.State.UserID;
            auth.ScreenName = completeResp.State.ScreenName;
            SuspensionManager.SessionState["Authorizer"] = auth;
            LocalDataCredentials cred = new LocalDataCredentials
                {
                    AccessToken = auth.Credentials.AccessToken,
                    ConsumerKey = auth.Credentials.ConsumerKey,
                    ConsumerSecret = auth.Credentials.ConsumerSecret,
                    OAuthToken = auth.Credentials.OAuthToken,
                    ScreenName = completeResp.State.ScreenName,
                    UserId = completeResp.State.UserID
                };
            cred.Save();
            SuspensionManager.SessionState["SavedAuthorizer"] = auth;
        }
    }
}
