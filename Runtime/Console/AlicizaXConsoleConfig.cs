using System;
using UnityEngine;

namespace AlicizaX.Console
{
    [Serializable]
    public sealed class AlicizaXConsoleConfig
    {
        public KeyCode SubmitCommandKey = KeyCode.Return;
        public ModifierKeyCombo SelectNextSuggestionKey = KeyCode.Tab;
        public ModifierKeyCombo SelectPreviousSuggestionKey = new ModifierKeyCombo { Key = KeyCode.Tab, Shift = true };
        public KeyCode NextCommandKey = KeyCode.UpArrow;
        public KeyCode PreviousCommandKey = KeyCode.DownArrow;
        public ModifierKeyCombo CancelActionsKey = new ModifierKeyCombo { Key = KeyCode.C, Ctrl = true };

        public bool VerboseErrors;
        public LoggingThreshold VerboseLogging = LoggingThreshold.Never;
        public LoggingThreshold LoggingLevel = LoggingThreshold.Always;
        public AutoScrollOptions AutoScroll = AutoScrollOptions.OnInvoke;
        public string[] CommandAssemblyNames = { AlicizaXConsoleProcessor.DefaultCommandAssemblyName };

        public bool EnableAutocomplete = true;
        public bool ShowPopupDisplay = true;
        public SortOrder SuggestionDisplayOrder = SortOrder.Descending;
        public int MaxSuggestionDisplaySize = -1;
        public bool UseFuzzySearch;
        public bool CaseSensitiveSearch = true;
        public bool CollapseSuggestionOverloads = true;

        public bool ShowCurrentJobs = true;
        public bool BlockOnAsync;

        public bool StoreCommandHistory = true;
        public bool StoreDuplicateCommands = true;
        public bool StoreAdjacentDuplicateCommands;
        public int CommandHistorySize = -1;

        public int MaxStoredLogs = 1024;
        public int MaxLogSize = 8192;
        public bool ShowInitLogs = true;
    }
}
