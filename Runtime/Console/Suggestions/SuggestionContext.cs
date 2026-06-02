using System;
using System.Collections.Generic;

namespace AlicizaX.Console
{
    /// <summary>
    /// 用于提供候选项的上下文。
    /// </summary>
    public struct SuggestionContext
    {
        /// <summary>
        /// 提示上下文的深度。
        /// </summary>
        public int Depth;

        /// <summary>
        /// 当前深度用于生成候选项的提示文本。
        /// </summary>
        public string Prompt;

        /// <summary>
        /// 生成候选项时要针对的指定类型；没有则不限制。
        /// </summary>
        public Type TargetType;

        /// <summary>
        /// 加入提示上下文的所有标签，提示器可以按需查询。
        /// </summary>
        public IAlicizaXConsoleSuggestorTag[] Tags;

        /// <summary>
        /// 检查指定标签是否存在。
        /// </summary>
        /// <typeparam name="T">要检查的标签。</typeparam>
        /// <returns>标签是否存在。</returns>
        public bool HasTag<T>() where T : IAlicizaXConsoleSuggestorTag
        {
            if (Tags == null)
            {
                return false;
            }

            // foreach 循环性能更好。
            // ReSharper：本处禁用“循环可转查询”的提示。
            foreach (IAlicizaXConsoleSuggestorTag tag in Tags)
            {
                if (tag is T)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 从上下文中获取指定标签。
        /// </summary>
        /// <typeparam name="T">要从上下文中获取的标签。</typeparam>
        /// <returns>找到时返回标签；找不到会抛出 KeyNotFoundException。</returns>
        public T GetTag<T>() where T : IAlicizaXConsoleSuggestorTag
        {
            if (Tags != null)
            {
                foreach (IAlicizaXConsoleSuggestorTag tag in Tags)
                {
                    if (tag is T foundTag)
                    {
                        return foundTag;
                    }
                }
            }

            throw new KeyNotFoundException($"No tags of type {typeof(T)} could be found.");
        }

        /// <summary>
        /// 从上下文中获取指定标签的所有实例。
        /// </summary>
        /// <typeparam name="T">要从上下文中获取的标签。</typeparam>
        /// <returns>上下文里的标签。</returns>
        public IEnumerable<T> GetTags<T>() where T : IAlicizaXConsoleSuggestorTag
        {
            if (Tags != null)
            {
                foreach (IAlicizaXConsoleSuggestorTag tag in Tags)
                {
                    if (tag is T foundTag)
                    {
                        yield return foundTag;
                    }
                }
            }
        }
    }
}