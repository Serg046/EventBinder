using System.Windows.Markup;

namespace EventBinder.Tests
{
    public static class XamlReader
    {
        private static readonly ParserContext _parserContext;

        static XamlReader()
        {
            _parserContext = new ParserContext();
            _parserContext.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
            _parserContext.XmlnsDictionary.Add("e", "clr-namespace:EventBinder;assembly=EventBinder");
            _parserContext.XmlnsDictionary.Add("local", "clr-namespace:EventBinder.Tests;assembly=EventBinder.Tests");
        }

        public static T Parse<T>(string xaml) => (T)System.Windows.Markup.XamlReader.Parse(xaml, _parserContext);
    }
}
