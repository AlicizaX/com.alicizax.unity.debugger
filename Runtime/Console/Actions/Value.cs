namespace AlicizaX.Console.Actions
{
    /// <summary>
    /// 序列化一个值并输出到控制台。
    /// </summary>
    public class Value : ICommandAction
    {
        private readonly object _value;
        private readonly bool _newline;

        public bool IsFinished => true;
        public bool StartsIdle => false;

        /// <param name="value">要输出到控制台的值。</param>
        /// <param name="newline">是否把值输出到新的一行。</param>
        public Value(object value, bool newline = true)
        {
            _value = value;
            _newline = newline;
        }

        public void Start(ActionContext context) { }

        public void Finalize(ActionContext context)
        {
            IAlicizaXConsoleSerialization serializer = context.ConsoleSerialization;
            IAlicizaXConsoleOutput output = context.ConsoleOutput;
            string serialized = _value as string ?? serializer.Serialize(_value);
            output.LogToConsole(serialized, _newline);
        }
    }
}
