using System;

namespace AlicizaX.Console
{
    /// <summary>
    /// 创建一个自定义语法结构解析器，会被 AlicizaXConsoleParser 加载和使用。
    /// 解析对象值时会先尝试语法结构，失败后才使用 IAlicizaXConsoleParser。
    /// </summary>
    public interface IAlicizaXConsoleGrammarConstruct
    {
        /// <summary>
        /// 此语法结构的优先级。
        /// </summary>
        int Precedence { get; }

        /// <summary>
        /// 输入数据是否匹配这个语法结构。
        /// </summary>
        /// <param name="value">输入的字符串数据。</param>
        /// <param name="type">要检查的类型。</param>
        /// <returns>是否匹配此结构定义的语法。</returns>
        bool Match(string value, Type type);

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
