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
        private readonly SuggestionComparer _suggestionComparer = new SuggestionComparer();

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

            _suggestionBuffer.Clear();
            for (int i = 0; i < _suggestors.Length; i++)
            {
                _suggestionBuffer.AddRange(_suggestors[i].GetSuggestions(context, options));
            }

            // 直接原地排序：OrderBy/ThenBy 链每次按键都要分配 4~5 个缓冲与委托。
            _suggestionComparer.Configure(options.Fuzzy ? context.Prompt : null, options.CaseSensitive);
            _suggestionBuffer.Sort(_suggestionComparer);
            return _suggestionBuffer;
        }

        private void PreprocessContext(ref SuggestionContext context)
        {
            TextProcessing.ReduceScopeOptions options = TextProcessing.ReduceScopeOptions.Default;
            options.ReduceIncompleteScope = true;

            context.Prompt = context.Prompt.ReduceScope(options);
        }

        /// <summary>
        /// 候选排序：主签名长度 → 主签名自然序 → 次签名长度 → 次签名自然序 → 模糊匹配位置。
        /// </summary>
        private sealed class SuggestionComparer : IComparer<IAlicizaXConsoleSuggestion>
        {
            private static readonly AlphanumComparator Alphanum = new AlphanumComparator();

            private string _fuzzyPrompt;
            private StringComparison _comparisonType;

            public void Configure(string fuzzyPrompt, bool caseSensitive)
            {
                _fuzzyPrompt = fuzzyPrompt;
                _comparisonType = caseSensitive
                    ? StringComparison.CurrentCulture
                    : StringComparison.CurrentCultureIgnoreCase;
            }

            public int Compare(IAlicizaXConsoleSuggestion x, IAlicizaXConsoleSuggestion y)
            {
                int result = CompareSignature(x, y);
                if (result != 0 || _fuzzyPrompt == null)
                {
                    return result;
                }

                return x.PrimarySignature.IndexOf(_fuzzyPrompt, _comparisonType)
                    .CompareTo(y.PrimarySignature.IndexOf(_fuzzyPrompt, _comparisonType));
            }

            private static int CompareSignature(IAlicizaXConsoleSuggestion x, IAlicizaXConsoleSuggestion y)
            {
                int result = x.PrimarySignature.Length.CompareTo(y.PrimarySignature.Length);
                if (result != 0)
                {
                    return result;
                }

                result = Alphanum.Compare(x.PrimarySignature, y.PrimarySignature);
                if (result != 0)
                {
                    return result;
                }

                result = x.SecondarySignature.Length.CompareTo(y.SecondarySignature.Length);
                if (result != 0)
                {
                    return result;
                }

                return Alphanum.Compare(x.SecondarySignature, y.SecondarySignature);
            }
        }
    }
}
