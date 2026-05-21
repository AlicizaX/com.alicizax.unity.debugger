using System;

namespace AlicizaX.Console
{
    public class AlicizaXConsolePreprocessor
    {
        public string Process(string text)
        {
            if (text.StartsWith("#define", StringComparison.CurrentCulture))
            {
                return text;
            }

            return AlicizaXConsoleMacros.ExpandMacros(text);
        }
    }
}
