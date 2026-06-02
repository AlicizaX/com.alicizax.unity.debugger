using System;
using System.Collections.Generic;
using System.Reflection;

namespace AlicizaX.Console
{
    /// <summary>
    /// 包装 Action、Func 这类动态委托，让 lambda 可以作为命令使用。
    /// </summary>
    public class LambdaCommandData : CommandData
    {
        private readonly object _lambdaTarget;

        /// <summary>
        /// 根据给定 lambda 创建命令数据。
        /// </summary>
        /// <param name="lambda">用于创建命令的 lambda。为绕过类型系统限制，可能需要先把 lambda 存到 Action 或 Func 这类强类型委托中。</param>
        /// <param name="commandName">命令使用的名称。</param>
        /// <param name="commandDescription">命令说明；没有则为空。</param>
        public LambdaCommandData(Delegate lambda, string commandName, string commandDescription = "")
            : base(lambda.Method, new CommandAttribute(commandName, commandDescription, MonoTargetType.Registry))
        {
            _lambdaTarget = lambda.Target;
        }

        protected override IEnumerable<object> GetInvocationTargets(MethodInfo invokingMethod)
        {
            yield return _lambdaTarget;
        }
    }
}