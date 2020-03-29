using System;
using System.Threading.Tasks;
using Xunit;
#if AVALONIA
	using Avalonia.Controls;
	using EventBinder.Tests.Avalonia;
	using Avalonia.Interactivity;
#else
	using System.Windows;
	using System.Windows.Controls;
#endif

namespace EventBinder.Tests
{
    public class EventBindingTests
    {
        [WpfFact]
        public void EventBinding_SimpleAction_Executed()
        {
            var executed = false;
            var button = XamlReader.Parse<Button>("<Button Click=\"{e:EventBinding Invoke}\"/>");
            button.DataContext = new Action(() => executed = true);

            button.RaiseClickEvent();

            Assert.True(executed);
        }

        [WpfFact]
        public void EventBinding_UnloadedButton_NoSense()
        {
            var executed = false;
            var button = XamlReader.Parse<Button>("<Button Click=\"{e:EventBinding Invoke}\"/>");

            button.DataContext = new Action(() => executed = true);
#if AVALONIA
            button.RaiseDetachedFromVisualTreeEvent();
#else
            button.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, button));
#endif
            button.RaiseClickEvent();

            Assert.False(executed);
        }

        [WpfFact]
        public void EventBinding_ReloadedButton_StillWorks()
        {
	        var executed = false;
	        var button = XamlReader.Parse<Button>("<Button Click=\"{e:EventBinding Invoke}\"/>");

	        button.DataContext = new Action(() => executed = true);
#if AVALONIA
            button.RaiseDetachedFromVisualTreeEvent();
            button.RaiseAttachedToVisualTreeEvent();
#else
            button.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, button));
	        button.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, button));
#endif
	        button.RaiseClickEvent();

	        Assert.True(executed);
        }

        [WpfFact]
        public void EventBinding_TwoDataContexts_TheLastExecuted()
        {
            var executed = false;
            var button = XamlReader.Parse<Button>("<Button Click=\"{e:EventBinding Invoke}\"/>");
            button.DataContext = new Action(() => executed = false);
            button.DataContext = new Action(() => executed = true);

            button.RaiseClickEvent();

            Assert.True(executed);
        }

        [WpfFact]
        public void EventBinding_Func_Executed()
        {
            var executed = false;
            var button = XamlReader.Parse<Button>("<Button Click=\"{e:EventBinding Invoke}\"/>");
            Func<int> func = () => { executed = true; return -1; };
            button.DataContext = func;

            button.RaiseClickEvent();

            Assert.True(executed);
        }

        [WpfFact]
        public async Task EventBinding_AsyncMethod_Executed()
        {
            var executed = false;
            var button = XamlReader.Parse<Button>("<Button Click=\"{e:EventBinding Invoke}\"/>");
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
            var button = XamlReader.Parse<Button>($"<Button Click=\"{{e:EventBinding Invoke, {expectedNum}, `{expectedStr}` }}\"/>");
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
            var button = XamlReader.Parse<Button>($"<Button Click=\"{{e:EventBinding Invoke, {expectedNum}.0, `{expectedStr}` }}\"/>");
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
            var button = XamlReader.Parse<Button>($"<Button Click=\"{{e:EventBinding Invoke, {expectedNum}m, `{expectedStr}` }}\"/>");
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
            var button = XamlReader.Parse<Button>("<Button Click=\"{e:EventBinding Invoke, $0, $1 }\"/>");
            Action<object, EventArgs> action = (obj, e) => { sender = obj; args = e; };
            button.DataContext = action;

            button.RaiseClickEvent();

            Assert.Equal(button, sender);
            var eArgs = Assert.IsType<RoutedEventArgs>(args);
            Assert.Equal(Button.ClickEvent, eArgs.RoutedEvent);
        }

        [WpfFact]
        public void EventBinding_EventAndUserArgs_Executed()
        {
            EventArgs args = null;
            int num = -1;
            string str = "-1";
            object sender = null;
            var button = XamlReader.Parse<Button>("<Button Click=\"{e:EventBinding Invoke, $1, 7, $0, `7`}\"/>");
            Action<EventArgs, int, object, string> action = (e, n, obj, s)
                => { args = e; num = n; sender = obj; str = s; };
            button.DataContext = action;

            button.RaiseClickEvent();

            var eArgs = Assert.IsType<RoutedEventArgs>(args);
            Assert.Equal(Button.ClickEvent, eArgs.RoutedEvent);
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
            var textBox = XamlReader.Parse<TextBox>($"<TextBox Text=\"{expected}\" Name=\"Tbl\" LostFocus=\"{{e:EventBinding Invoke, {{Binding ElementName=Tbl, Path=Text}}}}\"/>");
            textBox.DataContext = new Action<string>(s => str = s);

            textBox.RaiseEvent(new RoutedEventArgs(Button.LostFocusEvent));

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

        [WpfFact]
        public void EventBinding_DataContextIsSetBeforeEvaluation_Success()
        {
	        var stackPanel = XamlReader.Parse<StackPanel>("<StackPanel ><StackPanel.DataContext><local:XamlViewModel/></StackPanel.DataContext><StackPanel.Children><Button Click=\"{e:EventBinding Invoke}\"/></StackPanel.Children></StackPanel>");
	        var button = stackPanel.Children[0] as Button;
	        var dataContext = button.DataContext as XamlViewModel;

            Assert.False(dataContext.Executed);
            button.RaiseClickEvent();

            Assert.True(dataContext.Executed);
        }

        [WpfFact]
        public void EventBinding_SimpleActionViaCSharp_Executed()
        {
	        var executed = false;
	        var button = XamlReader.Parse<Button>("<Button />");
	        button.DataContext = new Action(() => executed = true);
            EventBinding.Bind(button, "Click", "Invoke");

            button.RaiseClickEvent();

	        Assert.True(executed);
        }

        [WpfFact]
        public void EventBinding_NestedModel_Executed()
        {
	        var viewModel = new NestedViewModel();
	        var button = XamlReader.Parse<Button>("<Button Click=\"{e:EventBinding ViewModel1.ViewModel2.Invoke}\"/>");
	        button.DataContext = viewModel;

            Assert.False(viewModel.ViewModel1.ViewModel2.Executed);
	        button.RaiseClickEvent();

            Assert.True(viewModel.ViewModel1.ViewModel2.Executed);
        }

        [WpfFact]
        public void EventBinding_BindedNestedProp_Executed()
        {
	        const double expected = 200;
	        double num = -1;
	        var button = XamlReader.Parse<Button>($"<Button Margin=\"{expected},0,0,0\" Name=\"Btn\" Click=\"{{e:EventBinding Invoke, {{Binding ElementName=Btn, Path=Margin.Left}}}}\"/>");
	        button.DataContext = new Action<double>(n => num = n);

	        button.RaiseClickEvent();

	        Assert.Equal(expected, num);
        }

        [WpfFact]
        public void EventBinding_BindedVmProp_Executed()
        {
	        const double expected = 200;
	        double num = -1;
	        var button = XamlReader.Parse<Button>($"<Button Margin=\"{expected},0,0,0\" Click=\"{{e:EventBinding Action.Invoke, {{Binding Num}}}}\"/>");
	        button.DataContext = new
	        {
                Num = expected,
                Action = new Action<double>(n => num = n)
            };

	        button.RaiseClickEvent();

	        Assert.Equal(expected, num);
        }

        [WpfFact]
        public void EventBinding_BindedPropWithoutPath_Executed()
        {
	        var executed = false;
	        var button = XamlReader.Parse<Button>("<Button Name=\"Btn\" Click=\"{e:EventBinding Invoke, {Binding ElementName=Btn}}\"/>");
	        button.DataContext = new Action<Button>(btn => executed = button == btn);

	        button.RaiseClickEvent();

	        Assert.True(executed);
        }

        [WpfFact]
        public void EventBinding_BindedVmPropWithoutPath_Executed()
        {
	        var executed = false;
            object viewModel = null;
	        viewModel = new
            {
	            Action = new Action<object>(vm => executed = vm == viewModel)
            };
            var button = XamlReader.Parse<Button>("<Button Click=\"{e:EventBinding Action.Invoke, {Binding}}\"/>");
            button.DataContext = viewModel;

	        button.RaiseClickEvent();

	        Assert.True(executed);
        }

        [WpfFact]
        public async Task EventBinding_DebouncedAction_Executed()
        {
	        var counter = 0;
	        string prm = null;
	        var button = XamlReader.Parse<Button>("<Button Click=\"{e:EventBinding Invoke, `prm`, Debounce = 200}\"/>");
	        button.DataContext = new Action<string>(p =>
	        {
		        counter++;
		        prm = p;
	        });

	        button.RaiseClickEvent();
	        button.RaiseClickEvent();
	        button.RaiseClickEvent();

	        await Task.Delay(500);
	        Assert.Equal(1, counter);
	        Assert.Equal("prm", prm);
        }

        [WpfFact]
        public async Task EventBinding_DebouncedUIAction_Executed()
        {
	        var executed = false;
	        var button = XamlReader.Parse<Button>("<Button Click=\"{e:EventBinding Invoke, $0, Debounce = 200}\"/>");
	        button.DataContext = new Action<object>(btn =>
	        {
		        ((Button)btn).Opacity -= 0.1;
		        executed = true;
	        });

	        button.RaiseClickEvent();

	        await Task.Delay(500);
	        Assert.True(executed);
        }

        [WpfFact]
        public async Task EventBinding_DebouncedAsyncUIAction_Executed()
        {
	        var executed = false;
	        var button = XamlReader.Parse<Button>("<Button Click=\"{e:EventBinding Invoke, $0, Debounce = 200}\"/>");
	        button.DataContext = new Action<object>(async btn =>
	        {
                await Task.Delay(1);
                ((Button)btn).Opacity -= 0.1;
                executed = true;
	        });

	        button.RaiseClickEvent();

	        await Task.Delay(500);
	        Assert.True(executed);
        }
    }

    public class XamlViewModel
    {
	    public bool Executed { get; private set; }

	    public void Invoke() => Executed = true;
    }

    public class NestedViewModel
    {
	    public ViewModel ViewModel1 { get; } = new ViewModel();

	    public class ViewModel
        {
		    public XamlViewModel ViewModel2 = new XamlViewModel();
        }
    }
}
