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

        public static readonly DependencyProperty MethodProperty = DependencyProperty.RegisterAttached("Method",
            typeof(object), typeof(Bind), new PropertyMetadata(MethodPropertyChangedCallback));
        public static object GetMethod(DependencyObject element) => element.GetValue(MethodProperty);
        public static void SetMethod(DependencyObject element, object value) => element.SetValue(MethodProperty, value);

        public static readonly DependencyProperty MethodProperty2 = DependencyProperty.RegisterAttached("Method2",
            typeof(object), typeof(Bind), new PropertyMetadata(MethodPropertyChangedCallback));
        public static object GetMethod2(DependencyObject element) => element.GetValue(MethodProperty2);
        public static void SetMethod2(DependencyObject element, object value) => element.SetValue(MethodProperty2, value);

        public static readonly DependencyProperty MethodProperty3 = DependencyProperty.RegisterAttached("Method3",
            typeof(object), typeof(Bind), new PropertyMetadata(MethodPropertyChangedCallback));
        public static object GetMethod3(DependencyObject element) => element.GetValue(MethodProperty3);
        public static void SetMethod3(DependencyObject element, object value) => element.SetValue(MethodProperty3, value);

        public static readonly DependencyProperty MethodProperty4 = DependencyProperty.RegisterAttached("Method4",
            typeof(object), typeof(Bind), new PropertyMetadata(MethodPropertyChangedCallback));
        public static object GetMethod4(DependencyObject element) => element.GetValue(MethodProperty4);
        public static void SetMethod4(DependencyObject element, object value) => element.SetValue(MethodProperty4, value);

        public static readonly DependencyProperty MethodProperty5 = DependencyProperty.RegisterAttached("Method5",
            typeof(object), typeof(Bind), new PropertyMetadata(MethodPropertyChangedCallback));
        public static object GetMethod5(DependencyObject element) => element.GetValue(MethodProperty5);
        public static void SetMethod5(DependencyObject element, object value) => element.SetValue(MethodProperty5, value);

        public static readonly DependencyProperty MethodProperty6 = DependencyProperty.RegisterAttached("Method6",
            typeof(object), typeof(Bind), new PropertyMetadata(MethodPropertyChangedCallback));
        public static object GetMethod6(DependencyObject element) => element.GetValue(MethodProperty6);
        public static void SetMethod6(DependencyObject element, object value) => element.SetValue(MethodProperty6, value);

        public static readonly DependencyProperty MethodProperty7 = DependencyProperty.RegisterAttached("Method7",
            typeof(object), typeof(Bind), new PropertyMetadata(MethodPropertyChangedCallback));
        public static object GetMethod7(DependencyObject element) => element.GetValue(MethodProperty7);
        public static void SetMethod7(DependencyObject element, object value) => element.SetValue(MethodProperty7, value);

        public static readonly DependencyProperty MethodProperty8 = DependencyProperty.RegisterAttached("Method8",
            typeof(object), typeof(Bind), new PropertyMetadata(MethodPropertyChangedCallback));
        public static object GetMethod8(DependencyObject element) => element.GetValue(MethodProperty8);
        public static void SetMethod8(DependencyObject element, object value) => element.SetValue(MethodProperty8, value);

        public static readonly DependencyProperty MethodProperty9 = DependencyProperty.RegisterAttached("Method9",
            typeof(object), typeof(Bind), new PropertyMetadata(MethodPropertyChangedCallback));
        public static object GetMethod9(DependencyObject element) => element.GetValue(MethodProperty9);
        public static void SetMethod9(DependencyObject element, object value) => element.SetValue(MethodProperty9, value);
    }
}
