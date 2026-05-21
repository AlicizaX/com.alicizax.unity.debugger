using System;

namespace AlicizaX.Console.Actions
{
    /// <summary>
    /// 把用户接下来在控制台输入的一行文本当作用户 
    /// 响应，而不是把它当作命令执行。
    /// </summary>
    public class ReadLine : ICommandAction
    {
        private readonly Action<string> _getInput;
        private readonly ResponseConfig _config;
        private IAlicizaXConsoleResponse _console;
        private string _response;

        public bool IsFinished => _response != null;

        public bool StartsIdle => true;

        /// <param name="getInput">返回用户输入内容的委托。</param>
        /// <param name="config">提供给响应流程的配置。</param>
        /// <exception cref="ArgumentNullException"></exception>
        public ReadLine(Action<string> getInput, ResponseConfig config)
        {
            // 校验。
            if (getInput == null)
            {
                throw new ArgumentNullException(nameof(getInput));
            }

            // 设置字段。
            _getInput = getInput;
            _config = config;
            _console = null;
            _response = null;
        }

        /// <param name="getInput">返回用户输入内容的委托。</param>
        /// <param name="config">提供给响应流程的配置。</param>
        /// <exception cref="ArgumentNullException"></exception>
        public ReadLine(Action<string> getInput) : this(getInput, ResponseConfig.Default)
        {

        }

        public void Finalize(ActionContext context)
        {
            _getInput(_response); // push value to the caller
        }

        public void Start(ActionContext context)
        {
            _response = null; // reset flag
            _console = context.ConsoleResponse;
            _console.BeginResponse(OnResponseSubmittedHandler, _config);
        }

        private void OnResponseSubmittedHandler(string response)
        {
            _response = response; // changes IsFinished flag
        }
    }
}
