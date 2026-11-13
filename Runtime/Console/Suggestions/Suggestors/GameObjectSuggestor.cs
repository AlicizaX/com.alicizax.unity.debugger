using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AlicizaX.Console.Suggestors
{
    public class GameObjectSuggestor : BasicCachedAlicizaXConsoleSuggestor<string>
    {
        protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options)
        {
            return context.TargetType == typeof(GameObject);
        }

        protected override IAlicizaXConsoleSuggestion ItemToSuggestion(string name)
        {
            return new RawSuggestion(name, true);
        }

        protected override IEnumerable<string> GetItems(SuggestionContext context, SuggestorOptions options)
        {
            return UnityObjectQuery.FindAll<GameObject>()
                .Select(obj => obj.name);
        }
    }
}
