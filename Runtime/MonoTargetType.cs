namespace AlicizaX.Console
{
    /// <summary>
    /// 指定非静态 MonoBehaviour 命令的目标查找方式。
    /// </summary>
    public enum MonoTargetType
    {
        /// <summary>
        /// 以找到的第一个 MonoBehaviour 实例为目标。
        /// </summary>
        Single = 0,

        /// <summary>
        /// 以找到的所有 MonoBehaviour 实例为目标。
        /// </summary>
        All = 1,

        /// <summary>
        /// 以 AlicizaXConsoleRegistry 中注册的所有实例为目标；可通过 <c>AlicizaX.Console.AlicizaXConsoleRegistry.RegisterObject</c> 添加实例。
        /// 非 MonoBehaviour 命令唯一支持的目标类型。
        /// </summary>
        Registry = 2,

        /// <summary>
        /// 如果实例还不存在就自动创建并放入注册表，后续函数调用都会使用它。
        /// </summary>
        Singleton = 3,

        /// <summary>
        /// 以找到的第一个 MonoBehaviour 实例为目标，搜索时也包含未激活对象。
        /// </summary>
        SingleInactive = 4,

        /// <summary>
        /// 以找到的所有 MonoBehaviour 实例为目标，搜索时也包含未激活对象。
        /// </summary>
        AllInactive = 5,

        /// <summary>
        /// 调用命令时，第一个参数会指定目标实例。
        /// </summary>
        Argument = 6,

        /// <summary>
        /// 调用命令时，第一个数组参数会指定目标实例列表。
        /// </summary>
        ArgumentMulti = 7
    }
}