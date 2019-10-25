using System;
using System.Threading;
using System.Threading.Tasks;
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
        public void MethodProperty_AsyncMethod_Success(string eventName, Func<DependencyObject, RoutedEventArgs> args)
        {
            var testValue = -1;
            var btn = new Button();
            Func<Task> func = async () =>
            {
                await Task.Run(() => {}).ConfigureAwait(false);
                Interlocked.Exchange(ref testValue, 7);
            };

            BindingOperations.SetBinding(btn, Bind.MethodProperty,
                new EventBinding(eventName, "Invoke") { Source = func });
            btn.RaiseEvent(args(btn));

            Thread.Sleep(20);
            Assert.Equal(7, Thread.VolatileRead(ref testValue));
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
                eventName, "Invoke", "$0", "$1") { Source = action });
            btn.RaiseEvent(args);

            Assert.Equal(1, counter);
        }

        [StaTheory]
        [MemberData(nameof(ValidEventData))]
        public void MethodProperty_EventAndUserParameters_Success(string eventName, Func<DependencyObject, RoutedEventArgs> argsFunc)
        {
            var numCounter = 0;
            var strCounter = string.Empty;
            var btn = new Button();
            var args = argsFunc(btn);
            Action<EventArgs, int, object, string> action = (eventArgs, num, sender, str) =>
            {
                Assert.Equal(btn, sender);
                Assert.Equal(args, eventArgs);
                numCounter += num;
                strCounter += str;
            };

            BindingOperations.SetBinding(btn, Bind.MethodProperty, new EventBinding(
                eventName, "Invoke", "$1", 1, "$0", "1") { Source = action });
            btn.RaiseEvent(args);

            Assert.Equal(1, numCounter);
            Assert.Equal(1, strCounter.Length);
        }

        [WpfFact]
        public void MethodProperty_Binding_Success()
        {
            var properties = new[] { Bind.MethodProperty, Bind.MethodProperty2, Bind.MethodProperty3, Bind.MethodProperty4,
                Bind.MethodProperty5, Bind.MethodProperty6, Bind.MethodProperty7, Bind.MethodProperty8, Bind.MethodProperty9 };

            var btn = new Button();
            for (var i = 0; i < properties.Length; i++)
            {
                BindingOperations.SetBinding(btn, properties[i], new EventBinding("Click", "Equals", i) { Source = i });
            }

            for (var i = 0; i < properties.Length; i++)
            {
                var binding = BindingOperations.GetBinding(btn, properties[i]) as EventBinding;
                Assert.Equal(i, binding.Source);
            }
        }
    }
}
