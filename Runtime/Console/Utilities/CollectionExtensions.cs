using System;
using System.Collections.Generic;
using System.Linq;

namespace AlicizaX.Console.Utilities
{
    public static class CollectionExtensions
    {
        /// <summary>反转字典中键和值的关系。</summary>
        /// <returns>键值关系反转后的字典。</returns>
        public static Dictionary<TValue, TKey> Invert<TKey, TValue>(this IDictionary<TKey, TValue> source)
        {
            Dictionary<TValue, TKey> dictionary = new Dictionary<TValue, TKey>();
            foreach (KeyValuePair<TKey, TValue> item in source)
            {
                if (!dictionary.ContainsKey(item.Value))
                {
                    dictionary.Add(item.Value, item.Key);
                }
            }

            return dictionary;
        }

        /// <summary>从已有数组中取出一段子数组。</summary>
        /// <param name="index">子数组开始截取的位置。</param>
        /// <param name="length">子数组长度。</param>
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        /// <summary>跳过序列最后一个元素。</summary>
        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source)
        {
            using (IEnumerator<T> enumurator = source.GetEnumerator())
            {
                if (enumurator.MoveNext())
                {
                    for (T value = enumurator.Current; enumurator.MoveNext(); value = enumurator.Current)
                    {
                        yield return value;
                    }
                }
            }
        }

        /// <summary>反转序列顺序。</summary>
        public static IEnumerable<T> Reversed<T>(this IReadOnlyList<T> source)
        {
            for (int i = source.Count - 1; i >= 0; i--)
            {
                yield return source[i];
            }
        }

        /// <summary>
        /// 按自定义规则创建去重后的序列。
        /// </summary>
        /// <typeparam name="TValue">IEnumerable 的元素类型。</typeparam>
        /// <typeparam name="TDistinct">用于去重判断的值类型。</typeparam>
        /// <param name="source">源 IEnumerable。</param>
        /// <param name="predicate">自定义去重项生成器。</param>
        /// <returns>去重后的序列。</returns>
        public static IEnumerable<TValue> DistinctBy<TValue, TDistinct>(this IEnumerable<TValue> source, Func<TValue, TDistinct> predicate)
        {
            HashSet<TDistinct> set = new HashSet<TDistinct>();
            foreach (TValue value in source)
            {
                if (set.Add(predicate(value)))
                {
                    yield return value;
                }
            }
        }

        public static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }

        public static T LastOr<T>(this IEnumerable<T> source, T value)
        {
            try
            {
                return source.Last();
            }
            catch (InvalidOperationException)
            {
                return value;
            }
        }

        public static unsafe void InsertionSortBy<T>(this IList<T> collection, Func<T, int> keySelector)
        {
            const int maxStackSize = 512;

            if (collection.Count <= maxStackSize)
            {
                int* keyBuffer = stackalloc int[collection.Count];
                InsertionSortBy(collection, keySelector, keyBuffer);
            }
            else
            {
                int[] keyArray = new int[collection.Count];
                fixed (int* keyBuffer = keyArray)
                {
                    InsertionSortBy(collection, keySelector, keyBuffer);
                }
            }
        }

        private static unsafe void InsertionSortBy<T>(this IList<T> collection, Func<T, int> keySelector, int* keyBuffer)
        {
            int n = collection.Count;
            for (int i = 0; i < n; i++)
            {
                keyBuffer[i] = keySelector(collection[i]);
            }

            for (int i = 1; i < n; i++)
            {
                T item = collection[i];
                int key = keyBuffer[i];
                int j = i - 1;

                while (j >= 0 && keyBuffer[j] > key)
                {
                    collection[j + 1] = collection[j];
                    keyBuffer[j + 1] = keyBuffer[j];
                    j -= 1;
                }

                collection[j + 1] = item;
                keyBuffer[j + 1] = key;
            }
        }
    }
}