using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinqToTwitter;
using Twittelytics.Common;
using Twittelytics.Data;
using Twittelytics.DataModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Twittelytics
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class SendUpdate : Twittelytics.Common.LayoutAwarePage
    {
        PinAuthorizer _auth;
        TweetItem _inReplyToStatus;

        public SendUpdate()
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
            _inReplyToStatus = navigationParameter != null ? navigationParameter as TweetItem : null;
            if (_inReplyToStatus != null)
            {
                updateText.Text = "@" + _inReplyToStatus.Subtitle;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

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

        private void updateText_TextChanged(object sender, RoutedEventArgs e)
        {
            tweetCharactersLeft.Text = (140 - updateText.Text.Length).ToString() + " characters left";
            statusText.Text = string.Empty;
        }

        private void tweetCharactersLeft_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void sendUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            Status tweet = TwitterDataSource.SendUpdate(updateText.Text, _inReplyToStatus);
            if (tweet != null && !string.IsNullOrEmpty(tweet.StatusID))
            {
                statusText.Text = string.Empty;
            }

        }

    }
}
