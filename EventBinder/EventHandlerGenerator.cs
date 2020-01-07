using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Windows;

namespace EventBinder
{
    internal class EventHandlerGenerator
    {
        private readonly ModuleBuilder _module;
		private readonly Dictionary<Type, Delegate> _emptyHandlerCache = new Dictionary<Type, Delegate>();
		private readonly Dictionary<string, Type> _handlerCache = new Dictionary<string, Type>();

        public EventHandlerGenerator(ModuleBuilder module) => _module = module;

        public Delegate GenerateEmptyHandler(Type eventHandler)
        {
	        if (!_emptyHandlerCache.TryGetValue(eventHandler, out var handler))
	        {
		        var method = new DynamicMethod("EmptyHandler", typeof(void), GetParameterTypes(eventHandler), _module);
		        method.GetILGenerator().Emit(OpCodes.Ret);
		        handler = method.CreateDelegate(eventHandler);
				_emptyHandlerCache.Add(eventHandler, handler);
	        }
            return handler;
        }

        public Delegate GenerateHandler(Type eventHandler, EventBindingExtension binding, FrameworkElement source)
        {
            var parameterTypes = GetParameterTypes(eventHandler);
            var arguments = ResolveArguments(binding.Arguments);
            var key = GetKey(eventHandler, source.DataContext, binding.MethodPath, arguments);

			if (!_handlerCache.TryGetValue(key, out var handlerType))
            {
	            var type = _module.DefineType(Guid.NewGuid().ToString(), TypeAttributes.Public);
				var emitter = new Emitter(type, binding.MethodPath, source);
				emitter.GenerateHandler(arguments, parameterTypes);
	            handlerType = type.CreateType();
				_handlerCache.Add(key, handlerType);
			}

			var instance = Activator.CreateInstance(handlerType, new object[] { source, arguments });
            return Delegate.CreateDelegate(eventHandler, instance, Emitter.HANDLER_METHOD_NAME);
        }

        private string GetKey(Type eventHandler, object dataContext, string methodPath, object[] arguments)
        {
	        var sb = new StringBuilder(eventHandler.FullName + methodPath);
		    sb.Append(dataContext.GetType().FullName);
	        foreach (var argument in arguments)
	        {
		        sb.Append(argument.GetType().FullName);
	        }
	        return sb.ToString();
        }

        private static Type[] GetParameterTypes(Type eventHandler)
        {
            var parameters = eventHandler.GetMethod("Invoke").GetParameters();
            var parameterTypes = new Type[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                parameterTypes[i] = parameters[i].ParameterType;
            }

            return parameterTypes;
        }

        private object[] ResolveArguments(object[] arguments)
        {
            var result = new object[arguments.Length];
            for (var i = 0; i < arguments.Length; i++)
            {
                result[i] = ResolveArgument(arguments[i]);
            }
            return result;
        }

        private object ResolveArgument(object argument)
        {
            switch (argument)
            {
                case string str:
                    {
                        if (str.Length > 0)
                        {
                            if (str[0] == '$' && ushort.TryParse(str.Substring(1), NumberStyles.Any, CultureInfo.InvariantCulture, out var position))
                            {
                                return new Emitter.EventArg(str, position);
                            }
                            else if (str[str.Length - 1] == 'm' && decimal.TryParse(str.Substring(0, str.Length - 1), NumberStyles.Any, CultureInfo.InvariantCulture, out var dec))
                            {
                                return dec;
                            }
                            else if (str.Length > 1 && str[0] == '`' && str[str.Length - 1] == '`')
                            {
                                return str.Substring(1, str.Length - 2);
                            }
                            else if (str.Contains(".") && double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var dbl))
                            {
                                return dbl;
                            }
                            else if (int.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var integer))
                            {
                                return integer;
                            }
                        }
                        throw new InvalidOperationException("Cannot parse the expression. If you need a string, wrap the value with ` symbol");
                    }
                default: return argument;
            }
        }
    }
}
