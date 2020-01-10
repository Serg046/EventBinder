using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Windows;
using System.Windows.Markup;

namespace EventBinder
{
    public class EventBindingExtension : MarkupExtension
    {
        public const string ASSEMBLY_NAME = "EventBinder.EventHandler";
        private static readonly EventHandlerGenerator _eventHandlerGenerator;

        static EventBindingExtension()
        {
#if NET30
	        var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(ASSEMBLY_NAME), AssemblyBuilderAccess.Run);
#else
	        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(ASSEMBLY_NAME), AssemblyBuilderAccess.Run);
#endif
            _eventHandlerGenerator = new EventHandlerGenerator(assemblyBuilder.DefineDynamicModule(ASSEMBLY_NAME));
        }

        internal string MethodPath { get; }
        internal object[] Arguments { get; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null) return this;
            var target = (IProvideValueTarget)serviceProvider.GetService(typeof(IProvideValueTarget));
            var frameworkElement = target.TargetObject as FrameworkElement ?? throw new InvalidOperationException("Only FrameworkElements are supported");
            var eventInfo = target.TargetProperty as EventInfo ?? throw new InvalidOperationException("Only events are supported");
            var parameters = eventInfo.EventHandlerType.GetMethod("Invoke").GetParameters();
            var parameterTypes = new Type[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                parameterTypes[i] = parameters[i].ParameterType;
            }
            var handler = frameworkElement.DataContext != null
                ? _eventHandlerGenerator.GenerateHandler(eventInfo.EventHandlerType, this, frameworkElement)
                : _eventHandlerGenerator.GenerateEmptyHandler(eventInfo.EventHandlerType);
            frameworkElement.DataContextChanged += (sender, e) =>
            {
                eventInfo.RemoveEventHandler(frameworkElement, handler);
                handler = _eventHandlerGenerator.GenerateHandler(eventInfo.EventHandlerType, this, frameworkElement);
                eventInfo.AddEventHandler(frameworkElement, handler);
            };
            frameworkElement.Unloaded += (sender, e) => eventInfo.RemoveEventHandler(frameworkElement, handler);
            return handler;
        }

        private EventBindingExtension(string methodPath, object[] arguments)
        {
            MethodPath = methodPath ?? throw new ArgumentNullException(nameof(methodPath));
            Arguments = arguments;
        }

        public EventBindingExtension(string methodPath) : this(methodPath, new object[0])
        {
        }

        public EventBindingExtension(string methodPath, object arg) : this(methodPath, new[] { arg })
        {
        }

        public EventBindingExtension(string methodPath, object arg1, object arg2) : this(methodPath, new[] { arg1, arg2 })
        {
        }

        public EventBindingExtension(string methodPath, object arg1, object arg2, object arg3) : this(methodPath, new[] { arg1, arg2, arg3 })
        {
        }

        public EventBindingExtension(string methodPath, object arg1, object arg2, object arg3, object arg4) : this(methodPath, new[] { arg1, arg2, arg3, arg4 })
        {
        }

        public EventBindingExtension(string methodPath, object arg1, object arg2, object arg3, object arg4, object arg5) : this(methodPath, new[] { arg1, arg2, arg3, arg4, arg5 })
        {
        }

        public EventBindingExtension(string methodPath, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6) : this(methodPath, new[] { arg1, arg2, arg3, arg4, arg5, arg6 })
        {
        }

        public EventBindingExtension(string methodPath, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7) : this(methodPath, new[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7 })
        {
        }

        public EventBindingExtension(string methodPath, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8) : this(methodPath, new[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 })
        {
        }

        public EventBindingExtension(string methodPath, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9) : this(methodPath, new[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 })
        {
        }
    }
}
