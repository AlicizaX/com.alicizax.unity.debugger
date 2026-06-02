using System;

namespace AlicizaX.Console
{
    /// <summary>
    /// 创建会被 AlicizaXConsoleParser 加载和使用的解析器。
    /// </summary>
    public interface IAlicizaXConsoleParser
    {
        /// <summary>
        /// 此解析器的优先级，用于解决多个解析器覆盖同一类型的情况。
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 此解析器是否可以解析成输入的类型。
        /// </summary>
        /// <param name="type">要检查的类型。</param>
        /// <returns>是否可以解析。</returns>
        bool CanParse(Type type);

        /// <summary>
        /// 把输入字符串解析成指定类型。
        /// </summary>
        /// <param name="value">输入的字符串数据。</param>
        /// <param name="type">输入字符串要解析成的类型。</param>
        /// <param name="recursiveParser">回调主解析器，以便递归解析子元素。</param>
        /// <returns>解析后的对象。</returns>
        object Parse(string value, Type type, Func<string, Type, object> recursiveParser);
    }
}
