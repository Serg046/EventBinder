using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Moq;
using Xunit;

namespace EventBinder.Tests
{
    public class MultiBindTests : EventTests
    {
        [WpfFact]
        public void CommandProperty_CommandBinding_CommandExecuted()
        {
            var counter = 0;
            var btn = new Button();
            btn.SetValue(Bind.CommandParameterProperty, 5);
            var commandMock = new Mock<ICommand>();
            commandMock.Setup(c => c.Execute(5)).Callback(() => counter++);

            BindingOperations.SetBinding(btn, Bind.CommandProperty,
                new EventBinding { Source = commandMock.Object, EventPath = JoinedEventPath });
            RaiseEvents(btn);

            Assert.Equal(ValidEventData.Count, counter);
            commandMock.VerifyAll();
        }

        [WpfFact]
        public void PrmActionProperty_PrmActionBinding_ActionExecuted()
        {
            var counter = 0;
            var btn = new Button();
            btn.SetValue(Bind.ActionParameterProperty, 5);
            Action<object> action = parameter =>
            {
                Assert.Equal(5, parameter);
                counter++;
            };

            BindingOperations.SetBinding(btn, Bind.PrmActionProperty,
                new EventBinding { Source = action, EventPath = JoinedEventPath });
            RaiseEvents(btn);

            Assert.Equal(ValidEventData.Count, counter);
        }

        [WpfFact]
        public void ActionProperty_ActionBinding_ActionExecuted()
        {
            var counter = 0;
            var btn = new Button();
            Action action = () => counter++;

            BindingOperations.SetBinding(btn, Bind.ActionProperty,
                new EventBinding { Source = action, EventPath = JoinedEventPath });
            RaiseEvents(btn);

            Assert.Equal(ValidEventData.Count, counter);
        }

        [WpfFact]
        public void AwaitablePrmActionProperty_AwaitablePrmActionBinding_ActionExecuted()
        {
            var counter = 0;
            var btn = new Button();
            btn.SetValue(Bind.AwaitableActionParameterProperty, 5);
            Func<object, Task> action = parameter =>
            {
                Assert.Equal(5, parameter);
                return Task.Run(() => Interlocked.Increment(ref counter));
            };

            BindingOperations.SetBinding(btn, Bind.AwaitablePrmActionProperty,
                new EventBinding { Source = action, EventPath = JoinedEventPath });
            RaiseEvents(btn);

            for (int i = 0; i < 30 && Thread.VolatileRead(ref counter) != ValidEventData.Count; i++)
            {
                Thread.Sleep(50);
            }
            Assert.Equal(ValidEventData.Count, counter);
        }

        [WpfFact]
        public void AwaitableActionProperty_AwaitableActionBinding_ActionExecuted()
        {
            var counter = 0;
            var btn = new Button();
            Func<Task> action = () => Task.Run(() => Interlocked.Increment(ref counter));

            BindingOperations.SetBinding(btn, Bind.AwaitableActionProperty,
                new EventBinding { Source = action, EventPath = JoinedEventPath });
            RaiseEvents(btn);

            for (int i = 0; i < 30 && Thread.VolatileRead(ref counter) != ValidEventData.Count; i++)
            {
                Thread.Sleep(50);
            }
            Assert.Equal(ValidEventData.Count, counter);
        }
    }
}
