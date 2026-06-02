using Cysharp.Text;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace AlicizaX.Console
{
    /// <summary>
    /// 把关联方法标记为命令，使它能被 AlicizaXConsoleProcessor 加载，并在 AlicizaX Console 中使用。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
    public sealed class CommandAttribute : Attribute
    {
        public readonly string Alias;
        public readonly string Description;
        public readonly Platform SupportedPlatforms;
        public readonly MonoTargetType MonoTarget;
        public readonly bool Valid = true;

        private static readonly char[] _bannedAliasChars = new char[] { ' ', '(', ')', '{', '}', '[', ']', '<', '>' };

        public CommandAttribute([CallerMemberName] string aliasOverride = "", Platform supportedPlatforms = Platform.AllPlatforms, MonoTargetType targetType = MonoTargetType.Single)
        {
            Alias = aliasOverride;
            MonoTarget = targetType;
            SupportedPlatforms = supportedPlatforms;

            for (int i = 0; i < _bannedAliasChars.Length; i++)
            {
                if (Alias.Contains(_bannedAliasChars[i]))
                {
                    string errorMessage = ZString.Format("Development Processor Error: Command with alias '{0}' contains the char '{1}' which is banned. Unexpected behaviour may occur.", Alias, _bannedAliasChars[i]);
                    Debug.LogError(errorMessage);
                    Valid = false;
                    throw new ArgumentException(errorMessage, nameof(aliasOverride));
                }
            }
        }

        public CommandAttribute(string aliasOverride, MonoTargetType targetType, Platform supportedPlatforms = Platform.AllPlatforms) : this(aliasOverride, supportedPlatforms, targetType) { }

        public CommandAttribute(string aliasOverride, string description, Platform supportedPlatforms = Platform.AllPlatforms, MonoTargetType targetType = MonoTargetType.Single) : this(aliasOverride, supportedPlatforms, targetType)
        {
            Description = description;
        }

        public CommandAttribute(string aliasOverride, string description, MonoTargetType targetType, Platform supportedPlatforms = Platform.AllPlatforms) : this(aliasOverride, description, supportedPlatforms, targetType) { }
    }
}
