using System.Windows;
using System.Windows.Controls.Primitives;

namespace EventBinder.Tests
{
    public static class Extensions
    {
        public static void RaiseClickEvent(this ButtonBase button)
            => button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));
    }
}
