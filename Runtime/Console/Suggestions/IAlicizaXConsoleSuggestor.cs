using System.Collections.Generic;

namespace AlicizaX.Console
{
    /// <summary>
    /// 由 AlicizaXConsoleSuggestor 加载，用来生成 IAlicizaXConsoleSuggestion 的提示器。
    /// </summary>
    public interface IAlicizaXConsoleSuggestor
    {
        /// <summary>
        /// 获取指定上下文的候选项。
        /// </summary>
        /// <param name="context">用于提供候选项的上下文。</param>
        /// <param name="options">提示器使用的选项。</param>
        /// <returns>为上下文生成的候选项。</returns>
        IEnumerable<IAlicizaXConsoleSuggestion> GetSuggestions(SuggestionContext context, SuggestorOptions options);
    }
}