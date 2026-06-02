using AlicizaX.Console.Utilities;

namespace AlicizaX.Console
{
    /// <summary>
    /// 提示系统共用的工具方法。
    /// </summary>
    public static class SuggestorUtilities
    {
        /// <summary>
        /// 判断提示文本是否适配字符串候选项。
        /// </summary>
        /// <param name="prompt">要测试的提示文本。</param>
        /// <param name="suggestion">要再次测试的字符串候选项。</param>
        /// <param name="options">提示器使用的选项。</param>
        /// <returns>提示文本是否兼容。</returns>
        public static bool IsCompatible(string prompt, string suggestion, SuggestorOptions options)
        {
            if (prompt.Length > suggestion.Length)
            {
                return false;
            }

            if (options.Fuzzy)
            {
                return options.CaseSensitive
                    ? suggestion.Contains(prompt)
                    : suggestion.ContainsCaseInsensitive(prompt);
            }

            return suggestion.StartsWith(prompt, !options.CaseSensitive, null);
        }
    }
}