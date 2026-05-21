namespace AlicizaX.Console
{
    /// <summary>
    /// 可自动补全并显示的候选项。
    /// </summary>
    public interface IAlicizaXConsoleSuggestion
    {
        /// <summary>
        /// 用于显示的候选项完整签名。
        /// </summary>
        string FullSignature { get; }

        /// <summary>
        /// 签名的主要部分。
        /// </summary>
        string PrimarySignature { get; }

        /// <summary>
        /// 签名的次要部分。
        /// </summary>
        string SecondarySignature { get; }

        /// <summary>
        /// 判断给定提示文本是否匹配这个候选项。
        /// </summary>
        /// <param name="prompt">要检查的提示文本。</param>
        /// <returns>提示文本是否匹配该候选项。</returns>
        bool MatchesPrompt(string prompt);

        /// <summary>
        /// 获取这个候选项针对提示文本的完整补全值。
        /// </summary>
        /// <param name="prompt">要补全的提示文本。</param>
        /// <returns>补全值。</returns>
        string GetCompletion(string prompt);

        /// <summary>
        /// 获取这个候选项针对提示文本的补全尾部，类似次级签名。
        /// </summary>
        /// <param name="prompt">要生成补全尾部的提示文本。</param>
        /// <returns>补全尾部。</returns>
        string GetCompletionTail(string prompt);

        /// <summary>
        /// 获取这个候选项的内部提示上下文，用来继续生成下一层候选项。
        /// </summary>
        /// <param name="context">外层提示上下文。</param>
        /// <returns>内部提示上下文；无法创建时为 null。</returns>
        SuggestionContext? GetInnerSuggestionContext(SuggestionContext context);
    }
}