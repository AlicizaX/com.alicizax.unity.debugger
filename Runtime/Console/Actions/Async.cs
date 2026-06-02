using System;
using System.Threading.Tasks;

namespace AlicizaX.Console.Actions
{
    /// <summary>
    /// 把异步 Task 转成一个动作。
    /// </summary>
    public class Async : ICommandAction
    {
        private readonly Task _task;

        public bool IsFinished => _task.IsCompleted ||
                                  _task.IsCanceled ||
                                  _task.IsFaulted;
        public bool StartsIdle => false;

        /// <param name="task">要转换的异步 Task。</param>
        public Async(Task task)
        {
            _task = task;
        }

        public void Start(ActionContext context) { }

        public void Finalize(ActionContext context)
        {
            if (_task.IsFaulted)
            {
                throw _task.Exception.InnerException;
            }
            if (_task.IsCanceled)
            {
                throw new TaskCanceledException();
            }
        }

    }

    /// <summary>
    /// 把异步 Task 转成一个动作。
    /// </summary>
    /// <typeparam name="T">要转换的 Task 返回类型。</typeparam>
    public class Async<T> : ICommandAction
    {
        private readonly Task<T> _task;
        private readonly Action<T> _onResult;

        public bool IsFinished => _task.IsCompleted ||
                                  _task.IsCanceled ||
                                  _task.IsFaulted;
        public bool StartsIdle => false;

        /// <param name="task">要转换的异步 Task。</param>
        /// <param name="onResult">Task 完成后要调用的动作。</param>
        public Async(Task<T> task, Action<T> onResult)
        {
            _task = task;
            _onResult = onResult;
        }

        public void Start(ActionContext context) { }

        public void Finalize(ActionContext context)
        {
            if (_task.IsFaulted)
            {
                throw _task.Exception.InnerException;
            }
            if (_task.IsCanceled)
            {
                throw new TaskCanceledException();
            }

            _onResult(_task.Result);
        }
    }
}