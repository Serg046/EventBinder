using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace EventBinder
{
    internal class EventHandlerGenerator
    {
        private const string HANDLER_METHOD_NAME = "Handle";
        private readonly ModuleBuilder _module;
        private static readonly DependencyPropertyCollection _properties = new DependencyPropertyCollection("Argument");
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
            var key = GetKey(eventHandler, binding.MethodPath, arguments);

			if (!_handlerCache.TryGetValue(key, out var handlerType))
            {
	            var type = _module.DefineType(Guid.NewGuid().ToString(), TypeAttributes.Public);
	            var instanceFld = type.DefineField("_instance", source.GetType(), FieldAttributes.Private);
	            var argumentsFld = type.DefineField("_arguments", typeof(object[]), FieldAttributes.Private);
	            GenerateCtor(type, instanceFld, argumentsFld);
	            GenerateHandler(binding.MethodPath, arguments, source, instanceFld, argumentsFld, type, parameterTypes);
	            handlerType = type.CreateType();
				_handlerCache.Add(key, handlerType);
			}

			var instance = Activator.CreateInstance(handlerType, new object[] { source, arguments });
            return Delegate.CreateDelegate(eventHandler, instance, HANDLER_METHOD_NAME);
        }

        private string GetKey(Type eventHandler, string methodPath, object[] arguments)
        {
	        var sb = new StringBuilder(eventHandler.FullName + methodPath);
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

        private void GenerateHandler(string methodPath, object[] arguments, FrameworkElement source, FieldBuilder instanceFld,
            FieldBuilder argumentsFld, TypeBuilder typeBuilder, Type[] parameterTypes)
        {
            var method = typeBuilder.DefineMethod(HANDLER_METHOD_NAME, MethodAttributes.Public, typeof(void), parameterTypes);
            var body = method.GetILGenerator();
            body.Emit(OpCodes.Ldarg_0);
            body.Emit(OpCodes.Ldfld, instanceFld);
            var dataContextProp = source.GetType().GetProperty(nameof(FrameworkElement.DataContext));
            body.Emit(OpCodes.Call, dataContextProp.GetGetMethod());
            var argumentTypes = new Type[arguments.Length];
            for (var i = 0; i < arguments.Length; i++)
            {
                var argument = arguments[i];
                Type argumentType;
                if (argument is EventArg eventArg)
                {
                    body.Emit(OpCodes.Ldarg, eventArg.Position + 1);
                    if (parameterTypes.Length <= eventArg.Position) throw new ArgumentOutOfRangeException($"{eventArg.OriginalString} is not available");
                    argumentType = parameterTypes[eventArg.Position];
                    if (argumentType.IsValueType)
                    {
                        body.Emit(OpCodes.Unbox_Any, argumentType);
                    }
                }
                else
                {
                    body.Emit(OpCodes.Ldarg_0);
                    body.Emit(OpCodes.Ldfld, argumentsFld);
                    body.Emit(OpCodes.Ldc_I4, i);
                    body.Emit(OpCodes.Ldelem_Ref);
                    argumentType = argument.GetType();
                    if (argumentType.IsValueType)
                    {
                        body.Emit(OpCodes.Unbox_Any, argumentType);
                    }
                }
                if (argument is Binding binding)
                {
					var parent = GetParent(source) as FrameworkElement;
					var obj = parent.FindName(binding.ElementName) as DependencyObject;
					var propType = obj.GetType().GetProperty(binding.Path.Path).PropertyType;
					body.Emit(OpCodes.Ldarg_0);
                    body.Emit(OpCodes.Ldfld, instanceFld);
                    body.Emit(OpCodes.Ldc_I4, i);
                    var resolveBindingMethod = GetType().GetMethod(nameof(ResolveBinding), BindingFlags.NonPublic | BindingFlags.Static);
                    body.Emit(OpCodes.Call, resolveBindingMethod);
                    if (propType != typeof(object))
                    {
	                    body.Emit(propType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, propType);
                    }
                    argumentType = propType;
                }
                argumentTypes[i] = argumentType;
            }
            var innerMethod = source.DataContext.GetType().GetMethod(methodPath, argumentTypes);
            if (innerMethod == null) ThrowMissingMethodException(methodPath, argumentTypes);
            body.Emit(OpCodes.Callvirt, innerMethod);
            if (innerMethod.ReturnType != typeof(void))
            {
                body.Emit(OpCodes.Pop);
            }
            body.Emit(OpCodes.Ret);
        }

        private DependencyObject GetParent(DependencyObject obj)
        {
            var parent = VisualTreeHelper.GetParent(obj);
            return parent != null ? GetParent(parent) : obj;
        }

        internal static object ResolveBinding(Binding binding, FrameworkElement source, int position)
        {
            if (!BindingOperations.IsDataBound(source, _properties[position]))
            {
                BindingOperations.SetBinding(source, _properties[position], binding);
            }
            return source.GetValue(_properties[position]);
        }

        private static void ThrowMissingMethodException(string methodPath, Type[] argumentTypes)
        {
            var sb = new StringBuilder("Cannot find ")
                .Append(methodPath)
                .Append("(");
            for (var i = 0; i < argumentTypes.Length; i++)
            {
                sb.Append(argumentTypes[i].Name);
                if (i < argumentTypes.Length - 1)
                {
                    sb.Append(",");
                }
            }
            sb.Append(")");
            throw new MissingMethodException(sb.ToString());
        }

        private static void GenerateCtor(TypeBuilder type, FieldBuilder instanceFld, FieldBuilder argumentsFld)
        {
            var ctor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard,
                            new[] { instanceFld.FieldType, argumentsFld.FieldType });
            var body = ctor.GetILGenerator();
            body.Emit(OpCodes.Ldarg_0);
            body.Emit(OpCodes.Ldarg_1);
            body.Emit(OpCodes.Stfld, instanceFld);
            body.Emit(OpCodes.Ldarg_0);
            body.Emit(OpCodes.Ldarg_2);
            body.Emit(OpCodes.Stfld, argumentsFld);
            body.Emit(OpCodes.Ret);
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
                                return new EventArg(str, position);
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

        private class EventArg
        {
            public EventArg(string originalString, ushort position)
            {
                OriginalString = originalString;
                Position = position;
            }

            public string OriginalString { get; }
            public ushort Position { get; }
        }
    }
}
