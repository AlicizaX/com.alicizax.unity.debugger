using System;
using System.Collections.Generic;
using System.Reflection;
using AlicizaX;

namespace AlicizaX.Debugger
{
    /// <summary>
    /// 启动时扫描程序集，找出可自动注册的调试器窗口。
    /// 只做"发现"，实例化与注册由 <see cref="DebuggerComponent"/> 负责。
    /// </summary>
    internal static class DebuggerWindowScanner
    {
        internal const string FallbackGroup = "Auto";

        internal readonly struct DiscoveredWindow
        {
            public DiscoveredWindow(string path, Type type)
            {
                Path = path;
                Type = type;
            }

            public string Path { get; }

            public Type Type { get; }
        }

        /// <summary>
        /// 扫描本包程序集与 <paramref name="extraAssemblyNames"/> 指定的程序集。
        /// </summary>
        /// <param name="extraAssemblyNames">额外要扫描的程序集名；为空时只扫本包。</param>
        /// <param name="results">扫描结果，调用方负责去重与注册。</param>
        internal static void Discover(IEnumerable<string> extraAssemblyNames, List<DiscoveredWindow> results)
        {
            results.Clear();

            List<Assembly> assemblies = ResolveAssemblies(extraAssemblyNames);
            HashSet<string> paths = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < assemblies.Count; i++)
            {
                Type[] types;
                try
                {
                    types = assemblies[i].GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    // 部分类型加载失败是常态（编辑器/热更程序集），能扫到多少算多少。
                    types = e.Types;
                }
                catch (Exception e)
                {
                    Log.Warning(Utility.Text.Format("Debugger: failed to scan assembly '{0}': {1}", assemblies[i].GetName().Name, e.Message));
                    continue;
                }

                for (int j = 0; j < types.Length; j++)
                {
                    DiscoverType(types[j], paths, results);
                }
            }

            results.Sort(CompareByPath);
        }

        private static void DiscoverType(Type type, HashSet<string> paths, List<DiscoveredWindow> results)
        {
            if (type == null || !type.IsClass || type.IsAbstract || type.ContainsGenericParameters)
            {
                return;
            }

            bool hasAttribute = type.IsDefined(typeof(DebuggerWindowAttribute), false);
            if (!typeof(IDebuggerWindow).IsAssignableFrom(type))
            {
                if (hasAttribute)
                {
                    Log.Warning(Utility.Text.Format("[DebuggerWindow] on '{0}' is ignored: the type does not implement IDebuggerWindow.", type.FullName));
                }

                return;
            }

            // 组本身也实现 IDebuggerWindow，不能当成可实例化的窗口。
            if (typeof(IDebuggerWindowGroup).IsAssignableFrom(type))
            {
                return;
            }

            // 组件内部窗口必须显式声明路径，否则会把内置窗口整体搬进 Auto/ 造成重复。
            if (!hasAttribute && type.DeclaringType == typeof(DebuggerComponent))
            {
                return;
            }

            DebuggerWindowAttribute attribute = type.GetCustomAttribute<DebuggerWindowAttribute>(false);
            string path = attribute != null && !string.IsNullOrWhiteSpace(attribute.Path)
                ? attribute.Path.Trim()
                : string.Concat(FallbackGroup, "/", type.Name);

            if (!paths.Add(path))
            {
                Log.Warning(Utility.Text.Format("Debugger: duplicate auto-register path '{0}' for '{1}', skipped.", path, type.FullName));
                return;
            }

            results.Add(new DiscoveredWindow(path, type));
        }

        private static List<Assembly> ResolveAssemblies(IEnumerable<string> extraAssemblyNames)
        {
            List<Assembly> assemblies = new List<Assembly>(4);
            Assembly self = typeof(DebuggerWindowScanner).Assembly;
            assemblies.Add(self);

            if (extraAssemblyNames == null)
            {
                return assemblies;
            }

            Assembly[] loaded = AppDomain.CurrentDomain.GetAssemblies();
            foreach (string rawName in extraAssemblyNames)
            {
                if (string.IsNullOrWhiteSpace(rawName))
                {
                    continue;
                }

                string name = rawName.Trim();
                Assembly resolved = null;
                for (int i = 0; i < loaded.Length; i++)
                {
                    if (AssemblyNameMatches(loaded[i], name))
                    {
                        resolved = loaded[i];
                        break;
                    }
                }

                if (resolved == null)
                {
                    Log.Warning(Utility.Text.Format("Debugger: auto-register assembly '{0}' was not found, skipped.", name));
                    continue;
                }

                if (!assemblies.Contains(resolved))
                {
                    assemblies.Add(resolved);
                }
            }

            return assemblies;
        }

        private static bool AssemblyNameMatches(Assembly assembly, string name)
        {
            return string.Equals(assembly.GetName().Name, name, StringComparison.Ordinal)
                || string.Equals(assembly.FullName, name, StringComparison.Ordinal);
        }

        private static int CompareByPath(DiscoveredWindow left, DiscoveredWindow right)
        {
            return string.CompareOrdinal(left.Path, right.Path);
        }
    }
}
