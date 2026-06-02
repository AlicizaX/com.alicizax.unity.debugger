using AlicizaX.Console.Utilities;
using Cysharp.Text;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AlicizaX.Console
{
    /// <summary>
    /// 处理控制台输入值的解析。
    /// </summary>
    public class AlicizaXConsoleParser
    {
        private readonly IAlicizaXConsoleParser[] _parsers;
        private readonly IAlicizaXConsoleGrammarConstruct[] _grammarConstructs;
        private readonly ConcurrentDictionary<Type, IAlicizaXConsoleParser> _parserLookup = new ConcurrentDictionary<Type, IAlicizaXConsoleParser>();
        private readonly HashSet<Type> _unparseableLookup = new HashSet<Type>();

        private readonly Func<string, Type, object> _recursiveParser;

        /// <summary>
        /// 用自定义解析器集合创建 AlicizaXConsole Parser。
        /// </summary>
        /// <param name="parsers">此 AlicizaXConsole Parser 要使用的 IAlicizaXConsoleParser。</param>
        /// <param name="grammarConstructs">此 AlicizaXConsole Parser 要使用的 IAlicizaXConsoleGrammarConstruct。</param>
        public AlicizaXConsoleParser(IEnumerable<IAlicizaXConsoleParser> parsers, IEnumerable<IAlicizaXConsoleGrammarConstruct> grammarConstructs)
        {
            _recursiveParser = Parse;

            _parsers = parsers.OrderByDescending(x => x.Priority)
                              .ToArray();

            _grammarConstructs = grammarConstructs.OrderBy(x => x.Precedence)
                                                  .ToArray();
        }

        /// <summary>
        /// 用默认注入的解析器创建 AlicizaXConsole Parser。
        /// </summary>
        public AlicizaXConsoleParser() : this(new InjectionLoader<IAlicizaXConsoleParser>().GetInjectedInstances(), new InjectionLoader<IAlicizaXConsoleGrammarConstruct>().GetInjectedInstances())
        {

        }

        public IAlicizaXConsoleParser GetParser(Type type)
        {
            if (_parserLookup.ContainsKey(type))
            {
                return _parserLookup[type];
            }
            else if (!_unparseableLookup.Contains(type))
            {
                foreach (IAlicizaXConsoleParser parser in _parsers)
                {
                    try
                    {
                        if (parser.CanParse(type))
                        {
                            return _parserLookup[type] = parser;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(ZString.Format("{0}.CanParse is malformed and throws", parser.GetType().GetDisplayName()));
                        Debug.LogException(e);
                    }
                }

                _unparseableLookup.Add(type);
            }

            return null;
        }

        public bool CanParse(Type type)
        {
            return GetParser(type) != null;
        }

        private IAlicizaXConsoleGrammarConstruct GetMatchingGrammar(string value, Type type)
        {
            foreach (IAlicizaXConsoleGrammarConstruct grammar in _grammarConstructs)
            {
                try
                {
                    if (grammar.Match(value, type))
                    {
                        return grammar;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(ZString.Format("{0}.Match is malformed and throws", grammar.GetType().GetDisplayName()));
                    Debug.LogException(e);
                }
            }

            return null;
        }

        /// <summary>
        /// 解析序列化后的字符串数据。
        /// </summary>
        /// <typeparam name="T">要解析值的类型。</typeparam>
        /// <param name="value">要解析的字符串。</param>
        /// <returns>解析后的值。</returns>
        public T Parse<T>(string value)
        {
            return (T)Parse(value, typeof(T));
        }

        /// <summary>
        /// 解析序列化后的字符串数据。
        /// </summary>
        /// <param name="value">要解析的字符串。</param>
        /// <param name="type">要解析值的类型。</param>
        /// <returns>解析后的值。</returns>
        public object Parse(string value, Type type)
        {
            value = value.ReduceScope('(', ')');

            if (type.IsClass && value == "null")
            {
                return null;
            }

            IAlicizaXConsoleGrammarConstruct grammar = GetMatchingGrammar(value, type);
            if (grammar != null)
            {
                try
                {
                    return grammar.Parse(value, type, _recursiveParser);
                }
                catch (ParserException) { throw; }
                catch (Exception e)
                {
                    throw new Exception(ZString.Format("Parsing of {0} via {1} failed:\n{2}", type.GetDisplayName(), grammar, e.Message), e);
                }
            }

            IAlicizaXConsoleParser parser = GetParser(type);
            if (parser == null)
            {
                throw new ArgumentException(ZString.Format("Cannot parse object of type '{0}'", type.GetDisplayName()));
            }

            try
            {
                return parser.Parse(value, type, _recursiveParser);
            }
            catch (ParserException) { throw; }
            catch (Exception e)
            {
                throw new Exception(ZString.Format("Parsing of {0} via {1} failed:\n{2}", type.GetDisplayName(), parser, e.Message), e);
            }
        }


        #region Type Parser
        private static readonly Dictionary<Type, string> _typeDisplayNames = new Dictionary<Type, string>
        {
            { typeof(int), "int" }, { typeof(float), "float" }, { typeof(decimal), "decimal" },
            { typeof(double), "double" }, { typeof(string), "string" }, { typeof(bool), "bool" },
            { typeof(byte), "byte" }, { typeof(sbyte), "sbyte" }, { typeof(uint), "uint" },
            { typeof(short), "short" }, { typeof(ushort), "ushort" }, { typeof(long), "long" },
            { typeof(ulong), "ulong" }, { typeof(char), "char" }, { typeof(object), "object" }
        };

        private static readonly Dictionary<string, Type> _reverseTypeDisplayNames = _typeDisplayNames.Invert();
        private static readonly Assembly[] _loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        private static readonly string[] _defaultNamespaces = new string[] { "System", "System.Collections", "System.Collections.Generic", "UnityEngine", "UnityEngine.UI", "AlicizaX.Debugger","AlicizaX.Console" };
        private static readonly List<string> _namespaceTable = new List<string>(_defaultNamespaces);

        private static readonly Regex _arrayTypeRegex = new Regex(@"^.*\[,*\]$");
        private static readonly Regex _genericTypeRegex = new Regex(@"^.+<.*>$");
        private static readonly Regex _tupleTypeRegex = new Regex(@"^\(.*\)$");
        private static readonly Regex _nullableTypeRegex = new Regex(@"^.*\?$");

        /// <summary>
        /// 把命名空间表恢复到初始状态。
        /// </summary>
        [Command("reset-namespaces", "Resets the namespace table to its initial state")]
        public static void ResetNamespaceTable()
        {
            _namespaceTable.Clear();
            _namespaceTable.AddRange(_defaultNamespaces);
        }

        /// <summary>
        /// 把命名空间加入表中，后续解析类型时可以使用。
        /// </summary>
        [Command("use-namespace", "Adds a namespace to the table so that it can be used to type resolution")]
        public static void AddNamespace(string namespaceName)
        {
            if (!_namespaceTable.Contains(namespaceName))
            {
                _namespaceTable.Add(namespaceName);
            }
        }

        /// <summary>
        /// 从表中移除命名空间，使类型解析不再使用它。
        /// </summary>
        [Command("remove-namespace", "Removes a namespace from the table")]
        public static void RemoveNamespace(string namespaceName)
        {
            if (_namespaceTable.Contains(namespaceName))
            {
                _namespaceTable.Remove(namespaceName);
            }
            else
            {
                throw new ArgumentException(ZString.Format("No namespace named {0} was present in the table", namespaceName));
            }
        }

        [Command("all-namespaces", "Displays all of the namespaces currently in use by the namespace table")]
        private static string ShowNamespaces()
        {
            _namespaceTable.Sort();
            if (_namespaceTable.Count == 0) { return "Namespace table is empty"; }
            else { return ZString.Join("\n", _namespaceTable); }
        }

        /// <summary>
        /// 返回命名空间表的一份副本。
        /// </summary>
        public static IEnumerable<string> GetAllNamespaces() { return _namespaceTable; }

        /// <summary>
        /// 解析并推断字符串指定的类型。
        /// </summary>
        /// <returns>解析后的类型。</returns>
        /// <param name="typeName">要解析的类型。</param>
        public static Type ParseType(string typeName)
        {
            typeName = typeName.Trim();

            if (_reverseTypeDisplayNames.ContainsKey(typeName))
            {
                return _reverseTypeDisplayNames[typeName];
            }

            if (_tupleTypeRegex.IsMatch(typeName))
            {
                return ParseTupleType(typeName);
            }

            if (_arrayTypeRegex.IsMatch(typeName))
            {
                return ParseArrayType(typeName);
            }

            if (_genericTypeRegex.IsMatch(typeName))
            {
                return ParseGenericType(typeName);
            }

            if (_nullableTypeRegex.IsMatch(typeName))
            {
                return ParseNullableType(typeName);
            }

            if (typeName.Contains('`'))
            {
                string genericName = typeName.Split('`')[0];
                if (_reverseTypeDisplayNames.ContainsKey(genericName))
                {
                    return _reverseTypeDisplayNames[genericName];
                }
            }

            return ParseTypeBaseCase(typeName);
        }

        private static Type ParseArrayType(string typeName)
        {
            int arrayPos = typeName.LastIndexOf('[');
            int arrayRank = typeName.CountFromIndex(',', arrayPos) + 1;
            Type elementType = ParseType(typeName.Substring(0, arrayPos));

            return arrayRank > 1
                ? elementType.MakeArrayType(arrayRank)
                : elementType.MakeArrayType();
        }

        private static Type ParseGenericType(string typeName)
        {
            string[] parts = typeName.Split(new[] { '<' }, 2);
            string[] genericArgNames = ZString.Concat('<', parts[1]).ReduceScope('<', '>').SplitScoped(',');
            string incompleteGenericName = ZString.Format("{0}`{1}", parts[0], Math.Max(1, genericArgNames.Length));

            Type incompleteGenericType = ParseType(incompleteGenericName);
            if (genericArgNames.All(string.IsNullOrWhiteSpace))
            {
                return incompleteGenericType;
            }

            Type[] genericArgs = genericArgNames.Select(ParseType).ToArray();

            return incompleteGenericType.MakeGenericType(genericArgs);
        }

        private static Type ParseNullableType(string typeName)
        {
            string innerTypeName = typeName.Substring(0, typeName.Length - 1);
            Type innerType = ParseType(innerTypeName);

            return innerType.IsClass
                ? innerType
                : typeof(Nullable<>).MakeGenericType(innerType);
        }

        private static Type ParseTupleType(string typeName)
        {
            string inner = typeName.Substring(1, typeName.Length - 2);

            Type[] parts = inner
                .SplitScoped(',')
                .Select(ParseType)
                .ToArray();

            return CreateTupleType(parts);
        }

        private static readonly Type[] _valueTupleTypes =
        {
            typeof(ValueTuple<>),
            typeof(ValueTuple<,>),
            typeof(ValueTuple<,,>),
            typeof(ValueTuple<,,,>),
            typeof(ValueTuple<,,,,>),
            typeof(ValueTuple<,,,,,>),
            typeof(ValueTuple<,,,,,,>),
            typeof(ValueTuple<,,,,,,,>)
        };

        private static Type CreateTupleType(Type[] types)
        {
            const int maxFlatTupleSize = 8;

            if (types.Length > maxFlatTupleSize - 1)
            {
                Type[] innerTypes = types.Skip(maxFlatTupleSize - 1).ToArray();
                types = types
                    .Take(maxFlatTupleSize - 1)
                    .Concat(CreateTupleType(innerTypes).Yield())
                    .ToArray();
            }

            return _valueTupleTypes[types.Length - 1].MakeGenericType(types);
        }

        private static Type ParseTypeBaseCase(string typeName)
        {
            return GetTypeFromAssemblies(typeName, _loadedAssemblies, false, false)
                ?? GetTypeFromAssemblies(typeName, _namespaceTable, _loadedAssemblies, false, false)
                ?? GetTypeFromAssemblies(typeName, _loadedAssemblies, false, true)
                ?? GetTypeFromAssemblies(typeName, _namespaceTable, _loadedAssemblies, true, true);
        }

        private static Type GetTypeFromAssemblies(string typeName, IEnumerable<string> namespaces, IEnumerable<Assembly> assemblies, bool throwOnError, bool ignoreCase)
        {
            foreach (string namespaceName in namespaces)
            {
                Type type = GetTypeFromAssemblies(ZString.Format("{0}.{1}", namespaceName, typeName), assemblies, false, ignoreCase);
                if (type != null) { return type; }
            }

            if (throwOnError)
            {
                throw new TypeLoadException(ZString.Format("No type of name '{0}' could be found in the specified assemblies and namespaces.", typeName));
            }

            return null;
        }

        private static Type GetTypeFromAssemblies(string typeName, IEnumerable<Assembly> assemblies, bool throwOnError, bool ignoreCase)
        {
            foreach (Assembly assembly in assemblies)
            {
                Type type = Type.GetType(ZString.Format("{0}, {1}", typeName, assembly.FullName), false, ignoreCase);
                if (type != null) { return type; }
            }

            if (throwOnError)
            {
                throw new TypeLoadException(ZString.Format("No type of name '{0}' could be found in the specified assemblies.", typeName));
            }

            return null;
        }
        #endregion

    }
}
