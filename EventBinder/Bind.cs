using System;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Reflection.Emit;

namespace EventBinder
{
    public static class Bind
    {
        private static readonly EventHandlerGenerator _eventHandlerGenerator;

        static Bind()
        {
            _eventHandlerGenerator = new EventHandlerGenerator(AppDomain.CurrentDomain
                .DefineDynamicAssembly(new AssemblyName("EventHadler"), AssemblyBuilderAccess.RunAndSave)
                .DefineDynamicModule("EventHadler"));
        }

        private static void MethodPropertyChangedCallback(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            var eventBinding = (EventBinding)BindingOperations.GetBinding(source, args.Property)
                               ?? throw new InvalidOperationException("Cannot get binding");
            Subscribe(source, eventBinding, args.NewValue);
        }

        private static void Subscribe(DependencyObject parent, EventBinding binding, object source)
        {
            foreach (var eventPath in binding.EventPath.Split(','))
            {
                var eventInfo = parent.GetType().GetEvent(eventPath) 
                    ?? throw new InvalidOperationException($"Cannot find the event '{eventPath}'");
                var parameters = eventInfo.EventHandlerType.GetMethod("Invoke").GetParameters();
                var parameterTypes = new Type[parameters.Length];
                for (var i = 0; i < parameters.Length; i++)
                {
                    parameterTypes[i] = parameters[i].ParameterType;
                }
                var eventHandler = _eventHandlerGenerator.GenerateHandler(eventInfo.EventHandlerType, binding, source);
                eventInfo.AddEventHandler(parent, eventHandler);
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
