using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using Moq;
using Xunit;

namespace EventBinder.Tests
{
    public class CommandTests
    {
        [Fact]
        public void SyncProperty_Binding_Fails()
        {
            var obj = new DependencyObject();

            Assert.Throws<InvalidCastException>(() => BindingOperations
                .SetBinding(obj, Command.SyncProperty, new Binding {Source = Mock.Of<ICommand>()}));
        }

        [Fact]
        public void SyncProperty_NoEvent_Fails()
        {
            var obj = new UIElement();

            Assert.Throws<InvalidOperationException>(() => BindingOperations.SetBinding(obj,
                Command.SyncProperty, new EventBinding { Source = Mock.Of<ICommand>(), EventPath = "IncorrectEvent"}));
        }

        public static IEnumerable<object[]> ValidEventData()
        {
            Func<DependencyObject, RoutedEventArgs> eventArgs = source => new RoutedEventArgs(ButtonBase.ClickEvent, source);
            yield return new object[] {"Click", eventArgs };

            eventArgs = source => new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Right)
            {
                RoutedEvent = UIElement.MouseRightButtonDownEvent,
                Source = source
            };
            yield return new object[] { "MouseRightButtonDown", eventArgs};

            eventArgs = source => (ContextMenuEventArgs)Activator.CreateInstance(typeof(ContextMenuEventArgs),
                BindingFlags.CreateInstance | BindingFlags.NonPublic | BindingFlags.Instance,
                null, new object[] {source, true}, CultureInfo.CurrentCulture);
            yield return new object[] { "ContextMenuOpening", eventArgs };

            eventArgs = source => new KeyEventArgs(Keyboard.PrimaryDevice,
                Mock.Of<PresentationSource>(), 0, Key.Enter)
            {
                RoutedEvent = UIElement.KeyUpEvent,
                Source = source
            };
            yield return new object[] { "KeyUp", eventArgs };
        }

        [WpfTheory]
        [MemberData(nameof(ValidEventData))]
        public void SyncProperty_ValidBinding_CommandExecuted(string eventName, Func<DependencyObject, RoutedEventArgs> args)
        {
            var testValue = -1;
            var obj = new Button();
            obj.SetValue(Command.SyncParameterProperty, 5);
            var commandMock = new Mock<ICommand>();
            commandMock.Setup(c => c.Execute(5)).Callback(() => testValue = 7);

            BindingOperations.SetBinding(obj, Command.SyncProperty,
                new EventBinding {Source = commandMock.Object, EventPath = eventName });
            obj.RaiseEvent(args(obj));

            Assert.Equal(7, testValue);
            commandMock.VerifyAll();
        }

        [WpfTheory]
        [MemberData(nameof(ValidEventData))]
        public void SyncProperty_ValidBinding_EnabledStateUpdated(string eventName, Func<DependencyObject, RoutedEventArgs> args)
        {
            var isEnabled = false;
            var obj = new Button();
            obj.SetValue(Command.SyncParameterProperty, 5);
            var commandMock = new Mock<ICommand>();
            commandMock.Setup(c => c.CanExecute(5)).Returns(() => isEnabled);

            BindingOperations.SetBinding(obj, Command.SyncProperty,
                new EventBinding { Source = commandMock.Object, EventPath = eventName });
            obj.RaiseEvent(args(obj));

            Assert.False(obj.IsEnabled);
            isEnabled = true;
            commandMock.Raise(c => c.CanExecuteChanged += null, EventArgs.Empty);
            Assert.True(obj.IsEnabled);
            commandMock.VerifyAll();
        }
    }
}
