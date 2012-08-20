using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;

namespace Twittelytics.Common
{
    public class TweetConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            RichTextBlock block = new RichTextBlock();
            Paragraph p = new Paragraph();
            Run r= new Run();
            r.Text = "This is a Test";
            p.Inlines.Add(r);
            block.Blocks.Add(p);
            return r.Text;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
