using System.Collections.Generic;

namespace AlicizaX.Console
{
    public struct ActionContext
    {
        public IAlicizaXConsole Console;

        public IAlicizaXConsole ConsoleInterface
        {
            get => Console;
            set => Console = value;
        }

        public IAlicizaXConsole ActiveConsole => Console;
        public IAlicizaXConsoleOutput ConsoleOutput => Console;
        public IAlicizaXConsoleResponse ConsoleResponse => Console;
        public IAlicizaXConsoleSerialization ConsoleSerialization => Console;
    }

    public static class ActionExecuter
    {
        public static ActionState Execute(this IEnumerator<ICommandAction> action, ActionContext context)
        {
            ActionState state = ActionState.Running;
            bool idle = false;

            void MoveNext()
            {
                if (action.MoveNext())
                {
                    action.Current?.Start(context);
                    idle = action.Current?.StartsIdle ?? false;
                }
                else
                {
                    idle = true;
                    state = ActionState.Complete;
                    action.Dispose();
                }
            }

            while (!idle)
            {
                if (action.Current == null)
                {
                    MoveNext();
                }
                else if (action.Current.IsFinished)
                {
                    action.Current.Finalize(context);
                    MoveNext();
                }
                else
                {
                    idle = true;
                }
            }

            return state;
        }
    }

    public enum ActionState
    {
        Unknown = 0,
        Running = 1,
        Complete = 2
    }

    public interface ICommandAction
    {
        void Start(ActionContext context);
        void Finalize(ActionContext context);

        bool IsFinished { get; }
        bool StartsIdle { get; }
    }
}
