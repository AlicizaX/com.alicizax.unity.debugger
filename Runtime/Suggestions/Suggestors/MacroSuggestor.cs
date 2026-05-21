using System.Collections.Generic;
using System.Linq;

namespace AlicizaX.Console.Suggestors
{
    public class MacroSuggestor : BasicCachedAlicizaXConsoleSuggestor<string>
    {
        protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options)
        {
            return context.Prompt.StartsWith("#");
        }

        protected override IAlicizaXConsoleSuggestion ItemToSuggestion(string macro)
        {
            return new RawSuggestion($"#{macro}");
        }

        protected override IEnumerable<string> GetItems(SuggestionContext context, SuggestorOptions options)
        {
            return AlicizaXConsoleMacros.GetMacros()
                .Select(x => x.Key);
        }
    }
}