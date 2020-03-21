using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
#if AVALONIA
	using Avalonia.Controls;
using Avalonia.Data;
using XamlControlBase = Avalonia.IStyledElement;
	using XamlControl = Avalonia.Visual;
	using XamlBinding = Avalonia.Data.Binding;
#else
	using XamlControlBase = System.Windows.DependencyObject;
	using XamlControl = System.Windows.FrameworkElement;
	using XamlBinding = System.Windows.Data.Binding;
#endif

namespace EventBinder
{
	internal class Emitter
	{
        internal const string HANDLER_METHOD_NAME = "Handle";

        private readonly TypeBuilder _typeBuilder;
		private readonly EventBinding _eventBinding;
		private readonly XamlControl _source;
		private readonly FieldInfo _instanceField;
		private readonly FieldInfo _argumentsField;
		private readonly FieldInfo _timerField;
		private static readonly DependencyPropertyCollection _properties = new DependencyPropertyCollection("Argument");

		public Emitter(TypeBuilder typeBuilder, EventBinding eventBinding, XamlControl source)
		{
			_typeBuilder = typeBuilder;
			_eventBinding = eventBinding;
			_source = source;
			_instanceField = _typeBuilder.DefineField("_instance", source.GetType(), FieldAttributes.Private);
			_argumentsField = _typeBuilder.DefineField("_arguments", typeof(object[]), FieldAttributes.Private);
			_timerField = _typeBuilder.DefineField("_timer", typeof(Timer), FieldAttributes.Private);
		}

		public void GenerateHandler(object[] arguments, Type[] parameterTypes)
		{
			GenerateCtor();
			var method = _typeBuilder.DefineMethod(HANDLER_METHOD_NAME, MethodAttributes.Public, typeof(void), parameterTypes);
			var body = method.GetILGenerator();
			body.Emit(OpCodes.Ldarg_0);
			body.Emit(OpCodes.Ldfld, _instanceField);
			var dataContextProp = _source.GetType().GetProperty(nameof(XamlControl.DataContext));
			body.Emit(OpCodes.Call, dataContextProp.GetGetMethod());
			var resolvedArguments = EmitArguments(arguments, parameterTypes);
			var innerMethod = GetMethod(body, _source.DataContext, _eventBinding.MethodPath, resolvedArguments.Types);
			if (innerMethod == null) ThrowMissingMethodException(_eventBinding.MethodPath, resolvedArguments.Types);
			foreach (var opCode in resolvedArguments.OpCodes)
			{
				opCode(body);
			}
			if (_eventBinding.Debounce.HasValue)
			{
				GenerateDebouncedHandler(resolvedArguments, innerMethod, body);
			}
			else
			{
				body.Emit(OpCodes.Callvirt, innerMethod);
				if (innerMethod.ReturnType != typeof(void))
				{
					body.Emit(OpCodes.Pop);
				}
			}

			body.Emit(OpCodes.Ret);
		}

		private void GenerateDebouncedHandler(ResolvedArguments resolvedArguments, MethodInfo innerMethod, ILGenerator body)
		{
			var argTypes = new Type[resolvedArguments.Types.Length + 1];
			Array.Copy(resolvedArguments.Types, 0, argTypes, 1, resolvedArguments.Types.Length);
			argTypes[0] = _source.DataContext.GetType();
			var debouncerModel = GenerateDebouncer(argTypes, innerMethod);
			body.Emit(OpCodes.Newobj, debouncerModel.Constructor);

			body.Emit(OpCodes.Ldarg_0);
			body.Emit(OpCodes.Ldfld, _timerField);
			var continuation = body.DefineLabel();
			body.Emit(OpCodes.Brfalse_S, continuation);
			body.Emit(OpCodes.Ldarg_0);
			body.Emit(OpCodes.Ldfld, _timerField);
			body.Emit(OpCodes.Call, typeof(Timer).GetMethod(nameof(Timer.Dispose),
				BindingFlags.Instance | BindingFlags.Public, null, new Type[0], null));
			body.MarkLabel(continuation);

			body.Emit(OpCodes.Ldftn, debouncerModel.Method);
			body.Emit(OpCodes.Newobj, typeof(TimerCallback).GetConstructors()[0]);
			body.Emit(OpCodes.Call, typeof(SynchronizationContext).GetProperty(nameof(SynchronizationContext.Current)).GetGetMethod());
			body.Emit(OpCodes.Ldc_I4, _eventBinding.Debounce.Value);
			body.Emit(OpCodes.Ldc_I4_M1);
			body.Emit(OpCodes.Newobj, typeof(Timer).GetConstructor(
				BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.Instance,
				null, new[] {typeof(TimerCallback), typeof(object), typeof(int), typeof(int)}, null));

			var variable = body.DeclareLocal(typeof(Timer));
			body.Emit(OpCodes.Stloc, variable);
			body.Emit(OpCodes.Ldarg_0);
			body.Emit(OpCodes.Ldloc, variable);
			body.Emit(OpCodes.Stfld, _timerField);
		}

		private DebouncerModel GenerateDebouncer(Type[] argTypes, MethodInfo bindedMethod)
		{
			var nestedTypeBuilder = _typeBuilder.DefineNestedType("Debouncer");
			var ctor = nestedTypeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, argTypes);
			var ctorBody = ctor.GetILGenerator();
			var fields = new FieldInfo[argTypes.Length];
			for (var i = 0; i < argTypes.Length; i++)
			{
				var field = nestedTypeBuilder.DefineField(i.ToString(), argTypes[i], FieldAttributes.Private);
				fields[i] = field;
				ctorBody.Emit(OpCodes.Ldarg_0);
				ctorBody.Emit(OpCodes.Ldarg, i + 1);
				ctorBody.Emit(OpCodes.Stfld, field);
			}
			ctorBody.Emit(OpCodes.Ret);
			var method = GenerateExecutor(nestedTypeBuilder, bindedMethod, fields);

			// Should be pre-created
			nestedTypeBuilder.CreateType();
			return new DebouncerModel(ctor, method);
		}

		private static MethodBuilder GenerateExecutor(TypeBuilder nestedTypeBuilder, MethodInfo bindedMethod, FieldInfo[] fields)
		{
			var method = nestedTypeBuilder.DefineMethod("ExecuteUsingContext", MethodAttributes.Public,
				typeof(void), new[] { typeof(object) });
			var methodBody = method.GetILGenerator();
			methodBody.Emit(OpCodes.Ldarg_1);
			methodBody.Emit(OpCodes.Castclass, typeof(SynchronizationContext));
			methodBody.Emit(OpCodes.Ldarg_0);
			methodBody.Emit(OpCodes.Ldftn, GenerateExecuteMethod(nestedTypeBuilder, bindedMethod, fields));
			methodBody.Emit(OpCodes.Newobj, typeof(SendOrPostCallback).GetConstructors()[0]);
			methodBody.Emit(OpCodes.Ldnull);
			methodBody.Emit(OpCodes.Callvirt, typeof(SynchronizationContext).GetMethod(nameof(SynchronizationContext.Post)));
			methodBody.Emit(OpCodes.Ret);
			return method;
		}

		private static MethodBuilder GenerateExecuteMethod(TypeBuilder nestedTypeBuilder, MethodInfo bindedMethod, FieldInfo[] fields)
		{
			var method = nestedTypeBuilder.DefineMethod("Execute", MethodAttributes.Private,
				typeof(void), new[] {typeof(object)});
			var methodBody = method.GetILGenerator();
			for (var i = 0; i < fields.Length; i++)
			{
				methodBody.Emit(OpCodes.Ldarg_0);
				methodBody.Emit(OpCodes.Ldfld, fields[i]);
			}

			methodBody.Emit(OpCodes.Callvirt, bindedMethod);
			if (bindedMethod.ReturnType != typeof(void))
			{
				methodBody.Emit(OpCodes.Pop);
			}

			methodBody.Emit(OpCodes.Ret);
			return method;
		}

		private MethodInfo GetMethod(ILGenerator body, object instance, string path, Type[] argumentTypes)
		{
			if (instance == null) return null;

			var type = instance.GetType();
			var idx = path.IndexOf('.');
			if (idx != -1)
			{
				var memberName = path.Substring(0, idx);
				object newInstance = null;
				var property = type.GetProperty(memberName);
				if (property != null)
				{
					newInstance = property.GetValue(instance, null);
					body.Emit(OpCodes.Call, property.GetGetMethod());
				}
				else
				{
					var field = type.GetField(memberName);
					if (field != null)
					{
						newInstance = field.GetValue(instance);
						body.Emit(OpCodes.Ldfld, field);
					}
				}
				return GetMethod(body, newInstance, path.Substring(idx + 1), argumentTypes);
			}
			return type.GetMethod(path, argumentTypes);
		}

		private ResolvedArguments EmitArguments(object[] arguments, Type[] parameterTypes)
		{
			var argumentTypes = new Type[arguments.Length];
			var opCodes = new List<Action<ILGenerator>>();
			for (var i = 0; i < arguments.Length; i++)
			{
				var argument = arguments[i];
				Type argumentType;
				if (argument is EventArg eventArg)
				{
					argumentType = HandleEventArg(parameterTypes, opCodes, eventArg);
				}
				else
				{
					argumentType = HandleArg(opCodes, i, argument);
				}
				if (argument is XamlBinding binding)
				{
					argumentType = HandleBindingArg(binding, opCodes, i);
				}
				argumentTypes[i] = argumentType;
			}
			return new ResolvedArguments(argumentTypes, opCodes);
		}

		private Type HandleArg(ICollection<Action<ILGenerator>> opCodes, int position, object argument)
		{
			opCodes.Add(b => b.Emit(OpCodes.Ldarg_0));
			opCodes.Add(b => b.Emit(OpCodes.Ldfld, _argumentsField));
			opCodes.Add(b => b.Emit(OpCodes.Ldc_I4, position));
			opCodes.Add(b => b.Emit(OpCodes.Ldelem_Ref));
			var argumentType = argument.GetType();
			if (argumentType.IsValueType)
			{
				opCodes.Add(b => b.Emit(OpCodes.Unbox_Any, argumentType));
			}
			return argumentType;
		}

		private static Type HandleEventArg(Type[] parameterTypes, ICollection<Action<ILGenerator>> opCodes, EventArg eventArg)
		{
			opCodes.Add(b => b.Emit(OpCodes.Ldarg, eventArg.Position + 1));
			if (parameterTypes.Length <= eventArg.Position)
				throw new ArgumentOutOfRangeException($"{eventArg.OriginalString} is not available");
			var argumentType = parameterTypes[eventArg.Position];
			if (argumentType.IsValueType)
			{
				opCodes.Add(b => b.Emit(OpCodes.Unbox_Any, argumentType));
			}
			return argumentType;
		}

		private Type HandleBindingArg(XamlBinding binding, ICollection<Action<ILGenerator>> opCodes, int position)
		{
			var argType = GetArgType(binding);
			opCodes.Add(b => b.Emit(OpCodes.Ldarg_0));
			opCodes.Add(b => b.Emit(OpCodes.Ldfld, _instanceField));
			opCodes.Add(b => b.Emit(OpCodes.Ldc_I4, position));
			var resolveBindingMethod = GetType().GetMethod(nameof(ResolveBinding), BindingFlags.NonPublic | BindingFlags.Static);
			opCodes.Add(b => b.Emit(OpCodes.Call, resolveBindingMethod));
			if (argType != typeof(object))
			{
				opCodes.Add(b => b.Emit(argType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, argType));
			}
			return argType;
		}

		private Type GetArgType(XamlBinding binding)
		{
			object context;
			if (string.IsNullOrEmpty(binding.ElementName))
			{
				context = _source.DataContext;
			}
			else
            {
#if AVALONIA
	            var root = GetRootParent(_source) as IControl;
	            context = root.FindControl<IControl>(binding.ElementName);
#else
				var root = System.Windows.Window.GetWindow(_source) ?? (XamlControl)GetRootParent(_source);
				context = root.FindName(binding.ElementName) as System.Windows.DependencyObject;
#endif
            }

#if AVALONIA
			var path = binding.Path;
#else
			var path = binding.Path?.Path;
#endif
			return string.IsNullOrEmpty(path) || path == "."
				? context.GetType()
				: GetArgType(context, path);
		}

		private Type GetArgType(object instance, string path)
		{
			var idx = path.IndexOf('.');
			var type = instance.GetType();
			if (idx != -1)
			{
				var memberName = path.Substring(0, idx);
				var newInstance = type.GetProperty(memberName)?.GetValue(instance, null)
					?? type.GetField(memberName)?.GetValue(instance);
				return GetArgType(newInstance, path.Substring(idx + 1));
			}
			return type.GetProperty(path)?.PropertyType ?? type.GetField(path)?.FieldType;
		}

		internal static object ResolveBinding(XamlBinding binding, XamlControl source, int position)
		{
#if AVALONIA
			var instancedBinding = binding.Initiate(source, _properties[position]);
			BindingOperations.Apply(source, _properties[position], instancedBinding, null);
#else
			if (!System.Windows.Data.BindingOperations.IsDataBound(source, _properties[position]))
			{
				System.Windows.Data.BindingOperations.SetBinding(source, _properties[position], binding);
			}
#endif
			return source.GetValue(_properties[position]);
		}

		private void GenerateCtor()
		{
			var ctor = _typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard,
				new[] { _instanceField.FieldType, _argumentsField.FieldType });
			var body = ctor.GetILGenerator();
			body.Emit(OpCodes.Ldarg_0);
			body.Emit(OpCodes.Ldarg_1);
			body.Emit(OpCodes.Stfld, _instanceField);
			body.Emit(OpCodes.Ldarg_0);
			body.Emit(OpCodes.Ldarg_2);
			body.Emit(OpCodes.Stfld, _argumentsField);
			body.Emit(OpCodes.Ret);
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

		private XamlControlBase GetRootParent(XamlControlBase obj)
		{
#if AVALONIA
			var parent = obj.Parent;
#else
			var parent = System.Windows.Media.VisualTreeHelper.GetParent(obj);
#endif
			return parent != null ? GetRootParent(parent) : obj;
		}

		internal class EventArg
		{
			public EventArg(string originalString, ushort position)
			{
				OriginalString = originalString;
				Position = position;
			}

			public string OriginalString { get; }
			public ushort Position { get; }
		}

		private class ResolvedArguments
		{
			public ResolvedArguments(Type[] types, IEnumerable<Action<ILGenerator>> opCodes)
			{
				Types = types;
				OpCodes = opCodes;
			}

			public Type[] Types { get; }
			public IEnumerable<Action<ILGenerator>> OpCodes { get; }
		}

		private class DebouncerModel
		{
			public DebouncerModel(ConstructorInfo constructor, MethodInfo method)
			{
				Constructor = constructor;
				Method = method;
			}

			public ConstructorInfo Constructor { get; }
			public MethodInfo Method { get; }
		}
	}
}
