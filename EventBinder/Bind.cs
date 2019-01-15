using System;
using System.Linq;
using System.Threading.Tasks;
using LinqExpr = System.Linq.Expressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace EventBinder
{
    public static class Bind
    {
        public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached("Command",
            typeof(ICommand), typeof(Bind), new PropertyMetadata(CommandPropertyChangedCallback));
        public static ICommand GetCommand(DependencyObject element) => (ICommand)element.GetValue(CommandProperty);
        public static void SetCommand(DependencyObject element, ICommand value) => element.SetValue(CommandProperty, value);

        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.RegisterAttached("CommandParameter",
            typeof(object), typeof(Bind));
        public static object GetCommandParameter(DependencyObject element) => element.GetValue(CommandParameterProperty);
        public static void SetCommandParameter(DependencyObject element, object value) => element.SetValue(CommandParameterProperty, value);

        private static void CommandPropertyChangedCallback(DependencyObject parent, DependencyPropertyChangedEventArgs args)
        {
            var command = (ICommand)args.NewValue;
            Subscribe(parent, args, () => command.Execute(parent.GetValue(CommandParameterProperty)));

            if (parent is UIElement uiElement)
            {
                UpdateState();
                command.CanExecuteChanged += (sender, eventArgs) => UpdateState();
                void UpdateState() => uiElement.IsEnabled = command.CanExecute(parent.GetValue(CommandParameterProperty));
            }
        }

        //-------------------------------------------------------------------------------------------------------------------

        public static readonly DependencyProperty PrmActionProperty = DependencyProperty.RegisterAttached("PrmAction",
            typeof(Action<object>), typeof(Bind), new PropertyMetadata(PrmActionPropertyChangedCallback));
        public static Action<object> GetPrmAction(DependencyObject element) => (Action<object>)element.GetValue(PrmActionProperty);
        public static void SetPrmAction(DependencyObject element, Action<object> value) => element.SetValue(PrmActionProperty, value);

        public static readonly DependencyProperty ActionParameterProperty = DependencyProperty.RegisterAttached("ActionParameter",
            typeof(object), typeof(Bind));
        public static object GetActionParameter(DependencyObject element) => element.GetValue(ActionParameterProperty);
        public static void SetActionParameter(DependencyObject element, object value) => element.SetValue(ActionParameterProperty, value);

        private static void PrmActionPropertyChangedCallback(DependencyObject parent, DependencyPropertyChangedEventArgs args)
        {
            var action = (Action<object>)args.NewValue;
            Subscribe(parent, args, () => action.Invoke(parent.GetValue(ActionParameterProperty)));
        }

        //-------------------------------------------------------------------------------------------------------------------

        public static readonly DependencyProperty ActionProperty = DependencyProperty.RegisterAttached("Action",
            typeof(Action), typeof(Bind), new PropertyMetadata(ActionPropertyChangedCallback));
        public static Action GetAction(DependencyObject element) => (Action)element.GetValue(ActionProperty);
        public static void SetAction(DependencyObject element, Action value) => element.SetValue(ActionProperty, value);

        private static void ActionPropertyChangedCallback(DependencyObject parent, DependencyPropertyChangedEventArgs args)
        {
            var action = (Action)args.NewValue;
            Subscribe(parent, args, () => action());
        }

        //-------------------------------------------------------------------------------------------------------------------

        public static readonly DependencyProperty AwaitablePrmActionProperty = DependencyProperty.RegisterAttached("AwaitablePrmAction",
            typeof(Func<object, Task>), typeof(Bind), new PropertyMetadata(AwaitablePrmActionPropertyChangedCallback));
        public static Func<object, Task> GetAwaitablePrmAction(DependencyObject element) => (Func<object, Task>)element.GetValue(AwaitablePrmActionProperty);
        public static void SetAwaitablePrmAction(DependencyObject element, Func<object, Task> value) => element.SetValue(AwaitablePrmActionProperty, value);

        public static readonly DependencyProperty AwaitableActionParameterProperty = DependencyProperty.RegisterAttached("AwaitableActionParameter",
            typeof(object), typeof(Bind));
        public static object GetAwaitableActionParameter(DependencyObject element) => element.GetValue(AwaitableActionParameterProperty);
        public static void SetAwaitableActionParameter(DependencyObject element, object value) => element.SetValue(AwaitableActionParameterProperty, value);

        private static void AwaitablePrmActionPropertyChangedCallback(DependencyObject parent, DependencyPropertyChangedEventArgs args)
        {
            var awaitableAction = (Func<object, Task>)args.NewValue;
            Action action = () => awaitableAction.Invoke(parent.GetValue(AwaitableActionParameterProperty));
            Subscribe(parent, args, () => action());
        }

        //-------------------------------------------------------------------------------------------------------------------

        public static readonly DependencyProperty AwaitableActionProperty = DependencyProperty.RegisterAttached("AwaitableAction",
            typeof(Func<Task>), typeof(Bind), new PropertyMetadata(AwaitableActionPropertyChangedCallback));
        public static Func<Task> GetAwaitableAction(DependencyObject element) => (Func<Task>)element.GetValue(AwaitableActionProperty);
        public static void SetAwaitableAction(DependencyObject element, Func<Task> value) => element.SetValue(AwaitableActionProperty, value);

        private static void AwaitableActionPropertyChangedCallback(DependencyObject parent, DependencyPropertyChangedEventArgs args)
        {
            var awaitableAction = (Func<Task>)args.NewValue;
            Action action = () => awaitableAction.Invoke();
            Subscribe(parent, args, () => action());
        }

        //-------------------------------------------------------------------------------------------------------------------

        private static void Subscribe(DependencyObject parent, DependencyPropertyChangedEventArgs args, LinqExpr.Expression<Action> callExpression)
        {
            var binding = (EventBinding)BindingOperations.GetBinding(parent, args.Property)
                          ?? throw new InvalidOperationException("Cannot get binding");

            foreach (var eventPath in binding.EventPath.Split(','))
            {
                var eventInfo = parent.GetType().GetEvent(eventPath)
                                ?? throw new InvalidOperationException($"Cannot find the event '{eventPath}'");
                var parameters = eventInfo.EventHandlerType.GetMethods().Single(m => m.Name == "Invoke").GetParameters();

                var methodExpression = LinqExpr.Expression.Lambda(eventInfo.EventHandlerType, callExpression.Body,
                    parameters.Select(p => LinqExpr.Expression.Parameter(p.ParameterType)));
                var method = methodExpression.Compile();

                eventInfo.AddEventHandler(parent, method);
            }
        }
    }
}
