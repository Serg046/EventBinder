using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace EventBinder
{
    internal class EventHandlerGenerator
    {
        private const string HANDLER_METHOD_NAME = "Handle";
        private readonly ModuleBuilder _module;

        public EventHandlerGenerator(ModuleBuilder module) => _module = module;

        public Delegate GenerateHandler(Type eventHandler, EventBinding binding)
        {
            var parameters = eventHandler.GetMethod("Invoke").GetParameters();
            var parameterTypes = new Type[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                parameterTypes[i] = parameters[i].ParameterType;
            }

            var type = _module.DefineType(Guid.NewGuid().ToString(), TypeAttributes.Public);
            var instanceFld = type.DefineField("_instance", typeof(object), FieldAttributes.Private);
            var argumentsFld = type.DefineField("_arguments", typeof(object[]), FieldAttributes.Private);
            GenerateCtor(type, instanceFld, argumentsFld);
            GenerateHander(binding, instanceFld, argumentsFld, type, parameterTypes);
            var instance = Activator.CreateInstance(type.CreateType(), new[] { binding.Source, binding.Arguments });
            return Delegate.CreateDelegate(eventHandler, instance, HANDLER_METHOD_NAME);
        }

        private void GenerateHander(EventBinding binding, FieldBuilder instanceFld, FieldBuilder argumentsFld,
            TypeBuilder typeBuilder, Type[] parameterTypes)
        {
            var method = typeBuilder.DefineMethod(HANDLER_METHOD_NAME, MethodAttributes.Public, typeof(void), parameterTypes);
            var body = method.GetILGenerator();
            body.Emit(OpCodes.Ldarg_0);
            body.Emit(OpCodes.Ldfld, instanceFld);
            var argumentTypes = new Type[binding.Arguments.Length];
            for (var i = 0; i < binding.Arguments.Length; i++)
            {
                var argument = binding.Arguments[i];
                Type argumentType;
                if (argument is string str && str.Length > 1 && str[0] == '$' &&
                    int.TryParse(str.Substring(1), out var eventArgIdx))
                {
                    body.Emit(OpCodes.Ldarg, eventArgIdx + 1);
                    if (parameterTypes.Length <= eventArgIdx) throw new ArgumentOutOfRangeException($"{str} is not available");
                    argumentType = parameterTypes[eventArgIdx];
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
                argumentTypes[i] = argumentType;
            }
            var innerMethod = binding.Source.GetType().GetMethod(binding.MethodPath, argumentTypes);
            if (innerMethod == null) ThrowMissingMethodException(binding, argumentTypes);
            body.Emit(OpCodes.Callvirt, innerMethod);
            if (innerMethod.ReturnType != typeof(void))
            {
                body.Emit(OpCodes.Pop);
            }
            body.Emit(OpCodes.Ret);
        }

        private static void ThrowMissingMethodException(EventBinding binding, Type[] argumentTypes)
        {
            var sb = new StringBuilder("Cannot find ")
                .Append(binding.MethodPath)
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
    }
}
