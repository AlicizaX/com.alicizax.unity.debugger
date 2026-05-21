using Cysharp.Text;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace AlicizaX.Console
{
    /// <summary>
    /// 创建命令前缀，会加到这个类内所有命令前面；对子类也会递归生效。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class CommandPrefixAttribute : Attribute
    {
        public readonly string Prefix;
        public readonly bool Valid = true;

        private static readonly char[] _bannedAliasChars = { ' ', '(', ')', '{', '}', '[', ']', '<', '>' };

        public CommandPrefixAttribute([CallerMemberName] string prefixName = "")
        {
            Prefix = prefixName;
            foreach (var c in _bannedAliasChars)
            {
                if (Prefix.Contains(c))
                {
                    string errorMessage = ZString.Format("Development Processor Error: Command prefix '{0}' contains the char '{1}' which is banned. Unexpected behaviour may occurr.", Prefix, c);
                    Debug.LogError(errorMessage);

                    Valid = false;
                    throw new ArgumentException(errorMessage, nameof(prefixName));
                }
            }
        }
    }
}
