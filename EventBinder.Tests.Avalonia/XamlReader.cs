using System;
using Avalonia.Markup.Xaml;

namespace EventBinder.Tests.Avalonia
{
    public static class XamlReader
    {
	    const string NAMESPACES =
            @" xmlns='https://github.com/avaloniaui'
xmlns:e='clr-namespace:EventBinder;assembly=EventBinder.Avalonia'
xmlns:local= 'clr-namespace:EventBinder.Tests;assembly=EventBinder.Tests.Avalonia'";

        public static T Parse<T>(string xaml)
        {
	        var startIndex = xaml.IndexOf(' ');
            if (startIndex == -1) throw new InvalidOperationException("Xaml is incorrect, the space symbol is required");
	        xaml = xaml.Insert(startIndex, NAMESPACES);
	        return AvaloniaXamlLoader.Parse<T>(xaml);
        }
    }
}
