using System.Collections.Generic;

namespace AlicizaX.Console.Suggestors
{
    public class BoolSuggestor : BasicCachedAlicizaXConsoleSuggestor<string>
    {
        private readonly string[] _values =
        {
            "true",
            "false"
        };

        protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options)
        {
            return context.TargetType == typeof(bool);
        }

        protected override IAlicizaXConsoleSuggestion ItemToSuggestion(string value)
        {
            return new RawSuggestion(value);
        }

        protected override IEnumerable<string> GetItems(SuggestionContext context, SuggestorOptions options)
        {
            return _values;
        }
    }
}