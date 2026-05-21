using AlicizaX.Console.Utilities;
using Cysharp.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using AlicizaX.Console.Pooling;

namespace AlicizaX.Console
{
    /// <summary>
    /// 根据用户提示文本更新的候选集栈。
    /// 每个候选集代表一层候选项，因此可以支持嵌套提示。
    /// </summary>
    public class SuggestionStack
    {
        private readonly AlicizaXConsoleSuggestor _suggestor;
        private readonly List<SuggestionSet> _suggestionSets = new List<SuggestionSet>();
        private readonly Pool<SuggestionSet> _setPool = new Pool<SuggestionSet>();

        /// <summary>
        /// 候选栈里最上层的有效候选集。
        /// </summary>
        public SuggestionSet TopmostSuggestionSet => _suggestionSets.LastOrDefault();

        /// <summary>
        /// 最上层候选集里选中的候选项；没有则为空。
        /// </summary>
        public IAlicizaXConsoleSuggestion TopmostSuggestion => TopmostSuggestionSet?.CurrentSelection;

        /// <summary>
        /// 创建新的候选集时调用的回调。
        /// </summary>
        public event Action<SuggestionSet> OnSuggestionSetCreated;

        /// <summary>
        /// 用默认 AlicizaXConsoleSuggestor 创建 SuggestionStack。
        /// </summary>
        public SuggestionStack() : this(new AlicizaXConsoleSuggestor())
        {

        }

        /// <summary>
        /// 用用户提供的 AlicizaXConsoleSuggestor 创建 SuggestionStack。
        /// </summary>
        /// <param name="suggestor">为候选栈创建候选项时使用的 AlicizaXConsoleSuggestor。</param>
        public SuggestionStack(AlicizaXConsoleSuggestor suggestor)
        {
            _suggestor = suggestor;
        }

        /// <summary>
        /// 清空整个候选栈。
        /// </summary>
        public void Clear()
        {
            while (PopSet()) { }
        }

        /// <summary>
        /// 用新的提示文本更新候选栈。
        /// </summary>
        /// <param name="prompt">用于更新候选栈的提示文本。</param>
        /// <param name="options">传给提示器的选项。</param>
        public void UpdateStack(string prompt, SuggestorOptions options)
        {
            // 如果为空就清理。
            if (string.IsNullOrWhiteSpace(prompt))
            {
                Clear();
                return;
            }

            PropagateContextChanges(prompt);
            PopInvalidLayers();
            BuildInitialLayer(prompt, options);
            BuildNewLayers(options);
        }

        private SuggestionContext? GetInnerSuggestionContext(SuggestionSet set)
        {
            IAlicizaXConsoleSuggestion currentSuggestion = set.CurrentSelection;
            SuggestionContext currentContext = set.Context;
            return currentSuggestion?.GetInnerSuggestionContext(currentContext);
        }

        private void InvalidateLayersFrom(int index)
        {
            PopSets(_suggestionSets.Count - index);
        }

        private void PropagateContextChanges(string prompt)
        {
            if (_suggestionSets.Count == 0)
            {
                // 没有需要向下传递的内容。
                return;
            }

            // 用新的提示文本初始化第一层候选集。
            _suggestionSets[0].Context.Prompt = prompt;

            // 向后传递上下文变化。
            for (int i = 0; i < _suggestionSets.Count - 1; i++)
            {
                SuggestionSet currentSet = _suggestionSets[i];
                SuggestionContext? newNextContext = GetInnerSuggestionContext(currentSet);

                if (newNextContext != null)
                {
                    // 更新已有候选层的上下文。
                    SuggestionSet nextSet = _suggestionSets[i + 1];
                    nextSet.Context = newNextContext.Value;
                }
                else
                {
                    // 上下文为空表示这一层必须失效。
                    InvalidateLayersFrom(i + 1);
                }
            }
        }

        private void PopInvalidLayers()
        {
            for (int i = 0; i < _suggestionSets.Count; i++)
            {
                SuggestionSet set = _suggestionSets[i];
                SuggestionContext context = set.Context;
                IAlicizaXConsoleSuggestion suggestion = set.CurrentSelection;

                if (suggestion == null || !suggestion.MatchesPrompt(context.Prompt))
                {
                    InvalidateLayersFrom(i);
                }
            }
        }
        private void BuildInitialLayer(string prompt, SuggestorOptions options)
        {
            if (_suggestionSets.Count == 0)
            {
                SuggestionContext context = new SuggestionContext
                {
                    Prompt = prompt,
                    Depth = 0,
                    TargetType = null,
                };

                CreateLayer(context, options);
            }
        }

        private void BuildNewLayers(SuggestorOptions options)
        {
            // 如果可以，就创建新的候选层。
            if (TopmostSuggestion != null)
            {
                SuggestionSet set = TopmostSuggestionSet;
                SuggestionContext? newNextContext = GetInnerSuggestionContext(set);

                if (newNextContext != null)
                {
                    // 创建新层，然后递归尝试继续构建。
                    if (CreateLayer(newNextContext.Value, options))
                    {
                        BuildNewLayers(options);
                    }
                }
            }
        }

        private void TryAutoSelectSuggestion(SuggestionSet set, string prompt)
        {
            if (set.CurrentSelection != null)
            {
                // 已经有选中项时不要再尝试自动选择。
                return;
            }

            // 如果第一项匹配，就尝试选中它。
            IAlicizaXConsoleSuggestion candidate = set.Suggestions.FirstOrDefault();
            if (candidate != null && candidate.MatchesPrompt(prompt))
            {
                set.SelectionIndex = 0;
            }
        }

        private bool CreateLayer(SuggestionContext context, SuggestorOptions options)
        {
            // 获取候选项并创建新的候选集。
            IEnumerable<IAlicizaXConsoleSuggestion> suggestions =
                _suggestor.GetSuggestions(context, options);

            SuggestionSet set = PushSet();
            set.Context = context;
            set.Suggestions.AddRange(suggestions);

            // 如果新层为空就移除。
            if (set.Suggestions.Count == 0)
            {
                PopSet();
                return false;
            }

            OnSuggestionSetCreated?.Invoke(set);

            // 尝试在候选集中自动选择一项。
            TryAutoSelectSuggestion(set, context.Prompt);
            return true;
        }

        /// <summary>
        /// 获取当前候选栈的补全值。
        /// </summary>
        /// <returns>候选栈合并后的补全值。</returns>
        public string GetCompletion()
        {
            if (_suggestionSets.Count == 0)
            {
                return string.Empty;
            }

            IEnumerable<IAlicizaXConsoleSuggestion> suggestionChain =
                _suggestionSets
                    .Select(x => x.CurrentSelection)
                    .Where(x => x != null);

            SuggestionContext context = _suggestionSets[0].Context;
            Utf16ValueStringBuilder stringBuilder = ZString.CreateStringBuilder();
            try
            {
                foreach (IAlicizaXConsoleSuggestion suggestion in suggestionChain)
                {
                    string part = context.Prompt;
                    SuggestionContext? newContext = suggestion.GetInnerSuggestionContext(context);

                    if (newContext != null)
                    {
                        stringBuilder.Append(part, 0, part.Length - newContext.Value.Prompt.Length);
                    }
                    else
                    {
                        stringBuilder.Append(suggestion.GetCompletion(part));
                    }
                }

                return stringBuilder.ToString();
            }
            finally
            {
                stringBuilder.Dispose();
            }
        }

        /// <summary>
        /// 获取当前候选栈的补全尾部。
        /// </summary>
        /// <returns>候选栈合并后的补全尾部。</returns>
        public string GetCompletionTail()
        {
            Utf16ValueStringBuilder stringBuilder = ZString.CreateStringBuilder();
            try
            {
                foreach (SuggestionSet set in _suggestionSets.Reversed())
                {
                    SuggestionContext context = set.Context;
                    stringBuilder.Append(set.CurrentSelection?.GetCompletionTail(context.Prompt));
                }

                return stringBuilder.ToString();
            }
            finally
            {
                stringBuilder.Dispose();
            }
        }

        /// <summary>
        /// 尝试设置最上层候选集里的当前选中项。
        /// </summary>
        /// <param name="suggestionIndex">要在最上层候选集中选中的候选项索引。</param>
        /// <returns>候选项是否选中成功。</returns>
        public bool SetSuggestionIndex(int suggestionIndex)
        {
            if (_suggestionSets.Count == 0)
            {
                return false;
            }

            if (suggestionIndex < 0 || suggestionIndex > TopmostSuggestionSet.Suggestions.Count)
            {
                return false;
            }

            TopmostSuggestionSet.SelectionIndex = suggestionIndex;
            TopmostSuggestionSet.Context.Prompt = TopmostSuggestion.PrimarySignature;
            return true;
        }

        private SuggestionSet PushSet()
        {
            SuggestionSet set = _setPool.GetObject();
            set.SelectionIndex = -1;
            set.Suggestions.Clear();

            _suggestionSets.Add(set);
            return set;
        }

        private bool PopSet()
        {
            if (_suggestionSets.Count > 0)
            {
                int removeIndex = _suggestionSets.Count - 1;
                SuggestionSet set = _suggestionSets[removeIndex];
                _suggestionSets.RemoveAt(removeIndex);
                _setPool.Release(set);

                return true;
            }

            return false;
        }

        private bool PopSets(int count)
        {
            bool successful = true;
            while (successful && count-- > 0)
            {
                successful &= PopSet();
            }

            return successful;
        }
    }
}
