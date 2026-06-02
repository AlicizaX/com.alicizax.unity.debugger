using System;

namespace AlicizaX.Console
{
    /// <summary>
    /// 单个类型的解析器。
    /// </summary>
    /// <typeparam name="T">要解析的类型。</typeparam>
    public abstract class BasicAlicizaXConsoleParser<T> : IAlicizaXConsoleParser
    {
        private Func<string, Type, object> _recursiveParser;

        public virtual int Priority => 0;

        public bool CanParse(Type type)
        {
            return type == typeof(T);
        }

        public virtual object Parse(string value, Type type, Func<string, Type, object> recursiveParser)
        {
            _recursiveParser = recursiveParser;
            return Parse(value);
        }

        protected object ParseRecursive(string value, Type type)
        {
            return _recursiveParser(value, type);
        }

        protected TElement ParseRecursive<TElement>(string value)
        {
            return (TElement)_recursiveParser(value, typeof(TElement));
        }

        public abstract T Parse(string value);
    }
}
