using System;
using System.Collections.Generic;

namespace AlicizaX.Console.Actions
{
    /// <summary>
    /// 把用户接下来在控制台输入的一行文本当作用户响应。 
    /// 并把它解析成指定类型的值。
    /// </summary>
    public class ReadValue<T> : Composite
    {
        private static readonly AlicizaXConsoleParser Parser = new AlicizaXConsoleParser();

        /// <param name="getValue">返回用户输入并解析后的值的委托。</param>
        /// <param name="config">提供给响应流程的配置。</param>
        public ReadValue(Action<T> getValue, ResponseConfig config)
            : base(Generate(getValue, config))
        { }

        /// <param name="getValue">返回用户输入并解析后的值的委托。</param>
        public ReadValue(Action<T> getValue)
            : this(getValue, ResponseConfig.Default)
        { }

        private static IEnumerator<ICommandAction> Generate(Action<T> getValue, ResponseConfig config)
        {
            string line = default;
            yield return new ReadLine(t => line = t, config);

            T value = Parser.Parse<T>(line);
            getValue(value);
        }
    }
}
