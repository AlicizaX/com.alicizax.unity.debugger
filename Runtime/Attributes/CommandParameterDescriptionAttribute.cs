using System;

namespace AlicizaX.Console
{
    /// <summary>给命令参数提供说明。</summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class CommandParameterDescriptionAttribute : Attribute
    {
        public readonly string Description;
        public readonly bool Valid;

        public CommandParameterDescriptionAttribute(string description)
        {
            Description = description;
            Valid = !string.IsNullOrWhiteSpace(description);
        }
    }
}