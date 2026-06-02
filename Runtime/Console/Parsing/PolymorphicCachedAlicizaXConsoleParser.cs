using System;
using System.Collections.Generic;

namespace AlicizaX.Console
{
    /// <summary>
    /// 单一基类所有派生类型的解析器。
    /// 缓存解析结果；同一个字符串解析过后会直接复用。
    /// </summary>
    /// <typeparam name="T">要解析类型的基类。</typeparam>
    public abstract class PolymorphicCachedAlicizaXConsoleParser<T> : PolymorphicAlicizaXConsoleParser<T> where T : class
    {
        private readonly Dictionary<(string, Type), T> _cacheLookup = new Dictionary<(string, Type), T>();

        public override object Parse(string value, Type type, Func<string, Type, object> recursiveParser)
        {
            (string value, Type type) key = (value, type);
            if (_cacheLookup.ContainsKey(key))
            {
                return _cacheLookup[key];
            }

            T result = (T)base.Parse(value, type, recursiveParser);
            _cacheLookup[key] = result;
            return result;
        }
    }
}
