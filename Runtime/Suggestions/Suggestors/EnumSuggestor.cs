using System;
using System.Collections.Generic;
using System.Linq;

namespace AlicizaX.Console.Suggestors
{
    public class EnumSuggestor : BasicCachedAlicizaXConsoleSuggestor<string>
    {
        private readonly Dictionary<Type, string[]> _enumCaseCache = new Dictionary<Type, string[]>();

        protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options)
        {
            Type targetType = context.TargetType;
            return targetType != null 
                && targetType.IsEnum;
        }

        protected override IAlicizaXConsoleSuggestion ItemToSuggestion(string item)
        {
            return new RawSuggestion(item);
        }

        protected override IEnumerable<string> GetItems(SuggestionContext context, SuggestorOptions options)
        {
            return GetEnumCases(context.TargetType);
        }

        private string[] GetEnumCases(Type enumType)
        {
            if (_enumCaseCache.TryGetValue(enumType, out string[] cachedEnumCases))
            {
                return cachedEnumCases;
            }

            string[] enumCases =
                enumType.GetEnumNames()
                    .Select(x => x.ToString())
                    .ToArray();

            return _enumCaseCache[enumType] = enumCases;
        }
    }
}