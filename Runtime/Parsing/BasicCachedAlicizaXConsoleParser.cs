using System;
using System.Collections.Generic;

namespace AlicizaX.Console
{
    /// <summary>
    /// 单个类型的解析器。
    /// 缓存解析结果；同一个字符串解析过后会直接复用。
    /// </summary>
    /// <typeparam name="T">要解析的类型。</typeparam>
    public abstract class BasicCachedAlicizaXConsoleParser<T> : BasicAlicizaXConsoleParser<T>
    {
        private readonly Dictionary<string, T> _cacheLookup = new Dictionary<string, T>();

        public override object Parse(string value, Type type, Func<string, Type, object> recursiveParser)
        {
            if (_cacheLookup.ContainsKey(value))
            {
                return _cacheLookup[value];
            }

            T result = (T)base.Parse(value, type, recursiveParser);
            _cacheLookup[value] = result;
            return result;
        }
    }
}
