#if !AlicizaXConsole_DISABLED && !AlicizaXConsole_DISABLE_BUILTIN_ALL && !AlicizaXConsole_DISABLE_BUILTIN_EXTRA
using AlicizaX.Console.Utilities;
using Cysharp.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AlicizaX.Console.Extras
{
    public static class MegaCommands
    {
        private static readonly AlicizaXConsoleSerializer Serializer = new AlicizaXConsoleSerializer();
        private static readonly AlicizaXConsoleParser Parser = new AlicizaXConsoleParser();

        private static MethodInfo[] ExtractMethods(Type type, string name)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod |
                                       BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

            MethodInfo[] methods = type.GetMethods(flags).Where(x => x.Name == name).ToArray();
            if (!methods.Any())
            {
                PropertyInfo property = type.GetProperty(name, flags);
                if (property != null)
                {
                    methods = new[] {property.GetMethod, property.SetMethod}.Where(x => x != null).ToArray();
                    if (methods.Length > 0)
                    {
                        return methods;
                    }
                }

                throw new ArgumentException(ZString.Format("No method or property named {0} could be found in class {1}", name, Serializer.SerializeFormatted(type)));
            }

            return methods;
        }

        private static string GenerateSignature(MethodInfo method)
        {
            IEnumerable<string> paramParts = method.GetParameters()
                .Select(x => (x.Name, x.ParameterType))
                .Select(x => ZString.Format("{0} {1}", x.ParameterType.GetDisplayName(), x.Name));

            string paramSignature = ZString.Join(", ", paramParts);
            return ZString.Format("{0}({1})", method.Name, paramSignature);
        }

        private static MethodInfo GetIdealOverload(MethodInfo[] methods, bool isStatic, int argc)
        {
            methods = methods.Where(x => x.IsStatic == isStatic).ToArray();

            if (methods.Length == 0)
            {
                throw new ArgumentException(ZString.Format("No {0} overloads could be found.", isStatic ? "static" : "non-static"));
            }

            if (methods.Length == 1)
            {
                return methods[0];
            }

            methods = methods.Where(x => !x.IsGenericMethod).ToArray();
            if (methods.Length == 0)
            {
                throw new ArgumentException("Generic methods are not supported.");
            }

            MethodInfo[] argcMatches = methods.Where(x => x.GetParameters().Length == argc).ToArray();
            if (argcMatches.Length == 1)
            {
                return argcMatches[0];
            }
            else if (argcMatches.Length == 0)
            {
                IEnumerable<string> signatures = methods.Select(GenerateSignature);
                string combinedSignatures = ZString.Join("\n", signatures);
                throw new ArgumentException(ZString.Format("No overloads with {0} arguments were found. the following overloads are available:\n{1}", argc, combinedSignatures));
            }
            else
            {
                IEnumerable<string> signatures = argcMatches.Select(GenerateSignature);
                string combinedSignatures = ZString.Join("\n", signatures);
                throw new ArgumentException(ZString.Format("Multiple overloads with the same argument count were found: please specify the types explicitly.\n{0}", combinedSignatures));
            }
        }

        private static MethodInfo GetIdealOverload(MethodInfo[] methods, bool isStatic, Type[] argTypes)
        {
            // 精确匹配。
            foreach (MethodInfo method in methods)
            {
                if (method.IsStatic == isStatic)
                {
                    IEnumerable<Type> methodParamTypes = method.GetParameters().Select(x => x.ParameterType);
                    if (methodParamTypes.SequenceEqual(argTypes))
                    {
                        return method;
                    }
                }
            }

            // 多态匹配。
            foreach (MethodInfo method in methods)
            {
                if (method.IsStatic == isStatic)
                {
                    ParameterInfo[] methodParams = method.GetParameters();
                    if (methodParams.Length == argTypes.Length)
                    {
                        bool isMatch = methodParams
                            .Select(x => x.ParameterType)
                            .Zip(argTypes, (x, y) => (x, y))
                            .All(pair => pair.x.IsAssignableFrom(pair.y));

                        if (isMatch)
                        {
                            return method;
                        }
                    }
                }
            }

            throw new ArgumentException("No overload with the supplied argument types could be found.");
        }

        private static object[] CreateArgs(MethodInfo method, string[] rawArgs)
        {
            ParameterInfo[] methodParams = method.GetParameters();
            Type[] argTypes = methodParams.Select(x => x.ParameterType).ToArray();
            return CreateArgs(method, argTypes, rawArgs);
        }

        private static object[] CreateArgs(MethodInfo method, Type[] argTypes, string[] rawArgs)
        {
            ParameterInfo[] methodParams = method.GetParameters();
            int defaultArgs = methodParams.Count(x => x.HasDefaultValue);

            if (rawArgs.Length < argTypes.Length - defaultArgs || rawArgs.Length > argTypes.Length)
            {
                throw new ArgumentException(ZString.Format("Incorrect number ({0}) of arguments supplied for {1}.{2}, expected {3}",
                    rawArgs.Length,
                    Serializer.SerializeFormatted(method.DeclaringType),
                    method.Name,
                    argTypes.Length));
            }

            object[] parsedArgs = new object[argTypes.Length];
            for (int i = 0; i < parsedArgs.Length; i++)
            {
                if (i < rawArgs.Length)
                {
                    parsedArgs[i] = Parser.Parse(rawArgs[i], argTypes[i]);
                }
                else
                {
                    parsedArgs[i] = methodParams[i].DefaultValue;
                }
            }

            return parsedArgs;
        }

        private static object InvokeAndUnwrapException(this MethodInfo method, object[] args)
        {
            try
            {
                return method.Invoke(null, args);
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException;
            }
        }

        private static object InvokeAndUnwrapException(this MethodInfo method, IEnumerable<object> targets, object[] args)
        {
            try
            {
                return InvocationTargetFactory.InvokeOnTargets(method, targets, args);
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException;
            }
        }

        [Command("call-static")]
        private static object CallStatic(Type classType, string funcName)
        {
            return CallStatic(classType, funcName, Array.Empty<string>());
        }

        [Command("call-static")]
        private static object CallStatic(Type classType, string funcName, string[] args)
        {
            MethodInfo[] methods = ExtractMethods(classType, funcName);
            MethodInfo method = GetIdealOverload(methods, true, args.Length);

            object[] parsedArgs = CreateArgs(method, args);
            return method.InvokeAndUnwrapException(parsedArgs);
        }

        [Command("call-static")]
        [CommandDescription("调用静态方法或属性")]
        private static object CallStatic(
            [CommandParameterDescription("完整类型名")] Type classType,
            [CommandParameterDescription("方法或属性名")] string funcName,
            [CommandParameterDescription("调用参数")] string[] args,
            [CommandParameterDescription("参数类型")] Type[] argTypes)
        {
            MethodInfo[] methods = ExtractMethods(classType, funcName);
            MethodInfo method = GetIdealOverload(methods, true, argTypes);

            object[] parsedArgs = CreateArgs(method, argTypes, args);
            return method.InvokeAndUnwrapException(parsedArgs);
        }

        [Command("call-instance")]
        private static object CallInstance(Type classType, string funcName, MonoTargetType targetType)
        {
            return CallInstance(classType, funcName, targetType, Array.Empty<string>());
        }

        [Command("call-instance")]
        private static object CallInstance(Type classType, string funcName, MonoTargetType targetType, string[] args)
        {
            MethodInfo[] methods = ExtractMethods(classType, funcName);
            MethodInfo method = GetIdealOverload(methods, false, args.Length);

            object[] parsedArgs = CreateArgs(method, args);
            IEnumerable<object> targets = InvocationTargetFactory.FindTargets(classType, targetType);
            return method.InvokeAndUnwrapException(targets, parsedArgs);
        }

        [Command("call-instance")]
        [CommandDescription("调用实例方法或属性")]
        private static object CallInstance(
            [CommandParameterDescription("完整类型名")] Type classType,
            [CommandParameterDescription("方法或属性名")] string funcName,
            [CommandParameterDescription("目标实例类型")] MonoTargetType targetType,
            [CommandParameterDescription("调用参数")] string[] args,
            [CommandParameterDescription("参数类型")] Type[] argTypes)
        {
            MethodInfo[] methods = ExtractMethods(classType, funcName);
            MethodInfo method = GetIdealOverload(methods, false, argTypes);

            object[] parsedArgs = CreateArgs(method, argTypes, args);
            IEnumerable<object> targets = InvocationTargetFactory.FindTargets(classType, targetType);
            return method.InvokeAndUnwrapException(targets, parsedArgs);
        }
    }
}
#endif
