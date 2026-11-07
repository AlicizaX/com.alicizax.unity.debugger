using System;
using System.Collections.Generic;
using AlicizaX;
using UnityEngine.UIElements;

namespace AlicizaX.Debugger
{
    /// <summary>
    /// 调试器窗口路径树。
    /// 注册、按路径查找/选中、生命周期分发，以及侧栏渲染所需的子节点枚举都在这一个结构里，
    /// 不再有第二份菜单树。
    /// </summary>
    internal sealed class DebuggerWindowGroup : IDebuggerWindowGroup
    {
        internal readonly struct Entry
        {
            public Entry(string name, string path, IDebuggerWindow window)
            {
                Name = name;
                Path = path;
                Window = window;
            }

            /// <summary>单段名称，例如 "System"。</summary>
            public string Name { get; }

            /// <summary>完整路径，例如 "Information/System"。</summary>
            public string Path { get; }

            /// <summary>叶子窗口或子组；子组本身也实现 <see cref="IDebuggerWindow"/>。</summary>
            public IDebuggerWindow Window { get; }

            public bool IsGroup => Window is DebuggerWindowGroup;
        }

        private readonly List<Entry> _entries = new List<Entry>(8);
        private readonly Dictionary<IDebuggerWindow, string> _pathsByWindow = new Dictionary<IDebuggerWindow, string>();
        private int _selectedIndex;
        private string[] _names;

        /// <summary>子节点（组与窗口混合，保持注册顺序；排序由视图方负责）。</summary>
        internal IReadOnlyList<Entry> Entries => _entries;

        public int DebuggerWindowCount => _entries.Count;

        public int SelectedIndex
        {
            get => _selectedIndex;
            set => _selectedIndex = value;
        }

        public IDebuggerWindow SelectedWindow
        {
            get
            {
                if (_selectedIndex < 0 || _selectedIndex >= _entries.Count)
                {
                    return null;
                }

                return _entries[_selectedIndex].Window;
            }
        }

        public void Initialize(params object[] args)
        {
        }

        public void Shutdown()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                _entries[i].Window.Shutdown();
            }

            _entries.Clear();
            _pathsByWindow.Clear();
            _names = null;
            _selectedIndex = 0;
        }

        public void OnEnter()
        {
            SelectedWindow?.OnEnter();
        }

        public void OnLeave()
        {
            SelectedWindow?.OnLeave();
        }

        public void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            SelectedWindow?.OnUpdate(elapseSeconds, realElapseSeconds);
        }

        public VisualElement CreateView()
        {
            return null;
        }

        public string[] GetDebuggerWindowNames()
        {
            if (_names == null)
            {
                _names = new string[_entries.Count];
                for (int i = 0; i < _entries.Count; i++)
                {
                    _names[i] = _entries[i].Name;
                }
            }

            return _names;
        }

        public IDebuggerWindow GetDebuggerWindow(string path)
        {
            path = NormalizePath(path);
            return path.Length == 0 ? null : GetRecursive(path);
        }

        public void RegisterDebuggerWindow(string path, IDebuggerWindow debuggerWindow)
        {
            path = NormalizePath(path);
            if (path.Length == 0)
            {
                throw new GameFrameworkException("Path is invalid.");
            }

            if (debuggerWindow == null)
            {
                throw new GameFrameworkException("Debugger window is invalid.");
            }

            if (!TryRegister(path, debuggerWindow))
            {
                throw new GameFrameworkException("Debugger window has been registered.");
            }
        }

        /// <summary>
        /// 按路径注册窗口；路径已被占用时返回 false 而不是抛异常，
        /// 供自动发现路径做"告警并跳过"。
        /// </summary>
        internal bool TryRegister(string path, IDebuggerWindow debuggerWindow)
        {
            path = NormalizePath(path);
            if (path.Length == 0 || debuggerWindow == null)
            {
                return false;
            }

            return TryRegisterRecursive(string.Empty, path, debuggerWindow);
        }

        public bool SelectDebuggerWindow(string path)
        {
            path = NormalizePath(path);
            return path.Length != 0 && SelectRecursive(path);
        }

        public bool UnregisterDebuggerWindow(string path)
        {
            path = NormalizePath(path);
            return path.Length != 0 && UnregisterRecursive(path);
        }

        /// <summary>取窗口注册时的完整路径。</summary>
        internal bool TryGetPath(IDebuggerWindow window, out string path)
        {
            return _pathsByWindow.TryGetValue(window, out path);
        }

        /// <summary>收集所有叶子窗口路径（注销/销毁时用）。</summary>
        internal void CollectWindowPaths(List<string> results)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry entry = _entries[i];
                if (entry.Window is DebuggerWindowGroup group)
                {
                    group.CollectWindowPaths(results);
                }
                else
                {
                    results.Add(entry.Path);
                }
            }
        }

        private IDebuggerWindow GetRecursive(string path)
        {
            int pos = path.IndexOf('/');
            if (pos < 0)
            {
                int index = IndexOfEntry(path);
                return index < 0 ? null : _entries[index].Window;
            }

            if (!TryGetChildGroup(path.Substring(0, pos), out DebuggerWindowGroup group))
            {
                return null;
            }

            return group.GetRecursive(path.Substring(pos + 1));
        }

        private bool SelectRecursive(string path)
        {
            int pos = path.IndexOf('/');
            if (pos < 0)
            {
                int index = IndexOfEntry(path);
                if (index < 0)
                {
                    return false;
                }

                _selectedIndex = index;
                return true;
            }

            if (!TryGetChildGroup(path.Substring(0, pos), out DebuggerWindowGroup group))
            {
                return false;
            }

            _selectedIndex = IndexOfEntry(path.Substring(0, pos));
            return group.SelectRecursive(path.Substring(pos + 1));
        }

        private bool TryRegisterRecursive(string parentPath, string path, IDebuggerWindow window)
        {
            int pos = path.IndexOf('/');
            if (pos < 0)
            {
                if (IndexOfEntry(path) >= 0)
                {
                    return false;
                }

                string fullPath = CombinePath(parentPath, path);
                _entries.Add(new Entry(path, fullPath, window));
                _pathsByWindow[window] = fullPath;
                _names = null;
                return true;
            }

            string groupName = path.Substring(0, pos);
            string leftPath = path.Substring(pos + 1);

            int groupIndex = IndexOfEntry(groupName);
            DebuggerWindowGroup group;
            bool createdGroup = false;
            if (groupIndex >= 0)
            {
                group = _entries[groupIndex].Window as DebuggerWindowGroup;
                if (group == null)
                {
                    return false;
                }
            }
            else
            {
                group = new DebuggerWindowGroup();
                groupIndex = _entries.Count;
                _entries.Add(new Entry(groupName, CombinePath(parentPath, groupName), group));
                createdGroup = true;
                _names = null;
            }

            if (group.TryRegisterRecursive(CombinePath(parentPath, groupName), leftPath, window))
            {
                return true;
            }

            // 注册失败时把本次刚建出来的空组回滚掉，避免侧栏里留下点不动的空分组。
            if (createdGroup)
            {
                RemoveEntryAt(groupIndex);
            }

            return false;
        }

        private bool UnregisterRecursive(string path)
        {
            int pos = path.IndexOf('/');
            if (pos < 0)
            {
                int index = IndexOfEntry(path);
                if (index < 0)
                {
                    return false;
                }

                Entry entry = _entries[index];
                RemoveEntryAt(index);
                if (entry.Window is DebuggerWindowGroup group)
                {
                    group.Shutdown();
                }
                else
                {
                    _pathsByWindow.Remove(entry.Window);
                    entry.Window.Shutdown();
                }

                return true;
            }

            if (!TryGetChildGroup(path.Substring(0, pos), out DebuggerWindowGroup childGroup))
            {
                return false;
            }

            int groupIndex = IndexOfEntry(path.Substring(0, pos));
            if (!childGroup.UnregisterRecursive(path.Substring(pos + 1)))
            {
                return false;
            }

            // 空组不再留在侧栏里。
            if (childGroup.DebuggerWindowCount == 0)
            {
                RemoveEntryAt(groupIndex);
            }

            return true;
        }

        private bool TryGetChildGroup(string name, out DebuggerWindowGroup group)
        {
            int index = IndexOfEntry(name);
            group = index < 0 ? null : _entries[index].Window as DebuggerWindowGroup;
            return group != null;
        }

        private int IndexOfEntry(string name)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (string.Equals(_entries[i].Name, name, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private void RemoveEntryAt(int index)
        {
            _entries.RemoveAt(index);
            _names = null;

            if (_selectedIndex > index)
            {
                _selectedIndex--;
            }

            if (_selectedIndex >= _entries.Count)
            {
                _selectedIndex = Math.Max(0, _entries.Count - 1);
            }
        }

        private static string CombinePath(string parentPath, string name)
        {
            return parentPath.Length == 0 ? name : string.Concat(parentPath, "/", name);
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            string[] segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join("/", segments);
        }
    }
}
