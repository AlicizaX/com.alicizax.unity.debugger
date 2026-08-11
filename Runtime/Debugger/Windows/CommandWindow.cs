using System;
using System.Reflection;
using AlicizaX.Console;
using UnityEngine;
using UnityEngine.UIElements;

namespace AlicizaX.Debugger
{
    public sealed partial class DebuggerComponent
    {
        [Serializable]
        private sealed class CommandWindow : IDebuggerWindow, ICommandConsoleView
        {
            [SerializeField] private KeyCode m_SubmitCommandKey = KeyCode.Return;
            [SerializeField] private ModifierKeyCombo m_SelectNextSuggestionKey = KeyCode.Tab;
            [SerializeField] private ModifierKeyCombo m_SelectPreviousSuggestionKey = new ModifierKeyCombo { Key = KeyCode.Tab, Shift = true };
            [SerializeField] private KeyCode m_NextCommandKey = KeyCode.UpArrow;
            [SerializeField] private KeyCode m_PreviousCommandKey = KeyCode.DownArrow;
            [SerializeField] private ModifierKeyCombo m_CancelActionsKey = new ModifierKeyCombo { Key = KeyCode.C, Ctrl = true };

            [SerializeField] private bool m_VerboseErrors;
            [SerializeField] private LoggingThreshold m_VerboseLogging = LoggingThreshold.Never;
            [SerializeField] private LoggingThreshold m_LoggingLevel = LoggingThreshold.Always;
            [SerializeField] private AutoScrollOptions m_AutoScroll = AutoScrollOptions.OnInvoke;
            [SerializeField] private string[] m_CommandAssemblyNames = { AlicizaXConsoleProcessor.DefaultCommandAssemblyName };

            [SerializeField] private bool m_EnableAutocomplete = true;
            [SerializeField] private bool m_ShowPopupDisplay = true;
            [SerializeField] private SortOrder m_SuggestionDisplayOrder = SortOrder.Descending;
            [SerializeField] private int m_MaxSuggestionDisplaySize = -1;
            [SerializeField] private bool m_UseFuzzySearch;
            [SerializeField] private bool m_CaseSensitiveSearch = true;
            [SerializeField] private bool m_CollapseSuggestionOverloads = true;

            [SerializeField] private bool m_ShowCurrentJobs = true;
            [SerializeField] private bool m_BlockOnAsync;

            [SerializeField] private bool m_StoreCommandHistory = true;
            [SerializeField] private bool m_StoreDuplicateCommands = true;
            [SerializeField] private bool m_StoreAdjacentDuplicateCommands;
            [SerializeField] private int m_CommandHistorySize = -1;

            [SerializeField] private int m_MaxStoredLogs = 1024;
            [SerializeField] private int m_MaxLogSize = 8192;
            [SerializeField] private bool m_ShowInitLogs = true;

            private AlicizaXConsoleController _controller;
            private VisualElement _root;
            private ScrollView _logScroll;
            private Label _logLabel;
            private Label _jobCounterLabel;
            private Label _suggestionPopupLabel;
            private Label _ghostLabel;
            private Label _placeholderLabel;
            private TextField _inputField;
            private VisualElement _inputStack;
            private VisualElement _caretBar;
            private Button _submitButton;
            private Button _clearButton;
            private string _placeholderText = "Enter Command...";
            private bool _suppressInputCallback;
            private float _inputScale = 1f;
            private bool _caretBlinkOn = true;
            private IVisualElementScheduledItem _caretBlinkSchedule;
            private IVisualElementScheduledItem _caretLayoutSchedule;

            public void Initialize(params object[] args)
            {
                if (_controller != null)
                {
                    return;
                }

                AlicizaXConsoleConfig config = BuildConfig();
                _controller = new AlicizaXConsoleController(config);
                _controller.Initialize();
                AlicizaXConsoleRouter.Register(_controller);
            }

            public void Shutdown()
            {
                if (_controller == null)
                {
                    return;
                }

                _controller.CancelAllActions();
                AlicizaXConsoleRouter.Deregister(_controller);
                _controller.Shutdown();
                _controller = null;
                UnbindElements();
            }

            public void OnEnter()
            {
                if (_controller == null)
                {
                    return;
                }

                AlicizaXConsoleRouter.SetActive(_controller);
                SetLogText(_controller.GetLogString());
                _controller.FocusInput();
            }

            public void OnLeave()
            {
                SetPopupText(string.Empty, false);
            }

            public void OnUpdate(float elapseSeconds, float realElapseSeconds)
            {
            }

            public void TickRuntime()
            {
                _controller?.Tick();
            }

            public VisualElement CreateView()
            {
                float scale = DebuggerComponent.Instance != null ? DebuggerComponent.Instance.GetUiScale() : 1f;

                _root = new VisualElement();
                _root.style.flexGrow = 1f;
                _root.style.flexDirection = FlexDirection.Column;
                _root.style.minHeight = 0f;
                _root.style.paddingLeft = 12f * scale;
                _root.style.paddingRight = 12f * scale;
                _root.style.paddingTop = 12f * scale;
                _root.style.paddingBottom = 12f * scale;

                VisualElement toolbar = ScrollableDebuggerWindowBase.CreateToolbarRow();
                toolbar.style.marginBottom = 8f * scale;
                toolbar.style.alignItems = Align.Center;

                _clearButton = ScrollableDebuggerWindowBase.CreateActionButton("Clear", () => _controller?.ClearConsole(), DebuggerTheme.Danger);
                _clearButton.style.height = 30f * scale;
                _clearButton.style.minHeight = 30f * scale;
                _clearButton.style.marginRight = 8f * scale;
                toolbar.Add(_clearButton);

                VisualElement spacer = new VisualElement();
                spacer.style.flexGrow = 1f;
                toolbar.Add(spacer);

                _jobCounterLabel = new Label();
                ApplyFont(_jobCounterLabel);
                _jobCounterLabel.style.color = DebuggerTheme.SecondaryText;
                _jobCounterLabel.style.fontSize = 15f * scale;
                _jobCounterLabel.style.display = DisplayStyle.None;
                toolbar.Add(_jobCounterLabel);
                _root.Add(toolbar);

                VisualElement logPanel = new VisualElement();
                logPanel.style.flexGrow = 1f;
                logPanel.style.flexShrink = 1f;
                logPanel.style.minHeight = 0f;
                logPanel.style.backgroundColor = DebuggerTheme.PanelSurface;
                logPanel.style.borderTopWidth = 1f;
                logPanel.style.borderBottomWidth = 1f;
                logPanel.style.borderLeftWidth = 0f;
                logPanel.style.borderRightWidth = 0f;
                logPanel.style.borderTopColor = DebuggerTheme.Border;
                logPanel.style.borderBottomColor = DebuggerTheme.Border;
                logPanel.style.marginBottom = 8f * scale;
                logPanel.style.overflow = Overflow.Hidden;

                _logScroll = new ScrollView(ScrollViewMode.Vertical);
                _logScroll.style.flexGrow = 1f;
                _logScroll.style.minHeight = 0f;
                _logScroll.style.backgroundColor = Color.clear;

                _logLabel = new Label();
                ApplyFont(_logLabel);
                _logLabel.enableRichText = true;
                _logLabel.style.whiteSpace = WhiteSpace.Normal;
                _logLabel.style.color = DebuggerTheme.PrimaryText;
                _logLabel.style.fontSize = 15f * scale;
                _logLabel.style.paddingLeft = 8f * scale;
                _logLabel.style.paddingRight = 8f * scale;
                _logLabel.style.paddingTop = 6f * scale;
                _logLabel.style.paddingBottom = 6f * scale;
                _logScroll.Add(_logLabel);
                logPanel.Add(_logScroll);
                _root.Add(logPanel);

                _logScroll.schedule.Execute(() => ScrollableDebuggerWindowBase.StyleScrollers(_logScroll, scale)).ExecuteLater(0);

                _suggestionPopupLabel = new Label();
                ApplyFont(_suggestionPopupLabel);
                _suggestionPopupLabel.enableRichText = true;
                _suggestionPopupLabel.style.display = DisplayStyle.None;
                _suggestionPopupLabel.style.backgroundColor = new Color(0.07f, 0.1f, 0.12f, 0.96f);
                _suggestionPopupLabel.style.color = DebuggerTheme.PrimaryText;
                _suggestionPopupLabel.style.fontSize = 14f * scale;
                _suggestionPopupLabel.style.paddingLeft = 8f * scale;
                _suggestionPopupLabel.style.paddingRight = 8f * scale;
                _suggestionPopupLabel.style.paddingTop = 6f * scale;
                _suggestionPopupLabel.style.paddingBottom = 6f * scale;
                _suggestionPopupLabel.style.marginBottom = 6f * scale;
                _suggestionPopupLabel.style.borderTopWidth = 1f;
                _suggestionPopupLabel.style.borderBottomWidth = 1f;
                _suggestionPopupLabel.style.borderLeftWidth = 1f;
                _suggestionPopupLabel.style.borderRightWidth = 1f;
                _suggestionPopupLabel.style.borderTopColor = DebuggerTheme.Border;
                _suggestionPopupLabel.style.borderBottomColor = DebuggerTheme.Border;
                _suggestionPopupLabel.style.borderLeftColor = DebuggerTheme.Border;
                _suggestionPopupLabel.style.borderRightColor = DebuggerTheme.Border;
                _suggestionPopupLabel.style.maxHeight = 160f * scale;
                _suggestionPopupLabel.style.overflow = Overflow.Hidden;
                _suggestionPopupLabel.RegisterCallback<PointerDownEvent>(OnSuggestionPointerDown);
                _root.Add(_suggestionPopupLabel);

                VisualElement inputRow = new VisualElement();
                inputRow.style.flexDirection = FlexDirection.Row;
                inputRow.style.flexShrink = 0f;
                inputRow.style.minHeight = 36f * scale;
                inputRow.style.alignItems = Align.Center;

                _inputScale = scale;
                _inputStack = new VisualElement();
                _inputStack.style.flexGrow = 1f;
                _inputStack.style.flexShrink = 1f;
                _inputStack.style.position = Position.Relative;
                _inputStack.style.height = 36f * scale;
                _inputStack.style.minHeight = 36f * scale;
                _inputStack.style.backgroundColor = DebuggerTheme.PanelSurface;
                _inputStack.style.borderTopWidth = 1f;
                _inputStack.style.borderBottomWidth = 1f;
                _inputStack.style.borderLeftWidth = 1f;
                _inputStack.style.borderRightWidth = 1f;
                _inputStack.style.borderTopColor = DebuggerTheme.Border;
                _inputStack.style.borderBottomColor = DebuggerTheme.Border;
                _inputStack.style.borderLeftColor = DebuggerTheme.Border;
                _inputStack.style.borderRightColor = DebuggerTheme.Border;
                _inputStack.style.marginRight = 8f * scale;
                _inputStack.style.overflow = Overflow.Hidden;

                _ghostLabel = new Label();
                ApplyFont(_ghostLabel);
                _ghostLabel.enableRichText = true;
                _ghostLabel.pickingMode = PickingMode.Ignore;
                _ghostLabel.style.position = Position.Absolute;
                _ghostLabel.style.left = 0f;
                _ghostLabel.style.right = 0f;
                _ghostLabel.style.top = 0f;
                _ghostLabel.style.bottom = 0f;
                _ghostLabel.style.paddingLeft = 8f * scale;
                _ghostLabel.style.paddingRight = 8f * scale;
                _ghostLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                _ghostLabel.style.fontSize = 15f * scale;
                _ghostLabel.style.color = DebuggerTheme.SecondaryText;
                _inputStack.Add(_ghostLabel);

                _placeholderLabel = new Label();
                ApplyFont(_placeholderLabel);
                _placeholderLabel.pickingMode = PickingMode.Ignore;
                _placeholderLabel.style.position = Position.Absolute;
                _placeholderLabel.style.left = 0f;
                _placeholderLabel.style.right = 0f;
                _placeholderLabel.style.top = 0f;
                _placeholderLabel.style.bottom = 0f;
                _placeholderLabel.style.paddingLeft = 8f * scale;
                _placeholderLabel.style.paddingRight = 8f * scale;
                _placeholderLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                _placeholderLabel.style.fontSize = 15f * scale;
                _placeholderLabel.style.color = new Color(DebuggerTheme.SecondaryText.r, DebuggerTheme.SecondaryText.g, DebuggerTheme.SecondaryText.b, 0.7f);
                _placeholderLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                _placeholderLabel.text = _placeholderText;
                _inputStack.Add(_placeholderLabel);

                _inputField = new TextField();
                ApplyFont(_inputField);
                _inputField.multiline = false;
                _inputField.style.position = Position.Absolute;
                _inputField.style.left = 0f;
                _inputField.style.right = 0f;
                _inputField.style.top = 0f;
                _inputField.style.bottom = 0f;
                _inputField.style.marginLeft = 0f;
                _inputField.style.marginRight = 0f;
                _inputField.style.marginTop = 0f;
                _inputField.style.marginBottom = 0f;
                _inputField.style.backgroundColor = Color.clear;
                _inputField.style.color = DebuggerTheme.PrimaryText;
                _inputField.style.fontSize = 15f * scale;
                _inputField.style.borderTopWidth = 0f;
                _inputField.style.borderBottomWidth = 0f;
                _inputField.style.borderLeftWidth = 0f;
                _inputField.style.borderRightWidth = 0f;
                StyleTextFieldInput(_inputField, scale);
                _inputField.RegisterValueChangedCallback(OnInputValueChanged);
                _inputField.RegisterCallback<FocusInEvent>(OnInputFocusIn);
                _inputField.RegisterCallback<FocusOutEvent>(OnInputFocusOut);
                _inputField.RegisterCallback<KeyDownEvent>(OnInputKeyDown, TrickleDown.TrickleDown);
                _inputField.RegisterCallback<GeometryChangedEvent>(_ => QueueCaretLayout());
                _inputField.RegisterCallback<PointerDownEvent>(_ =>
                {
                    RestartCaretBlink();
                    QueueCaretLayout();
                }, TrickleDown.TrickleDown);
                _inputField.RegisterCallback<PointerUpEvent>(_ =>
                {
                    RestartCaretBlink();
                    QueueCaretLayout();
                }, TrickleDown.TrickleDown);
                _inputStack.Add(_inputField);

                _caretBar = new VisualElement();
                _caretBar.name = "command-input-caret";
                _caretBar.pickingMode = PickingMode.Ignore;
                _caretBar.style.position = Position.Absolute;
                _caretBar.style.display = DisplayStyle.None;
                _caretBar.style.width = Mathf.Max(2f, 2.5f * scale);
                _caretBar.style.backgroundColor = DebuggerTheme.Caret;
                _caretBar.style.borderTopLeftRadius = 1f * scale;
                _caretBar.style.borderTopRightRadius = 1f * scale;
                _caretBar.style.borderBottomLeftRadius = 1f * scale;
                _caretBar.style.borderBottomRightRadius = 1f * scale;
                _caretBar.style.borderLeftWidth = 1f * scale;
                _caretBar.style.borderRightWidth = 1f * scale;
                _caretBar.style.borderLeftColor = DebuggerTheme.CaretGlow;
                _caretBar.style.borderRightColor = DebuggerTheme.CaretGlow;
                _inputStack.Add(_caretBar);

                inputRow.Add(_inputStack);

                _submitButton = ScrollableDebuggerWindowBase.CreateActionButton("Submit", () => _controller?.InvokeCommandFromInput(), DebuggerTheme.ButtonSurface);
                _submitButton.style.height = 36f * scale;
                _submitButton.style.minHeight = 36f * scale;
                _submitButton.style.minWidth = 88f * scale;
                inputRow.Add(_submitButton);
                _root.Add(inputRow);

                _controller?.AttachView(this);
                return _root;
            }

            public string InputValue
            {
                get => _inputField != null ? _inputField.value : string.Empty;
                set
                {
                    if (_inputField == null)
                    {
                        return;
                    }

                    _suppressInputCallback = true;
                    _inputField.SetValueWithoutNotify(value ?? string.Empty);
                    _suppressInputCallback = false;
                    UpdatePlaceholderVisibility();
                    RestartCaretBlink();
                    QueueCaretLayout();
                }
            }

            public bool InputEnabled
            {
                get => _inputField != null && _inputField.enabledSelf;
                set
                {
                    _inputField?.SetEnabled(value);
                    _submitButton?.SetEnabled(value);
                }
            }

            public bool IsInputFocused
            {
                get
                {
                    if (_inputField == null || _inputField.panel == null)
                    {
                        return false;
                    }

                    return _inputField.panel.focusController?.focusedElement == _inputField;
                }
            }

            public void SetPlaceholder(string text)
            {
                _placeholderText = text ?? string.Empty;
                if (_placeholderLabel != null)
                {
                    _placeholderLabel.text = _placeholderText;
                }

                if (_inputField != null)
                {
                    _inputField.tooltip = _placeholderText;
                }

                UpdatePlaceholderVisibility();
            }

            public void SetLogText(string text)
            {
                if (_logLabel != null)
                {
                    _logLabel.text = text ?? string.Empty;
                }
            }

            public void SetGhostText(string richText)
            {
                if (_ghostLabel != null)
                {
                    _ghostLabel.text = richText ?? string.Empty;
                }
            }

            public void SetPopupText(string richText, bool visible)
            {
                if (_suggestionPopupLabel == null)
                {
                    return;
                }

                _suggestionPopupLabel.text = richText ?? string.Empty;
                _suggestionPopupLabel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }

            public void SetJobCounter(string text, bool visible)
            {
                if (_jobCounterLabel == null)
                {
                    return;
                }

                _jobCounterLabel.text = text ?? string.Empty;
                _jobCounterLabel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }

            public void ScrollToLatest()
            {
                if (_logScroll == null)
                {
                    return;
                }

                _logScroll.schedule.Execute(() =>
                {
                    _logScroll.scrollOffset = new Vector2(_logScroll.scrollOffset.x, float.MaxValue);
                });
            }

            public void FocusInput()
            {
                if (_inputField == null)
                {
                    return;
                }

                _inputField.Focus();
                _inputField.schedule.Execute(() =>
                {
                    if (_inputField == null)
                    {
                        return;
                    }

                    int textLength = _inputField.value?.Length ?? 0;
                    _inputField.cursorIndex = textLength;
                    _inputField.selectIndex = textLength;
                    ApplyTextFieldCaretColors(_inputField);
                    StartCaretBlink();
                    UpdateCustomCaret();
                });
            }

            private AlicizaXConsoleConfig BuildConfig()
            {
                return new AlicizaXConsoleConfig
                {
                    SubmitCommandKey = m_SubmitCommandKey,
                    SelectNextSuggestionKey = m_SelectNextSuggestionKey,
                    SelectPreviousSuggestionKey = m_SelectPreviousSuggestionKey,
                    NextCommandKey = m_NextCommandKey,
                    PreviousCommandKey = m_PreviousCommandKey,
                    CancelActionsKey = m_CancelActionsKey,
                    VerboseErrors = m_VerboseErrors,
                    VerboseLogging = m_VerboseLogging,
                    LoggingLevel = m_LoggingLevel,
                    AutoScroll = m_AutoScroll,
                    CommandAssemblyNames = m_CommandAssemblyNames != null && m_CommandAssemblyNames.Length > 0
                        ? m_CommandAssemblyNames
                        : new[] { AlicizaXConsoleProcessor.DefaultCommandAssemblyName },
                    EnableAutocomplete = m_EnableAutocomplete,
                    ShowPopupDisplay = m_ShowPopupDisplay,
                    SuggestionDisplayOrder = m_SuggestionDisplayOrder,
                    MaxSuggestionDisplaySize = m_MaxSuggestionDisplaySize,
                    UseFuzzySearch = m_UseFuzzySearch,
                    CaseSensitiveSearch = m_CaseSensitiveSearch,
                    CollapseSuggestionOverloads = m_CollapseSuggestionOverloads,
                    ShowCurrentJobs = m_ShowCurrentJobs,
                    BlockOnAsync = m_BlockOnAsync,
                    StoreCommandHistory = m_StoreCommandHistory,
                    StoreDuplicateCommands = m_StoreDuplicateCommands,
                    StoreAdjacentDuplicateCommands = m_StoreAdjacentDuplicateCommands,
                    CommandHistorySize = m_CommandHistorySize,
                    MaxStoredLogs = m_MaxStoredLogs,
                    MaxLogSize = m_MaxLogSize,
                    ShowInitLogs = m_ShowInitLogs,
                };
            }

            private void OnInputValueChanged(ChangeEvent<string> evt)
            {
                if (_suppressInputCallback)
                {
                    return;
                }

                UpdatePlaceholderVisibility();
                _controller?.OnInputChanged(evt.newValue);
                RestartCaretBlink();
                QueueCaretLayout();
            }

            private void OnInputFocusIn(FocusInEvent evt)
            {
                UpdatePlaceholderVisibility();
                ApplyTextFieldCaretColors(_inputField);
                StartCaretBlink();
                QueueCaretLayout();
            }

            private void OnInputFocusOut(FocusOutEvent evt)
            {
                UpdatePlaceholderVisibility();
                StopCaretBlink();
                HideCustomCaret();
            }

            private void OnInputKeyDown(KeyDownEvent evt)
            {
                if (_controller == null)
                {
                    return;
                }

                if (evt.keyCode == m_SubmitCommandKey)
                {
                    _controller.InvokeCommandFromInput();
                    evt.StopPropagation();
                }
                else if (m_StoreCommandHistory && evt.keyCode == m_NextCommandKey)
                {
                    _controller.SelectPreviousHistory();
                    evt.StopPropagation();
                }
                else if (m_StoreCommandHistory && evt.keyCode == m_PreviousCommandKey)
                {
                    _controller.SelectNextHistory();
                    evt.StopPropagation();
                }
                else if (m_EnableAutocomplete && MatchesCombo(evt, m_SelectNextSuggestionKey))
                {
                    _controller.SelectSuggestionOffset(1);
                    evt.StopPropagation();
                }
                else if (m_EnableAutocomplete && MatchesCombo(evt, m_SelectPreviousSuggestionKey))
                {
                    _controller.SelectSuggestionOffset(-1);
                    evt.StopPropagation();
                }

                RestartCaretBlink();
                QueueCaretLayout();
            }

            private void OnSuggestionPointerDown(PointerDownEvent evt)
            {
                if (_controller == null || _suggestionPopupLabel == null)
                {
                    return;
                }

                float lineHeight = Mathf.Max(1f, _suggestionPopupLabel.resolvedStyle.fontSize * 1.25f);
                int index = Mathf.FloorToInt(evt.localPosition.y / lineHeight);
                _controller.SelectSuggestionAtDisplayIndex(index);
                FocusInput();
                evt.StopPropagation();
            }

            private static bool MatchesCombo(KeyDownEvent evt, ModifierKeyCombo combo)
            {
                if (combo.Key == KeyCode.None || evt.keyCode != combo.Key)
                {
                    return false;
                }

                return evt.ctrlKey == combo.Ctrl && evt.altKey == combo.Alt && evt.shiftKey == combo.Shift;
            }

            private void UpdatePlaceholderVisibility()
            {
                if (_placeholderLabel == null || _inputField == null)
                {
                    return;
                }

                bool showPlaceholder = string.IsNullOrEmpty(_inputField.value) && !IsInputFocused;
                _placeholderLabel.style.display = showPlaceholder ? DisplayStyle.Flex : DisplayStyle.None;
            }

            private void UnbindElements()
            {
                StopCaretBlink();
                _caretLayoutSchedule?.Pause();
                _caretLayoutSchedule = null;
                _root = null;
                _logScroll = null;
                _logLabel = null;
                _jobCounterLabel = null;
                _suggestionPopupLabel = null;
                _ghostLabel = null;
                _placeholderLabel = null;
                _inputField = null;
                _inputStack = null;
                _caretBar = null;
                _submitButton = null;
                _clearButton = null;
            }

            private static void ApplyFont(VisualElement element)
            {
                if (element == null || DebuggerComponent.Instance == null)
                {
                    return;
                }

                element.style.unityFontDefinition = DebuggerComponent.Instance.ResolveFontDefinition();
            }

            private void StyleTextFieldInput(TextField textField, float scale)
            {
                if (textField == null)
                {
                    return;
                }

                textField.style.color = DebuggerTheme.PrimaryText;
                ApplyTextFieldCaretColors(textField);

                textField.schedule.Execute(() =>
                {
                    ApplyTextFieldCaretColors(textField);

                    VisualElement input = textField.Q(className: "unity-base-text-field__input")
                                         ?? textField.Q(className: "unity-text-field__input")
                                         ?? textField.Q(className: "unity-text-input");
                    if (input == null)
                    {
                        return;
                    }

                    ApplyFont(input);
                    input.style.backgroundColor = Color.clear;
                    input.style.borderTopWidth = 0f;
                    input.style.borderBottomWidth = 0f;
                    input.style.borderLeftWidth = 0f;
                    input.style.borderRightWidth = 0f;
                    input.style.paddingLeft = 8f * scale;
                    input.style.paddingRight = 8f * scale;
                    input.style.paddingTop = 0f;
                    input.style.paddingBottom = 0f;
                    input.style.marginLeft = 0f;
                    input.style.marginRight = 0f;
                    input.style.marginTop = 0f;
                    input.style.marginBottom = 0f;
                    input.style.color = DebuggerTheme.PrimaryText;
                    input.style.unityTextAlign = TextAnchor.MiddleLeft;
                    input.style.fontSize = 15f * scale;
                    input.style.height = 36f * scale;
                    input.style.minHeight = 36f * scale;
                    input.style.overflow = Overflow.Visible;

                    ApplyTextFieldCaretColors(textField);
                    QueueCaretLayout();
                }).ExecuteLater(0);
            }

            private static void ApplyTextFieldCaretColors(TextField textField)
            {
                if (textField == null)
                {
                    return;
                }

#if UNITY_6000_0_OR_NEWER
                textField.cursorColor = Color.clear;
                textField.selectionColor = DebuggerTheme.TextSelection;
#endif

                ITextSelection selection = null;
                try
                {
                    selection = textField.textSelection;
                }
                catch (Exception)
                {
                }

                if (selection != null)
                {
                    selection.cursorColor = Color.clear;
                    selection.selectionColor = DebuggerTheme.TextSelection;
                    TrySetCursorWidth(selection, 1f);
                }

                TextElement textElement = textField.Q<TextElement>()
                                         ?? textField.Q(className: "unity-base-text-field__input")?.Q<TextElement>()
                                         ?? textField.Q(className: "unity-text-field__input")?.Q<TextElement>();
                if (textElement == null)
                {
                    return;
                }

                try
                {
                    textElement.selection.cursorColor = Color.clear;
                    textElement.selection.selectionColor = DebuggerTheme.TextSelection;
                    TrySetCursorWidth(textElement.selection, 1f);
                }
                catch (Exception)
                {
                }

                textElement.style.color = DebuggerTheme.PrimaryText;
            }

            private static void TrySetCursorWidth(ITextSelection selection, float width)
            {
                if (selection == null)
                {
                    return;
                }

                try
                {
                    PropertyInfo property = selection.GetType().GetProperty("cursorWidth");
                    if (property != null && property.CanWrite)
                    {
                        property.SetValue(selection, width);
                    }
                }
                catch (Exception)
                {
                }
            }

            private void StartCaretBlink()
            {
                if (_inputField == null)
                {
                    return;
                }

                _caretBlinkOn = true;
                if (_caretBlinkSchedule == null)
                {
                    _caretBlinkSchedule = _inputField.schedule.Execute(TickCaretBlink).Every(530);
                }
                else
                {
                    _caretBlinkSchedule.Resume();
                }

                UpdateCustomCaret();
            }

            private void RestartCaretBlink()
            {
                _caretBlinkOn = true;
                if (IsInputFocused)
                {
                    StartCaretBlink();
                }
            }

            private void StopCaretBlink()
            {
                _caretBlinkSchedule?.Pause();
                _caretBlinkOn = false;
            }

            private void TickCaretBlink()
            {
                if (!IsInputFocused)
                {
                    HideCustomCaret();
                    return;
                }

                _caretBlinkOn = !_caretBlinkOn;
                UpdateCustomCaret();
            }

            private void QueueCaretLayout()
            {
                if (_inputField == null)
                {
                    return;
                }

                if (_caretLayoutSchedule == null)
                {
                    _caretLayoutSchedule = _inputField.schedule.Execute(UpdateCustomCaret);
                }

                _caretLayoutSchedule.ExecuteLater(0);
                _inputField.schedule.Execute(UpdateCustomCaret).ExecuteLater(16);
            }

            private void HideCustomCaret()
            {
                if (_caretBar != null)
                {
                    _caretBar.style.display = DisplayStyle.None;
                }
            }

            private void UpdateCustomCaret()
            {
                if (_caretBar == null || _inputField == null || _inputStack == null)
                {
                    return;
                }

                if (!IsInputFocused || !_caretBlinkOn)
                {
                    _caretBar.style.display = DisplayStyle.None;
                    return;
                }

                ApplyTextFieldCaretColors(_inputField);

                float leftPadding = 8f * _inputScale;
                float caretHeight = 18f * _inputScale;
                float caretWidth = Mathf.Max(3f, 3.5f * _inputScale);
                float caretX = leftPadding;
                float caretY = 0f;
                bool positioned = false;

                TextElement textElement = _inputField.Q<TextElement>()
                                         ?? _inputField.Q(className: "unity-base-text-field__input")?.Q<TextElement>()
                                         ?? _inputField.Q(className: "unity-text-field__input")?.Q<TextElement>();

                try
                {
                    Vector2 cursorPos = _inputField.textSelection.cursorPosition;
                    if (!float.IsNaN(cursorPos.x) && !float.IsInfinity(cursorPos.x))
                    {
                        if (textElement != null && textElement.panel != null && _inputStack.panel != null)
                        {
                            Vector2 world = textElement.LocalToWorld(cursorPos);
                            Vector2 local = _inputStack.WorldToLocal(world);
                            caretX = local.x;
                            positioned = true;
                        }
                        else
                        {
                            caretX = leftPadding + Mathf.Max(0f, cursorPos.x);
                            positioned = true;
                        }
                    }
                }
                catch (Exception)
                {
                }

                if (!positioned)
                {
                    caretX = leftPadding + EstimateCaretX(_inputField.value, _inputField.cursorIndex, 15f * _inputScale);
                }

                float maxX = Mathf.Max(leftPadding, _inputStack.layout.width - caretWidth - 4f * _inputScale);
                if (!float.IsNaN(maxX) && maxX > 0f)
                {
                    caretX = Mathf.Clamp(caretX, 2f * _inputScale, maxX);
                }

                float stackHeight = _inputStack.layout.height;
                if (!float.IsNaN(stackHeight) && stackHeight > 0f)
                {
                    caretHeight = Mathf.Clamp(stackHeight - 10f * _inputScale, 16f * _inputScale, 24f * _inputScale);
                    caretY = (stackHeight - caretHeight) * 0.5f;
                }

                _caretBar.style.left = caretX;
                _caretBar.style.top = caretY;
                _caretBar.style.width = caretWidth;
                _caretBar.style.height = caretHeight;
                _caretBar.style.backgroundColor = DebuggerTheme.Caret;
                _caretBar.style.display = DisplayStyle.Flex;
                _caretBar.BringToFront();
            }

            private static float EstimateCaretX(string text, int cursorIndex, float fontSize)
            {
                if (string.IsNullOrEmpty(text) || cursorIndex <= 0)
                {
                    return 0f;
                }

                int count = Mathf.Clamp(cursorIndex, 0, text.Length);
                float width = 0f;
                for (int i = 0; i < count; i++)
                {
                    char c = text[i];
                    if (c <= 0x007F)
                    {
                        width += fontSize * (char.IsUpper(c) ? 0.62f : 0.52f);
                    }
                    else
                    {
                        width += fontSize * 0.95f;
                    }
                }

                return width;
            }
        }
    }
}
