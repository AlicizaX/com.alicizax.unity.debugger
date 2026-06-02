using System;

namespace AlicizaX.Console
{
    /// <summary>
    /// 告诉 AlicizaXConsole 扫描命令时忽略这个对象。
    /// 大型代码库里，如果某些大对象没有任何命令，可以用它跳过扫描，从而缩短 AlicizaXConsole 加载时间。
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class AlicizaXConsoleIgnoreAttribute : Attribute { }
}
