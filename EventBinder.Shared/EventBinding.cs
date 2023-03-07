using System;
using System.Reflection;
using System.Reflection.Emit;
#if AVALONIA
	using XamlMarkupExtension = Avalonia.Markup.Xaml.MarkupExtension;
	using IXamlProvideValueTarget = Avalonia.Markup.Xaml.IProvideValueTarget;
	using XamlControl = Avalonia.Visual;
#else
	using XamlMarkupExtension = System.Windows.Markup.MarkupExtension;
	using IXamlProvideValueTarget = System.Windows.Markup.IProvideValueTarget;
	using XamlControl = System.Windows.FrameworkElement;
#endif

namespace EventBinder
{
    public class EventBinding : XamlMarkupExtension
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
            var target = (IXamlProvideValueTarget)serviceProvider.GetService(typeof(IXamlProvideValueTarget));
            var frameworkElement = target.TargetObject as XamlControl ?? throw new InvalidOperationException("Only FrameworkElements are supported");
#if AVALONIA
	        EventInfo eventInfo = null;
	        if (target.TargetProperty is string eventName)
	        {
		        eventInfo = frameworkElement.GetType().GetEvent(eventName);
	        }
            if (eventInfo == null) throw new InvalidOperationException("Only events are supported");
#else
            var eventInfo = target.TargetProperty as EventInfo
                            ?? TryDetermineEventInfo(serviceProvider, frameworkElement)
                            ?? throw new InvalidOperationException("Only events are supported");
#endif       
            return Bind(frameworkElement, eventInfo);
        }

        private EventInfo TryDetermineEventInfo(IServiceProvider serviceProvider, XamlControl frameworkElement)
        {
            var xamlContextField = serviceProvider.GetType().GetField("_xamlContext", BindingFlags.Instance | BindingFlags.NonPublic);
            if (xamlContextField != null)
            {
	            var xamlContext = xamlContextField.GetValue(serviceProvider);
	            var parentPropertyProp = xamlContext.GetType().GetProperty("ParentProperty");
	            var parentProperty = parentPropertyProp?.GetValue(xamlContext, null);
	            var nameProp = parentProperty?.GetType().GetProperty("Name");
	            if (nameProp != null)
	            {
		            var name = nameProp.GetValue(parentProperty, null).ToString();
		            return frameworkElement.GetType().GetEvent(name);
	            }
            }

            return null;
        }

        public static void Bind(XamlControl frameworkElement, string eventName, string methodPath, params object[] arguments)
        {
	        var eventInfo = frameworkElement.GetType().GetEvent(eventName) ?? throw new MissingFieldException($"Cannot find \"{eventName}\" event");
	        var binding = new EventBinding(methodPath, arguments);
	        var handler = binding.Bind(frameworkElement, eventInfo);
		    eventInfo.AddEventHandler(frameworkElement, handler);
        }

        private Delegate Bind(XamlControl frameworkElement, EventInfo eventInfo)
        {
            var parameters = eventInfo.EventHandlerType.GetMethod("Invoke").GetParameters();
	        var parameterTypes = new Type[parameters.Length];
	        for (var i = 0; i < parameters.Length; i++)
	        {
		        parameterTypes[i] = parameters[i].ParameterType;
	        }

	        var handler = GenerateHandler(frameworkElement, eventInfo);
	        var sync = new object();
	        var binded = false;
            frameworkElement.DataContextChanged += (sender, e) =>
	        {
                lock (sync)
		        {
			        eventInfo.RemoveEventHandler(frameworkElement, handler);
			        handler = GenerateHandler(frameworkElement, eventInfo);
                    eventInfo.AddEventHandler(frameworkElement, handler);
			        binded = true;
		        }
	        };
#if AVALONIA
            frameworkElement.DetachedFromVisualTree += (sender, e) =>
            {
	            lock (sync)
	            {
                    eventInfo.RemoveEventHandler(frameworkElement, handler);
                    binded = false;
	            }
            };
            frameworkElement.AttachedToVisualTree += (sender, e) =>
            {
				lock (sync)
				{
					if (!binded)
					{
						eventInfo.AddEventHandler(frameworkElement, handler);
						binded = true;
					}
				}
			};
#else
            frameworkElement.Unloaded += (sender, e) =>
            {
	            lock (sync)
	            {
                    eventInfo.RemoveEventHandler(frameworkElement, handler);
			        binded = false;
                }
            };
            frameworkElement.Loaded += (sender, e) =>
            {
	            lock (sync)
	            {
		            if (!binded)
		            {
			            eventInfo.AddEventHandler(frameworkElement, handler);
			            binded = true;
		            }
	            }
            };
#endif
            return handler;
        }

        private Delegate GenerateHandler(XamlControl frameworkElement, EventInfo eventInfo)
        {
	        return frameworkElement.DataContext != null
		        ? _eventHandlerGenerator.GenerateHandler(eventInfo.EventHandlerType, this, frameworkElement)
		        : _eventHandlerGenerator.GenerateEmptyHandler(eventInfo.EventHandlerType);
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
