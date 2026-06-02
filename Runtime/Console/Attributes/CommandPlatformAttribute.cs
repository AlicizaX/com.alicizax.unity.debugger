using System;

namespace AlicizaX.Console
{
    /// <summary>指定命令可用的平台，会覆盖 [Command] 里配置的平台。</summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class CommandPlatformAttribute : Attribute
    {
        public readonly Platform SupportedPlatforms;

        public CommandPlatformAttribute(Platform supportedPlatforms)
        {
            SupportedPlatforms = supportedPlatforms;
        }
    }
}