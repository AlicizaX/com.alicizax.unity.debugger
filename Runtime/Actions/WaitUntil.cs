using System;

namespace AlicizaX.Console.Actions
{
    /// <summary>
    /// 等待到给定条件满足。
    /// </summary>
    public class WaitUntil : WaitWhile
    {
        /// <param name="condition">要等待的条件。</param>
        public WaitUntil(Func<bool> condition) : base(() => !condition())
        {

        }
    }
}