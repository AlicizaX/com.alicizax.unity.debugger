using AlicizaX.Console.Pooling;
using AlicizaX.Console.Utilities;
using Cysharp.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using ColorExtensions = AlicizaX.Console.Utilities.ColorExtensions;

namespace AlicizaX.Console
{
    public sealed class AlicizaXConsoleController : IAlicizaXConsole
    {
        private static readonly Color ColorCommandLog = new Color(0f, 1f, 1f);
        private static readonly Color ColorSelectedSugg = new Color(1f, 1f, 0.55f);
        private static readonly Color ColorSuggestion = Color.gray;
        private static readonly Color ColorError = Color.red;
        private static readonly Color ColorSuccess = Color.green;
        private static readonly Color ColorReturnDefault = Color.white;
        private const string CommandLogFormat = "> {0}";

        private readonly AlicizaXConsoleConfig _config;
        private readonly AlicizaXConsoleSerializer _serializer = new AlicizaXConsoleSerializer();
        private readonly List<string> _previousCommands = new List<string>();
        private readonly List<Task> _currentTasks = new List<Task>();
        private readonly List<IEnumerator<ICommandAction>> _currentActions = new List<IEnumerator<ICommandAction>>();

        private SuggestionStack _suggestionStack;
        private LogStorage _logStorage;
        private LogQueue _logQueue;
        private ICommandConsoleView _view;

        private int _selectedPreviousCommandIndex = -1;
        private string _currentInput = string.Empty;
        private bool _isGeneratingTable;
        private bool _consoleRequiresFlush;
        private bool _isHandlingUserResponse;
        private ResponseConfig _currentResponseConfig;
        private Action<string> _onSubmitResponseCallback;
        private Type _voidTaskType;
        private string _placeholder = "Enter Command...";
        private bool _initialized;

        public event Action OnStateChange;
        public event Action<string> OnInvoke;
        public event Action OnClear;
        public event Action<AlicizaXConsoleLog> OnLog;
        public event Action<SuggestionSet> OnSuggestionSetGenerated;

        public AlicizaXConsoleController(AlicizaXConsoleConfig config)
        {
            _config = config ?? new AlicizaXConsoleConfig();
        }

        public bool IsActive { get; private set; }
        public bool AreActionsExecuting => _currentActions.Count > 0;
        public bool IsHandlingUserResponse => _isHandlingUserResponse;

        private bool IsBlockedByAsync =>
            (_config.BlockOnAsync && (_currentTasks.Count > 0 || _currentActions.Count > 0))
            && !_isHandlingUserResponse;

        public bool VerboseErrors
        {
            get => _config.VerboseErrors;
            set => _config.VerboseErrors = value;
        }

        public LoggingThreshold VerboseLogging
        {
            get => _config.VerboseLogging;
            set => _config.VerboseLogging = value;
        }

        public LoggingThreshold LoggingLevel
        {
            get => _config.LoggingLevel;
            set => _config.LoggingLevel = value;
        }

        public int MaxStoredLogs
        {
            get => _config.MaxStoredLogs;
            set
            {
                _config.MaxStoredLogs = value;
                if (_logStorage != null) { _logStorage.MaxStoredLogs = value; }
                if (_logQueue != null) { _logQueue.MaxStoredLogs = value; }
            }
        }

        public void Initialize()
        {
            if (_initialized) { return; }

            _voidTaskType = typeof(Task<>).MakeGenericType(Type.GetType("System.Threading.Tasks.VoidTaskResult"));
            InitializeLogging();
            InitializeSuggestionStack();

            if (!AlicizaXConsoleProcessor.TableGenerated)
            {
                AlicizaXConsoleProcessor.GenerateCommandTableFromAssemblyNames(_config.CommandAssemblyNames, true);
                _isGeneratingTable = true;
                SetInputEnabled(false);
                SetPlaceholder("Loading...");
            }

            IsActive = true;
            _initialized = true;
        }

        public void Shutdown()
        {
            if (!_initialized) { return; }

            CancelAllActions();
            _currentTasks.Clear();
            DetachView();
            IsActive = false;
            _initialized = false;
        }

        public void AttachView(ICommandConsoleView view)
        {
            _view = view;
            if (_view == null) { return; }

            _view.SetLogText(_logStorage != null ? _logStorage.GetLogString() : string.Empty);
            _view.SetPlaceholder(_placeholder);
            _view.InputValue = _currentInput ?? string.Empty;
            UpdateSuggestionUi();
            UpdateJobCounterUi();

            if (_isGeneratingTable || AlicizaXConsoleProcessor.TableIsGenerating)
            {
                SetInputEnabled(false);
                SetPlaceholder("Loading...");
            }
            else if (IsBlockedByAsync)
            {
                SetInputEnabled(false);
                SetPlaceholder("Executing async command...");
            }
            else if (_isHandlingUserResponse)
            {
                SetInputEnabled(true);
                SetPlaceholder(_currentResponseConfig.InputPrompt);
            }
            else
            {
                SetInputEnabled(true);
                SetPlaceholder("Enter Command...");
            }

            FlushToView();
        }

        public void DetachView()
        {
            _view = null;
        }

        public void Tick()
        {
            if (!_initialized) { return; }

            ProcessAsyncTasks();
            ProcessActions();
            HandleAsyncJobCounter();
            FlushQueuedLogs();
            FlushToView();
            UpdateTableGenerationState();
            UpdateBlockedState();
        }

        public void FocusInput()
        {
            _view?.FocusInput();
        }

        public void OnInputChanged(string value)
        {
            _currentInput = value ?? string.Empty;

            if (_selectedPreviousCommandIndex >= 0
                && _selectedPreviousCommandIndex < _previousCommands.Count
                && _currentInput.Trim() != _previousCommands[_previousCommands.Count - _selectedPreviousCommandIndex - 1])
            {
                _selectedPreviousCommandIndex = -1;
            }

            if (_config.EnableAutocomplete)
            {
                UpdateSuggestions();
            }
        }

        public void InvokeCommandFromInput()
        {
            string userInput = _view != null ? _view.InputValue : _currentInput;
            if (string.IsNullOrWhiteSpace(userInput)) { return; }

            string command = userInput.Trim();
            if (_isHandlingUserResponse)
            {
                HandleUserResponse(command);
                return;
            }

            InvokeCommand(command);
            OverrideInput(string.Empty);
            StoreCommand(command);
        }

        public object InvokeCommand(string command)
        {
            object commandResult = null;
            if (string.IsNullOrWhiteSpace(command))
            {
                OverrideInput(string.Empty);
                return null;
            }

            LogUserInput(command);

            string logTrace = string.Empty;
            try
            {
                commandResult = AlicizaXConsoleProcessor.InvokeCommand(command);
                switch (commandResult)
                {
                    case Task task:
                        _currentTasks.Add(task);
                        break;
                    case IEnumerator<ICommandAction> action:
                        StartAction(action);
                        break;
                    case IEnumerable<ICommandAction> actions:
                        StartAction(actions.GetEnumerator());
                        break;
                    default:
                        logTrace = Serialize(commandResult);
                        break;
                }
            }
            catch (System.Reflection.TargetInvocationException e)
            {
                logTrace = GetInvocationErrorMessage(e.InnerException);
            }
            catch (Exception e)
            {
                logTrace = GetErrorMessage(e);
            }

            LogToConsole(logTrace);
            OnInvoke?.Invoke(command);

            if (_config.AutoScroll == AutoScrollOptions.OnInvoke)
            {
                _view?.ScrollToLatest();
            }

            return commandResult;
        }

        public async Task InvokeExternalCommandsAsync(string filePath)
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                while (!reader.EndOfStream)
                {
                    string command = await reader.ReadLineAsync();
                    if (InvokeCommand(command) is Task ret)
                    {
                        await ret;
                        ProcessAsyncTasks();
                    }
                }
            }
        }

        public async Task InvokeCommandsAsync(IEnumerable<string> commands)
        {
            foreach (string command in commands)
            {
                if (InvokeCommand(command) is Task ret)
                {
                    await ret;
                    ProcessAsyncTasks();
                }
            }
        }

        public void BeginResponse(Action<string> onSubmitResponseCallback, ResponseConfig config)
        {
            if (onSubmitResponseCallback == null)
            {
                throw new ArgumentNullException(nameof(onSubmitResponseCallback));
            }

            _onSubmitResponseCallback = onSubmitResponseCallback;
            _currentResponseConfig = config;
            _isHandlingUserResponse = true;
            OnStateChange?.Invoke();

            SetInputEnabled(true);
            SetPlaceholder(_currentResponseConfig.InputPrompt);
            FocusInput();
        }

        public void StartAction(IEnumerator<ICommandAction> action)
        {
            _currentActions.Add(action);
            ProcessActions();
        }

        public void CancelAllActions()
        {
            _currentActions.Clear();
        }

        public string Serialize(object value)
        {
            string result = _serializer.SerializeFormatted(value, null);
            if (!string.IsNullOrWhiteSpace(result))
            {
                result = result.ColorText(ColorReturnDefault);
            }

            return result;
        }

        public void ClearConsole()
        {
            _logStorage?.Clear();
            _logQueue?.Clear();
            ClearBuffers();
            _view?.SetLogText(string.Empty);
            OnClear?.Invoke();
        }

        public void LogToConsole(string logText, bool newLine = true)
        {
            if (string.IsNullOrEmpty(logText)) { return; }
            LogToConsole(new AlicizaXConsoleLog(logText, LogType.Log, newLine));
        }

        public void LogToConsole(AlicizaXConsoleLog log)
        {
            FlushQueuedLogs();
            AppendLog(log);
            OnLog?.Invoke(log);

            if (_config.AutoScroll == AutoScrollOptions.Always)
            {
                _view?.ScrollToLatest();
            }
        }

        public void LogToConsoleAsync(string logText, LogType logType = LogType.Log)
        {
            if (string.IsNullOrWhiteSpace(logText)) { return; }
            LogToConsoleAsync(new AlicizaXConsoleLog(logText, logType));
        }

        public void LogToConsoleAsync(AlicizaXConsoleLog log)
        {
            OnLog?.Invoke(log);
            _logQueue?.QueueLog(log);
        }

        public void RemoveLogTrace()
        {
            _logStorage?.RemoveLog();
            RequireFlush();
        }

        public string GetLogString()
        {
            return _logStorage != null ? _logStorage.GetLogString() : string.Empty;
        }

        public void OverrideInput(string newInput, bool shouldFocus = true)
        {
            _currentInput = newInput ?? string.Empty;
            if (_view != null)
            {
                _view.InputValue = _currentInput;
            }

            if (shouldFocus)
            {
                FocusInput();
            }

            OnInputChanged(_currentInput);
        }

        public void SelectSuggestionOffset(int offset)
        {
            SuggestionSet set = _suggestionStack?.TopmostSuggestionSet;
            if (set == null || set.Suggestions.Count == 0) { return; }

            set.SelectionIndex += offset;
            set.SelectionIndex += set.Suggestions.Count;
            set.SelectionIndex %= set.Suggestions.Count;
            SetSuggestion(set.SelectionIndex);
        }

        public void SetSuggestion(int suggestionIndex)
        {
            if (_suggestionStack == null || !_suggestionStack.SetSuggestionIndex(suggestionIndex))
            {
                return;
            }

            OverrideInput(_suggestionStack.GetCompletion());
            UpdateSuggestionText();
        }

        public void SelectSuggestionAtDisplayIndex(int displayIndex)
        {
            SuggestionSet set = _suggestionStack?.TopmostSuggestionSet;
            if (set == null || set.Suggestions.Count == 0) { return; }

            int index = displayIndex;
            if (_config.SuggestionDisplayOrder == SortOrder.Ascending)
            {
                int displaySize = GetSuggestionDisplaySize(set);
                index = displaySize - index - 1;
            }

            if (index >= 0 && index < set.Suggestions.Count)
            {
                SetSuggestion(index);
            }
        }

        public void SelectPreviousHistory()
        {
            if (!_config.StoreCommandHistory || _previousCommands.Count == 0) { return; }

            _selectedPreviousCommandIndex++;
            _selectedPreviousCommandIndex = Mathf.Clamp(_selectedPreviousCommandIndex, -1, _previousCommands.Count - 1);
            ApplySelectedHistoryCommand();
        }

        public void SelectNextHistory()
        {
            if (!_config.StoreCommandHistory || _previousCommands.Count == 0) { return; }

            if (_selectedPreviousCommandIndex > 0) { _selectedPreviousCommandIndex--; }
            _selectedPreviousCommandIndex = Mathf.Clamp(_selectedPreviousCommandIndex, -1, _previousCommands.Count - 1);
            ApplySelectedHistoryCommand();
        }

        public void HidePopup()
        {
            _view?.SetPopupText(string.Empty, false);
        }

        private void InitializeLogging()
        {
            _logStorage ??= new LogStorage(_config.MaxStoredLogs);
            _logQueue ??= new LogQueue(_config.MaxStoredLogs);
            _logStorage.MaxStoredLogs = _config.MaxStoredLogs;
            _logQueue.MaxStoredLogs = _config.MaxStoredLogs;
        }

        private void InitializeSuggestionStack()
        {
            if (_suggestionStack != null) { return; }

            _suggestionStack = new SuggestionStack();
            _suggestionStack.OnSuggestionSetCreated += set => OnSuggestionSetGenerated?.Invoke(set);
        }

        private void UpdateTableGenerationState()
        {
            if (!_isGeneratingTable && !AlicizaXConsoleProcessor.TableIsGenerating)
            {
                return;
            }

            if (AlicizaXConsoleProcessor.TableIsGenerating)
            {
                SetInputEnabled(false);
                SetPlaceholder("Loading...");
                if (_config.ShowInitLogs)
                {
                    string consoleText = $"{_logStorage.GetLogString()}\n{GetTableGenerationText()}".Trim();
                    _view?.SetLogText(consoleText);
                }

                return;
            }

            if (_isGeneratingTable)
            {
                if (_config.ShowInitLogs)
                {
                    AppendLog(new AlicizaXConsoleLog(GetTableGenerationText()));
                }

                _isGeneratingTable = false;
                SetInputEnabled(true);
                SetPlaceholder("Enter Command...");
                OverrideInput(string.Empty, false);
                _view?.ScrollToLatest();
                OnStateChange?.Invoke();
            }
        }

        private void UpdateBlockedState()
        {
            if (_isGeneratingTable || AlicizaXConsoleProcessor.TableIsGenerating || _isHandlingUserResponse)
            {
                return;
            }

            if (IsBlockedByAsync)
            {
                SetInputEnabled(false);
                SetPlaceholder("Executing async command...");
            }
            else if (_view != null && !_view.InputEnabled)
            {
                SetInputEnabled(true);
                SetPlaceholder("Enter Command...");
                OverrideInput(string.Empty, false);
                OnStateChange?.Invoke();
            }
        }

        private string GetTableGenerationText()
        {
            string text = string.Format("S:\\>{0} commands have been loaded", AlicizaXConsoleProcessor.LoadedCommandCount);
            if (AlicizaXConsoleProcessor.TableIsGenerating)
            {
                text += "...";
            }
            else
            {
                string completionText = "S:\\>AlicizaX Console Processor ready".ColorText(ColorSuccess);
                text += $"\n{completionText}";
            }

            return text;
        }

        private void ApplySelectedHistoryCommand()
        {
            if (_selectedPreviousCommandIndex > -1)
            {
                string command = _previousCommands[_previousCommands.Count - _selectedPreviousCommandIndex - 1];
                OverrideInput(command);
            }
        }

        private void UpdateSuggestions()
        {
            if (_isHandlingUserResponse || _suggestionStack == null)
            {
                ClearSuggestions();
                return;
            }

            SuggestorOptions options = new SuggestorOptions
            {
                CaseSensitive = _config.CaseSensitiveSearch,
                Fuzzy = _config.UseFuzzySearch,
                CollapseOverloads = _config.CollapseSuggestionOverloads,
            };

            _suggestionStack.UpdateStack(_currentInput, options);
            UpdateSuggestionUi();
        }

        private void UpdateSuggestionUi()
        {
            UpdateSuggestionText();
            if (_config.ShowPopupDisplay)
            {
                UpdatePopupDisplay();
            }
            else
            {
                _view?.SetPopupText(string.Empty, false);
            }
        }

        private void FormatSuggestion(IAlicizaXConsoleSuggestion suggestion, bool selected, ref Utf16ValueStringBuilder buffer)
        {
            Color primaryColor = Color.white;
            Color secondaryColor = ColorSuggestion;
            if (selected)
            {
                primaryColor *= ColorSelectedSugg;
                secondaryColor *= ColorSelectedSugg;
            }

            buffer.AppendColoredText(suggestion.PrimarySignature, primaryColor);
            buffer.AppendColoredText(suggestion.SecondarySignature, secondaryColor);
        }

        private string GetFormattedSuggestions(SuggestionSet suggestionSet)
        {
            Utf16ValueStringBuilder buffer = StringBuilderPool.GetStringBuilder();
            int displaySize = GetSuggestionDisplaySize(suggestionSet);
            for (int i = 0; i < displaySize; i++)
            {
                if (_config.MaxSuggestionDisplaySize > 0 && i >= _config.MaxSuggestionDisplaySize)
                {
                    const string remainingSuggestion = "...";
                    if (suggestionSet.SelectionIndex >= _config.MaxSuggestionDisplaySize)
                    {
                        buffer.AppendColoredText(remainingSuggestion, ColorSelectedSugg);
                    }
                    else
                    {
                        buffer.Append(remainingSuggestion);
                    }
                }
                else
                {
                    bool selected = i == suggestionSet.SelectionIndex;
                    FormatSuggestion(suggestionSet.Suggestions[i], selected, ref buffer);
                }

                if (i < displaySize - 1)
                {
                    buffer.AppendLine();
                }
            }

            return StringBuilderPool.ReleaseAndToString(buffer);
        }

        private int GetSuggestionDisplaySize(SuggestionSet suggestionSet)
        {
            int displaySize = suggestionSet.Suggestions.Count;
            if (_config.MaxSuggestionDisplaySize > 0)
            {
                displaySize = Mathf.Min(displaySize, _config.MaxSuggestionDisplaySize + 1);
            }

            return displaySize;
        }

        private void UpdatePopupDisplay()
        {
            SuggestionSet suggestionSet = _suggestionStack?.TopmostSuggestionSet;
            if (suggestionSet == null || suggestionSet.Suggestions.Count == 0)
            {
                _view?.SetPopupText(string.Empty, false);
                return;
            }

            string formattedSuggestions = GetFormattedSuggestions(suggestionSet);
            if (_config.SuggestionDisplayOrder == SortOrder.Ascending)
            {
                formattedSuggestions = formattedSuggestions.ReverseItems('\n');
            }

            _view?.SetPopupText(formattedSuggestions, true);
        }

        private void UpdateSuggestionText()
        {
            if (_view == null || _suggestionStack == null)
            {
                return;
            }

            Utf16ValueStringBuilder buffer = StringBuilderPool.GetStringBuilder();
            buffer.AppendColoredText(_currentInput, Color.clear);
            buffer.AppendColoredText(_suggestionStack.GetCompletionTail(), ColorSuggestion);
            _view.SetGhostText(StringBuilderPool.ReleaseAndToString(buffer));
        }

        private void ClearSuggestions()
        {
            _suggestionStack?.Clear();
            _view?.SetGhostText(string.Empty);
            _view?.SetPopupText(string.Empty, false);
        }

        private void ClearBuffers()
        {
            _selectedPreviousCommandIndex = -1;
            ClearSuggestions();
        }

        private void HandleUserResponse(string command)
        {
            if (_currentResponseConfig.LogInput)
            {
                LogUserInput(command);
                StoreCommand(command);
            }

            Action<string> callback = _onSubmitResponseCallback;
            _onSubmitResponseCallback = null;
            _isHandlingUserResponse = false;
            SetInputEnabled(false);
            OnStateChange?.Invoke();
            callback?.Invoke(command);
        }

        private void LogUserInput(string input)
        {
            LogToConsole(GenerateCommandLog(input));
        }

        private AlicizaXConsoleLog GenerateCommandLog(string command)
        {
            if (command.Contains("<"))
            {
                command = $"<noparse>{command}</noparse>";
            }

            string logValue = string.Format(CommandLogFormat, command).ColorText(ColorCommandLog);
            return new AlicizaXConsoleLog(logValue);
        }

        private string GetErrorMessage(Exception e)
        {
            string message = _config.VerboseErrors
                ? $"AlicizaXConsole Processor Error ({e.GetType()}): {e.Message}\n{e.StackTrace}"
                : $"AlicizaXConsole Processor Error: {e.Message}";
            return message.ColorText(ColorError);
        }

        private string GetInvocationErrorMessage(Exception e)
        {
            if (e == null)
            {
                return "Error: Unknown invocation error".ColorText(ColorError);
            }

            string message = _config.VerboseErrors
                ? $"Error ({e.GetType()}): {e.Message}\n{e.StackTrace}"
                : $"Error: {e.Message}";
            return message.ColorText(ColorError);
        }

        private void FlushQueuedLogs()
        {
            if (_logQueue == null) { return; }

            bool scroll = false;
            while (_logQueue.TryDequeue(out AlicizaXConsoleLog log))
            {
                AppendLog(log);
                scroll |= _config.AutoScroll == AutoScrollOptions.Always;
            }

            if (scroll)
            {
                _view?.ScrollToLatest();
            }
        }

        private void ProcessAsyncTasks()
        {
            for (int i = _currentTasks.Count - 1; i >= 0; i--)
            {
                if (!_currentTasks[i].IsCompleted) { continue; }

                if (_currentTasks[i].IsFaulted)
                {
                    foreach (Exception e in _currentTasks[i].Exception.InnerExceptions)
                    {
                        LogToConsole(GetInvocationErrorMessage(e));
                    }
                }
                else
                {
                    Type taskType = _currentTasks[i].GetType();
                    if (taskType.IsGenericTypeOf(typeof(Task<>)) && !_voidTaskType.IsAssignableFrom(taskType))
                    {
                        System.Reflection.PropertyInfo resultProperty = _currentTasks[i].GetType().GetProperty("Result");
                        object result = resultProperty.GetValue(_currentTasks[i]);
                        LogToConsole(Serialize(result));
                    }
                }

                _currentTasks.RemoveAt(i);
            }
        }

        private void ProcessActions()
        {
            if (_config.CancelActionsKey.IsPressed())
            {
                CancelAllActions();
                return;
            }

            ActionContext context = new ActionContext
            {
                ConsoleInterface = this
            };

            for (int i = _currentActions.Count - 1; i >= 0; i--)
            {
                IEnumerator<ICommandAction> action = _currentActions[i];
                try
                {
                    if (action.Execute(context) != ActionState.Running)
                    {
                        _currentActions.RemoveAt(i);
                    }
                }
                catch (Exception e)
                {
                    _currentActions.RemoveAt(i);
                    LogToConsole(GetInvocationErrorMessage(e));
                    break;
                }
            }
        }

        private void HandleAsyncJobCounter()
        {
            if (!_config.ShowCurrentJobs)
            {
                _view?.SetJobCounter(string.Empty, false);
                return;
            }

            if (_currentTasks.Count == 0)
            {
                _view?.SetJobCounter(string.Empty, false);
            }
            else
            {
                string text = $"{_currentTasks.Count} job{(_currentTasks.Count == 1 ? string.Empty : "s")} in progress";
                _view?.SetJobCounter(text, true);
            }
        }

        private void UpdateJobCounterUi()
        {
            HandleAsyncJobCounter();
        }

        private void AppendLog(AlicizaXConsoleLog log)
        {
            _logStorage?.AddLog(TruncateLog(log));
            RequireFlush();
        }

        private AlicizaXConsoleLog TruncateLog(AlicizaXConsoleLog log)
        {
            if (log.Text.Length <= _config.MaxLogSize || _config.MaxLogSize < 0)
            {
                return log;
            }

            string msg = string.Format(
                    "Log of size {0} exceeded the maximum log size of {1}",
                    log.Text.Length,
                    _config.MaxLogSize)
                .ColorText(ColorError);
            return new AlicizaXConsoleLog(msg, LogType.Error);
        }

        private void RequireFlush()
        {
            _consoleRequiresFlush = true;
        }

        private void FlushToView()
        {
            if (!_consoleRequiresFlush || _view == null || _logStorage == null) { return; }

            _consoleRequiresFlush = false;
            _view.SetLogText(_logStorage.GetLogString());
        }

        private void StoreCommand(string command)
        {
            if (!_config.StoreCommandHistory) { return; }

            if (!_config.StoreDuplicateCommands)
            {
                _previousCommands.Remove(command);
            }

            if (_config.StoreAdjacentDuplicateCommands
                || _previousCommands.Count == 0
                || _previousCommands[_previousCommands.Count - 1] != command)
            {
                _previousCommands.Add(command);
            }

            if (_config.CommandHistorySize > 0 && _previousCommands.Count > _config.CommandHistorySize)
            {
                _previousCommands.RemoveAt(0);
            }
        }

        private void SetInputEnabled(bool enabled)
        {
            if (_view != null)
            {
                _view.InputEnabled = enabled;
            }
        }

        private void SetPlaceholder(string placeholder)
        {
            _placeholder = placeholder ?? string.Empty;
            _view?.SetPlaceholder(_placeholder);
        }
    }
}
