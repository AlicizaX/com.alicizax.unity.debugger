using AlicizaX.Console.Containers;
using AlicizaX.Console.Pooling;
using Cysharp.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AlicizaX.Console
{
    public static class TextProcessing
    {
        public static readonly char[] DefaultLeftScopers = { '<', '[', '(', '{', '"' };
        public static readonly char[] DefaultRightScopers = { '>', ']', ')', '}', '"' };

        /// <summary>
        /// 传给 ReduceScoped 函数的选项。
        /// </summary>
        public struct ReduceScopeOptions
        {
            /// <summary>
            /// 作用域最多可以被去掉外层多少次。
            /// 设为 -1 表示不限制数量。
            /// </summary>
            public int MaxReductions;

            /// <summary>
            /// 是否也处理未闭合的作用域。
            /// 比如使用 () 作为作用域时，下面的文本会变成 "((foo0 foo1)"。
            /// - false："(foo0 foo1"
            /// - true："foo0 foo1"
            /// </summary>
            public bool ReduceIncompleteScope;

            /// <summary>
            /// ReduceScope 函数的默认选项。
            /// </summary>
            public static readonly ReduceScopeOptions Default = new ReduceScopeOptions
            {
                MaxReductions = -1,
                ReduceIncompleteScope = false
            };
        }

        /// <summary>
        /// 传给 SplitScoped 函数的选项。
        /// </summary>
        public struct ScopedSplitOptions
        {
            /// <summary>
            /// 字符串最多拆分成多少项。
            /// 设为 -1 表示不限制数量。
            /// </summary>
            public int MaxCount;

            /// <summary>
            /// 按作用域拆分时是否自动去掉外层作用域。
            /// </summary>
            public bool AutoReduceScope;

            /// <summary>
            /// SplitScoped 函数的默认选项。
            /// </summary>
            public static readonly ScopedSplitOptions Default = new ScopedSplitOptions
            {
                MaxCount = -1,
                AutoReduceScope = false,
            };
        }

        #region GetMaxScopeDepthAtEnd

        public static int GetMaxScopeDepthAtEnd(this string input)
        {
            return input.GetMaxScopeDepthAtEnd(DefaultLeftScopers, DefaultRightScopers);
        }

        public static int GetMaxScopeDepthAtEnd(this string input, char leftScoper, char rightScoper)
        {
            return input.GetMaxScopeDepthAtEnd(leftScoper.AsArraySingle(), rightScoper.AsArraySingle());
        }

        public static int GetMaxScopeDepthAtEnd<T>(this string input, T leftScopers, T rightScopers)
            where T : IReadOnlyList<char>
        {
            return input.GetMaxScopeDepthAt(input.Length - 1, leftScopers, rightScopers);
        }

        #endregion

        #region GetMaxScopeDepthAt

        public static int GetMaxScopeDepthAt(this string input, int cursor)
        {
            return input.GetMaxScopeDepthAt(cursor, DefaultLeftScopers, DefaultRightScopers);
        }

        public static int GetMaxScopeDepthAt(this string input, int cursor, char leftScoper, char rightScoper)
        {
            return input.GetMaxScopeDepthAt(cursor, leftScoper.AsArraySingle(), rightScoper.AsArraySingle());
        }

        public static int GetMaxScopeDepthAt<T>(this string input, int cursor, T leftScopers, T rightScopers)
            where T : IReadOnlyList<char>
        {
            int[] scopes = new int[leftScopers.Count];
            for (int i = 0; i <= cursor; i++)
            {
                if (i == 0 || input[i - 1] != '\\')
                {
                    for (int j = 0; j < leftScopers.Count; j++)
                    {
                        char leftScoper = leftScopers[j];
                        char rightScoper = rightScopers[j];

                        if (input[i] == leftScoper && leftScoper == rightScoper) { scopes[j] = 1 - scopes[j]; }
                        else if (input[i] == leftScoper) { scopes[j]++; }
                        else if (input[i] == rightScoper) { scopes[j]--; }
                    }
                }
            }

            return scopes.Max();
        }

        #endregion

        #region ReduceScope

        public static string ReduceScope(this string input)
        {
            return input.ReduceScope(DefaultLeftScopers, DefaultRightScopers, ReduceScopeOptions.Default);
        }

        public static string ReduceScope(this string input, ReduceScopeOptions options)
        {
            return input.ReduceScope(DefaultLeftScopers, DefaultRightScopers, options);
        }

        public static string ReduceScope(this string input, char leftScoper, char rightScoper)
        {
            return input.ReduceScope(leftScoper.AsArraySingle(), rightScoper.AsArraySingle(), ReduceScopeOptions.Default);
        }

        public static string ReduceScope(this string input, char leftScoper, char rightScoper, ReduceScopeOptions options)
        {
            return input.ReduceScope(leftScoper.AsArraySingle(), rightScoper.AsArraySingle(), options);
        }

        public static string ReduceScope<T>(this string input, T leftScopers, T rightScopers)
            where T : IReadOnlyList<char>
        {
            return ReduceScope(input, leftScopers, rightScopers, ReduceScopeOptions.Default);
        }

        public static string ReduceScope<T>(this string input, T leftScopers, T rightScopers, ReduceScopeOptions options)
            where T : IReadOnlyList<char>
        {
            if (leftScopers.Count != rightScopers.Count)
            {
                throw new ArgumentException("There must be an equal number of corresponding left and right scopers");
            }

            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            if (options.MaxReductions == 0)
            {
                return input;
            }

            // 使用游标指向当前字符串，避免重复处理。
            // 通过子字符串提升性能。
            int leftCursor = 0;
            int rightCursor = input.Length - 1;

            // 判断游标当前是否指向转义字符。
            bool IsEscaped(int cursor)
            {
                return cursor > 0 && input[cursor - 1] == '\\';
            }

            int totalScopeReductions = 0;
            bool workRemaining = true;

            // 持续去掉字符串外层，直到没有可处理内容或达到最大处理次数。
            while (workRemaining && (totalScopeReductions < options.MaxReductions || options.MaxReductions < 0))
            {
                // 如果左游标超过右游标，说明去掉外层后已经是空字符串。
                if (leftCursor > rightCursor)
                {
                    return string.Empty;
                }

                // 移动游标跳过空白字符，模拟 Trim 的效果。
                workRemaining = false;
                while (char.IsWhiteSpace(input[leftCursor]))  { leftCursor++; }
                while (char.IsWhiteSpace(input[rightCursor])) { rightCursor--; }

                // 如果右游标被转义，就在这里结束。
                if (IsEscaped(rightCursor))
                {
                    break;
                }

                // 逐对检查作用域符号。
                for (int i = 0; i < leftScopers.Count; i++)
                {
                    char leftScoper = leftScopers[i];
                    char rightScoper = rightScopers[i];
                    bool sameScoper = leftScoper == rightScoper;

                    // 判断是否找到了有效的一对作用域符号。
                    bool validScoperPair = input[leftCursor] == leftScoper && input[rightCursor] == rightScoper;
                    bool incompleteReduction = false;

                    if (!validScoperPair && options.ReduceIncompleteScope)
                    {
                        // 处理未闭合作用域时，只需要左游标匹配。
                        validScoperPair = input[leftCursor] == leftScoper;
                        incompleteReduction = validScoperPair;
                    }

                    if (validScoperPair)
                    {
                        // 在两个游标之间搜索，确保中间的作用域深度不会降到 0。
                        // 因为这样会把两个独立作用域错误地去掉外层。
                        bool scopeBreaks = false;
                        int currentScope = 1;
                        int leftSearch = leftCursor + 1;
                        int rightSearch = rightCursor - 1;

                        // 只有搜索范围有效时才执行搜索。
                        if (leftSearch <= rightSearch)
                        {
                            // 同符号作用域的逻辑稍有不同，因为无法真正定义作用域深度。
                            // 如果只去掉最外层一对符号后仍无法移除内部作用域符号。
                            // 说明这是破损作用域；否则就是正常情况，并更新搜索范围以允许这种情况。
                            // 正常示例：""foo""
                            // 异常示例："foo1""foo2"
                            if (sameScoper)
                            {
                                // 判断当前位置是否可以跳过同符号作用域的搜索。
                                bool SkipSearch(int cursor)
                                {
                                    if (IsEscaped(cursor))
                                    {
                                        return false;
                                    }

                                    return input[cursor] == leftScoper || char.IsWhiteSpace(input[cursor]);
                                }

                                while (SkipSearch(leftSearch))  { leftSearch++; }
                                while (SkipSearch(rightSearch)) { rightSearch--; }
                            }

                            // 执行搜索。
                            for (int j = leftSearch; j <= rightSearch; j++)
                            {
                                // 忽略转义字符。
                                if (IsEscaped(j))
                                {
                                    continue;
                                }

                                if (sameScoper)
                                {
                                    // 如果在收窄后的范围里还能找到作用域符号，就不能去掉外层。
                                    if (input[j] == leftScoper)
                                    {
                                        scopeBreaks = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    // 普通作用域符号只要检查作用域深度不会回到 0。
                                    // 更新当前作用域深度。
                                    if      (input[j] == leftScoper)  { currentScope++; }
                                    else if (input[j] == rightScoper) { currentScope--; }

                                    // 作用域深度回到 0 就表示作用域断开。
                                    if (currentScope == 0)
                                    {
                                        scopeBreaks = true;
                                        break;
                                    }
                                }
                            }
                        }

                        // 如果作用域一直没有断开，就可以成功去掉外层。
                        if (!scopeBreaks)
                        {
                            // 更新游标并跳出 for 循环。
                            // 处理未闭合范围时不要移动右游标。
                            if (!incompleteReduction)
                            {
                                rightCursor--;
                            }

                            leftCursor++;
                            totalScopeReductions++;
                            workRemaining = true;
                            break;
                        }
                    }
                }
            }

            // 需要时按游标截取字符串，得到最终去掉外层后的结果。
            return totalScopeReductions > 0
                ? input.Substring(leftCursor, rightCursor - leftCursor + 1)
                : input;
        }

        #endregion

        #region SplitScoped

        public static string[] SplitScoped(this string input, char splitChar)
        {
            return input.SplitScoped(splitChar, ScopedSplitOptions.Default);
        }

        public static string[] SplitScoped(this string input, char splitChar, ScopedSplitOptions options)
        {
            return input.SplitScoped(splitChar, DefaultLeftScopers, DefaultRightScopers, options);
        }

        public static string[] SplitScoped(this string input, char splitChar, char leftScoper, char rightScoper)
        {
            return input.SplitScoped(splitChar, leftScoper.AsArraySingle(), rightScoper.AsArraySingle(), ScopedSplitOptions.Default);
        }

        public static string[] SplitScoped(this string input, char splitChar, char leftScoper, char rightScoper, ScopedSplitOptions options)
        {
            return input.SplitScoped(splitChar, leftScoper.AsArraySingle(), rightScoper.AsArraySingle(), options);
        }

        public static string[] SplitScoped<T>(this string input, char splitChar, T leftScopers, T rightScopers)
            where T : IReadOnlyList<char>
        {
            return SplitScoped(input, splitChar, leftScopers, rightScopers, ScopedSplitOptions.Default);
        }

        public static string[] SplitScoped<T>(this string input, char splitChar, T leftScopers, T rightScopers, ScopedSplitOptions options)
            where T : IReadOnlyList<char>
        {
            if (options.AutoReduceScope)
            {
                input = input.ReduceScope(leftScopers, rightScopers);
            }

            if (string.IsNullOrWhiteSpace(input))
            {
                return Array.Empty<string>();
            }

            IEnumerable<int> rawSplitIndices = GetScopedSplitPoints(input, splitChar, leftScopers, rightScopers);
            int[] splitIndices =
                options.MaxCount > 0
                    ? rawSplitIndices.Take(options.MaxCount - 1).ToArray()
                    : rawSplitIndices.ToArray();

            // 没有发生拆分时返回单个数组。
            if (splitIndices.Length == 0)
            {
                return new[] { input };
            }

            string[] splitString = new string[splitIndices.Length + 1];
            int lastSplitIndex = 0;
            for (int i = 0; i < splitIndices.Length; i++)
            {
                splitString[i] = input.Substring(lastSplitIndex, splitIndices[i] - lastSplitIndex).Trim();
                lastSplitIndex = splitIndices[i] + 1;
            }

            splitString[splitIndices.Length] = input.Substring(lastSplitIndex).Trim();
            return splitString;
        }

        #endregion

        #region GetScopedSplitPoints

        public static IEnumerable<int> GetScopedSplitPoints<T>(string input, char splitChar, T leftScopers, T rightScopers)
            where T : IReadOnlyList<char>
        {
            return GetScopedSplitPoints(input, splitChar, leftScopers, rightScopers, ScopedSplitOptions.Default);
        }

        public static IEnumerable<int> GetScopedSplitPoints<T>(
            string input, char splitChar, T leftScopers, T rightScopers, ScopedSplitOptions options)
            where T : IReadOnlyList<char>
        {
            if (leftScopers.Count != rightScopers.Count)
            {
                throw new ArgumentException("There must be an equal number of corresponding left and right scopers");
            }

            int[] scopes = new int[leftScopers.Count];
            for (int i = 0; i < input.Length; i++)
            {
                if (i == 0 || input[i - 1] != '\\')
                {
                    for (int j = 0; j < leftScopers.Count; j++)
                    {
                        char leftScoper = leftScopers[j];
                        char rightScoper = rightScopers[j];

                        if (input[i] == leftScoper && leftScoper == rightScoper) { scopes[j] = 1 - scopes[j]; }
                        else if (input[i] == leftScoper) { scopes[j]++; }
                        else if (input[i] == rightScoper) { scopes[j]--; }
                    }
                }

                if (input[i] == splitChar && scopes.All(x => x == 0))
                {
                    yield return i;
                }
            }
        }

        #endregion

        public static bool CanSplitScoped(this string input, char splitChar)
        {
            return input.CanSplitScoped(splitChar, DefaultLeftScopers, DefaultRightScopers);
        }

        public static bool CanSplitScoped(this string input, char splitChar, char leftScoper, char rightScoper)
        {
            return input.CanSplitScoped(splitChar, leftScoper.AsArraySingle(), rightScoper.AsArraySingle());
        }

        public static bool CanSplitScoped<T>(this string input, char splitChar, T leftScopers, T rightScopers)
            where T : IReadOnlyList<char>
        {
            return GetScopedSplitPoints(input, splitChar, leftScopers, rightScopers).Any();
        }

        public static string SplitFirst(this string input, char splitChar)
        {
            return input.SplitScopedFirst(splitChar, Array.Empty<char>(), Array.Empty<char>());
        }

        public static string SplitScopedFirst(this string input, char splitChar)
        {
            return input.SplitScopedFirst(splitChar, DefaultLeftScopers, DefaultRightScopers);
        }

        public static string SplitScopedFirst(this string input, char splitChar, char leftScoper, char rightScoper)
        {
            return input.SplitScopedFirst(splitChar, leftScoper.AsArraySingle(), rightScoper.AsArraySingle());
        }

        public static string SplitScopedFirst<T>(this string input, char splitChar, T leftScopers, T rightScopers)
            where T : IReadOnlyList<char>
        {
            IEnumerable<int> splitPoints = GetScopedSplitPoints(input, splitChar, leftScopers, rightScopers);
            foreach (int splitPoint in splitPoints)
            {
                return input.Substring(0, splitPoint).Trim();
            }

            return input;
        }

        public static string UnescapeText(this string input, char escapeChar) { return input.UnescapeText(escapeChar.AsArraySingle()); }
        public static string UnescapeText<T>(this string input, T escapeChars)
            where T : IReadOnlyCollection<char>
        {
            foreach (char escapeChar in escapeChars)
            {
                input = input.Replace($"\\{escapeChar}", escapeChar.ToString());
            }

            return input;
        }

        public static string ReverseItems(this string input, char splitChar)
        {
            int lastSplit = input.Length;
            Utf16ValueStringBuilder buffer = StringBuilderPool.GetStringBuilder(input.Length);

            for (int i = input.Length - 1; i >= 0; i--)
            {
                if (input[i] == splitChar)
                {
                    int substringIndex = i + 1;
                    if (substringIndex < input.Length)
                    {
                        buffer.Append(input, substringIndex, lastSplit - substringIndex);
                    }

                    buffer.Append(splitChar);
                    lastSplit = i;
                }
                else if (i == 0)
                {
                    buffer.Append(input, 0, lastSplit);
                }
            }

            return StringBuilderPool.ReleaseAndToString(buffer);
        }
    }
}
