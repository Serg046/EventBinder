using System;
using System.Linq;
using LinqExpr = System.Linq.Expressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace EventBinder
{
    public static class Command
    {
        public static readonly DependencyProperty SyncProperty = DependencyProperty.RegisterAttached("Sync",
            typeof(ICommand), typeof(Command), new PropertyMetadata(SyncPropertyChangedCallback));
        public static ICommand GetSync(DependencyObject element) => (ICommand)element.GetValue(SyncProperty);
        public static void SetSync(DependencyObject element, ICommand value) => element.SetValue(SyncProperty, value);

        public static readonly DependencyProperty SyncParameterProperty = DependencyProperty.RegisterAttached("SyncParameter",
            typeof(object), typeof(Command));
        public static object GetSyncParameter(DependencyObject element) => element.GetValue(SyncParameterProperty);
        public static void SetSyncParameter(DependencyObject element, object value) => element.SetValue(SyncParameterProperty, value);

        private static void SyncPropertyChangedCallback(DependencyObject parent, DependencyPropertyChangedEventArgs args)
        {
            var binding = (EventBinding)BindingOperations.GetBinding(parent, args.Property)
                          ?? throw new InvalidOperationException("Cannot get binding");
            var eventInfo = parent.GetType().GetEvent(binding.EventPath)
                            ?? throw new InvalidOperationException($"Cannot find the event '{binding.EventPath}'");
            var parameters = eventInfo.EventHandlerType.GetMethods().First().GetParameters();

            var command = (ICommand)args.NewValue;
            LinqExpr.Expression<Action> executeExpression = () => command.Execute(parent.GetValue(SyncParameterProperty));
            var methodExpression = LinqExpr.Expression.Lambda(eventInfo.EventHandlerType, executeExpression.Body,
                parameters.Select(p => LinqExpr.Expression.Parameter(p.ParameterType)));
            var method = methodExpression.Compile();

            eventInfo.AddEventHandler(parent, method);

            if (parent is UIElement uiElement)
            {
                UpdateState();
                command.CanExecuteChanged += (sender, eventArgs) => UpdateState();
                void UpdateState() => uiElement.IsEnabled = command.CanExecute(parent.GetValue(SyncParameterProperty));
            }
        }
    }
}
