using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LinqExpr = System.Linq.Expressions;
using System.Windows;
using System.Windows.Data;

namespace EventBinder
{
    public static class Bind
    {
        private static readonly DependencyPropertyCollection _properties = new DependencyPropertyCollection("Argument");

        public static readonly DependencyProperty MethodProperty = DependencyProperty.RegisterAttached("Method",
            typeof(object), typeof(Bind), new PropertyMetadata(MethodPropertyChangedCallback));
        public static object GetMethod(DependencyObject element) => element.GetValue(MethodProperty);
        public static void SetMethod(DependencyObject element, object value) => element.SetValue(MethodProperty, value);

        public static readonly DependencyProperty MethodProperty2 = DependencyProperty.RegisterAttached("Method2",
            typeof(object), typeof(Bind), new PropertyMetadata(MethodPropertyChangedCallback));
        public static object GetMethod2(DependencyObject element) => element.GetValue(MethodProperty2);
        public static void SetMethod2(DependencyObject element, object value) => element.SetValue(MethodProperty2, value);

        private static void MethodPropertyChangedCallback(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            var eventBinding = (EventBinding)BindingOperations.GetBinding(source, args.Property)
                               ?? throw new InvalidOperationException("Cannot get binding");
            Subscribe(source, eventBinding, (sender, eventArgs) => CallBindedMethod(source, eventBinding, args, sender, eventArgs));
        }

        private static void Subscribe(DependencyObject parent, EventBinding binding, Action<object, EventArgs> callback)
        {
            foreach (var eventPath in binding.EventPath.Split(','))
            {
                var eventInfo = parent.GetType().GetEvent(eventPath)
                                ?? throw new InvalidOperationException($"Cannot find the event '{eventPath}'");
                var parameters = eventInfo.EventHandlerType.GetMethods().Single(m => m.Name == "Invoke").GetParameters();
                var parameterExpressions = parameters.Select(
                    p => LinqExpr.Expression.Parameter(p.ParameterType)).ToArray();

                var expr = LinqExpr.Expression.Call(LinqExpr.Expression.Constant(callback.Target),
                    callback.Method, parameterExpressions);

                var methodExpression = LinqExpr.Expression.Lambda(eventInfo.EventHandlerType, expr,
                    parameterExpressions);
                var method = methodExpression.Compile();

                eventInfo.AddEventHandler(parent, method);
            }
        }

        private static void CallBindedMethod(DependencyObject source, EventBinding eventBinding, DependencyPropertyChangedEventArgs args,
            object sender, EventArgs eventArgs)
        {
            var arguments = ResolveArguments(source, eventBinding, sender, eventArgs);
            var type = args.NewValue.GetType();
            try
            {
                type.InvokeMember(eventBinding.MethodPath, BindingFlags.InvokeMethod, null, args.NewValue, arguments);
            }
            catch (MissingMethodException ex)
            {
                throw new MissingMethodException(
                    $"Cannot find {eventBinding.MethodPath}({string.Join(",", arguments.Select(a => a?.GetType().Name ?? "null"))})", ex);
            }
        }

        private static object[] ResolveArguments(DependencyObject source, EventBinding eventBinding, object sender, EventArgs eventArgs)
        {
            var arguments = new object[eventBinding.Arguments.Length];
            for (var i = 0; i < eventBinding.Arguments.Length; i++)
            {
                var argument = eventBinding.Arguments[i];
                switch (argument)
                {
                    case EventSender eventSender:
                        {
                            arguments[i] = sender;
                            break;
                        }
                    case EventArguments eventArguments:
                        {
                            arguments[i] = eventArgs;
                            break;
                        }
                    case Binding binding:
                        {
                            var property = _properties[i];
                            BindingOperations.SetBinding(source, property, binding);
                            arguments[i] = source.GetValue(property);
                            BindingOperations.ClearBinding(source, property);
                            break;
                        }
                    default: arguments[i] = argument; break;
                }
            }
            return arguments;
        }

        private class DependencyPropertyCollection
        {
            private readonly List<DependencyProperty> _depProperties = new List<DependencyProperty>();
            private readonly string _propertyName;
            public DependencyPropertyCollection(string propertyName) => _propertyName = propertyName;

            public DependencyProperty this[int index]
            {
                get
                {
                    var idx = index + 1;
                    if (_depProperties.Count < idx)
                    {
                        for (var i = _depProperties.Count; i < idx; i++)
                        {
                            var property = DependencyProperty.RegisterAttached(_propertyName + i,
                                typeof(object), typeof(Bind), new PropertyMetadata());
                            _depProperties.Add(property);
                        }
                    }
                    return _depProperties[index];
                }
            }
        }
    }
}
