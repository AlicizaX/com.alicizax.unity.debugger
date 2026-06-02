using System;

namespace AlicizaX.Console.Actions
{
    /// <summary>
    /// 通过委托实现的自定义动作。
    /// 更复杂的动作通常建议新建一个实现 <c>ICommandAction</c> 的动作类。
    /// </summary>
    public class Custom : ICommandAction
    {
        private readonly Func<bool> _isFinished;
        private readonly Func<bool> _startsIdle;
        private readonly Action<ActionContext> _start;
        private readonly Action<ActionContext> _finalize;

        public Custom(
            Func<bool> isFinished,
            Func<bool> startsIdle,
            Action<ActionContext> start,
            Action<ActionContext> finalize
        )
        {
            _isFinished = isFinished;
            _startsIdle = startsIdle;
            _start = start;
            _finalize = finalize;
        }

        public bool IsFinished => _isFinished();
        public bool StartsIdle => _startsIdle();

        public void Start(ActionContext context) { _start(context); }
        public void Finalize(ActionContext context) { _finalize(context); }
    }

}