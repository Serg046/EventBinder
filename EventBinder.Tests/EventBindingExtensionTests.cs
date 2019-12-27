using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Xunit;

namespace EventBinder.Tests
{
    public class EventBindingExtensionTests
    {
        [WpfFact]
        public void EventBinding_SimpleAction_Executed()
        {
            var executed = false;
            var button = XamlReader.Parse<Button>("<Button Name=\"Btn\" Click=\"{e:EventBinding Invoke}\"/>");
            button.DataContext = new Action(() => executed = true);

            button.RaiseClickEvent();

            Assert.True(executed);
        }

        [WpfFact]
        public void EventBinding_UnloadedButton_NoSense()
        {
            var executed = false;
            var button = XamlReader.Parse<Button>("<Button Name=\"Btn\" Click=\"{e:EventBinding Invoke}\"/>");
            button.DataContext = new Action(() => executed = true);

            button.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, button));
            button.RaiseClickEvent();

            Assert.False(executed);
        }

        [WpfFact]
        public void EventBinding_TwoDataContexts_TheLastExecuted()
        {
            var executed = false;
            var button = XamlReader.Parse<Button>("<Button Name=\"Btn\" Click=\"{e:EventBinding Invoke}\"/>");
            button.DataContext = new Action(() => executed = false);
            button.DataContext = new Action(() => executed = true);

            button.RaiseClickEvent();

            Assert.True(executed);
        }

        [WpfFact]
        public void EventBinding_Func_Executed()
        {
            var executed = false;
            var button = XamlReader.Parse<Button>("<Button Name=\"Btn\" Click=\"{e:EventBinding Invoke}\"/>");
            Func<int> func = () => { executed = true; return -1; };
            button.DataContext = func;

            button.RaiseClickEvent();

            Assert.True(executed);
        }

        [WpfFact]
        public async Task EventBinding_AsyncMethod_Executed()
        {
            var executed = false;
            var button = XamlReader.Parse<Button>("<Button Name=\"Btn\" Click=\"{e:EventBinding Invoke}\"/>");
            Func<Task> func = async () =>
            {
                await Task.Run(() => { }).ConfigureAwait(false);
                executed = true;
            };
            button.DataContext = func;

            button.RaiseClickEvent();
            await Task.Delay(500);

            Assert.True(executed);
        }

        [WpfFact]
        public void EventBinding_IntStr_Executed()
        {
            const int expectedNum = 7;
            const string expectedStr = "7";
            var num = 0;
            var str = "";
            var button = XamlReader.Parse<Button>($"<Button Name=\"Btn\" Click=\"{{e:EventBinding Invoke, {expectedNum}, `{expectedStr}` }}\"/>");
            Action<int, string> action = (n, s) => { num = n; str = s; };
            button.DataContext = action;

            button.RaiseClickEvent();

            Assert.Equal(expectedNum, num);
            Assert.Equal(expectedStr, str);
        }

        [WpfFact]
        public void EventBinding_DoubleStr_Executed()
        {
            const double expectedNum = 7;
            const string expectedStr = "7";
            double num = 0;
            var str = "";
            var button = XamlReader.Parse<Button>($"<Button Name=\"Btn\" Click=\"{{e:EventBinding Invoke, {expectedNum}.0, `{expectedStr}` }}\"/>");
            Action<double, string> action = (n, s) => { num = n; str = s; };
            button.DataContext = action;

            button.RaiseClickEvent();

            Assert.Equal(expectedNum, num);
            Assert.Equal(expectedStr, str);
        }

        [WpfFact]
        public void EventBinding_DecimalStr_Executed()
        {
            const decimal expectedNum = 7;
            const string expectedStr = "7";
            decimal num = 0;
            var str = "";
            var button = XamlReader.Parse<Button>($"<Button Name=\"Btn\" Click=\"{{e:EventBinding Invoke, {expectedNum}m, `{expectedStr}` }}\"/>");
            Action<decimal, string> action = (n, s) => { num = n; str = s; };
            button.DataContext = action;

            button.RaiseClickEvent();

            Assert.Equal(expectedNum, num);
            Assert.Equal(expectedStr, str);
        }

        [WpfFact]
        public void EventBinding_EventArgs_Executed()
        {
            object sender = null;
            EventArgs args = null;
            var button = XamlReader.Parse<Button>("<Button Name=\"Btn\" Click=\"{e:EventBinding Invoke, $0, $1 }\"/>");
            Action<object, EventArgs> action = (obj, e) => { sender = obj; args = e; };
            button.DataContext = action;

            button.RaiseClickEvent();

            Assert.Equal(button, sender);
            var eArgs = Assert.IsType<RoutedEventArgs>(args);
            Assert.Equal(ButtonBase.ClickEvent, eArgs.RoutedEvent);
        }

        [WpfFact]
        public void EventBinding_EventAndUserArgs_Executed()
        {
            EventArgs args = null;
            int num = -1;
            string str = "-1";
            object sender = null;
            var button = XamlReader.Parse<Button>("<Button Name=\"Btn\" Click=\"{e:EventBinding Invoke, $1, 7, $0, `7`}\"/>");
            Action<EventArgs, int, object, string> action = (e, n, obj, s)
                => { args = e; num = n; sender = obj; str = s; };
            button.DataContext = action;

            button.RaiseClickEvent();

            var eArgs = Assert.IsType<RoutedEventArgs>(args);
            Assert.Equal(ButtonBase.ClickEvent, eArgs.RoutedEvent);
            Assert.Equal(7, num);
            Assert.Equal(button, sender);
            Assert.Equal("7", str);
        }

        [WpfFact]
        public void EventBinding_BindedNumberAsParam_Executed()
        {
            const double expected = 200;
            double num = -1;
            var button = XamlReader.Parse<Button>($"<Button Height=\"{expected}\" Name=\"Btn\" Click=\"{{e:EventBinding Invoke, {{Binding ElementName=Btn, Path=Height}}}}\"/>");
            button.DataContext = new Action<double>(n => num = n);

            button.RaiseClickEvent();

            Assert.Equal(expected, num);
        }

        [WpfFact]
        public void EventBinding_BindedStringAsParam_Executed()
        {
            const string expected = "test";
            string str = "-1";
            var textBox = XamlReader.Parse<TextBox>($"<TextBox Text=\"{expected}\" Name=\"Tbl\" MouseDown=\"{{e:EventBinding Invoke, {{Binding ElementName=Tbl, Path=Text}}}}\"/>");
            textBox.DataContext = new Action<string>(s => str = s);

			textBox.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
			{
				RoutedEvent = Mouse.MouseDownEvent,
				Source = textBox
			});

			Assert.Equal(expected, str);
        }

        [WpfFact]
        public void EventBinding_BindedValueChanged_ExecutedWithChanges()
        {
            const double expected = 200;
            double num = -1;
            var button = XamlReader.Parse<Button>($"<Button Height=\"{expected}\" Name=\"Btn\" Click=\"{{e:EventBinding Invoke, {{Binding ElementName=Btn, Path=Height}}}}\"/>");
            button.DataContext = new Action<double>(n => num = n);
            button.RaiseClickEvent();
            Assert.Equal(expected, num);

            button.Height++;
            button.RaiseClickEvent();
            Assert.Equal(expected + 1, num);
        }
    }
}
