using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace EventBinder
{
	internal class Emitter
	{
        internal const string HANDLER_METHOD_NAME = "Handle";

		private readonly TypeBuilder _typeBuilder;
		private readonly string _methodPath;
		private readonly FrameworkElement _source;
		private readonly FieldInfo _instanceField;
		private readonly FieldInfo _argumentsField;
		private static readonly DependencyPropertyCollection _properties = new DependencyPropertyCollection("Argument");

		public Emitter(TypeBuilder typeBuilder, string methodPath, FrameworkElement source)
		{
			_typeBuilder = typeBuilder;
			_methodPath = methodPath;
			_source = source;
			_instanceField = _typeBuilder.DefineField("_instance", source.GetType(), FieldAttributes.Private);
			_argumentsField = _typeBuilder.DefineField("_arguments", typeof(object[]), FieldAttributes.Private);
		}

		public void GenerateHandler(object[] arguments, Type[] parameterTypes)
		{
			GenerateCtor();
			var method = _typeBuilder.DefineMethod(HANDLER_METHOD_NAME, MethodAttributes.Public, typeof(void), parameterTypes);
			var body = method.GetILGenerator();
			body.Emit(OpCodes.Ldarg_0);
			body.Emit(OpCodes.Ldfld, _instanceField);
			var dataContextProp = _source.GetType().GetProperty(nameof(FrameworkElement.DataContext));
			body.Emit(OpCodes.Call, dataContextProp.GetGetMethod());
			var resolvedArguments = EmitArguments(arguments, parameterTypes);
			var innerMethod = GetMethod(body, _source.DataContext, _methodPath, resolvedArguments.Types);
			if (innerMethod == null) ThrowMissingMethodException(_methodPath, resolvedArguments.Types);
			foreach (var opCode in resolvedArguments.OpCodes)
			{
				opCode(body);
			}
			body.Emit(OpCodes.Callvirt, innerMethod);
			if (innerMethod.ReturnType != typeof(void))
			{
				body.Emit(OpCodes.Pop);
			}
			body.Emit(OpCodes.Ret);
		}

		private MethodInfo GetMethod(ILGenerator body, object instance, string path, Type[] argumentTypes)
		{
			if (instance == null) return null;

			var idx = path.IndexOf('.');
			if (idx != -1)
			{
				var type = instance.GetType();
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
			return instance.GetType().GetMethod(path, argumentTypes);
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
				if (argument is Binding binding)
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

		private Type HandleBindingArg(Binding binding, ICollection<Action<ILGenerator>> opCodes, int position)
		{
			var parent = GetParent(_source) as FrameworkElement;
			var obj = parent.FindName(binding.ElementName) as DependencyObject;
			var propType = obj.GetType().GetProperty(binding.Path.Path).PropertyType;
			opCodes.Add(b => b.Emit(OpCodes.Ldarg_0));
			opCodes.Add(b => b.Emit(OpCodes.Ldfld, _instanceField));
			opCodes.Add(b => b.Emit(OpCodes.Ldc_I4, position));
			var resolveBindingMethod = GetType().GetMethod(nameof(ResolveBinding), BindingFlags.NonPublic | BindingFlags.Static);
			opCodes.Add(b => b.Emit(OpCodes.Call, resolveBindingMethod));
			if (propType != typeof(object))
			{
				opCodes.Add(b => b.Emit(propType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, propType));
			}
			return propType;
		}

		internal static object ResolveBinding(Binding binding, FrameworkElement source, int position)
		{
			if (!BindingOperations.IsDataBound(source, _properties[position]))
			{
				BindingOperations.SetBinding(source, _properties[position], binding);
			}
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

		private DependencyObject GetParent(DependencyObject obj)
		{
			var parent = VisualTreeHelper.GetParent(obj);
			return parent != null ? GetParent(parent) : obj;
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
	}
}
