using System;

namespace AlicizaX.Debugger
{
    /// <summary>
    /// 把实现 <see cref="IDebuggerWindow"/> 的类型标记为可自动注册，并声明它在侧栏里的路径。
    /// </summary>
    /// <remarks>
    /// <para>路径用 <c>/</c> 分段，例如 <c>"Information/System"</c>；留空时回退为 <c>Auto/&lt;类名&gt;</c>。</para>
    /// <para>
    /// IL2CPP 下只被反射发现的类型会被裁剪，必须在 <c>link.xml</c> 或 <c>[Preserve]</c> 里保留，
    /// 否则发布包里该窗口会消失。
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class DebuggerWindowAttribute : Attribute
    {
        /// <summary>用指定路径标记窗口。</summary>
        /// <param name="path">侧栏路径，如 <c>"Information/System"</c>；为空时使用类名。</param>
        public DebuggerWindowAttribute(string path = null)
        {
            Path = path;
        }

        /// <summary>获取侧栏路径。</summary>
        public string Path { get; }
    }
}
