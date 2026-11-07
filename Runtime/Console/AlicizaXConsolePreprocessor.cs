using System;

namespace AlicizaX.Console
{
    public class AlicizaXConsolePreprocessor
    {
        public string Process(string text)
        {
            // 关键字匹配必须用序数比较：区域性比较会让土耳其语等区域下的 "#define" 行为不同。
            if (text.StartsWith("#define", StringComparison.Ordinal))
            {
                return text;
            }

            return AlicizaXConsoleMacros.ExpandMacros(text);
        }
    }
}
