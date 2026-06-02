using AlicizaX.Console.Utilities;
using System;

namespace AlicizaX.Console
{
    /// <summary>
    /// 单个泛型定义构造出的类型解析器。
    /// </summary>
    public abstract class GenericAlicizaXConsoleParser : IAlicizaXConsoleParser
    {
        /// <summary>
        /// 此解析器对应的未闭合泛型类型。
        /// </summary>
        protected abstract Type GenericType { get; }

        private Func<string, Type, object> _recursiveParser;

        protected GenericAlicizaXConsoleParser()
        {
            if (!GenericType.IsGenericType)
            {
                throw new ArgumentException("Generic Parsers must use a generic type as their base");
            }

            if (GenericType.IsConstructedGenericType)
            {
                throw new ArgumentException("Generic Parsers must use an incomplete generic type as their base");
            }
        }

        public virtual int Priority => -500;

        public bool CanParse(Type type)
        {
            return type.IsGenericTypeOf(GenericType);
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

        public abstract object Parse(string value, Type type);
    }
}
