using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Moq;
using Xunit;

namespace EventBinder.Tests
{
    public class BindTests : EventTests
    {
        [Fact]
        public void CommandProperty_Binding_Fails()
        {
            var obj = new DependencyObject();

            Assert.Throws<InvalidCastException>(() => BindingOperations
                .SetBinding(obj, Bind.CommandProperty, new Binding {Source = Mock.Of<ICommand>()}));
        }

        [Fact]
        public void CommandProperty_NoEvent_Fails()
        {
            var obj = new UIElement();

            Assert.Throws<InvalidOperationException>(() => BindingOperations.SetBinding(obj,
                Bind.CommandProperty, new EventBinding { Source = Mock.Of<ICommand>(), EventPath = "IncorrectEvent"}));
        }

        [WpfTheory]
        [MemberData(nameof(ValidEventData))]
        public void CommandProperty_CommandBinding_CommandExecuted(string eventName, Func<DependencyObject, RoutedEventArgs> args)
        {
            var testValue = -1;
            var btn = new Button();
            btn.SetValue(Bind.CommandParameterProperty, 5);
            var commandMock = new Mock<ICommand>();
            commandMock.Setup(c => c.Execute(5)).Callback(() => testValue = 7);

            BindingOperations.SetBinding(btn, Bind.CommandProperty,
                new EventBinding {Source = commandMock.Object, EventPath = eventName });
            btn.RaiseEvent(args(btn));

            Assert.Equal(7, testValue);
            commandMock.VerifyAll();
        }

        [WpfTheory]
        [MemberData(nameof(ValidEventData))]
        public void CommandProperty_CommandBinding_EnabledStateUpdated(string eventName, Func<DependencyObject, RoutedEventArgs> args)
        {
            var isEnabled = false;
            var btn = new Button();
            btn.SetValue(Bind.CommandParameterProperty, 5);
            var commandMock = new Mock<ICommand>();
            commandMock.Setup(c => c.CanExecute(5)).Returns(() => isEnabled);

            BindingOperations.SetBinding(btn, Bind.CommandProperty,
                new EventBinding { Source = commandMock.Object, EventPath = eventName });
            btn.RaiseEvent(args(btn));

            Assert.False(btn.IsEnabled);
            isEnabled = true;
            commandMock.Raise(c => c.CanExecuteChanged += null, EventArgs.Empty);
            Assert.True(btn.IsEnabled);
            commandMock.VerifyAll();
        }

        [WpfTheory]
        [MemberData(nameof(ValidEventData))]
        public void PrmActionProperty_PrmActionBinding_ActionExecuted(string eventName, Func<DependencyObject, RoutedEventArgs> args)
        {
            var testValue = -1;
            var btn = new Button();
            btn.SetValue(Bind.ActionParameterProperty, 5);
            Action<object> action = parameter =>
            {
                Assert.Equal(5, parameter);
                testValue = 7;
            };

            BindingOperations.SetBinding(btn, Bind.PrmActionProperty,
                new EventBinding { Source = action, EventPath = eventName });
            btn.RaiseEvent(args(btn));

            Assert.Equal(7, testValue);
        }

        [WpfTheory]
        [MemberData(nameof(ValidEventData))]
        public void ActionProperty_ActionBinding_ActionExecuted(string eventName, Func<DependencyObject, RoutedEventArgs> args)
        {
            var testValue = -1;
            var btn = new Button();
            Action action = () => testValue = 7;

            BindingOperations.SetBinding(btn, Bind.ActionProperty,
                new EventBinding { Source = action, EventPath = eventName });
            btn.RaiseEvent(args(btn));

            Assert.Equal(7, testValue);
        }

        [WpfTheory]
        [MemberData(nameof(ValidEventData))]
        public void AwaitablePrmActionProperty_AwaitablePrmActionBinding_ActionExecuted(string eventName, Func<DependencyObject, RoutedEventArgs> args)
        {
            var testValue = -1;
            const int expectedValue = 7;
            var btn = new Button();
            btn.SetValue(Bind.AwaitableActionParameterProperty, 5);
            Func<object, Task> action = parameter =>
            {
                Assert.Equal(5, parameter);
                return Task.Run(() => Thread.VolatileWrite(ref testValue, expectedValue));
            };

            BindingOperations.SetBinding(btn, Bind.AwaitablePrmActionProperty,
                new EventBinding { Source = action, EventPath = eventName });
            btn.RaiseEvent(args(btn));

            for (int i = 0; i < 30 && Thread.VolatileRead(ref testValue) != expectedValue; i++)
            {
                Thread.Sleep(50);
            }
            Assert.Equal(expectedValue, Thread.VolatileRead(ref testValue));
        }

        [WpfTheory]
        [MemberData(nameof(ValidEventData))]
        public void AwaitableActionProperty_AwaitableActionBinding_ActionExecuted(string eventName, Func<DependencyObject, RoutedEventArgs> args)
        {
            var testValue = -1;
            const int expectedValue = 7;
            var btn = new Button();
            Func<Task> action = () => Task.Run(() => Thread.VolatileWrite(ref testValue, expectedValue));

            BindingOperations.SetBinding(btn, Bind.AwaitableActionProperty,
                new EventBinding { Source = action, EventPath = eventName });
            btn.RaiseEvent(args(btn));

            for (int i = 0; i < 30 && Thread.VolatileRead(ref testValue) != expectedValue; i++)
            {
                Thread.Sleep(50);
            }
            Assert.Equal(expectedValue, Thread.VolatileRead(ref testValue));
        }
    }
}
