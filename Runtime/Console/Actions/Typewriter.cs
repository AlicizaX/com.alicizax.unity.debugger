using Cysharp.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AlicizaX.Console.Actions
{
    /// <summary>
    /// 把消息逐字输出到控制台。
    /// </summary>
    public class Typewriter : Composite
    {
        /// <summary>
        /// Typewriter 动作的配置。
        /// </summary>
        public struct Config
        {
            public enum ChunkType
            {
                Character,
                Word,
                Line
            }

            public float PrintInterval;
            public ChunkType Chunks;

            public static readonly Config Default = new Config
            {
                PrintInterval = 0f,
                Chunks = ChunkType.Character
            };
        }

        private static readonly Regex WhiteRegex = new Regex(@"(?<=[\s+])", RegexOptions.Compiled);
        private static readonly Regex LineRegex = new Regex(@"(?<=[\n+])", RegexOptions.Compiled);

        /// <param name="message">要显示到控制台的消息。</param>
        public Typewriter(string message)
            : this(message, Config.Default)
        { }

        /// <param name="message">要显示到控制台的消息。</param>
        /// <param name="config">要使用的配置。</param>
        public Typewriter(string message, Config config)
            : base(Generate(message, config))
        { }

        private static IEnumerator<ICommandAction> Generate(string message, Config config)
        {
            string[] chunks;
            switch (config.Chunks)
            {
                case Config.ChunkType.Character: chunks = message.Select(c => c.ToString()).ToArray(); break;
                case Config.ChunkType.Word: chunks = WhiteRegex.Split(message); break;
                case Config.ChunkType.Line: chunks = LineRegex.Split(message); break;
                default: throw new ArgumentException(ZString.Format("Chunk type {0} is not supported.", config.Chunks));
            }

            for (int i = 0; i < chunks.Length; i++)
            {
                yield return new WaitRealtime(config.PrintInterval);
                yield return new Value(chunks[i], i == 0);
            }
        }
    }
}
