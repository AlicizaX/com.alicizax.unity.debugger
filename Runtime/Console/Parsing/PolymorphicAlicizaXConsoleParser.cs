using System;

namespace AlicizaX.Console
{
    /// <summary>
    /// 单一基类所有派生类型的解析器。
    /// </summary>
    /// <typeparam name="T">要解析类型的基类。</typeparam>
    public abstract class PolymorphicAlicizaXConsoleParser<T> : IAlicizaXConsoleParser where T : class
    {
        private Func<string, Type, object> _recursiveParser;

        public virtual int Priority => -1000;

        public bool CanParse(Type type)
        {
            return typeof(T).IsAssignableFrom(type);
        }

        public virtual object Parse(string value, Type type, Func<string, Type, object> recursiveParser)
        {
            _recursiveParser = recursiveParser;
            return Parse(value, type);
        }

        protected object ParseRecursive(string value, Type type)
        {
            return _recursiveParser(value, type);
        }

        protected TElement ParseRecursive<TElement>(string value)
        {
            return (TElement)_recursiveParser(value, typeof(TElement));
        }

        public abstract T Parse(string value, Type type);
    }
}
