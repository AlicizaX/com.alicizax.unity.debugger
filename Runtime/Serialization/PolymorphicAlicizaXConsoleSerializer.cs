using System;

namespace AlicizaX.Console
{
    /// <summary>
    /// 单一基类所有派生类型的序列化器。
    /// </summary>
    /// <typeparam name="T">要序列化类型的基类。</typeparam>
    public abstract class PolymorphicAlicizaXConsoleSerializer<T> : IAlicizaXConsoleSerializer where T : class
    {
        private Func<object, AlicizaXConsoleTheme, string> _recursiveSerializer;

        public virtual int Priority => -1000;

        public bool CanSerialize(Type type)
        {
            return typeof(T).IsAssignableFrom(type);
        }

        string IAlicizaXConsoleSerializer.SerializeFormatted(object value, AlicizaXConsoleTheme theme, Func<object, AlicizaXConsoleTheme, string> recursiveSerializer)
        {
            _recursiveSerializer = recursiveSerializer;
            return SerializeFormatted((T)value, theme);
        }

        protected string SerializeRecursive(object value, AlicizaXConsoleTheme theme)
        {
            return _recursiveSerializer(value, theme);
        }

        public abstract string SerializeFormatted(T value, AlicizaXConsoleTheme theme);
    }
}
