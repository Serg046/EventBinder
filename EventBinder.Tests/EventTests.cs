using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Moq;

namespace EventBinder.Tests
{
    public abstract class EventTests
    {
        public static List<object[]> ValidEventData { get; } = ValidEventDataImpl().ToList();

        protected static string JoinedEventPath { get; } = string.Join(",", ValidEventData.Select(e => e[0].ToString()));

        private static IEnumerable<object[]> ValidEventDataImpl()
        {
            Func<DependencyObject, RoutedEventArgs> eventArgs = source => new RoutedEventArgs(ButtonBase.ClickEvent, source);
            yield return new object[] { "Click", eventArgs };

            eventArgs = source => new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Right)
            {
                RoutedEvent = UIElement.MouseRightButtonDownEvent,
                Source = source
            };
            yield return new object[] { "MouseRightButtonDown", eventArgs };

            eventArgs = source => (ContextMenuEventArgs)Activator.CreateInstance(typeof(ContextMenuEventArgs),
                BindingFlags.CreateInstance | BindingFlags.NonPublic | BindingFlags.Instance,
                null, new object[] { source, true }, CultureInfo.CurrentCulture);
            yield return new object[] { "ContextMenuOpening", eventArgs };

            eventArgs = source => new KeyEventArgs(Keyboard.PrimaryDevice,
                Mock.Of<PresentationSource>(), 0, Key.Enter)
            {
                RoutedEvent = UIElement.KeyUpEvent,
                Source = source
            };
            yield return new object[] { "KeyUp", eventArgs };
        }

        protected static void RaiseEvents(UIElement btn) => ValidEventData.ForEach(prm => btn.RaiseEvent(((dynamic)prm[1])(btn)));
    }
}
