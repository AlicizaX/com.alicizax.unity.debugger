using System.Collections.Generic;

namespace AlicizaX.Console.Suggestors
{
    /// <summary>
    /// 为提示系统生成可用候选项。
    /// </summary>
    public class InlineSuggestor : IAlicizaXConsoleSuggestor
    {
        public IEnumerable<IAlicizaXConsoleSuggestion> GetSuggestions(SuggestionContext context, SuggestorOptions options)
        {
            foreach (Tags.InlineSuggestionsTag t in context.GetTags<Tags.InlineSuggestionsTag>())
            {
                foreach (string s in t.Suggestions)
                {
                    yield return new RawSuggestion(s, singleLiteral: true);
                }
            }
        }
    }
}
