namespace AlicizaX.Console
{
    public interface ICommandConsoleView
    {
        string InputValue { get; set; }
        bool InputEnabled { get; set; }
        bool IsInputFocused { get; }

        void SetPlaceholder(string text);
        void SetLogText(string text);
        void SetGhostText(string richText);
        void SetPopupText(string richText, bool visible);
        void SetJobCounter(string text, bool visible);
        void ScrollToLatest();
        void FocusInput();
    }
}
