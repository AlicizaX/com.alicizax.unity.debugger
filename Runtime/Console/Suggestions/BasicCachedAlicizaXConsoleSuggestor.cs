using System.Collections.Generic;
using System.Linq;

namespace AlicizaX.Console
{
    /// <summary>
    /// 会缓存已创建 IAlicizaXConsoleSuggestion 对象的 IAlicizaXConsoleSuggestor。
    /// </summary>
    /// <typeparam name="TItem">用于生成候选项的条目类型。</typeparam>
    public abstract class BasicCachedAlicizaXConsoleSuggestor<TItem> : IAlicizaXConsoleSuggestor
    {
        private readonly Dictionary<TItem, IAlicizaXConsoleSuggestion> _suggestionCache = new Dictionary<TItem, IAlicizaXConsoleSuggestion>();

        /// <summary>
        /// 给定上下文是否可以生成候选项。
        /// </summary>
        /// <param name="context"></param>
        /// <param name="options">提示器使用的选项。</param>
        /// <returns>是否可以生成候选项。</returns>
        protected abstract bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options);

        /// <summary>
        /// 把一项数据转成候选项。
        /// </summary>
        /// <param name="item">要转换成候选项的条目。</param>
        /// <returns>转换后的候选项。</returns>
        protected abstract IAlicizaXConsoleSuggestion ItemToSuggestion(TItem item);

        /// <summary>
        /// 获取指定上下文下的条目。
        /// </summary>
        /// <param name="context">用于生成条目的上下文。</param>
        /// <param name="options">提示器使用的选项。</param>
        /// <returns>生成的条目。</returns>
        protected abstract IEnumerable<TItem> GetItems(SuggestionContext context, SuggestorOptions options);

        /// <summary>
        /// 判断给定候选项是否匹配给定上下文。
        /// 可重写此方法来移除默认过滤或增加自定义过滤。
        /// </summary>
        /// <param name="context">用于测试候选项的上下文。</param>
        /// <param name="suggestion">要测试的候选项。</param>
        /// <param name="options">测试候选项时使用的选项。</param>
        /// <returns>候选项是否匹配上下文。</returns>
        protected virtual bool IsMatch(SuggestionContext context, IAlicizaXConsoleSuggestion suggestion, SuggestorOptions options)
        {
            return SuggestorUtilities.IsCompatible(context.Prompt, suggestion.PrimarySignature, options);
        }

        public IEnumerable<IAlicizaXConsoleSuggestion> GetSuggestions(SuggestionContext context, SuggestorOptions options)
        {
            if (!CanProvideSuggestions(context, options))
            {
                return Enumerable.Empty<IAlicizaXConsoleSuggestion>();
            }

            return GetItems(context, options)
                .Select(ItemToSuggestionCached)
                .Where(suggestion => IsMatch(context, suggestion, options));
        }

        private IAlicizaXConsoleSuggestion ItemToSuggestionCached(TItem item)
        {
            if (_suggestionCache.TryGetValue(item, out IAlicizaXConsoleSuggestion suggestion))
            {
                return suggestion;
            }

            return _suggestionCache[item] = ItemToSuggestion(item);
        }
    }
}