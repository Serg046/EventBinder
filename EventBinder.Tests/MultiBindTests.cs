using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using Xunit;

namespace EventBinder.Tests
{
    public class MultiBindTests : EventTests
    {
        [WpfFact]
        public void MethodProperty_NoParameters_Success()
        {
            var counter = 0;
            var btn = new Button();
            Action action = () => counter++;

            BindingOperations.SetBinding(btn, Bind.MethodProperty,
                new EventBinding(JoinedEventPath, "Invoke") { Source = action });
            RaiseEvents(btn);

            Assert.Equal(ValidEventData.Count, counter);
        }

        [WpfFact]
        public void MethodProperty_FuncWithNoParameters_Success()
        {
            var counter = 0;
            var btn = new Button();
            Func<int> func = () => { counter++; return -1; };

            BindingOperations.SetBinding(btn, Bind.MethodProperty,
                new EventBinding(JoinedEventPath, "Invoke") { Source = func });
            RaiseEvents(btn);

            Assert.Equal(ValidEventData.Count, counter);
        }

        [WpfFact]
        public void MethodProperty_AsyncMethod_Success()
        {
            var counter = 0;
            var btn = new Button();
            Func<Task> func = async () =>
            {
                await Task.Run(() => {}).ConfigureAwait(false);
                Interlocked.Increment(ref counter);
            };

            BindingOperations.SetBinding(btn, Bind.MethodProperty,
                new EventBinding(JoinedEventPath, "Invoke") { Source = func });
            RaiseEvents(btn);

            Thread.Sleep(100);
            Assert.Equal(ValidEventData.Count, Thread.VolatileRead(ref counter));
        }

        [WpfFact]
        public void MethodProperty_UserParameters_Success()
        {
            var numCounter = 0;
            var strCounter = string.Empty;
            var btn = new Button();
            Action<int, string> action = (num, str) =>
            {
                numCounter += num;
                strCounter += str;
            };

            BindingOperations.SetBinding(btn, Bind.MethodProperty,
                new EventBinding(JoinedEventPath, "Invoke", 1, "1") { Source = action });
            RaiseEvents(btn);

            Assert.Equal(ValidEventData.Count, numCounter);
            Assert.Equal(ValidEventData.Count, strCounter.Length);
        }

        [WpfFact]
        public void MethodProperty_EventParameters_Success()
        {
            var counter = 0;
            var btn = new Button();
            Action<object, EventArgs> action = (sender, eventArgs) =>
            {
                Assert.Equal(btn, sender);
                Assert.Contains(ValidEventData.Select(d => (dynamic)d[1]), t => t(btn).GetType() == eventArgs.GetType());
                counter++;
            };

            BindingOperations.SetBinding(btn, Bind.MethodProperty, new EventBinding(
                JoinedEventPath, "Invoke", new EventSender(), new EventArguments()) { Source = action });
            RaiseEvents(btn);

            Assert.Equal(ValidEventData.Count, counter);
        }
    }
}
