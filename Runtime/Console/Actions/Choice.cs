using System;
using System.Collections.Generic;
using System.Linq;
using AlicizaX.Console.Utilities;
using Cysharp.Text;
using UnityEngine;

namespace AlicizaX.Console.Actions
{
    /// <summary>
    /// 给用户一组选项，可以用方向键和回车选择。
    /// </summary>
    /// <typeparam name="T">选项的类型。</typeparam>
    public class Choice<T> : Composite
    {
        /// <summary>
        /// Choice 动作的配置。
        /// </summary>
        public struct Config
        {
            public string ItemFormat;
            public string Delimiter;
            public Color SelectedColor;

            public static readonly Config Default = new Config
            {
                ItemFormat = "{0} [{1}]",
                Delimiter = " ",
                SelectedColor = Color.green
            };
        }

        /// <param name="choices">可供选择的选项。</param>
        /// <param name="onSelect">做出选择后要调用的动作。</param>
        public Choice(IEnumerable<T> choices, Action<T> onSelect)
            : this(choices, onSelect, Config.Default)
        { }

        /// <param name="choices">可供选择的选项。</param>
        /// <param name="onSelect">做出选择后要调用的动作。</param>
        /// <param name="config">要使用的配置。</param>
        public Choice(IEnumerable<T> choices, Action<T> onSelect, Config config)
            : base(Generate(choices, onSelect, config))
        { }

        private static IEnumerator<ICommandAction> Generate(IEnumerable<T> choices, Action<T> onSelect, Config config)
        {
            IAlicizaXConsoleSerialization serializer = null;
            IReadOnlyList<T> choiceList = choices as IReadOnlyList<T> ?? choices.ToList();
            KeyCode key = KeyCode.None;
            int choice = 0;

            yield return new GetContext(ctx => serializer = ctx.ConsoleSerialization);

            ICommandAction DrawRow()
            {
                Utf16ValueStringBuilder builder = ZString.CreateStringBuilder();
                try
                {
                    for (int i = 0; i < choiceList.Count; i++)
                    {
                        string item = serializer.Serialize(choiceList[i]);
                        string formattedItem = ZString.Format(config.ItemFormat, item, i == choice ? 'x' : ' ');
                        if (i == choice)
                        {
                            builder.AppendColoredText(formattedItem, config.SelectedColor);
                        }
                        else
                        {
                            builder.Append(formattedItem);
                        }

                        if (i != choiceList.Count - 1)
                        {
                            builder.Append(config.Delimiter);
                        }
                    }

                    return new Value(builder.ToString());
                }
                finally
                {
                    builder.Dispose();
                }
            }

            yield return DrawRow();
            while (key != KeyCode.Return)
            {
                yield return new GetKey(k => key = k);

                switch (key)
                {
                    case KeyCode.LeftArrow: choice--; break;
                    case KeyCode.RightArrow: choice++; break;
                    case KeyCode.DownArrow: choice++; break;
                    case KeyCode.UpArrow: choice--; break;
                }

                choice = (choice + choiceList.Count) % choiceList.Count;
                yield return new RemoveLog();
                yield return DrawRow();
            }

            onSelect(choiceList[choice]);
        }
    }
}
