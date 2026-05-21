namespace AlicizaX.Console.Actions
{
    /// <summary>
    /// 从控制台移除最近一条日志。
    /// </summary>
    public class RemoveLog : ICommandAction
    {
        public bool IsFinished => true;
        public bool StartsIdle => false;

        public void Start(ActionContext context) { }

        public void Finalize(ActionContext context)
        {
            context.ActiveConsole.RemoveLogTrace();
        }
    }
}
