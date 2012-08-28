using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Markup;

namespace Twittelytics.Common
{
    public class ParagraphTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DefaultTemplate { get; set; }

        
        protected override DataTemplate SelectTemplateCore(
            object item, DependencyObject container)
        {
            if (((ListViewItem)container).ContentTemplate == null)
            {
                string template = String.Format(@"
            <DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                <RichTextBlock>
                    <Paragraph>
                        {0}
                    </Paragraph>
                </RichTextBlock>
            </DataTemplate>", TokenizeTweet(item.ToString()));
                return XamlReader.Load(template) as DataTemplate;
            }
            else
            {
                return ((ListViewItem)container).ContentTemplate;
            }
        }

        private string TokenizeTweet(string currentValue)
        {
            string [] tweetTokens= currentValue.Split(default (Char[]), StringSplitOptions.RemoveEmptyEntries);
            StringBuilder builder = new StringBuilder();
            foreach (var token in tweetTokens)
            {
                if (token.StartsWith(@"@"))
                {
                    builder.AppendLine(AddAtLink(token));
                }
                else if (token.StartsWith(@"#"))
                {
                    builder.AppendLine(AddHashLink(token));
                }
                else if (token.StartsWith(@"http://t.co"))
                {
                    builder.AppendLine(AddHyperLink(token));
                }
                else
                {
                    builder.AppendLine("<Run Text = '" +  System.Net.WebUtility.HtmlEncode(token) + "' />");
                }
            }
            return builder.ToString();
        }

        private string AddHyperLink(string token)
        {
            StringBuilder container = new StringBuilder( "<InlineUIContainer>");
            container.AppendLine("<HyperlinkButton Margin='-14,0,-14,-14'");
            container.Append("Content= '" + token + "' ");
            container.Append("NavigateUri='" + token + "' />");
            container.AppendLine("</InlineUIContainer>");
            return container.ToString();
        }

        private string AddHashLink(string token)
        {
            StringBuilder container = new StringBuilder("<InlineUIContainer>");
            container.AppendLine("<HyperlinkButton Margin='-14,0,-14,-14'");
            container.Append("Content= '" + token + "' ");
            container.Append("NavigateUri='" + "http://www.twitter.com/#!/search/?q=" + System.Net.WebUtility.UrlEncode(token) + "&amp;src=hash' />");
            container.AppendLine("</InlineUIContainer>");
            return container.ToString();
        }

        private string AddAtLink(string token)
        {
            StringBuilder container = new StringBuilder("<InlineUIContainer>");
            container.AppendLine("<HyperlinkButton Margin='-14,0,-14,-14'");
            container.Append("Content= '" + token + "' ");
            container.Append("NavigateUri='" + "http://www.twitter.com/" + token.Trim(new char [] {'@',',',':'}) + "' />");
            container.AppendLine("</InlineUIContainer>");
            return container.ToString();
        }
    }
}
