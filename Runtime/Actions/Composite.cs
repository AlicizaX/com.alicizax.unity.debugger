using System.Collections.Generic;

namespace AlicizaX.Console.Actions
{
    /// <summary>
    /// 把一组动作组合成一个动作。
    /// </summary>
    public class Composite : ICommandAction
    {
        private ActionContext _context;
        private readonly IEnumerator<ICommandAction> _actions;

        public bool IsFinished => _actions.Execute(_context) == ActionState.Complete;
        public bool StartsIdle => false;

        /// <param name="actions">用于创建组合动作的动作序列。</param>
        public Composite(IEnumerator<ICommandAction> actions)
        {
            _actions = actions;
        }

        /// <param name="actions">用于创建组合动作的动作序列。</param>
        public Composite(IEnumerable<ICommandAction> actions) : this(actions.GetEnumerator())
        {

        }

        public void Start(ActionContext context)
        {
            _context = context;
        }

        public void Finalize(ActionContext context) { }
    }
}