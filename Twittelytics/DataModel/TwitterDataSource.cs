using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.Resources.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Twittelytics.DataModel;
using Twittelytics.Common;
using LinqToTwitter;

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
        private PinAuthorizer _auth;

        public ObservableCollection<TwitterList> AllGroups
        {
            get
            {
                if (_sampleDataSource._allGroups.Count == 0)
                {
                    _sampleDataSource.InitGroups();
                }
                return this._allGroups;
            }
        }

        public TweetItem LoggedInUser { get; set; }


        public static IEnumerable<TwitterList> GetGroups(string uniqueId)
        {
            if (!uniqueId.Equals("AllGroups")) throw new ArgumentException("Only 'AllGroups' is supported as a collection of groups");

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

            if (matches.Count > 0)
            {
                if (uniqueId == "timeline")
                {
                    _sampleDataSource.RefreshTimeLine(matches.First(), StatusType.Home);
                }
                else if (uniqueId == "atMentions")
                {
                    _sampleDataSource.RefreshTimeLine(matches.First(), StatusType.Mentions);
                }
                if (matches.Count() == 1) return matches.First();
            }
            return null;
        }

        private void RefreshTimeLine(TwitterList matches, StatusType type)
        {

            if (!SuspensionManager.SessionState.ContainsKey("Authorizer"))
            {
                IsolatedStorageCredentials cred = new IsolatedStorageCredentials();
                SuspensionManager.SessionState["SavedAuthorizer"] = cred;
                try
                {
                    PinAuthorizer auth = new PinAuthorizer
                    {
                        Credentials = (IsolatedStorageCredentials)SuspensionManager.SessionState["SavedAuthorizer"],
                        UseCompression = true
                    };
                    SuspensionManager.SessionState["Authorizer"] = auth;
                    auth.ScreenName = cred.ScreenName;
                    auth.UserId = cred.UserId;

                }
                catch (Exception ex)
                {
                    ((IsolatedStorageCredentials)SuspensionManager.SessionState["SavedAuthorizer"]).Clear();
                }
            }
            if(SuspensionManager.SessionState.ContainsKey("Authorizer"))
            {
                _auth = SuspensionManager.SessionState["Authorizer"] as PinAuthorizer;
                if (_auth != null)
                {
                    using (var twitterCtx = new TwitterContext(_auth))
                    {
                        var timelineResponse =
                            (from tweet in twitterCtx.Status
                             where tweet.Type == type &&
                             tweet.ScreenName == LoggedInUser.Title
                             select tweet)
                            .ToList();

                        List<TweetItem> tweets =
                            (from tweet in timelineResponse
                             select new TweetItem(tweet.StatusID,
                                 tweet.User.Name,
                                 tweet.User.Identifier.ScreenName,
                                 tweet.User.ProfileImageUrl,
                                 tweet.Text,
                                 tweet.Text,
                                 matches)
                            ).ToList();
                        foreach (var item in tweets)
                        {
                            matches.Items.Add(item);
                        }
                    }
                }
            }
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
            InitGroups();

            #region "Hard Coded Stuff"
            //String ITEM_CONTENT = String.Format("Item Content: {0}\n\n{0}\n\n{0}\n\n{0}\n\n{0}\n\n{0}\n\n{0}",
            //            "Curabitur class aliquam vestibulum nam curae maecenas sed integer cras phasellus suspendisse quisque donec dis praesent accumsan bibendum pellentesque condimentum adipiscing etiam consequat vivamus dictumst aliquam duis convallis scelerisque est parturient ullamcorper aliquet fusce suspendisse nunc hac eleifend amet blandit facilisi condimentum commodo scelerisque faucibus aenean ullamcorper ante mauris dignissim consectetuer nullam lorem vestibulum habitant conubia elementum pellentesque morbi facilisis arcu sollicitudin diam cubilia aptent vestibulum auctor eget dapibus pellentesque inceptos leo egestas interdum nulla consectetuer suspendisse adipiscing pellentesque proin lobortis sollicitudin augue elit mus congue fermentum parturient fringilla euismod feugiat");
            //var group1 = new TwitterList("Group-1",
            //        "Group Title: 1",
            //        "Group Subtitle: 1",
            //        "Assets/DarkGray.png",
            //        "Group Description: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus tempor scelerisque lorem in vehicula. Aliquam tincidunt, lacus ut sagittis tristique, turpis massa volutpat augue, eu rutrum ligula ante a ante");
            //group1.Items.Add(new TweetItem("Group-1-Item-1",
            //        "Item Title: 1",
            //        "Item Subtitle: 1",
            //        "Assets/LightGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group1));
            //group1.Items.Add(new TweetItem("Group-1-Item-2",
            //        "Item Title: 2",
            //        "Item Subtitle: 2",
            //        "Assets/DarkGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group1));
            //group1.Items.Add(new TweetItem("Group-1-Item-3",
            //        "Item Title: 3",
            //        "Item Subtitle: 3",
            //        "Assets/MediumGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group1));
            //group1.Items.Add(new TweetItem("Group-1-Item-4",
            //        "Item Title: 4",
            //        "Item Subtitle: 4",
            //        "Assets/DarkGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group1));
            //group1.Items.Add(new TweetItem("Group-1-Item-5",
            //        "Item Title: 5",
            //        "Item Subtitle: 5",
            //        "Assets/MediumGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group1));
            //this.AllGroups.Add(group1);

            //var group2 = new TwitterList("Group-2",
            //        "Group Title: 2",
            //        "Group Subtitle: 2",
            //        "Assets/LightGray.png",
            //        "Group Description: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus tempor scelerisque lorem in vehicula. Aliquam tincidunt, lacus ut sagittis tristique, turpis massa volutpat augue, eu rutrum ligula ante a ante");
            //group2.Items.Add(new TweetItem("Group-2-Item-1",
            //        "Item Title: 1",
            //        "Item Subtitle: 1",
            //        "Assets/DarkGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group2));
            //group2.Items.Add(new TweetItem("Group-2-Item-2",
            //        "Item Title: 2",
            //        "Item Subtitle: 2",
            //        "Assets/MediumGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group2));
            //group2.Items.Add(new TweetItem("Group-2-Item-3",
            //        "Item Title: 3",
            //        "Item Subtitle: 3",
            //        "Assets/LightGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group2));
            //this.AllGroups.Add(group2);

            //var group3 = new TwitterList("Group-3",
            //        "Group Title: 3",
            //        "Group Subtitle: 3",
            //        "Assets/MediumGray.png",
            //        "Group Description: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus tempor scelerisque lorem in vehicula. Aliquam tincidunt, lacus ut sagittis tristique, turpis massa volutpat augue, eu rutrum ligula ante a ante");
            //group3.Items.Add(new TweetItem("Group-3-Item-1",
            //        "Item Title: 1",
            //        "Item Subtitle: 1",
            //        "Assets/MediumGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group3));
            //group3.Items.Add(new TweetItem("Group-3-Item-2",
            //        "Item Title: 2",
            //        "Item Subtitle: 2",
            //        "Assets/LightGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group3));
            //group3.Items.Add(new TweetItem("Group-3-Item-3",
            //        "Item Title: 3",
            //        "Item Subtitle: 3",
            //        "Assets/DarkGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group3));
            //group3.Items.Add(new TweetItem("Group-3-Item-4",
            //        "Item Title: 4",
            //        "Item Subtitle: 4",
            //        "Assets/LightGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group3));
            //group3.Items.Add(new TweetItem("Group-3-Item-5",
            //        "Item Title: 5",
            //        "Item Subtitle: 5",
            //        "Assets/MediumGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group3));
            //group3.Items.Add(new TweetItem("Group-3-Item-6",
            //        "Item Title: 6",
            //        "Item Subtitle: 6",
            //        "Assets/DarkGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group3));
            //group3.Items.Add(new TweetItem("Group-3-Item-7",
            //        "Item Title: 7",
            //        "Item Subtitle: 7",
            //        "Assets/MediumGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group3));
            //this.AllGroups.Add(group3);

            //var group4 = new TwitterList("Group-4",
            //        "Group Title: 4",
            //        "Group Subtitle: 4",
            //        "Assets/LightGray.png",
            //        "Group Description: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus tempor scelerisque lorem in vehicula. Aliquam tincidunt, lacus ut sagittis tristique, turpis massa volutpat augue, eu rutrum ligula ante a ante");
            //group4.Items.Add(new TweetItem("Group-4-Item-1",
            //        "Item Title: 1",
            //        "Item Subtitle: 1",
            //        "Assets/DarkGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group4));
            //group4.Items.Add(new TweetItem("Group-4-Item-2",
            //        "Item Title: 2",
            //        "Item Subtitle: 2",
            //        "Assets/LightGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group4));
            //group4.Items.Add(new TweetItem("Group-4-Item-3",
            //        "Item Title: 3",
            //        "Item Subtitle: 3",
            //        "Assets/DarkGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group4));
            //group4.Items.Add(new TweetItem("Group-4-Item-4",
            //        "Item Title: 4",
            //        "Item Subtitle: 4",
            //        "Assets/LightGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group4));
            //group4.Items.Add(new TweetItem("Group-4-Item-5",
            //        "Item Title: 5",
            //        "Item Subtitle: 5",
            //        "Assets/MediumGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group4));
            //group4.Items.Add(new TweetItem("Group-4-Item-6",
            //        "Item Title: 6",
            //        "Item Subtitle: 6",
            //        "Assets/LightGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group4));
            //this.AllGroups.Add(group4);

            //var group5 = new TwitterList("Group-5",
            //        "Group Title: 5",
            //        "Group Subtitle: 5",
            //        "Assets/MediumGray.png",
            //        "Group Description: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus tempor scelerisque lorem in vehicula. Aliquam tincidunt, lacus ut sagittis tristique, turpis massa volutpat augue, eu rutrum ligula ante a ante");
            //group5.Items.Add(new TweetItem("Group-5-Item-1",
            //        "Item Title: 1",
            //        "Item Subtitle: 1",
            //        "Assets/LightGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group5));
            //group5.Items.Add(new TweetItem("Group-5-Item-2",
            //        "Item Title: 2",
            //        "Item Subtitle: 2",
            //        "Assets/DarkGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group5));
            //group5.Items.Add(new TweetItem("Group-5-Item-3",
            //        "Item Title: 3",
            //        "Item Subtitle: 3",
            //        "Assets/LightGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group5));
            //group5.Items.Add(new TweetItem("Group-5-Item-4",
            //        "Item Title: 4",
            //        "Item Subtitle: 4",
            //        "Assets/MediumGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group5));
            //this.AllGroups.Add(group5);

            //var group6 = new TwitterList("Group-6",
            //        "Group Title: 6",
            //        "Group Subtitle: 6",
            //        "Assets/DarkGray.png",
            //        "Group Description: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus tempor scelerisque lorem in vehicula. Aliquam tincidunt, lacus ut sagittis tristique, turpis massa volutpat augue, eu rutrum ligula ante a ante");
            //group6.Items.Add(new TweetItem("Group-6-Item-1",
            //        "Item Title: 1",
            //        "Item Subtitle: 1",
            //        "Assets/LightGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group6));
            //group6.Items.Add(new TweetItem("Group-6-Item-2",
            //        "Item Title: 2",
            //        "Item Subtitle: 2",
            //        "Assets/DarkGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group6));
            //group6.Items.Add(new TweetItem("Group-6-Item-3",
            //        "Item Title: 3",
            //        "Item Subtitle: 3",
            //        "Assets/MediumGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group6));
            //group6.Items.Add(new TweetItem("Group-6-Item-4",
            //        "Item Title: 4",
            //        "Item Subtitle: 4",
            //        "Assets/DarkGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group6));
            //group6.Items.Add(new TweetItem("Group-6-Item-5",
            //        "Item Title: 5",
            //        "Item Subtitle: 5",
            //        "Assets/LightGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group6));
            //group6.Items.Add(new TweetItem("Group-6-Item-6",
            //        "Item Title: 6",
            //        "Item Subtitle: 6",
            //        "Assets/MediumGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group6));
            //group6.Items.Add(new TweetItem("Group-6-Item-7",
            //        "Item Title: 7",
            //        "Item Subtitle: 7",
            //        "Assets/DarkGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group6));
            //group6.Items.Add(new TweetItem("Group-6-Item-8",
            //        "Item Title: 8",
            //        "Item Subtitle: 8",
            //        "Assets/LightGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        group6));
            //this.AllGroups.Add(group6);
            #endregion
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
        }

        internal static void Clear(string navigationParameter)
        {
            _sampleDataSource.AllGroups.Clear();
            _sampleDataSource.LoggedInUser = null;
        }
    }
}
