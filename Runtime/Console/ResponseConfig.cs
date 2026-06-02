namespace AlicizaX.Console
{
    /// <summary>
    /// 在控制台请求用户输入时使用的配置。
    /// </summary>
    public struct ResponseConfig
    {
        // 输入框里显示的提示文字。
        public string InputPrompt;

        // 是否把输入内容回显到控制台。
        public bool LogInput;

        public static readonly ResponseConfig Default = new ResponseConfig
        {
            InputPrompt = "Enter input...",
            LogInput = true
        };
    }
}