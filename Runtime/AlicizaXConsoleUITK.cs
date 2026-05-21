using AlicizaX.Console.Pooling;
using AlicizaX.Console.Utilities;
using Cysharp.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace AlicizaX.Console
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public class AlicizaXConsoleUITK : MonoBehaviour, IAlicizaXConsole
    {
        private const string ContainerName = "qc-container";
        private const string HeaderName = "qc-header";
        private const string ZoomOutButtonName = "qc-zoom-out";
        private const string ZoomLabelName = "qc-zoom-label";
        private const string ZoomInButtonName = "qc-zoom-in";
        private const string LogScrollName = "qc-log-scroll";
        private const string LogTextName = "qc-log-text";
        private const string FooterName = "qc-footer";
        private const string InputRowName = "qc-input-row";
        private const string SuggestionTextName = "qc-suggestion-text";
        private const string PlaceholderName = "qc-placeholder";
        private const string InputName = "qc-input";
        private const string SubmitButtonName = "qc-submit";
        private const string ClearButtonName = "qc-clear";
        private const string CloseButtonName = "qc-close";
        private const string PopupName = "qc-popup";
        private const string JobCounterName = "qc-job-counter";
        private const string ResizeHandleName = "qc-resize-handle";

        // ── 固定写在代码里的主题颜色 ─────────────────────────────────
        private static readonly Color _colorCommandLog      = new Color(0f, 1f, 1f);
        private static readonly Color _colorSelectedSugg    = new Color(1f, 1f, 0.55f);
        private static readonly Color _colorSuggestion      = Color.gray;
        private static readonly Color _colorError           = Color.red;
        private static readonly Color _colorWarning         = new Color(1f, 0.5f, 0f);
        private static readonly Color _colorSuccess         = Color.green;
        private static readonly Color _colorReturnDefault   = Color.white;
        private const string TimestampFormat   = "[{0:00}:{1:00}:{2:00}]";
        private const string CommandLogFormat  = "> {0}";

        public static AlicizaXConsoleUITK Instance { get; private set; }

#pragma warning disable 0414, 0067, 0649
        [SerializeField] private UIDocument _document;
        [SerializeField] private VisualTreeAsset _visualTree;
        [SerializeField] private StyleSheet _styleSheet;

        // ── 外观设置（替代 AlicizaXConsoleTheme）────────────────────────────
        [SerializeField] private Font _font;
        [SerializeField] private Color _panelColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        // ── 按键绑定（替代 AlicizaXConsoleKeyConfig）────────────────────
        [SerializeField] private KeyCode _submitCommandKey = KeyCode.Return;
        [SerializeField] private ModifierKeyCombo _showConsoleKey = KeyCode.None;
        [SerializeField] private ModifierKeyCombo _hideConsoleKey = KeyCode.None;
        [SerializeField] private ModifierKeyCombo _toggleConsoleKey = KeyCode.Escape;
        [SerializeField] private ModifierKeyCombo _zoomInKey = new ModifierKeyCombo { Key = KeyCode.Equals, Ctrl = true };
        [SerializeField] private ModifierKeyCombo _zoomOutKey = new ModifierKeyCombo { Key = KeyCode.Minus, Ctrl = true };
        [SerializeField] private ModifierKeyCombo _dragConsoleKey = new ModifierKeyCombo { Key = KeyCode.Mouse0, Shift = true };
        [SerializeField] private ModifierKeyCombo _selectNextSuggestionKey = KeyCode.Tab;
        [SerializeField] private ModifierKeyCombo _selectPreviousSuggestionKey = new ModifierKeyCombo { Key = KeyCode.Tab, Shift = true };
        [SerializeField] private KeyCode _nextCommandKey = KeyCode.UpArrow;
        [SerializeField] private KeyCode _previousCommandKey = KeyCode.DownArrow;
        [SerializeField] private ModifierKeyCombo _cancelActionsKey = new ModifierKeyCombo { Key = KeyCode.C, Ctrl = true };

        [SerializeField] private bool _verboseErrors = false;
        [SerializeField] private LoggingThreshold _verboseLogging = LoggingThreshold.Never;
        [SerializeField] private LoggingThreshold _loggingLevel = LoggingThreshold.Always;
        [SerializeField] private LoggingThreshold _openOnLogLevel = LoggingThreshold.Never;
        [SerializeField] private bool _interceptDebugLogger = true;
        [SerializeField] private bool _interceptWhilstInactive = true;
        [SerializeField] private bool _prependTimestamps = false;

        [SerializeField] private SupportedState _supportedState = SupportedState.Always;
        [SerializeField] private bool _activateOnStartup = true;
        [SerializeField] private bool _initialiseOnStartup = false;
        [SerializeField] private bool _focusOnActivate = true;
        [SerializeField] private bool _closeOnSubmit = false;
        [SerializeField] private AutoScrollOptions _autoScroll = AutoScrollOptions.OnInvoke;
        [SerializeField] private string[] _commandAssemblyNames = { AlicizaXConsoleProcessor.DefaultCommandAssemblyName };

        [SerializeField] private bool _enableAutocomplete = true;
        [SerializeField] private bool _showPopupDisplay = true;
        [SerializeField] private SortOrder _suggestionDisplayOrder = SortOrder.Descending;
        [SerializeField] private int _maxSuggestionDisplaySize = -1;
        [SerializeField] private bool _useFuzzySearch = false;
        [SerializeField] private bool _caseSensitiveSearch = true;
        [SerializeField] private bool _collapseSuggestionOverloads = true;

        [SerializeField] private bool _showCurrentJobs = true;
        [SerializeField] private bool _blockOnAsync = false;

        [SerializeField] private bool _storeCommandHistory = true;
        [SerializeField] private bool _storeDuplicateCommands = true;
        [SerializeField] private bool _storeAdjacentDuplicateCommands = false;
        [SerializeField] private int _commandHistorySize = -1;

        [SerializeField] private int _maxStoredLogs = 1024;
        [SerializeField] private int _maxLogSize = 8192;
        [SerializeField] private bool _showInitLogs = true;

        [SerializeField] private bool _allowDrag = true;
        [SerializeField] private bool _allowResize = true;
        [SerializeField] private bool _allowZoom = true;
        [SerializeField] private Vector2 _defaultSize = new Vector2(640, 420);
        [SerializeField] private Vector2 _minimumSize = new Vector2(400, 260);
        [SerializeField] private float _zoomStep = 0.1f;
        [SerializeField] private float _minimumZoom = 0.6f;
        [SerializeField] private float _maximumZoom = 1.8f;
#pragma warning restore 0414, 0067, 0649

        public LoggingThreshold VerboseLogging
        {
            get => _verboseLogging;
            set => _verboseLogging = value;
        }

        public LoggingThreshold LoggingLevel
        {
            get => _loggingLevel;
            set => _loggingLevel = value;
        }

        public bool VerboseErrors
        {
            get => _verboseErrors;
            set => _verboseErrors = value;
        }

        public int MaxStoredLogs
        {
            get => _maxStoredLogs;
            set
            {
                _maxStoredLogs = value;
                if (_logStorage != null) { _logStorage.MaxStoredLogs = value; }
                if (_logQueue != null) { _logQueue.MaxStoredLogs = value; }
            }
        }

        public event Action OnStateChange;
        public event Action<string> OnInvoke;
        public event Action OnClear;
        public event Action<AlicizaXConsoleLog> OnLog;
        public event Action OnActivate;
        public event Action OnDeactivate;
        public event Action<SuggestionSet> OnSuggestionSetGenerated;

        private bool IsBlockedByAsync => (_blockOnAsync
                                         && _currentTasks.Count > 0
                                         || _currentActions.Count > 0)
                                         && !_isHandlingUserResponse;

        private readonly AlicizaXConsoleSerializer _serializer = new AlicizaXConsoleSerializer();
        private SuggestionStack _suggestionStack;
        private LogStorage _logStorage;
        private LogQueue _logQueue;

        public bool IsActive { get; private set; }
        public bool IsFocused => IsActive && _consoleInput != null && _consoleInput.panel?.focusController?.focusedElement == _consoleInput;
        public bool AreActionsExecuting => _currentActions.Count > 0;

        private readonly List<string> _previousCommands = new List<string>();
        private readonly List<Task> _currentTasks = new List<Task>();
        private readonly List<IEnumerator<ICommandAction>> _currentActions = new List<IEnumerator<ICommandAction>>();

        private int _selectedPreviousCommandIndex = -1;
        private string _currentInput;
        private string _previousInput;
        private bool _isGeneratingTable;
        private bool _consoleRequiresFlush;
        private bool _isHandlingUserResponse;
        private ResponseConfig _currentResponseConfig;
        private Action<string> _onSubmitResponseCallback;
        private Type _voidTaskType;

        private VisualElement _root;
        private VisualElement _container;
        private VisualElement _header;
        private Label _zoomLabel;
        private Button _zoomOutButton;
        private Button _zoomInButton;
        private ScrollView _scrollView;
        private VisualElement _footer;
        private Label _consoleLogText;
        private Label _consoleSuggestionText;
        private Label _inputPlaceholderText;
        private Label _suggestionPopupText;
        private Label _jobCounterText;
        private TextField _consoleInput;
        private Button _submitButton;
        private Button _clearButton;
        private Button _closeButton;
        private VisualElement _resizeHandle;

        private bool _uiReady;
        private bool _dragging;
        private bool _resizing;
        private Vector2 _pointerStart;
        private Vector2 _containerStart;
        private Vector2 _sizeStart;
        private float _zoom = 1f;

        private void Awake()
        {
            _voidTaskType = typeof(Task<>).MakeGenericType(Type.GetType("System.Threading.Tasks.VoidTaskResult"));
            InitializeLogging();
        }

        private void OnEnable()
        {
            EnsureDocument();
            BuildOrBindUI();

            AlicizaXConsoleRegistry.RegisterObject(this);
            AlicizaXConsoleRouter.Register(this);
            Application.logMessageReceivedThreaded += DebugIntercept;

            if (IsSupportedState())
            {
                if (!RegisterSingleton())
                {
                    return;
                }

                if (_activateOnStartup)
                {
                    bool shouldFocus = SystemInfo.deviceType == DeviceType.Desktop;
                    Activate(shouldFocus);
                }
                else
                {
                    if (_initialiseOnStartup) { Initialize(); }
                    Deactivate();
                }
            }
            else
            {
                DisableAlicizaXConsole();
            }
        }

        private void OnDisable()
        {
            AlicizaXConsoleRegistry.DeregisterObject(this);
            AlicizaXConsoleRouter.Deregister(this);
            Application.logMessageReceivedThreaded -= DebugIntercept;

            Deactivate();
        }

        private bool RegisterSingleton()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                return true;
            }

            if (Instance == this)
            {
                return true;
            }

            Destroy(gameObject);
            return false;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        protected virtual void Update()
        {
            if (!IsActive)
            {
                if (_showConsoleKey.IsPressed() || _toggleConsoleKey.IsPressed())
                {
                    Activate();
                }

                return;
            }

            ProcessAsyncTasks();
            ProcessActions();
            HandleAsyncJobCounter();
            ProcessZoomInput();

            if (_hideConsoleKey.IsPressed() || _toggleConsoleKey.IsPressed())
            {
                Deactivate();
                return;
            }

            if (AlicizaXConsoleProcessor.TableIsGenerating)
            {
                SetInputEnabled(false);
                string consoleText = $"{_logStorage.GetLogString()}\n{GetTableGenerationText()}".Trim();
                if (consoleText != _consoleLogText.text)
                {
                    if (_showInitLogs)
                    {
                        OnStateChange?.Invoke();
                        _consoleLogText.text = consoleText;
                    }
                    SetInputPlaceholder("Loading...");
                }

                return;
            }

            if (IsBlockedByAsync)
            {
                OnStateChange?.Invoke();
                SetInputEnabled(false);
                SetInputPlaceholder("Executing async command...");
            }
            else if (!_consoleInput.enabledSelf)
            {
                OnStateChange?.Invoke();
                SetInputEnabled(true);
                SetInputPlaceholder("Enter Command...");
                OverrideConsoleInput(string.Empty);

                if (_isGeneratingTable)
                {
                    if (_showInitLogs)
                    {
                        AppendLog(new AlicizaXConsoleLog(GetTableGenerationText()));
                        _consoleLogText.text = _logStorage.GetLogString();
                    }

                    _isGeneratingTable = false;
                    ScrollConsoleToLatest();
                }
            }

            _previousInput = _currentInput;
            _currentInput = _consoleInput.value;
            if (_currentInput != _previousInput)
            {
                OnInputChange();
            }
            else if (!IsBlockedByAsync)
            {
                if (InputHelper.GetKeyDown(_submitCommandKey)) { InvokeCommand(); }
                if (_storeCommandHistory)
                {
                    if (InputHelper.GetKeyDown(_nextCommandKey)) { SelectPreviousHistoryCommand(); }
                    else if (InputHelper.GetKeyDown(_previousCommandKey)) { SelectNextHistoryCommand(); }
                }
                ProcessAutocomplete();
            }
        }

        private void LateUpdate()
        {
            if (IsActive)
            {
                FlushQueuedLogs();
                FlushToConsoleText();
            }
        }

        public void ApplyAppearance()
        {
            if (!_uiReady || _container == null) { return; }

            ApplyPanelBackground(_panelColor);

            if (_font != null)
            {
                _container.style.unityFont = new StyleFont(_font);
            }

            _container.style.transformOrigin = new TransformOrigin(
                new Length(0, LengthUnit.Percent),
                new Length(100, LengthUnit.Percent));
        }

        private void ApplyPanelBackground(Color color)
        {
            StyleColor backgroundColor = new StyleColor(color);
            _container.style.backgroundColor = backgroundColor;
            _scrollView.style.backgroundColor = backgroundColor;

            VisualElement scrollViewport = _scrollView.Q<VisualElement>("unity-content-viewport");
            if (scrollViewport != null)
            {
                scrollViewport.style.backgroundColor = backgroundColor;
            }

            VisualElement scrollContent = _scrollView.Q<VisualElement>("unity-content-container");
            if (scrollContent != null)
            {
                scrollContent.style.backgroundColor = backgroundColor;
            }
        }

        private void EnsureDocument()
        {
            if (!_document) { _document = GetComponent<UIDocument>(); }
        }

        private void BuildOrBindUI()
        {
            if (_uiReady || !_document) { return; }

            _root = _document.rootVisualElement;
            if (_visualTree && _root.childCount == 0)
            {
                _visualTree.CloneTree(_root);
            }

            if (_styleSheet && !_root.styleSheets.Contains(_styleSheet))
            {
                _root.styleSheets.Add(_styleSheet);
            }

            _container = _root.Q<VisualElement>(ContainerName);
            if (_container == null)
            {
                BuildDefaultTree();
            }

            BindElements();
            RegisterUiCallbacks();
            ApplyAppearance();
            // 也把默认尺寸应用到已有容器上，避免存在 UXML 时尺寸不对。
            _container.style.width = _defaultSize.x;
            _container.style.height = _defaultSize.y;
            SetContainerVisible(false);
            _uiReady = true;
        }

        private void BuildDefaultTree()
        {
            _container = new VisualElement { name = ContainerName };
            _container.AddToClassList("qc-container");
            _root.Add(_container);

            _header = new VisualElement { name = HeaderName };
            _header.AddToClassList("qc-header");
            _zoomOutButton = new Button { name = ZoomOutButtonName, text = "-" };
            _zoomOutButton.AddToClassList("qc-header-button");
            _zoomLabel = new Label { name = ZoomLabelName, text = "100%" };
            _zoomLabel.AddToClassList("qc-zoom-label");
            _zoomInButton = new Button { name = ZoomInButtonName, text = "+" };
            _zoomInButton.AddToClassList("qc-header-button");
            _header.Add(_zoomOutButton);
            _header.Add(_zoomLabel);
            _header.Add(_zoomInButton);
            _container.Add(_header);

            _scrollView = new ScrollView(ScrollViewMode.Vertical) { name = LogScrollName };
            _scrollView.AddToClassList("qc-log-scroll");
            _consoleLogText = new Label { name = LogTextName };
            _consoleLogText.AddToClassList("qc-log-text");
            _scrollView.Add(_consoleLogText);
            _container.Add(_scrollView);

            _footer = new VisualElement { name = FooterName };
            _footer.AddToClassList("qc-footer");
            VisualElement inputRow = new VisualElement { name = InputRowName };
            inputRow.AddToClassList("qc-input-row");
            _consoleSuggestionText = new Label { name = SuggestionTextName };
            _consoleSuggestionText.AddToClassList("qc-suggestion-text");
            _inputPlaceholderText = new Label { name = PlaceholderName };
            _inputPlaceholderText.AddToClassList("qc-placeholder");
            _consoleInput = new TextField { name = InputName };
            _consoleInput.AddToClassList("qc-input");
            inputRow.Add(_consoleSuggestionText);
            inputRow.Add(_inputPlaceholderText);
            inputRow.Add(_consoleInput);
            _footer.Add(inputRow);

            _submitButton = new Button { name = SubmitButtonName, text = "Submit" };
            _clearButton = new Button { name = ClearButtonName, text = "Clear" };
            _closeButton = new Button { name = CloseButtonName, text = "Close" };
            _submitButton.AddToClassList("qc-footer-button");
            _clearButton.AddToClassList("qc-footer-button");
            _closeButton.AddToClassList("qc-footer-button");
            _footer.Add(_submitButton);
            _footer.Add(_clearButton);
            _footer.Add(_closeButton);
            _container.Add(_footer);

            _suggestionPopupText = new Label { name = PopupName };
            _suggestionPopupText.AddToClassList("qc-popup");
            _container.Add(_suggestionPopupText);

            _jobCounterText = new Label { name = JobCounterName };
            _jobCounterText.AddToClassList("qc-job-counter");
            _container.Add(_jobCounterText);

            _resizeHandle = new VisualElement { name = ResizeHandleName };
            _resizeHandle.AddToClassList("qc-resize-handle");
            VisualElement resizeGripA = new VisualElement();
            resizeGripA.AddToClassList("qc-resize-grip-line");
            resizeGripA.AddToClassList("qc-resize-grip-line-a");
            VisualElement resizeGripB = new VisualElement();
            resizeGripB.AddToClassList("qc-resize-grip-line");
            resizeGripB.AddToClassList("qc-resize-grip-line-b");
            VisualElement resizeGripC = new VisualElement();
            resizeGripC.AddToClassList("qc-resize-grip-line");
            resizeGripC.AddToClassList("qc-resize-grip-line-c");
            _resizeHandle.Add(resizeGripA);
            _resizeHandle.Add(resizeGripB);
            _resizeHandle.Add(resizeGripC);
            _container.Add(_resizeHandle);
        }

        private void BindElements()
        {
            _header = _root.Q<VisualElement>(HeaderName);
            _zoomOutButton = _root.Q<Button>(ZoomOutButtonName);
            _zoomLabel = _root.Q<Label>(ZoomLabelName);
            _zoomInButton = _root.Q<Button>(ZoomInButtonName);
            _scrollView = _root.Q<ScrollView>(LogScrollName);
            _footer = _root.Q<VisualElement>(FooterName);
            _consoleLogText = _root.Q<Label>(LogTextName);
            _consoleSuggestionText = _root.Q<Label>(SuggestionTextName);
            _inputPlaceholderText = _root.Q<Label>(PlaceholderName);
            _consoleInput = _root.Q<TextField>(InputName);
            _submitButton = _root.Q<Button>(SubmitButtonName);
            _clearButton = _root.Q<Button>(ClearButtonName);
            _closeButton = _root.Q<Button>(CloseButtonName);
            _suggestionPopupText = _root.Q<Label>(PopupName);
            _jobCounterText = _root.Q<Label>(JobCounterName);
            _resizeHandle = _root.Q<VisualElement>(ResizeHandleName);

            if (_header == null || _zoomOutButton == null || _zoomLabel == null || _zoomInButton == null ||
                _scrollView == null || _footer == null || _consoleLogText == null || _consoleSuggestionText == null ||
                _inputPlaceholderText == null || _consoleInput == null || _submitButton == null || _clearButton == null ||
                _closeButton == null || _suggestionPopupText == null || _jobCounterText == null)
            {
                _root.Clear();
                BuildDefaultTree();
            }

            _consoleLogText.enableRichText = true;
            _consoleSuggestionText.enableRichText = true;
            _suggestionPopupText.enableRichText = true;
            _consoleInput.multiline = false;
            SetInputPlaceholder("Enter Command...");
        }

        private void RegisterUiCallbacks()
        {
            _consoleInput.RegisterValueChangedCallback(OnInputValueChanged);
            _consoleInput.RegisterCallback<FocusInEvent>(_ => UpdatePlaceholderVisibility());
            _consoleInput.RegisterCallback<FocusOutEvent>(_ => UpdatePlaceholderVisibility());
            _consoleInput.RegisterCallback<KeyDownEvent>(OnInputKeyDown, TrickleDown.TrickleDown);
            _suggestionPopupText.RegisterCallback<PointerDownEvent>(OnSuggestionPointerDown);
            _submitButton.clicked += InvokeCommand;
            _clearButton.clicked += ClearConsole;
            _closeButton.clicked += Deactivate;
            _zoomOutButton.clicked += () => SetZoom(_zoom - _zoomStep);
            _zoomInButton.clicked += () => SetZoom(_zoom + _zoomStep);

            if (_allowDrag)
            {
                _container.RegisterCallback<PointerDownEvent>(OnContainerPointerDown);
                _container.RegisterCallback<PointerMoveEvent>(OnContainerPointerMove);
                _container.RegisterCallback<PointerUpEvent>(OnContainerPointerUp);
            }

            if (_allowResize && _resizeHandle != null)
            {
                _resizeHandle.RegisterCallback<PointerDownEvent>(OnResizePointerDown);
                _resizeHandle.RegisterCallback<PointerMoveEvent>(OnResizePointerMove);
                _resizeHandle.RegisterCallback<PointerUpEvent>(OnResizePointerUp);
            }
        }

        private void OnInputValueChanged(ChangeEvent<string> evt)
        {
            _previousInput = _currentInput;
            _currentInput = evt.newValue;
            UpdatePlaceholderVisibility();
            OnInputChange();
        }

        private void OnInputKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == _submitCommandKey)
            {
                InvokeCommand();
                evt.StopPropagation();
            }
            else if (_storeCommandHistory && evt.keyCode == _nextCommandKey)
            {
                SelectPreviousHistoryCommand();
                evt.StopPropagation();
            }
            else if (_storeCommandHistory && evt.keyCode == _previousCommandKey)
            {
                SelectNextHistoryCommand();
                evt.StopPropagation();
            }
            else if (_enableAutocomplete && MatchesCombo(evt, _selectNextSuggestionKey))
            {
                SelectSuggestionOffset(1);
                evt.StopPropagation();
            }
            else if (_enableAutocomplete && MatchesCombo(evt, _selectPreviousSuggestionKey))
            {
                SelectSuggestionOffset(-1);
                evt.StopPropagation();
            }
        }

        private bool MatchesCombo(KeyDownEvent evt, ModifierKeyCombo combo)
        {
            if (combo.Key == KeyCode.None || evt.keyCode != combo.Key) { return false; }
            return evt.ctrlKey == combo.Ctrl && evt.altKey == combo.Alt && evt.shiftKey == combo.Shift;
        }

        private void OnSuggestionPointerDown(PointerDownEvent evt)
        {
            SuggestionSet set = _suggestionStack?.TopmostSuggestionSet;
            if (set == null || set.Suggestions.Count == 0) { return; }

            float lineHeight = Mathf.Max(1, _suggestionPopupText.resolvedStyle.fontSize * 1.25f);
            int index = Mathf.FloorToInt(evt.localPosition.y / lineHeight);
            if (_suggestionDisplayOrder == SortOrder.Ascending)
            {
                int displaySize = GetSuggestionDisplaySize(set);
                index = displaySize - index - 1;
            }

            if (index >= 0 && index < set.Suggestions.Count)
            {
                SetSuggestion(index);
                FocusConsoleInput();
                evt.StopPropagation();
            }
        }

        private void OnContainerPointerDown(PointerDownEvent evt)
        {
            if (!_dragConsoleKey.ModifiersActive || evt.button != 0) { return; }
            _dragging = true;
            _pointerStart = new Vector2(evt.position.x, evt.position.y);
            _containerStart = new Vector2(_container.resolvedStyle.left, _container.resolvedStyle.top);
            _container.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnContainerPointerMove(PointerMoveEvent evt)
        {
            if (!_dragging) { return; }
            Vector2 pointer = new Vector2(evt.position.x, evt.position.y);
            Vector2 delta = pointer - _pointerStart;
            _container.style.left = _containerStart.x + delta.x;
            _container.style.top = _containerStart.y + delta.y;
            RequestClampContainerToScreen();
            evt.StopPropagation();
        }

        private void OnContainerPointerUp(PointerUpEvent evt)
        {
            if (!_dragging) { return; }
            _dragging = false;
            _container.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnResizePointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) { return; }
            _resizing = true;
            _pointerStart = new Vector2(evt.position.x, evt.position.y);
            _sizeStart = new Vector2(_container.resolvedStyle.width, _container.resolvedStyle.height);
            _resizeHandle.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnResizePointerMove(PointerMoveEvent evt)
        {
            if (!_resizing) { return; }
            Vector2 pointer = new Vector2(evt.position.x, evt.position.y);
            Vector2 delta = pointer - _pointerStart;
            _container.style.width = Mathf.Max(_minimumSize.x, _sizeStart.x + delta.x);
            _container.style.height = Mathf.Max(_minimumSize.y, _sizeStart.y - delta.y);
            RequestClampContainerToScreen();
            evt.StopPropagation();
        }

        private void OnResizePointerUp(PointerUpEvent evt)
        {
            if (!_resizing) { return; }
            _resizing = false;
            _resizeHandle.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void ProcessZoomInput()
        {
            if (!_allowZoom || _container == null) { return; }

            if (_zoomInKey.IsPressed())
            {
                SetZoom(_zoom + _zoomStep);
            }
            else if (_zoomOutKey.IsPressed())
            {
                SetZoom(_zoom - _zoomStep);
            }
        }

        private void SetZoom(float zoom)
        {
            _zoom = Mathf.Clamp(zoom, _minimumZoom, _maximumZoom);
            _container.style.scale = new Scale(new Vector3(_zoom, _zoom, 1));
            RequestClampContainerToScreen();
            if (_zoomLabel != null)
            {
                _zoomLabel.text = $"{Mathf.RoundToInt(_zoom * 100)}%";
            }
        }

        private void RequestClampContainerToScreen()
        {
            ClampContainerToScreen();
            _container?.schedule.Execute(ClampContainerToScreen);
        }

        private void ClampContainerToScreen()
        {
            if (_container == null || _root == null || _container.panel == null) { return; }

            Rect rootBounds = _root.worldBound;
            Rect containerBounds = _container.worldBound;
            if (rootBounds.width <= 0 || rootBounds.height <= 0 ||
                containerBounds.width <= 0 || containerBounds.height <= 0)
            {
                return;
            }

            float dx = 0f;
            if (containerBounds.width >= rootBounds.width || containerBounds.xMin < rootBounds.xMin)
            {
                dx = rootBounds.xMin - containerBounds.xMin;
            }
            else if (containerBounds.xMax > rootBounds.xMax)
            {
                dx = rootBounds.xMax - containerBounds.xMax;
            }

            float dy = 0f;
            if (containerBounds.height >= rootBounds.height)
            {
                dy = rootBounds.yMax - containerBounds.yMax;
            }
            else if (containerBounds.yMin < rootBounds.yMin)
            {
                dy = rootBounds.yMin - containerBounds.yMin;
            }
            else if (containerBounds.yMax > rootBounds.yMax)
            {
                dy = rootBounds.yMax - containerBounds.yMax;
            }

            if (Mathf.Approximately(dx, 0f) && Mathf.Approximately(dy, 0f)) { return; }

            float left = _container.resolvedStyle.left;
            float top = _container.resolvedStyle.top;
            if (float.IsNaN(left)) { left = _container.layout.x; }
            if (float.IsNaN(top)) { top = _container.layout.y; }

            _container.style.left = left + dx;
            _container.style.top = top + dy;
        }

        private string GetTableGenerationText()
        {
            string text = string.Format("S:\\>{0} commands have been loaded",
                AlicizaXConsoleProcessor.LoadedCommandCount);

            if (AlicizaXConsoleProcessor.TableIsGenerating)
            {
                text += "...";
            }
            else
            {
                string completionText = "S:\\>AlicizaX Console Processor ready".ColorText(_colorSuccess);
                text += $"\n{completionText}";
            }

            return text;
        }

        private void SelectPreviousHistoryCommand()
        {
            _selectedPreviousCommandIndex++;
            _selectedPreviousCommandIndex = Mathf.Clamp(_selectedPreviousCommandIndex, -1, _previousCommands.Count - 1);
            ApplySelectedHistoryCommand();
        }

        private void SelectNextHistoryCommand()
        {
            if (_selectedPreviousCommandIndex > 0) { _selectedPreviousCommandIndex--; }
            _selectedPreviousCommandIndex = Mathf.Clamp(_selectedPreviousCommandIndex, -1, _previousCommands.Count - 1);
            ApplySelectedHistoryCommand();
        }

        private void ApplySelectedHistoryCommand()
        {
            if (_selectedPreviousCommandIndex > -1)
            {
                string command = _previousCommands[_previousCommands.Count - _selectedPreviousCommandIndex - 1];
                OverrideConsoleInput(command);
            }
        }

        private void UpdateSuggestions()
        {
            if (_isHandlingUserResponse)
            {
                ClearSuggestions();
                ClearPopup();
                return;
            }

            SuggestorOptions options = new SuggestorOptions
            {
                CaseSensitive = _caseSensitiveSearch,
                Fuzzy = _useFuzzySearch,
                CollapseOverloads = _collapseSuggestionOverloads,
            };

            _suggestionStack.UpdateStack(_currentInput, options);

            UpdateSuggestionText();
            if (_showPopupDisplay)
            {
                UpdatePopupDisplay();
            }
        }

        private void ProcessAutocomplete()
        {
            if (!_enableAutocomplete) { return; }

            if (_selectNextSuggestionKey.IsPressed()) { SelectSuggestionOffset(1); }
            if (_selectPreviousSuggestionKey.IsPressed()) { SelectSuggestionOffset(-1); }
        }

        private void SelectSuggestionOffset(int offset)
        {
            SuggestionSet set = _suggestionStack.TopmostSuggestionSet;
            if (set == null || set.Suggestions.Count == 0) { return; }

            set.SelectionIndex += offset;
            set.SelectionIndex += set.Suggestions.Count;
            set.SelectionIndex %= set.Suggestions.Count;
            SetSuggestion(set.SelectionIndex);
        }

        private void FormatSuggestion(IAlicizaXConsoleSuggestion suggestion, bool selected, ref Utf16ValueStringBuilder buffer)
        {
            Color primaryColor = Color.white;
            Color secondaryColor = _colorSuggestion;
            if (selected)
            {
                primaryColor *= _colorSelectedSugg;
                secondaryColor *= _colorSelectedSugg;
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
                if (_maxSuggestionDisplaySize > 0 && i >= _maxSuggestionDisplaySize)
                {
                    const string remainingSuggestion = "...";
                    if (suggestionSet.SelectionIndex >= _maxSuggestionDisplaySize)
                    {
                        buffer.AppendColoredText(remainingSuggestion, _colorSelectedSugg);
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
            if (_maxSuggestionDisplaySize > 0)
            {
                displaySize = Mathf.Min(displaySize, _maxSuggestionDisplaySize + 1);
            }

            return displaySize;
        }

        private void UpdatePopupDisplay()
        {
            SuggestionSet suggestionSet = _suggestionStack.TopmostSuggestionSet;
            if (suggestionSet == null || suggestionSet.Suggestions.Count == 0)
            {
                ClearPopup();
                return;
            }

            string formattedSuggestions = GetFormattedSuggestions(suggestionSet);
            if (_suggestionDisplayOrder == SortOrder.Ascending)
            {
                formattedSuggestions = formattedSuggestions.ReverseItems('\n');
            }

            _suggestionPopupText.style.display = DisplayStyle.Flex;
            _suggestionPopupText.text = formattedSuggestions;
        }

        public void SetSuggestion(int suggestionIndex)
        {
            if (!_suggestionStack.SetSuggestionIndex(suggestionIndex))
            {
                throw new ArgumentException($"Cannot set suggestion to index {suggestionIndex}.");
            }

            OverrideConsoleInput(_suggestionStack.GetCompletion());
            UpdateSuggestionText();
        }

        private void UpdateSuggestionText()
        {
            Utf16ValueStringBuilder buffer = StringBuilderPool.GetStringBuilder();
            buffer.AppendColoredText(_currentInput, Color.clear);
            buffer.AppendColoredText(_suggestionStack.GetCompletionTail(), _colorSuggestion);

            _consoleSuggestionText.text = StringBuilderPool.ReleaseAndToString(buffer);
        }

        public void OverrideConsoleInput(string newInput, bool shouldFocus = true)
        {
            _currentInput = newInput;
            _previousInput = newInput;
            _consoleInput.SetValueWithoutNotify(newInput);
            UpdatePlaceholderVisibility();

            if (shouldFocus)
            {
                FocusConsoleInput();
            }

            OnInputChange();
        }

        public void FocusConsoleInput()
        {
            _consoleInput.Focus();
            _consoleInput.schedule.Execute(() =>
            {
                int textLength = _consoleInput.value?.Length ?? 0;
                _consoleInput.cursorIndex = textLength;
                _consoleInput.selectIndex = textLength;
            });
        }

        private void OnInputChange()
        {
            if (_selectedPreviousCommandIndex >= 0 && _currentInput.Trim() !=
                _previousCommands[_previousCommands.Count - _selectedPreviousCommandIndex - 1])
            {
                ClearHistoricalSuggestions();
            }

            if (_enableAutocomplete)
            {
                UpdateSuggestions();
            }
        }

        private void ClearHistoricalSuggestions()
        {
            _selectedPreviousCommandIndex = -1;
        }

        private void ClearSuggestions()
        {
            _suggestionStack.Clear();
            _consoleSuggestionText.text = string.Empty;
        }

        private void ClearPopup()
        {
            _suggestionPopupText.style.display = DisplayStyle.None;
            _suggestionPopupText.text = string.Empty;
        }

        public void InvokeCommand()
        {
            string userInput = _consoleInput.value;
            if (!string.IsNullOrWhiteSpace(userInput))
            {
                string command = userInput.Trim();

                if (_isHandlingUserResponse)
                {
                    HandleUserResponse(command);
                }
                else
                {
                    InvokeCommand(command);
                    OverrideConsoleInput(string.Empty);
                    StoreCommand(command);
                }
            }
        }

        private void HandleUserResponse(string command)
        {
            if (_currentResponseConfig.LogInput)
            {
                LogUserInput(command);
                StoreCommand(command);
            }

            _onSubmitResponseCallback(command);
            _onSubmitResponseCallback = null;
            SetInputEnabled(false);
            _isHandlingUserResponse = false;

            OnStateChange?.Invoke();
        }

        public void LogUserInput(string input)
        {
            AlicizaXConsoleLog commandLog = GenerateCommandLog(input);
            LogToConsole(commandLog);
        }

        protected AlicizaXConsoleLog GenerateCommandLog(string command)
        {
            if (command.Contains("<"))
            {
                command = $"<noparse>{command}</noparse>";
            }

            string logValue = string.Format(CommandLogFormat, command).ColorText(_colorCommandLog);
            return new AlicizaXConsoleLog(logValue);
        }

        public object InvokeCommand(string command)
        {
            object commandResult = null;
            if (!string.IsNullOrWhiteSpace(command))
            {
                LogUserInput(command);

                string logTrace = string.Empty;
                try
                {
                    commandResult = AlicizaXConsoleProcessor.InvokeCommand(command);

                    switch (commandResult)
                    {
                        case Task task: _currentTasks.Add(task); break;
                        case IEnumerator<ICommandAction> action: StartAction(action); break;
                        case IEnumerable<ICommandAction> action: StartAction(action.GetEnumerator()); break;
                        default: logTrace = Serialize(commandResult); break;
                    }
                }
                catch (System.Reflection.TargetInvocationException e) { logTrace = GetInvocationErrorMessage(e.InnerException); }
                catch (Exception e) { logTrace = GetErrorMessage(e); }

                LogToConsole(logTrace);
                OnInvoke?.Invoke(command);

                if (_autoScroll == AutoScrollOptions.OnInvoke) { ScrollConsoleToLatest(); }
                if (_closeOnSubmit) { Deactivate(); }
            }
            else
            {
                OverrideConsoleInput(string.Empty);
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

        private string GetErrorMessage(Exception e)
        {
            string message = _verboseErrors
                ? $"AlicizaXConsole Processor Error ({e.GetType()}): {e.Message}\n{e.StackTrace}"
                : $"AlicizaXConsole Processor Error: {e.Message}";
            return message.ColorText(_colorError);
        }

        private string GetInvocationErrorMessage(Exception e)
        {
            string message = _verboseErrors
                ? $"Error ({e.GetType()}): {e.Message}\n{e.StackTrace}"
                : $"Error: {e.Message}";
            return message.ColorText(_colorError);
        }

        public void LogToConsoleAsync(string logText, LogType logType = LogType.Log)
        {
            if (!string.IsNullOrWhiteSpace(logText))
            {
                AlicizaXConsoleLog log = new AlicizaXConsoleLog(logText, logType);
                LogToConsoleAsync(log);
            }
        }

        public void LogToConsoleAsync(AlicizaXConsoleLog log)
        {
            OnLog?.Invoke(log);
            _logQueue.QueueLog(log);
        }

        private void FlushQueuedLogs()
        {
            bool scroll = false;
            bool open = false;

            while (_logQueue.TryDequeue(out AlicizaXConsoleLog log))
            {
                AppendLog(log);
                LoggingThreshold severity = log.Type.ToLoggingThreshold();
                scroll |= _autoScroll == AutoScrollOptions.Always;
                open |= severity <= _openOnLogLevel;
            }

            if (scroll) { ScrollConsoleToLatest(); }
            if (open) { Activate(false); }
        }

        private void ProcessAsyncTasks()
        {
            for (int i = _currentTasks.Count - 1; i >= 0; i--)
            {
                if (_currentTasks[i].IsCompleted)
                {
                    if (_currentTasks[i].IsFaulted)
                    {
                        foreach (Exception e in _currentTasks[i].Exception.InnerExceptions)
                        {
                            string error = GetInvocationErrorMessage(e);
                            LogToConsole(error);
                        }
                    }
                    else
                    {
                        Type taskType = _currentTasks[i].GetType();
                        if (taskType.IsGenericTypeOf(typeof(Task<>)) && !_voidTaskType.IsAssignableFrom(taskType))
                        {
                            System.Reflection.PropertyInfo resultProperty = _currentTasks[i].GetType().GetProperty("Result");
                            object result = resultProperty.GetValue(_currentTasks[i]);
                            string log = Serialize(result);
                            LogToConsole(log);
                        }
                    }

                    _currentTasks.RemoveAt(i);
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
            SetInputPlaceholder(_currentResponseConfig.InputPrompt);
            FocusConsoleInput();
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

        private void ProcessActions()
        {
            if (_cancelActionsKey.IsPressed())
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
                    string error = GetInvocationErrorMessage(e);
                    LogToConsole(error);
                    break;
                }
            }
        }

        private void HandleAsyncJobCounter()
        {
            if (!_showCurrentJobs || _jobCounterText == null) { return; }

            if (_currentTasks.Count == 0)
            {
                _jobCounterText.style.display = DisplayStyle.None;
            }
            else
            {
                _jobCounterText.style.display = DisplayStyle.Flex;
                _jobCounterText.text = $"{_currentTasks.Count} job{(_currentTasks.Count == 1 ? "" : "s")} in progress";
            }
        }

        public string Serialize(object value)
        {
            string result = _serializer.SerializeFormatted(value, null);
            if (!string.IsNullOrWhiteSpace(result))
            {
                result = result.ColorText(_colorReturnDefault);
            }
            return result;
        }

        public void LogToConsole(string logText, bool newLine = true)
        {
            bool logExists = !string.IsNullOrEmpty(logText);
            if (logExists)
            {
                LogToConsole(new AlicizaXConsoleLog(logText, LogType.Log, newLine));
            }
        }

        public void LogToConsole(AlicizaXConsoleLog log)
        {
            FlushQueuedLogs();
            AppendLog(log);
            OnLog?.Invoke(log);

            if (_autoScroll == AutoScrollOptions.Always)
            {
                ScrollConsoleToLatest();
            }
        }

        private void FlushToConsoleText()
        {
            if (_consoleRequiresFlush)
            {
                _consoleRequiresFlush = false;
                _consoleLogText.text = _logStorage.GetLogString();
            }
        }

        private AlicizaXConsoleLog TruncateLog(AlicizaXConsoleLog log)
        {
            if (log.Text.Length <= _maxLogSize || _maxLogSize < 0)
            {
                return log;
            }

            string msg = string.Format("Log of size {0} exceeded the maximum log size of {1}", log.Text.Length, _maxLogSize)
                              .ColorText(_colorError);
            return new AlicizaXConsoleLog(msg, LogType.Error);
        }

        protected void AppendLog(AlicizaXConsoleLog log)
        {
            _logStorage.AddLog(TruncateLog(log));
            RequireFlush();
        }

        protected void RequireFlush()
        {
            _consoleRequiresFlush = true;
        }

        public void RemoveLogTrace()
        {
            _logStorage.RemoveLog();
            RequireFlush();
        }

        private void ScrollConsoleToLatest()
        {
            _scrollView.schedule.Execute(() =>
            {
                _scrollView.scrollOffset = new Vector2(_scrollView.scrollOffset.x, float.MaxValue);
            });
        }

        private void StoreCommand(string command)
        {
            if (_storeCommandHistory)
            {
                if (!_storeDuplicateCommands) { _previousCommands.Remove(command); }
                if (_storeAdjacentDuplicateCommands || _previousCommands.Count == 0 || _previousCommands[_previousCommands.Count - 1] != command) { _previousCommands.Add(command); }
                if (_commandHistorySize > 0 && _previousCommands.Count > _commandHistorySize) { _previousCommands.RemoveAt(0); }
            }
        }

        public void ClearConsole()
        {
            _logStorage.Clear();
            _logQueue.Clear();
            _consoleLogText.text = string.Empty;
            ClearBuffers();
            OnClear?.Invoke();
        }

        public string GetConsoleText()
        {
            return _consoleLogText.text;
        }

        protected virtual void ClearBuffers()
        {
            ClearHistoricalSuggestions();
            ClearSuggestions();
            ClearPopup();
        }

        private bool IsSupportedState()
        {
#if AlicizaXConsole_DISABLED
            return false;
#endif
            SupportedState currentState = SupportedState.Always;
#if DEVELOPMENT_BUILD
            currentState = SupportedState.Development;
#elif UNITY_EDITOR
            currentState = SupportedState.Editor;
#endif
            return _supportedState <= currentState;
        }

        private void DisableAlicizaXConsole()
        {
            Deactivate();
            enabled = false;
        }

        private void Initialize()
        {
            EnsureDocument();
            BuildOrBindUI();

            if (!AlicizaXConsoleProcessor.TableGenerated)
            {
                AlicizaXConsoleProcessor.GenerateCommandTableFromAssemblyNames(_commandAssemblyNames, true);
                SetInputEnabled(false);
                _isGeneratingTable = true;
            }

            InitializeSuggestionStack();
            InitializeLogging();
            ApplyAppearance();
        }

        private void InitializeSuggestionStack()
        {
            if (_suggestionStack == null)
            {
                _suggestionStack = CreateSuggestionStack();
                _suggestionStack.OnSuggestionSetCreated += OnSuggestionSetGenerated;
            }
        }

        private void InitializeLogging()
        {
            _logStorage = _logStorage ?? CreateLogStorage();
            _logQueue = _logQueue ?? CreateLogQueue();
        }

        protected virtual LogStorage CreateLogStorage() => new LogStorage(_maxStoredLogs);
        protected virtual LogQueue CreateLogQueue() => new LogQueue(_maxStoredLogs);
        protected virtual SuggestionStack CreateSuggestionStack() => new SuggestionStack();

        public void Toggle()
        {
            if (IsActive) { Deactivate(); }
            else { Activate(); }
        }

        public void Activate()
        {
            Activate(_focusOnActivate);
        }

        public void Activate(bool shouldFocus)
        {
            Initialize();
            IsActive = true;
            AlicizaXConsoleRouter.SetActive(this);
            SetContainerVisible(true);
            OverrideConsoleInput(string.Empty, shouldFocus);

            OnActivate?.Invoke();
        }

        public void Deactivate()
        {
            IsActive = false;
            SetContainerVisible(false);

            OnDeactivate?.Invoke();
        }

        private void DebugIntercept(string condition, string stackTrace, LogType type)
        {
            if (_interceptDebugLogger && (IsActive || _interceptWhilstInactive) && _loggingLevel >= type.ToLoggingThreshold())
            {
                bool appendStackTrace = _verboseLogging >= type.ToLoggingThreshold();
                AlicizaXConsoleLog log = ConstructDebugLog(condition, stackTrace, type, _prependTimestamps, appendStackTrace);
                LogToConsoleAsync(log);
            }
        }

        protected virtual AlicizaXConsoleLog ConstructDebugLog(string condition, string stackTrace, LogType type, bool prependTimeStamp, bool appendStackTrace)
        {
            if (prependTimeStamp)
            {
                DateTime now = DateTime.Now;
                condition = $"{string.Format(TimestampFormat, now.Hour, now.Minute, now.Second)} {condition}";
            }

            if (appendStackTrace)
            {
                condition += $"\n{stackTrace}";
            }

            switch (type)
            {
                case LogType.Warning:
                    condition = ColorExtensions.ColorText(condition, _colorWarning);
                    break;
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    condition = ColorExtensions.ColorText(condition, _colorError);
                    break;
            }

            return new AlicizaXConsoleLog(condition, type, true);
        }

        private void SetContainerVisible(bool visible)
        {
            if (_container != null)
            {
                _container.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void SetInputEnabled(bool enabled)
        {
            _consoleInput.SetEnabled(enabled);
        }

        private void SetInputPlaceholder(string placeholder)
        {
            _consoleInput.tooltip = placeholder;
            if (_inputPlaceholderText != null)
            {
                _inputPlaceholderText.text = placeholder;
                UpdatePlaceholderVisibility();
            }
        }

        private void UpdatePlaceholderVisibility()
        {
            if (_inputPlaceholderText == null || _consoleInput == null) { return; }

            bool showPlaceholder = string.IsNullOrEmpty(_consoleInput.value) && !IsFocused;
            _inputPlaceholderText.style.display = showPlaceholder ? DisplayStyle.Flex : DisplayStyle.None;
        }

        protected virtual void OnValidate()
        {
            MaxStoredLogs = _maxStoredLogs;
        }
    }
}
