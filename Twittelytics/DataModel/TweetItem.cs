using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Twittelytics.DataModel
{

    /// <summary>
    /// Generic item data model.
    /// </summary>
    public class TweetItem : TwitterDataCommon
    {
        public TweetItem(String uniqueId, String title, String subtitle, String imagePath, String description, String content, TwitterList group)
            : base(uniqueId, title, subtitle, imagePath, description)
        {
            this._content = content;
            this._group = group;
        }

        private string _content = string.Empty;
        public string Content
        {
            get { return this._content; }
            set { this.SetProperty(ref this._content, value); }
        }

        private List<string> _contents = new List<string>();

        public List<string> Contents
        {
            get {
                if (_contents.Count == 0)
                {
                    _contents.Add(Content);
                }
                return _contents; 
            }
            set { _contents = value; }
        }


        private TwitterList _group;
        public TwitterList Group
        {
            get { return this._group; }
            set { this.SetProperty(ref this._group, value); }
        }

    }

    public class UniqueIdCompararer : IEqualityComparer<TweetItem>
    {
        public bool Equals(TweetItem x, TweetItem y)
        {
            return x.UniqueId == y.UniqueId;
        }

        public int GetHashCode(TweetItem obj)
        {
            return obj.UniqueId.GetHashCode();
        }
    }

}
