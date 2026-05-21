namespace AlicizaX.Console
{
    /// <summary>
    /// 提示器生成候选项时使用的选项。
    /// </summary>
    public struct SuggestorOptions
    {
        /// <summary>
        /// 生成候选项时是否区分提示文本的大小写。
        /// </summary>
        public bool CaseSensitive;

        /// <summary>
        /// 生成候选项时是否使用模糊搜索。
        /// </summary>
        public bool Fuzzy;

        /// <summary>
        /// 是否把同一候选项的多个重载合并成
        /// 尽量把可选部分合并成一个候选项。
        /// </summary>
        public bool CollapseOverloads;
    }
}