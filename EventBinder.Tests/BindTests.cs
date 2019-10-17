using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Xunit;

namespace EventBinder.Tests
{
    public class BindTests : EventTests
    {
        [StaTheory]
        [MemberData(nameof(ValidEventData))]
        public void MethodProperty_NoParameters_Success(string eventName, Func<DependencyObject, RoutedEventArgs> args)
        {
            var testValue = -1;
            var btn = new Button();
            Action action = () => testValue = 7;

            BindingOperations.SetBinding(btn, Bind.MethodProperty,
                new EventBinding(eventName, "Invoke") { Source = action });
            btn.RaiseEvent(args(btn));

            Assert.Equal(7, testValue);
        }

        [StaTheory]
        [MemberData(nameof(ValidEventData))]
        public void MethodProperty_FuncWithNoParameters_Success(string eventName, Func<DependencyObject, RoutedEventArgs> args)
        {
            var testValue = -1;
            var btn = new Button();
            Func<int> func = () => { testValue = 7; return -1; };

            BindingOperations.SetBinding(btn, Bind.MethodProperty,
                new EventBinding(eventName, "Invoke") { Source = func });
            btn.RaiseEvent(args(btn));

            Assert.Equal(7, testValue);
        }

        [StaTheory]
        [MemberData(nameof(ValidEventData))]
        public void MethodProperty_UserParameters_Success(string eventName, Func<DependencyObject, RoutedEventArgs> args)
        {
            var testNum = -1;
            var testStr = "-1";
            var btn = new Button();
            Action<int, string> action = (num, str) =>
            {
                testNum = num;
                testStr = str;
            };

            BindingOperations.SetBinding(btn, Bind.MethodProperty,
                new EventBinding(eventName, "Invoke", 7, "7") { Source = action });
            btn.RaiseEvent(args(btn));

            Assert.Equal(7, testNum);
            Assert.Equal("7", testStr);
        }

        [StaTheory]
        [MemberData(nameof(ValidEventData))]
        public void MethodProperty_EventParameters_Success(string eventName, Func<DependencyObject, RoutedEventArgs> argsFunc)
        {
            var counter = 0;
            var btn = new Button();
            var args = argsFunc(btn);
            Action<object, EventArgs> action = (sender, eventArgs) =>
            {
                Assert.Equal(btn, sender);
                Assert.Equal(args, eventArgs);
                counter++;
            };

            BindingOperations.SetBinding(btn, Bind.MethodProperty, new EventBinding(
                eventName, "Invoke", new EventSender(), new EventArguments())
            { Source = action });
            btn.RaiseEvent(args);

            Assert.Equal(1, counter);
        }
    }
}
