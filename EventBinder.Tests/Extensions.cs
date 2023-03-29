using System.Windows;
using System.Windows.Controls.Primitives;

namespace EventBinder.Tests
{
    public static class Extensions
    {
        public static void RaiseClickEvent(this ButtonBase button)
            => button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));

        public static void RaiseLoadedEvent(this ButtonBase button)
            => button.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, button));
    }
}
