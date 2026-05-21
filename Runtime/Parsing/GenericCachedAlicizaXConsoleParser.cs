using System;
using System.Collections.Generic;

namespace AlicizaX.Console
{
    /// <summary>
    /// 单个泛型定义构造出的类型解析器。
    /// 缓存解析结果；同一个字符串解析过后会直接复用。
    /// </summary>
    public abstract class GenericCachedAlicizaXConsoleParser : GenericAlicizaXConsoleParser
    {
        private readonly Dictionary<(string, Type), object> _cacheLookup = new Dictionary<(string, Type), object>();

        public override object Parse(string value, Type type, Func<string, Type, object> recursiveParser)
        {
            (string value, Type type) key = (value, type);
            if (_cacheLookup.ContainsKey(key))
            {
                return _cacheLookup[key];
            }

            object result = base.Parse(value, type, recursiveParser);
            _cacheLookup[key] = result;
            return result;
        }
    }
}
