using AlicizaX.Console.Comparators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AlicizaX.Console
{
    public class AlicizaXConsoleSuggestor
    {
        private readonly IAlicizaXConsoleSuggestor[] _suggestors;
        private readonly List<IAlicizaXConsoleSuggestion> _suggestionBuffer = new List<IAlicizaXConsoleSuggestion>();

        public AlicizaXConsoleSuggestor(IEnumerable<IAlicizaXConsoleSuggestor> suggestors)
        {
            _suggestors = suggestors.ToArray();
        }

        public AlicizaXConsoleSuggestor() : this(new InjectionLoader<IAlicizaXConsoleSuggestor>().GetInjectedInstances())
        {

        }

        public IEnumerable<IAlicizaXConsoleSuggestion> GetSuggestions(SuggestionContext context, SuggestorOptions options)
        {
            PreprocessContext(ref context);

            IEnumerable<IAlicizaXConsoleSuggestion> suggestions = _suggestors.SelectMany(x => x.GetSuggestions(context, options));

            _suggestionBuffer.Clear();
            _suggestionBuffer.AddRange(suggestions);

            AlphanumComparator comparator = new AlphanumComparator();
            IOrderedEnumerable<IAlicizaXConsoleSuggestion> sortedSuggestions =
                _suggestionBuffer
                    .OrderBy(x => x.PrimarySignature.Length)
                    .ThenBy(x => x.PrimarySignature, comparator)
                    .ThenBy(x => x.SecondarySignature.Length)
                    .ThenBy(x => x.SecondarySignature, comparator);

            if (options.Fuzzy)
            {
                StringComparison comparisonType = options.CaseSensitive
                    ? StringComparison.CurrentCulture
                    : StringComparison.CurrentCultureIgnoreCase;

                sortedSuggestions = sortedSuggestions.OrderBy(x => x.PrimarySignature.IndexOf(context.Prompt, comparisonType));
            }

            return sortedSuggestions;
        }

        private void PreprocessContext(ref SuggestionContext context)
        {
            TextProcessing.ReduceScopeOptions options = TextProcessing.ReduceScopeOptions.Default;
            options.ReduceIncompleteScope = true;

            context.Prompt = context.Prompt.ReduceScope(options);
        }
    }
}
