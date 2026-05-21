using System;

namespace AlicizaX.Console
{
    /// <summary>
    /// 所有 IAlicizaXConsoleSuggestorTag 来源的基础特性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true, Inherited = true)]
    public abstract class SuggestorTagAttribute : Attribute
    {
        public abstract IAlicizaXConsoleSuggestorTag[] GetSuggestorTags();
    }
}