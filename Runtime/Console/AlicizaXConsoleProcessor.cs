#if UNITY_EDITOR || !UNITY_WEBGL
#define THREADS_SUPPORTED
#endif

using AlicizaX.Console.Internal;
using AlicizaX.Console.Utilities;
using Cysharp.Text;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace AlicizaX.Console
{
    public enum LoggingLevel
    {
        None = 0,
        Errors = 1,
        Warnings = 2,
        Full = 3
    }

    /// <summary>AlicizaX Console 的核心处理器，负责生成命令表并调用命令。</summary>
    public static partial class AlicizaXConsoleProcessor
    {
        /// <summary>AlicizaX Console Processor 运行时使用的日志等级。</summary>
        public static LoggingLevel loggingLevel = LoggingLevel.Full;

        private static readonly AlicizaXConsoleParser _parser = new AlicizaXConsoleParser();
        private static readonly AlicizaXConsolePreprocessor _preprocessor = new AlicizaXConsolePreprocessor();
        private static readonly AlicizaXConsoleScanRuleset _scanRuleset = new AlicizaXConsoleScanRuleset();
        private static readonly ConcurrentDictionary<string, CommandData> _commandTable = new ConcurrentDictionary<string, CommandData>();
        private static readonly List<CommandData> _commandCache = new List<CommandData>();

        public const string DefaultCommandAssemblyName = "AlicizaX.Debugger";
        public static readonly string[] DefaultCommandAssemblyNames = { DefaultCommandAssemblyName };

        public static bool TableGenerated { get; private set; }
        public static bool TableIsGenerating { get; private set; }

        [Command("command-count", "Gets the number of loaded commands")]
        public static int LoadedCommandCount => _loadedCommandCount;
        private static int _loadedCommandCount = 0;
        private static bool _commandCacheDirty = true;

        private static readonly Assembly[] _loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

        /// <summary>
        /// 获取所有已加载的命令。
        /// </summary>
        /// <returns>所有已加载的命令。</returns>
        public static IEnumerable<CommandData> GetAllCommands()
        {
            if (_commandCacheDirty)
            {
                lock (_commandCache)
                {
                    _commandCache.Clear();
                    _commandCache.AddRange(_commandTable.Values);
                    _commandCacheDirty = false;
                }
            }

            return _commandCache;
        }

        #region Table Generation
        /// <summary>
        /// 生成命令表，让命令可以被调用。
        /// </summary>
        /// <param name="deployThread">设为 <c>true</c> 时，会使用第二个线程生成命令表。</param>
        /// <param name="forceReload">设为 <c>true</c> 时，会清空并重新生成命令表。</param>
        public static void GenerateCommandTable(bool deployThread = false, bool forceReload = false)
        {
            GenerateCommandTable(_loadedAssemblies, deployThread, forceReload);
        }

        public static void GenerateCommandTableFromAssemblyNames(IEnumerable<string> assemblyNames, bool deployThread = false, bool forceReload = false)
        {
            Assembly[] assemblies = ResolveCommandAssemblies(assemblyNames);
            GenerateCommandTable(assemblies, deployThread, forceReload);
        }

        public static Assembly[] ResolveCommandAssemblies(IEnumerable<string> assemblyNames)
        {
            string[] names = assemblyNames?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct()
                .ToArray();

            if (names == null || names.Length == 0)
            {
                names = DefaultCommandAssemblyNames;
            }

            Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            List<Assembly> resolvedAssemblies = new List<Assembly>(names.Length);
            foreach (string assemblyName in names)
            {
                Assembly assembly = loadedAssemblies.FirstOrDefault(x => AssemblyNameMatches(x, assemblyName));
                if (assembly != null)
                {
                    resolvedAssemblies.Add(assembly);
                }
                else if (loggingLevel >= LoggingLevel.Warnings)
                {
                    Debug.LogWarning(ZString.Format("AlicizaXConsole Processor Warning: Command assembly '{0}' could not be found.", assemblyName));
                }
            }

            return resolvedAssemblies.Distinct().ToArray();
        }

        private static bool AssemblyNameMatches(Assembly assembly, string assemblyName)
        {
            return string.Equals(assembly.GetName().Name, assemblyName, StringComparison.Ordinal)
                || string.Equals(assembly.FullName, assemblyName, StringComparison.Ordinal);
        }

        public static void GenerateCommandTable(IEnumerable<Assembly> assemblies, bool deployThread = false, bool forceReload = false)
        {
            Assembly[] assemblyList = assemblies?
                .Where(x => x != null)
                .Distinct()
                .ToArray() ?? _loadedAssemblies;

#if THREADS_SUPPORTED
            if (deployThread)
            {
                ThreadPool.QueueUserWorkItem(state =>
                {
                    try
                    {
                        GenerateCommandTable(assemblyList, false, forceReload);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                });

                return;
            }
#endif

            lock (_commandTable)
            {
                if (!TableGenerated || forceReload)
                {
                    TableIsGenerating = true;
                    {
                        if (forceReload && TableGenerated)
                        {
                            _commandTable.Clear();
                            _loadedCommandCount = 0;
                        }

#if THREADS_SUPPORTED
                        Parallel.ForEach(assemblyList, LoadCommandsFromAssembly);
#else
                        foreach (Assembly assembly in assemblyList)
                        {
                            LoadCommandsFromAssembly(assembly);
                        }
#endif
                    }

                    TableIsGenerating = false;
                    TableGenerated = true;
                    GC.Collect(3, GCCollectionMode.Forced, false, true);
                }
            }
        }

        private static IEnumerable<(MethodInfo method, MemberInfo member)> ExtractCommandMethods(Type type)
        {
            const BindingFlags flags =
                  BindingFlags.Static
                | BindingFlags.Instance
                | BindingFlags.Public
                | BindingFlags.NonPublic
                | BindingFlags.DeclaredOnly;

            MethodInfo[] methods = type.GetMethods(flags);
            PropertyInfo[] properties = type.GetProperties(flags);
            FieldInfo[] fields = type.GetFields(flags);

            foreach (MethodInfo method in methods)
            {
                yield return (method, method);
            }

            foreach (PropertyInfo property in properties)
            {
                if (property.CanWrite)
                {
                    yield return (property.SetMethod, property);
                }
                if (property.CanRead)
                {
                    yield return (property.GetMethod, property);
                }
            }

            foreach (FieldInfo field in fields)
            {
                if (field.HasAttribute<CommandAttribute>())
                {
                    if (field.IsDelegate())
                    {
                        if (field.IsStrongDelegate())
                        {
                            FieldDelegateMethod executer = new FieldDelegateMethod(field);
                            yield return (executer, field);
                        }
                        else if (loggingLevel >= LoggingLevel.Warnings)
                        {
                            Debug.LogWarning(ZString.Format("AlicizaXConsole Processor Warning: Could not add '{0}' from {1} to the table as it is an invalid delegate type.", field.Name, field.DeclaringType));
                        }
                    }
                    else
                    {
                        FieldAutoMethod reader = new FieldAutoMethod(field, FieldAutoMethod.AccessType.Read);
                        yield return (reader, field);

                        if (!(field.IsLiteral || field.IsInitOnly))
                        {
                            FieldAutoMethod writer = new FieldAutoMethod(field, FieldAutoMethod.AccessType.Write);
                            yield return (writer, field);
                        }
                    }
                }
            }
        }

        private static bool GetCommandSupported(CommandData command, out string unsupportedReason)
        {
            for (int i = 0; i < command.ParamCount; i++)
            {
                ParameterInfo param = command.MethodParamData[i];
                Type paramType = param.ParameterType;

                if (!_parser.CanParse(paramType) && !paramType.IsGenericParameter)
                {
                    unsupportedReason = ZString.Format("Parameter type {0} is not supported by the AlicizaXConsole Parser.", paramType);
                    return false;
                }
            }

            if (command.MonoTarget != MonoTargetType.Registry
                && !command.MethodData.IsStatic
                && !command.MethodData.DeclaringType.IsDerivedTypeOf(typeof(MonoBehaviour)))
            {
                unsupportedReason = ZString.Format("Non static non MonoBehaviour commands are incompatible with MonoTargetType.{0}.", command.MonoTarget);
                return false;
            }

            unsupportedReason = string.Empty;
            return true;
        }

        public static void LoadCommandsFromAssembly(Assembly assembly)
        {
            if (!_scanRuleset.ShouldScan(assembly))
            {
                return;
            }

            Type[] loadedTypes = assembly.GetTypes();
            foreach (Type type in loadedTypes)
            {
                try
                {
                    LoadCommandsFromType(type);
                }
                catch (TypeLoadException)
                {
                    // 此问题还在排查中。

                    /*
                    保留这段注释，用于说明这里原本有调试日志，现在不主动输出。



                    */
                }
                catch (BadImageFormatException)
                {
                    // 已确认这是 Unity/Mono 侧的问题。
                    // 这种情况几乎不会出现在用户代码里，所以这里静默忽略。
                    // AlicizaXConsole 问题：AlicizaX Console 的问题跟踪记录。
                    // Unity 问题链接：https://issuetracker.unity3d.com/issues/badimageformatexception-is-thrown-when-calling-getcustomattributes-on-certain-memberinfo-instances
                    // Mono 问题链接：https://github.com/mono/mono/issues/17278

                    /*
                    保留这段注释，用于说明这里原本有调试日志，现在不主动输出。



                    */
                }
            }
        }

        private static void LoadCommandsFromType(Type type)
        {
            if (!_scanRuleset.ShouldScan(type))
            {
                return;
            }

            foreach ((MethodInfo method, MemberInfo member) in ExtractCommandMethods(type))
            {
                if (member.DeclaringType == type)
                {
                    LoadCommandsFromMember(member, method);
                }
            }
        }

        private static void LoadCommandsFromMember(MemberInfo member, MethodInfo method)
        {
            if (!_scanRuleset.ShouldScan(member))
            {
                return;
            }

            IEnumerable<CommandAttribute> commandAttributes = member.GetCustomAttributes<CommandAttribute>();
            CommandDescriptionAttribute descriptionAttribute = member.GetCustomAttribute<CommandDescriptionAttribute>();

            foreach (CommandAttribute commandAttribute in commandAttributes)
            {
                if (!commandAttribute.Valid)
                {
                    if (loggingLevel >= LoggingLevel.Warnings)
                    {
                        Debug.LogWarning(ZString.Format("AlicizaXConsole Processor Warning: Could not add '{0}' to the table as it is invalid.", commandAttribute.Alias));
                    }
                }
                else
                {
                    CommandPlatformAttribute platformAttribute = member.GetCustomAttribute<CommandPlatformAttribute>();
                    Platform commandPlatforms = platformAttribute?.SupportedPlatforms ?? commandAttribute.SupportedPlatforms;
                    if (commandPlatforms.HasFlag(Application.platform.ToPlatform()))
                    {
                        IEnumerable<CommandData> newCommands = CreateCommandOverloads(method, commandAttribute, descriptionAttribute);
                        foreach (CommandData command in newCommands)
                        {
                            TryAddCommand(command);
                        }
                    }
                }
            }
        }

        private static IEnumerable<CommandData> CreateCommandOverloads(MethodInfo method, CommandAttribute commandAttribute, CommandDescriptionAttribute descriptionAttribute)
        {
            int defaultParameters = method.GetParameters().Count(x => x.HasDefaultValue);
            for (int i = 0; i < defaultParameters + 1; i++)
            {
                CommandData command = new CommandData(method, commandAttribute, descriptionAttribute, i);
                yield return command;
            }
        }

        private static string GenerateCommandKey(CommandData command)
        {
            return ZString.Format("{0}({1})", command.CommandName, command.ParamCount);
        }

        /// <summary>
        /// 注册新命令。
        /// </summary>
        /// <param name="command">要注册的命令。</param>
        /// <returns>是否添加成功。</returns>
        public static bool TryAddCommand(CommandData command)
        {
            if (!GetCommandSupported(command, out string reason))
            {
                if (loggingLevel >= LoggingLevel.Warnings)
                {
                    Debug.LogWarning(ZString.Format("AlicizaXConsole Processor Warning: Could not add '{0}' from {1} to the table as it is not supported. {2}",
                        command.CommandSignature,
                        command.MethodData.DeclaringType.GetDisplayName(),
                        reason));
                }

                return false;
            }

            string key = GenerateCommandKey(command);
            bool alreadyExists = !_commandTable.TryAdd(key, command);

            if (alreadyExists)
            {
                if (loggingLevel >= LoggingLevel.Warnings)
                {
                    string fullMethodName = ZString.Format("{0}.{1}", command.MethodData.DeclaringType.FullName, command.MethodData.Name);
                    Debug.LogWarning(ZString.Format("AlicizaXConsole Processor Warning: Could not add {0} to the table as another method with the same alias and parameter count, {1}, already exists.", fullMethodName, key));
                }

                return false;
            }

            _commandCacheDirty = true;
            Interlocked.Increment(ref _loadedCommandCount);
            return true;
        }

        /// <summary>
        /// 移除已有命令。
        /// </summary>
        /// <param name="command">要移除的命令。</param>
        /// <returns>是否移除成功。</returns>
        public static bool TryRemoveCommand(CommandData command)
        {
            string key = GenerateCommandKey(command);
            if (_commandTable.TryRemove(key, out _))
            {
                _commandCacheDirty = true;
                Interlocked.Decrement(ref _loadedCommandCount);
                return true;
            }

            return false;
        }
        #endregion

        #region Command Invocation
        /// <summary>在 AlicizaXConsoleProcessor 上调用命令。</summary>
        /// <returns>调用后的返回值。</returns>
        /// <param name="commandString">要调用的命令。</param>
        public static object InvokeCommand(string commandString)
        {
            GenerateCommandTable();

            commandString = commandString.Trim();
            commandString = _preprocessor.Process(commandString);

            if (string.IsNullOrWhiteSpace(commandString)) { throw new ArgumentException("Cannot parse an empty string."); }
            string[] commandParts = commandString.SplitScoped(' ');
            commandParts = commandParts.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

            string commandName = commandParts[0];
            string[] commandParams = commandParts.SubArray(1, commandParts.Length - 1);
            int paramCount = commandParams.Length;

            string[] commandNameParts = commandName.Split(new[] { '<' }, 2);
            string genericSignature = commandNameParts.Length > 1 ? ZString.Concat('<', commandNameParts[1]) : "";
            commandName = commandNameParts[0];

            string keyName = ZString.Format("{0}({1})", commandName, paramCount);
            if (!_commandTable.ContainsKey(keyName))
            {
                string overloadPrefix = ZString.Concat(commandName, '(');
                bool overloadExists = _commandTable.Keys.Any(key => key.Contains(overloadPrefix) && _commandTable[key].CommandName == commandName);
                if (overloadExists) { throw new ArgumentException(ZString.Format("No overload of '{0}' with {1} parameters could be found.", commandName, paramCount)); }
                else { throw new ArgumentException(ZString.Format("Command '{0}' could not be found.", commandName)); }
            }
            CommandData command = _commandTable[keyName];

            Type[] genericTypes = Array.Empty<Type>();
            if (command.IsGeneric)
            {
                int expectedArgCount = command.GenericParamTypes.Length;
                string[] genericArgNames = genericSignature.ReduceScope('<', '>').SplitScoped(',');
                if (genericArgNames.Length == expectedArgCount)
                {
                    genericTypes = new Type[genericArgNames.Length];
                    for (int i = 0; i < genericTypes.Length; i++)
                    {
                        genericTypes[i] = AlicizaXConsoleParser.ParseType(genericArgNames[i]);
                    }
                }
                else
                {
                    throw new ArgumentException(ZString.Format("Generic command '{0}' requires {1} generic parameter{2} but was supplied with {3}.",
                        commandName,
                        expectedArgCount,
                        expectedArgCount == 1 ? "" : "s",
                        genericArgNames.Length));
                }
            }
            else if (genericSignature != string.Empty)
            {
                throw new ArgumentException(ZString.Format("Command '{0}' is not a generic command and cannot be invoked as such.", commandName));
            }

#if !UNITY_EDITOR && ENABLE_IL2CPP && !UNITY_2022_2_OR_NEWER
            if (genericTypes.Any((Type x) => x.IsValueType))
            {
                throw new NotSupportedException("Value types in generic commands are not supported in IL2CPP before Unity 2022.2");
            }
#endif

            object[] parsedCommandParams = ParseParamData(command.MakeGenericArguments(genericTypes), commandParams);
            return command.Invoke(parsedCommandParams, genericTypes);
        }

        private static object[] ParseParamData(Type[] paramTypes, string[] paramData)
        {
            object[] parsedData = new object[paramData.Length];
            for (int i = 0; i < parsedData.Length; i++)
            {
                parsedData[i] = _parser.Parse(paramData[i], paramTypes[i]);
            }

            return parsedData;
        }
        #endregion
    }
}
