using System;
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
			var argumentTypes = HandleArguments(arguments, parameterTypes, body);
			var innerMethod = _source.DataContext.GetType().GetMethod(_methodPath, argumentTypes);
			if (innerMethod == null) ThrowMissingMethodException(_methodPath, argumentTypes);
			body.Emit(OpCodes.Callvirt, innerMethod);
			if (innerMethod.ReturnType != typeof(void))
			{
				body.Emit(OpCodes.Pop);
			}
			body.Emit(OpCodes.Ret);
		}

		private Type[] HandleArguments(object[] arguments, Type[] parameterTypes, ILGenerator body)
		{
			var argumentTypes = new Type[arguments.Length];
			for (var i = 0; i < arguments.Length; i++)
			{
				var argument = arguments[i];
				Type argumentType;
				if (argument is EventArg eventArg)
				{
					argumentType = HandleEventArg(parameterTypes, body, eventArg);
				}
				else
				{
					argumentType = HandleArg(body, i, argument);
				}
				if (argument is Binding binding)
				{
					argumentType = HandleBindingArg(binding, body, i);
				}
				argumentTypes[i] = argumentType;
			}
			return argumentTypes;
		}

		private Type HandleArg(ILGenerator body, int position, object argument)
		{
			body.Emit(OpCodes.Ldarg_0);
			body.Emit(OpCodes.Ldfld, _argumentsField);
			body.Emit(OpCodes.Ldc_I4, position);
			body.Emit(OpCodes.Ldelem_Ref);
			var argumentType = argument.GetType();
			if (argumentType.IsValueType)
			{
				body.Emit(OpCodes.Unbox_Any, argumentType);
			}
			return argumentType;
		}

		private static Type HandleEventArg(Type[] parameterTypes, ILGenerator body, EventArg eventArg)
		{
			body.Emit(OpCodes.Ldarg, eventArg.Position + 1);
			if (parameterTypes.Length <= eventArg.Position)
				throw new ArgumentOutOfRangeException($"{eventArg.OriginalString} is not available");
			var argumentType = parameterTypes[eventArg.Position];
			if (argumentType.IsValueType)
			{
				body.Emit(OpCodes.Unbox_Any, argumentType);
			}
			return argumentType;
		}

		private Type HandleBindingArg(Binding binding, ILGenerator body, int position)
		{
			var parent = GetParent(_source) as FrameworkElement;
			var obj = parent.FindName(binding.ElementName) as DependencyObject;
			var propType = obj.GetType().GetProperty(binding.Path.Path).PropertyType;
			body.Emit(OpCodes.Ldarg_0);
			body.Emit(OpCodes.Ldfld, _instanceField);
			body.Emit(OpCodes.Ldc_I4, position);
			var resolveBindingMethod = GetType().GetMethod(nameof(ResolveBinding), BindingFlags.NonPublic | BindingFlags.Static);
			body.Emit(OpCodes.Call, resolveBindingMethod);
			if (propType != typeof(object))
			{
				body.Emit(propType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, propType);
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
	}
}
