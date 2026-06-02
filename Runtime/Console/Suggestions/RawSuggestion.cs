namespace AlicizaX.Console
{
    /// <summary>
    /// 给定值的原始候选项。
    /// </summary>
    public class RawSuggestion : IAlicizaXConsoleSuggestion
    {
        private readonly string _value;
        private readonly bool _singleLiteral;
        private readonly string _completion;

        public string FullSignature => _value;
        public string PrimarySignature => _value;
        public string SecondarySignature => string.Empty;

        /// <summary>
        /// 根据给定值创建候选项。
        /// </summary>
        /// <param name="value">要作为候选项的值。</param>
        /// <param name="singleLiteral">如果该值要按单个字面量处理，会按需要使用 ""。</param>
        public RawSuggestion(string value, bool singleLiteral = false)
        {
            _value = value;
            _singleLiteral = singleLiteral;
            _completion = _value;

            if (_completion.CanSplitScoped(' ', '"', '"'))
            {
                _completion = $"\"{_completion}\"";
            }
        }

        public bool MatchesPrompt(string prompt)
        {
            if (_singleLiteral)
            {
                prompt = prompt.Trim('"');
            }

            return prompt == _value;
        }

        public string GetCompletion(string prompt)
        {
            return _completion;
        }

        public string GetCompletionTail(string prompt)
        {
            return string.Empty;
        }

        public SuggestionContext? GetInnerSuggestionContext(SuggestionContext context)
        {
            return null;
        }
    }
}