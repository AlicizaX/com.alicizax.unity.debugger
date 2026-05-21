using System.Collections.Generic;

namespace AlicizaX.Console
{
    /// <summary>
    /// 某个上下文对应的一组受管理的候选项。
    /// </summary>
    public class SuggestionSet
    {
        /// <summary>
        /// 生成此候选集时使用的上下文。
        /// </summary>
        public SuggestionContext Context;

        /// <summary>
        /// 候选集里当前选中项的索引。
        /// </summary>
        public int SelectionIndex;

        /// <summary>
        /// 候选集包含的候选项。
        /// </summary>
        public readonly List<IAlicizaXConsoleSuggestion> Suggestions = new List<IAlicizaXConsoleSuggestion>();

        /// <summary>
        /// 候选集里当前选中的候选项；没有则为空。
        /// </summary>
        public IAlicizaXConsoleSuggestion CurrentSelection =>
            SelectionIndex >= 0 && SelectionIndex < Suggestions.Count
                ? Suggestions[SelectionIndex]
                : null;
    }
}