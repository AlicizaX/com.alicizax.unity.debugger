using AlicizaX.Console.Utilities;
using Cysharp.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AlicizaX.Console.Internal;

namespace AlicizaX.Console
{
    /// <summary>
    /// 保存命令的完整数据，并提供调用命令的入口。
    /// </summary>
    public class CommandData
    {
        public readonly string CommandName;
        public readonly string CommandDescription;
        public readonly string CommandSignature;
        public readonly string ParameterSignature;
        public readonly string GenericSignature;

        public readonly ParameterInfo[] MethodParamData;
        public readonly Type[] ParamTypes;
        public readonly Type[] GenericParamTypes;
        public readonly MethodInfo MethodData;
        public readonly MonoTargetType MonoTarget;

        private readonly object[] _defaultParameters;

        public bool IsGeneric => GenericParamTypes.Length > 0;
        public bool IsStatic => MethodData.IsStatic;
        public bool HasDescription => !string.IsNullOrWhiteSpace(CommandDescription);
        public int ParamCount => ParamTypes.Length - _defaultParameters.Length;

        public Type[] MakeGenericArguments(params Type[] genericTypeArguments)
        {
            if (genericTypeArguments.Length != GenericParamTypes.Length)
            {
                throw new ArgumentException("Incorrect number of generic substitution types were supplied.");
            }

            Dictionary<string, Type> substitutionTable = new Dictionary<string, Type>();
            for (int i = 0; i < genericTypeArguments.Length; i++)
            {
                substitutionTable.Add(GenericParamTypes[i].Name, genericTypeArguments[i]);
            }

            Type[] types = new Type[ParamTypes.Length];
            for (int i = 0; i < types.Length; i++)
            {
                if (ParamTypes[i].ContainsGenericParameters)
                {
                    Type substitution = ConstructGenericType(ParamTypes[i], substitutionTable);
                    types[i] = substitution;
                }
                else
                {
                    types[i] = ParamTypes[i];
                }
            }

            return types;
        }

        private Type ConstructGenericType(Type genericType, Dictionary<string, Type> substitutionTable)
        {
            if (!genericType.ContainsGenericParameters) { return genericType; }
            if (substitutionTable.ContainsKey(genericType.Name)) { return substitutionTable[genericType.Name]; }
            if (genericType.IsArray) { return ConstructGenericType(genericType.GetElementType(), substitutionTable).MakeArrayType(); }
            if (genericType.IsGenericType)
            {
                Type baseType = genericType.GetGenericTypeDefinition();
                Type[] typeArguments = genericType.GetGenericArguments();
                for (int i = 0; i < typeArguments.Length; i++)
                {
                    typeArguments[i] = ConstructGenericType(typeArguments[i], substitutionTable);
                }

                return baseType.MakeGenericType(typeArguments);
            }

            throw new ArgumentException(ZString.Format("Could not construct the generic type {0}", genericType));
        }

        public object Invoke(object[] paramData, Type[] genericTypeArguments)
        {
            // 对于 MonoTargetType.Argument，需要把第一个参数作为调用目标。
            // 然后照常传递剩下的参数。
            int paramDataStart = 0;
            int paramDataLength = paramData.Length;
            if (MonoTarget == MonoTargetType.Argument || MonoTarget == MonoTargetType.ArgumentMulti)
            {
                paramDataStart++;
                paramDataLength--;
            }

            int numArguments = paramDataLength + _defaultParameters.Length;
            object[] arguments = new object[numArguments];

            // 复制传入参数和默认参数，拼出最终要传给方法的参数列表。
            Array.Copy(paramData, paramDataStart, arguments, 0, paramDataLength);
            Array.Copy(_defaultParameters, 0, arguments, paramDataLength, _defaultParameters.Length);

            MethodInfo invokingMethod = GetInvokingMethod(genericTypeArguments);

            if (IsStatic)
            {
                return invokingMethod.Invoke(null, arguments);
            }

            // 对于 MonoTargetType.Argument，把第一个参数作为目标。
            // 否则按正常方式获取调用目标。
            IEnumerable<object> targets = MonoTarget switch
            {
                MonoTargetType.Argument => paramData[0].Yield(),
                MonoTargetType.ArgumentMulti => paramData[0] as IEnumerable<object>,
                _ => GetInvocationTargets(invokingMethod)
            };

            return InvocationTargetFactory.InvokeOnTargets(invokingMethod, targets, arguments);
        }

        protected virtual IEnumerable<object> GetInvocationTargets(MethodInfo invokingMethod)
        {
            return InvocationTargetFactory.FindTargets(invokingMethod.DeclaringType, MonoTarget);
        }

        private MethodInfo GetInvokingMethod(Type[] genericTypeArguments)
        {
            if (!IsGeneric)
            {
                return MethodData;
            }

            T WrapConstruction<T>(Func<T> f)
            {
                try
                {
                    return f();
                }
                catch (ArgumentException)
                {
                    throw new ArgumentException(ZString.Format("Supplied generic parameters did not satisfy the generic constraints imposed by '{0}'", CommandName));
                }
            }

            Type declaringType = MethodData.DeclaringType;
            MethodInfo method = MethodData;

            if (declaringType.IsGenericTypeDefinition)
            {
                int typeCount = declaringType.GetGenericArguments().Length;

                Type[] genericTypes = genericTypeArguments
                    .Take(typeCount)
                    .ToArray();

                genericTypeArguments = genericTypeArguments
                    .Skip(typeCount)
                    .ToArray();

                declaringType = WrapConstruction(() => declaringType.MakeGenericType(genericTypes));
                method = method.RebaseMethod(declaringType);
            }

            return genericTypeArguments.Length == 0
                ? method
                : WrapConstruction(() => method.MakeGenericMethod(genericTypeArguments));
        }

        private string BuildPrefix(Type declaringType)
        {
            List<string> prefixes = new List<string>();
            Assembly assembly = declaringType.Assembly;

            void AddPrefixes(IEnumerable<CommandPrefixAttribute> prefixAttributes, string defaultName)
            {
                foreach (CommandPrefixAttribute prefixAttribute in prefixAttributes.Reverse())
                {
                    if (prefixAttribute.Valid)
                    {
                        string prefix = prefixAttribute.Prefix;
                        if (string.IsNullOrWhiteSpace(prefix)) { prefix = defaultName; }

                        prefixes.Add(prefix);
                    }
                }
            }

            while (declaringType != null)
            {
                IEnumerable<CommandPrefixAttribute> typePrefixes = declaringType.GetCustomAttributes<CommandPrefixAttribute>();
                AddPrefixes(typePrefixes, declaringType.Name);

                declaringType = declaringType.DeclaringType;
            }

            IEnumerable<CommandPrefixAttribute> assemblyPrefixes = assembly.GetCustomAttributes<CommandPrefixAttribute>();
            AddPrefixes(assemblyPrefixes, assembly.GetName().Name);

            return ZString.Join("", prefixes.Reversed());
        }

        private string BuildGenericSignature(Type[] genericParamTypes)
        {
            if (genericParamTypes.Length == 0)
            {
                return string.Empty;
            }

            IEnumerable<string> names = genericParamTypes.Select(x => x.Name);
            return ZString.Format("<{0}>", ZString.Join(", ", names));
        }

        private string BuildParameterSignature(ParameterInfo[] methodParams, int defaultParameterCount)
        {
            Utf16ValueStringBuilder signature = ZString.CreateStringBuilder();
            for (int i = 0; i < methodParams.Length - defaultParameterCount; i++)
            {
                if (i != 0) { signature.Append(' '); }
                signature.Append(methodParams[i].Name);
            }

            return signature.ToStringAndDispose();
        }

        private Type[] BuildGenericParamTypes(MethodInfo method, Type declaringType)
        {
            List<Type> types = new List<Type>();

            if (declaringType.IsGenericTypeDefinition)
            {
                types.AddRange(declaringType.GetGenericArguments());
            }

            if (method.IsGenericMethodDefinition)
            {
                types.AddRange(method.GetGenericArguments());
            }

            return types.ToArray();
        }

        public CommandData(MethodInfo methodData, string commandName, MonoTargetType monoTarget, int defaultParameterCount = 0)
        {
            CommandName = commandName;
            MethodData = methodData;
            MonoTarget = monoTarget;

            if (string.IsNullOrWhiteSpace(commandName))
            {
                CommandName = methodData.Name;
            }

            Type declaringType = methodData.DeclaringType;

            string prefix = BuildPrefix(declaringType);
            CommandName = ZString.Concat(prefix, CommandName);

            // 需要时添加一个占位参数，用来解析调用目标。
            List<ParameterInfo> parameters = methodData.GetParameters().ToList();
            if (MonoTarget == MonoTargetType.Argument)
            {
                parameters.Insert(0, new DummyParameter(methodData.DeclaringType, "target", 0));
            }
            else if (MonoTarget == MonoTargetType.ArgumentMulti)
            {
                parameters.Insert(0, new DummyParameter(methodData.DeclaringType.MakeArrayType(), "targets", 0));
            }

            MethodParamData = parameters.ToArray();
            ParamTypes = MethodParamData
                .Select(x => x.ParameterType)
                .ToArray();

            _defaultParameters = new object[defaultParameterCount];
            for (int i = 0; i < defaultParameterCount; i++)
            {
                int j = MethodParamData.Length - defaultParameterCount + i;
                _defaultParameters[i] = MethodParamData[j].DefaultValue;
            }

            GenericParamTypes = BuildGenericParamTypes(methodData, declaringType);

            ParameterSignature = BuildParameterSignature(MethodParamData, defaultParameterCount);
            GenericSignature = BuildGenericSignature(GenericParamTypes);
            CommandSignature = ParamCount > 0
                ? ZString.Format("{0}{1} {2}", CommandName, GenericSignature, ParameterSignature)
                : ZString.Concat(CommandName, GenericSignature);
        }

        public CommandData(MethodInfo methodData, MonoTargetType monoTarget, int defaultParameterCount = 0)
            : this(methodData, methodData.Name, monoTarget, defaultParameterCount)
        { }

        public CommandData(MethodInfo methodData, CommandAttribute commandAttribute, int defaultParameterCount = 0)
            : this(methodData, commandAttribute.Alias, commandAttribute.MonoTarget, defaultParameterCount)
        {
            CommandDescription = commandAttribute.Description;
        }

        public CommandData(MethodInfo methodData, CommandAttribute commandAttribute, CommandDescriptionAttribute descriptionAttribute, int defaultParameterCount = 0)
            : this(methodData, commandAttribute, defaultParameterCount)
        {
            if ((descriptionAttribute?.Valid ?? false) && string.IsNullOrWhiteSpace(commandAttribute.Description))
            {
                CommandDescription = descriptionAttribute.Description;
            }
        }
    }
}
