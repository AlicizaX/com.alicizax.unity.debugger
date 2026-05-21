using System;

namespace AlicizaX.Console.Actions
{
    /// <summary>
    /// 获取当前命令调用时使用的 <c>ActionContext</c>。
    /// </summary>
    public class GetContext : ICommandAction
    {
        private readonly Action<ActionContext> _onContext;

        public bool IsFinished => true;
        public bool StartsIdle => false;

        /// <param name="onContext">拿到上下文后要调用的动作。</param>
        public GetContext(Action<ActionContext> onContext)
        {
            _onContext = onContext;
        }

        public void Start(ActionContext context) { }

        public void Finalize(ActionContext context)
        {
            _onContext(context);
        }
    }
}