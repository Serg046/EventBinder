﻿using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Windows;
using System.Windows.Markup;

namespace EventBinder
{
    public class EventBinding : MarkupExtension
    {
        public const string ASSEMBLY_NAME = "EventBinder.EventHandler";
        private static readonly EventHandlerGenerator _eventHandlerGenerator;

        static EventBinding()
        {
#if NETFRAMEWORK
	        var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(ASSEMBLY_NAME), AssemblyBuilderAccess.Run);
#else
	        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(ASSEMBLY_NAME), AssemblyBuilderAccess.Run);
#endif
            _eventHandlerGenerator = new EventHandlerGenerator(assemblyBuilder.DefineDynamicModule(ASSEMBLY_NAME));
        }

        internal string MethodPath { get; }
        internal object[] Arguments { get; }

        public int? Debounce { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null) return this;
            var target = (IProvideValueTarget)serviceProvider.GetService(typeof(IProvideValueTarget));
            var frameworkElement = target.TargetObject as FrameworkElement ?? throw new InvalidOperationException("Only FrameworkElements are supported");
            var eventInfo = target.TargetProperty as EventInfo ?? throw new InvalidOperationException("Only events are supported");
            return Bind(frameworkElement, eventInfo);
        }

        public static void Bind(FrameworkElement frameworkElement, string eventName, string methodPath, params object[] arguments)
        {
	        var eventInfo = frameworkElement.GetType().GetEvent(eventName) ?? throw new MissingFieldException($"Cannot find \"{eventName}\" event");
	        var binding = new EventBinding(methodPath, arguments);
	        var handler = binding.Bind(frameworkElement, eventInfo);
		    eventInfo.AddEventHandler(frameworkElement, handler);
        }

        private Delegate Bind(FrameworkElement frameworkElement, EventInfo eventInfo)
        {
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

        private EventBinding(string methodPath, object[] arguments)
        {
            MethodPath = methodPath ?? throw new ArgumentNullException(nameof(methodPath));
            Arguments = arguments;
        }

        public EventBinding(string methodPath) : this(methodPath, new object[0])
        {
        }

        public EventBinding(string methodPath, object arg) : this(methodPath, new[] { arg })
        {
        }

        public EventBinding(string methodPath, object arg1, object arg2) : this(methodPath, new[] { arg1, arg2 })
        {
        }

        public EventBinding(string methodPath, object arg1, object arg2, object arg3) : this(methodPath, new[] { arg1, arg2, arg3 })
        {
        }

        public EventBinding(string methodPath, object arg1, object arg2, object arg3, object arg4) : this(methodPath, new[] { arg1, arg2, arg3, arg4 })
        {
        }

        public EventBinding(string methodPath, object arg1, object arg2, object arg3, object arg4, object arg5) : this(methodPath, new[] { arg1, arg2, arg3, arg4, arg5 })
        {
        }

        public EventBinding(string methodPath, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6) : this(methodPath, new[] { arg1, arg2, arg3, arg4, arg5, arg6 })
        {
        }

        public EventBinding(string methodPath, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7) : this(methodPath, new[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7 })
        {
        }

        public EventBinding(string methodPath, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8) : this(methodPath, new[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 })
        {
        }

        public EventBinding(string methodPath, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9) : this(methodPath, new[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 })
        {
        }
    }
}
