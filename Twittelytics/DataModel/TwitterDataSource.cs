using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using LinqToTwitter;
using Twittelytics.Common;
using Twittelytics.DataModel;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// The data model defined by this file serves as a representative example of a strongly-typed
// model that supports notification when members are added, removed, or modified.  The property
// names chosen coincide with data bindings in the standard item templates.
//
// Applications may use this model as a starting point and build on it, or discard it entirely and
// replace it with something appropriate to their needs.

namespace Twittelytics.Data
{
    /// <summary>
    /// Creates a collection of groups and items with hard-coded content.
    /// </summary>
    public sealed class TwitterDataSource
    {
        private static TwitterDataSource _sampleDataSource = new TwitterDataSource();
        private ObservableCollection<TwitterList> _allGroups = new ObservableCollection<TwitterList>();
        private Dictionary<string, ImageSource> _imageCache = new Dictionary<string, ImageSource>();
        private PinAuthorizer _auth;
        private DispatcherTimer _timer;

        public ObservableCollection<TwitterList> AllGroups
        {
            get
            {
                if (_sampleDataSource._allGroups.Count == 0)
                {
                    _sampleDataSource.InitGroups();
                }
                return _sampleDataSource._allGroups;
            }
        }

        public TweetItem LoggedInUser { get; set; }

        public static IEnumerable<TwitterList> GetGroups(string uniqueId)
        {
            if (!uniqueId.Equals("AllGroups")) throw new ArgumentException("Only 'AllGroups' is supported as a collection of groups");
            if (!_sampleDataSource._timer.IsEnabled)
            {
                _sampleDataSource.Refresh();
                _sampleDataSource._timer.Start();
            }
            return _sampleDataSource.AllGroups;
        }

        public static TweetItem GetCurrentUser()
        {
            return _sampleDataSource.LoggedInUser;
        }

        public static TwitterList GetGroup(string uniqueId)
        {
            // Simple linear search is acceptable for small data sets
            var matches = _sampleDataSource.AllGroups.Where((group) => group.UniqueId.Equals(uniqueId)).ToList<TwitterList>();


            if (matches.Count() == 1) return matches.First();

            return null;
        }

        public static TweetItem GetItem(string uniqueId)
        {
            // Simple linear search is acceptable for small data sets
            var matches = _sampleDataSource.AllGroups.SelectMany(group => group.Items).Where((item) => item.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        public TwitterDataSource()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = new TimeSpan(0, 0, 30);
            _timer.Tick += _timer_Tick;
            InitGroups();
        }

        void _timer_Tick(object sender, object e)
        {
            Refresh();
        }

        private void InitGroups()
        {
            if (SuspensionManager.SessionState.ContainsKey("Authorizer"))
            {
                _auth = SuspensionManager.SessionState["Authorizer"] as PinAuthorizer;
                if (_auth != null)
                {
                    using (var twitterCtx = new TwitterContext(_auth))
                    {
                        string screenName = twitterCtx.AuthorizedClient.ScreenName;

                        var users = (from usr in twitterCtx.User
                                     where usr.Type == UserType.Lookup &&
                                     usr.ScreenName == screenName
                                     select usr).ToList();
                        this.LoggedInUser = new TweetItem(users[0].ID, users[0].Name, users[0].ScreenName, users[0].ProfileImageUrl, "", "", null);
                        TwitterList timeline = new TwitterList("timeline", "Timeline", "Your Tweet Stream", null, users[0].Status.Text);
                        this._allGroups.Add(timeline);
                        TwitterList atMentions = new TwitterList("atMentions", "@ Connect", "Your @ Mentions", null, users[0].Status.Text);
                        this._allGroups.Add(atMentions);
                    }
                }
            }
            else if (!SuspensionManager.SessionState.ContainsKey("Authorizer"))
            {
                LocalDataCredentials cred = new LocalDataCredentials();
                if (cred.ToString() != ",,,,,")
                {
                    SuspensionManager.SessionState["SavedAuthorizer"] = cred;
                    try
                    {
                        PinAuthorizer auth = new PinAuthorizer
                        {
                            Credentials = (LocalDataCredentials)SuspensionManager.SessionState["SavedAuthorizer"],
                            UseCompression = true
                        };
                        SuspensionManager.SessionState["Authorizer"] = auth;
                        auth.ScreenName = cred.ScreenName;
                        auth.UserId = cred.UserId;

                    }
                    catch (Exception ex)
                    {
                        ((LocalDataCredentials)SuspensionManager.SessionState["SavedAuthorizer"]).Clear();
                    }
                }
            }
        }

        private async void RefreshTimeLine(TwitterList matches, StatusType type)
        {
            if (SuspensionManager.SessionState.ContainsKey("Authorizer"))
            {
                _auth = SuspensionManager.SessionState["Authorizer"] as PinAuthorizer;
            }
            if (_auth != null)
            {
                try
                {
                    using (var twitterCtx = new TwitterContext(_auth))
                    {
                        var timelineResponse =
                            (from tweet in twitterCtx.Status
                             where tweet.Type == type &&
                             tweet.ScreenName == LoggedInUser.Title
                             select tweet)
                            .ToList();

                        IEnumerable<TweetItem> tweets =
                            (from tweet in timelineResponse
                             select new TweetItem(tweet.StatusID,
                                 tweet.User.Name,
                                 tweet.User.Identifier.ScreenName,
                                 tweet.User.ProfileImageUrl,
                                 tweet.Text,
                                 tweet.Text,
                                 matches)
                            );
                        if (tweets.Count<TweetItem>() > 0)
                        {
                            IEnumerable<TweetItem> results = 
                                matches.Items.Union<TweetItem>(tweets, 
                                    new UniqueIdCompararer()).OrderByDescending
                                        <TweetItem, string>
                                            (k => k.UniqueId);
                            matches.Items.Clear();
                            foreach (var item in results)
                            {
                                matches.Items.Add(item);
                            }
                        }
                    }
                    await CreateCollage();
                }
                catch (LitJson.JsonException je)
                {
                    //TODO: Log the Json Exception and move on.
                }
            }
        }

        internal static void Clear(string navigationParameter)
        {
            _sampleDataSource.AllGroups.Clear();
            _sampleDataSource.LoggedInUser = null;
        }

        internal static void Retweet(TweetItem selectedItem)
        {
            if (_sampleDataSource._auth == null)
            {
                _sampleDataSource.InitGroups();
            }
            using (var twitterCtx = new TwitterContext(_sampleDataSource._auth))
            {
                Status response = twitterCtx.Retweet(selectedItem.UniqueId);
            }
        }

        internal static Status SendUpdate(string updateText, TweetItem inRepyTo)
        {
            if (_sampleDataSource._auth == null)
            {
                _sampleDataSource.InitGroups();
            }
            using (var twitterCtx = new TwitterContext(_sampleDataSource._auth))
            {
                Status tweet = null;
                if (inRepyTo != null)
                {
                    tweet = twitterCtx.UpdateStatus(updateText, true, inRepyTo.UniqueId);
                }
                else
                {
                    tweet = twitterCtx.UpdateStatus(updateText, true);
                }
                if (tweet != null && !string.IsNullOrEmpty(tweet.StatusID))
                {
                    updateText = string.Empty;
                }
                return tweet;
            }
        }
        internal static void Favorite(TweetItem selectedItem)
        {
            if (_sampleDataSource._auth == null)
            {
                _sampleDataSource.InitGroups();
            }
            using (var twitterCtx = new TwitterContext(_sampleDataSource._auth))
            {
                Status response = twitterCtx.CreateFavorite(selectedItem.UniqueId);
            }
        }


        async Task CreateCollage()
        {
            var sampleDataGroups = this._allGroups;
            if (sampleDataGroups.Count() > 0 && sampleDataGroups.ToList()[0].TopItems.Count() == 0) return;
            List<TwitterList> list = sampleDataGroups.ToList();

            foreach (var currentList in list)
            {
                try
                {
                    IEnumerable<TweetItem> topItems = currentList.TopItems;

                    List<Uri> uris = (from tweetItem in topItems
                                      select ((BitmapImage)tweetItem.Image).UriSource).ToList<Uri>();

                    if (uris.Count > 0)
                    {
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
                                PixelDataProvider pixelProvider = await frame.GetPixelDataAsync();
                                srcPixels = pixelProvider.DetachPixelData();
                                wid = (int)frame.PixelWidth;
                                hgt = (int)frame.PixelHeight;
                                bitmap = new WriteableBitmap(wid, hgt);
                            }
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
                        currentList.Image = destination;
                        ((WriteableBitmap)currentList.Image).Invalidate();
                    }

                }
                catch (Exception ex)
                {
                    // TODO: Log Error, unable to render image
                }
            }
        }

        public static void RefreshAll()
        {
            _sampleDataSource.Refresh();
        }

        internal void Refresh()
        {
            CoreDispatcher dispatcher = Window.Current.Dispatcher;
            Parallel.ForEach(_sampleDataSource.AllGroups, item =>
            {
                dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                {
                    var matches = _sampleDataSource.AllGroups.Where(
                        (group) => 
                            group.UniqueId.Equals(item.UniqueId)).ToList<TwitterList>();
                    if (matches.Count > 0)
                    {
                        if (item.UniqueId == "timeline")
                        {
                            _sampleDataSource.RefreshTimeLine(matches.First(), 
                                StatusType.Home);
                        }
                        else if (item.UniqueId == "atMentions")
                        {
                            _sampleDataSource.RefreshTimeLine(matches.First(), 
                                StatusType.Mentions);
                        }
                    }
                });
            });
            if (!_timer.IsEnabled)
            {
                _timer.Start();
            }
        }
    }
}
