using System;

namespace AlicizaX.Console
{
    /// <summary>
    /// 单个类型的序列化器。
    /// </summary>
    /// <typeparam name="T">要序列化的类型。</typeparam>
    public abstract class BasicAlicizaXConsoleSerializer<T> : IAlicizaXConsoleSerializer
    {
        private Func<object, AlicizaXConsoleTheme, string> _recursiveSerializer;

        public virtual int Priority => 0;

        public bool CanSerialize(Type type)
        {
            return type == typeof(T);
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
