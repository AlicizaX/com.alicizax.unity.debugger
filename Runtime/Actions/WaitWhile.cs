using System;

namespace AlicizaX.Console.Actions
{
    /// <summary>
    /// 在给定条件满足期间持续等待。
    /// </summary>
    public class WaitWhile : ICommandAction
    {
        private readonly Func<bool> _condition;

        public bool IsFinished => !_condition();
        public bool StartsIdle => true;

        /// <param name="condition">要等待的条件。</param>
        public WaitWhile(Func<bool> condition)
        {
            _condition = condition;
        }


        public void Start(ActionContext context) { }
        public void Finalize(ActionContext context) { }
    }
}