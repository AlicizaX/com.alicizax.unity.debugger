using System;

namespace AlicizaX.Console
{
    /// <summary>
    /// 创建会被 AlicizaXConsoleSerializer 加载和使用的序列化器。
    /// </summary>
    public interface IAlicizaXConsoleSerializer
    {
        /// <summary>
        /// 此序列化器的优先级，用于解决多个序列化器覆盖同一类型的情况。
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 此序列化器是否可以序列化输入的类型。
        /// </summary>
        /// <param name="type">要检查的类型。</param>
        /// <returns>是否可以序列化。</returns>
        bool CanSerialize(Type type);

        /// <summary>
        /// 序列化输入数据。
        /// </summary>
        /// <param name="value">要序列化的值。</param>
        /// <param name="theme">格式化序列化时使用的主题，可不传。</param>
        /// <param name="recursiveSerializer">回调主序列化器，以便递归序列化子元素。</param>
        /// <returns>序列化结果。</returns>
        string SerializeFormatted(object value, AlicizaXConsoleTheme theme, Func<object, AlicizaXConsoleTheme, string> recursiveSerializer);
    }
}
