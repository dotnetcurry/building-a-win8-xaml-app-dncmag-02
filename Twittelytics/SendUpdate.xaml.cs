using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinqToTwitter;
using Twittelytics.Common;
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
        TweetItem _inReplyToStatusID;

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
            _inReplyToStatusID = navigationParameter != null ? navigationParameter as TweetItem : null;
            if (_inReplyToStatusID != null)
            {
                updateText.Text = "@" + _inReplyToStatusID.Subtitle;
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
            if (SuspensionManager.SessionState.ContainsKey("Authorizer"))
            {
                _auth = SuspensionManager.SessionState["Authorizer"] as PinAuthorizer;
                if (_auth != null)
                {
                    using (var twitterCtx = new TwitterContext(_auth))
                    {
                        Status tweet = null;
                        if (_inReplyToStatusID != null)
                        {
                            tweet = twitterCtx.UpdateStatus(updateText.Text, true, _inReplyToStatusID.UniqueId);
                        }
                        else
                        {
                            tweet = twitterCtx.UpdateStatus(updateText.Text, true);
                        }
                        if (tweet!=null && !string.IsNullOrEmpty(tweet.StatusID))
                        {
                            updateText.Text = string.Empty;
                            statusText.Text = "Tweet Sent Successfully";
                        }
                    }
                }
            }
        }

    }
}
