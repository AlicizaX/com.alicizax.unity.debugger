using System;

namespace AlicizaX.Console
{
    /// <summary>给命令提供说明；如果 [Command] 已经有说明，则优先使用 [Command] 的说明。一个方法有多个 [Command] 时很有用。</summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class CommandDescriptionAttribute : Attribute
    {
        public readonly string Description;
        public readonly bool Valid;

        public CommandDescriptionAttribute(string description)
        {
            Description = description;
            Valid = !string.IsNullOrWhiteSpace(description);
        }
    }
}
