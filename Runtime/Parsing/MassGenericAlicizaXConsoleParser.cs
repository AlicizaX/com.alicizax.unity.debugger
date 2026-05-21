using System;
using System.Collections.Generic;

namespace AlicizaX.Console
{
    /// <summary>
    /// 多个泛型定义构造出的类型解析器。
    /// </summary>
    public abstract class MassGenericAlicizaXConsoleParser : IAlicizaXConsoleParser
    {
        /// <summary>
        /// 此解析器对应的未闭合泛型类型列表。
        /// </summary>
        protected abstract HashSet<Type> GenericTypes { get; }

        private Func<string, Type, object> _recursiveParser;

        protected MassGenericAlicizaXConsoleParser()
        {
            foreach (Type type in GenericTypes)
            {
                if (!type.IsGenericType)
                {
                    throw new ArgumentException("Generic Parsers must use a generic type as their base");
                }

                if (type.IsConstructedGenericType)
                {
                    throw new ArgumentException("Generic Parsers must use an incomplete generic type as their base");
                }
            }
        }

        public virtual int Priority => -2000;

        public bool CanParse(Type type)
        {
            if (type.IsGenericType)
            {
                return GenericTypes.Contains(type.GetGenericTypeDefinition());
            }

            return false;
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
