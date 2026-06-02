using AlicizaX.Console.Utilities;
using System;

namespace AlicizaX.Console
{
    /// <summary>
    /// 单个泛型定义构造出的类型序列化器。
    /// </summary>
    public abstract class GenericAlicizaXConsoleSerializer : IAlicizaXConsoleSerializer
    {
        /// <summary>
        /// 此序列化器对应的未闭合泛型类型。
        /// </summary>
        protected abstract Type GenericType { get; }

        private Func<object, AlicizaXConsoleTheme, string> _recursiveSerializer;

        protected GenericAlicizaXConsoleSerializer()
        {
            if (!GenericType.IsGenericType)
            {
                throw new ArgumentException($"Generic Serializers must use a generic type as their base");
            }

            if (GenericType.IsConstructedGenericType)
            {
                throw new ArgumentException($"Generic Serializers must use an incomplete generic type as their base");
            }
        }

        public virtual int Priority => -500;

        public bool CanSerialize(Type type)
        {
            return type.IsGenericTypeOf(GenericType);
        }

        string IAlicizaXConsoleSerializer.SerializeFormatted(object value, AlicizaXConsoleTheme theme, Func<object, AlicizaXConsoleTheme, string> recursiveSerializer)
        {
            _recursiveSerializer = recursiveSerializer;
            return SerializeFormatted(value, theme);
        }

        protected string SerializeRecursive(object value, AlicizaXConsoleTheme theme)
        {
            return _recursiveSerializer(value, theme);
        }

        public abstract string SerializeFormatted(object value, AlicizaXConsoleTheme theme);
    }
}