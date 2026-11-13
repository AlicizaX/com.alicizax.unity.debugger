using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using TextAsset = UnityEngine.TextAsset;

namespace AlicizaX.Debugger
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Debugger")]
    public sealed partial class DebuggerComponent : MonoBehaviour
    {
        private const string DefaultWindowPath = "Console";
        private const string RootWindowTitle = "ALICIZAX DEBUGGER";
        private const string DebuggerRootName = "alicizax-debugger-root";
        private const string SettingsPanelName = "Debugger Runtime Panel Settings";
        private const string DefaultPanelSettingsResourcePath = "DebuggerPanelSettings";
        private const string SettingsEventSystemName = "Debugger Runtime EventSystem";
        private const string LayoutIconXKey = "Debugger.Icon.X";
        private const string LayoutIconYKey = "Debugger.Icon.Y";
        private const string LayoutIconSnapKey = "Debugger.Icon.Snap";
        private const string LayoutWindowXKey = "Debugger.Window.X";
        private const string LayoutWindowYKey = "Debugger.Window.Y";
        private const string LayoutWindowWidthKey = "Debugger.Window.Width";
        private const string LayoutWindowHeightKey = "Debugger.Window.Height";
        private const string LayoutWindowScaleKey = "Debugger.Window.Scale";
        private const int DefaultPanelSortingOrder = short.MaxValue - 64;
        private const float MinWindowWidth = 420f;
        private const float MinWindowHeight = 320f;
        private const float MinWindowScale = 0.5f;
        private const float ReferenceScreenWidth = 1920f;
        private const float ReferenceScreenHeight = 1080f;
        private const float ToggleClickMoveThreshold = 10f;
        private const float ToggleClickSuppressAfterDrag = 0.35f;
        private const float ToggleDoubleClickInterval = 0.3f;
        private const float ToggleDoubleClickMaxDistance = 24f;
        private const float ToggleSnapSmoothTime = 0.08f;
        private const float ToggleSnapStopDistance = 0.5f;
        private const float WindowScaleApplyDelay = 0.15f;
        private const float SidebarRowRadius = 8f;
        private const float SidebarRowGap = 3f;
        private const float SidebarSectionGap = 10f;
        private const float SidebarTreeGuideInset = 17f;

        private static DebuggerComponent _instance;

        internal static readonly Rect DefaultIconRect = new Rect(24f, 24f, 180f, 56f);
        internal static readonly Rect DefaultWindowRect = new Rect(24f, 96f, 1320f, 760f);
        internal static readonly float DefaultWindowScale = 1.2f;

        [SerializeField] private DebuggerActiveWindowType m_ActiveWindow = DebuggerActiveWindowType.AlwaysOpen;
        [SerializeField] private bool m_ShowFullWindow;
        [SerializeField, Range(0.2f, 1f)] private float m_WindowOpacity = 1f;
        [SerializeField] private bool m_EnableFloatingToggleSnap = true;
        [SerializeField] private PanelSettings m_PanelSettings;
        [SerializeField] private FontAsset m_FontAsset;
        [SerializeField] private Font m_Font;
        [SerializeField] private ConsoleWindow m_ConsoleWindow = new ConsoleWindow();
        [SerializeField] private CommandWindow m_CommandWindow = new CommandWindow();
        [Tooltip("额外扫描 [DebuggerWindow] 的程序集名。本包程序集始终会被扫描。")]
        [SerializeField] private string[] m_AutoRegisterAssemblyNames = new string[0];

        private readonly Dictionary<IDebuggerWindow, VisualElement> _viewByWindow = new Dictionary<IDebuggerWindow, VisualElement>(32);
        private readonly Dictionary<string, SidebarRowState> _sidebarRowStates = new Dictionary<string, SidebarRowState>(64);
        private readonly Dictionary<string, VisualElement> _sidebarChildContainers = new Dictionary<string, VisualElement>(32);
        private readonly Dictionary<string, bool> _expansionByPath = new Dictionary<string, bool>(32);
        private bool _sidebarDirty;

        private IDebuggerService _mDebuggerService;
        private DebuggerService _debuggerService;
        private Rect m_IconRect = DefaultIconRect;
        private Rect m_WindowRect = DefaultWindowRect;
        private float m_WindowScale = DefaultWindowScale;

        private InputInformationWindow m_InputInformationWindow = new InputInformationWindow();
        private TimeInformationWindow m_TimeInformationWindow = new TimeInformationWindow();
        private ProfilerInformationWindow m_ProfilerInformationWindow = new ProfilerInformationWindow();
        private RuntimeMemorySummaryWindow m_RuntimeMemorySummaryWindow = new RuntimeMemorySummaryWindow();
        private RuntimeMemoryInformationWindow<Object> m_RuntimeMemoryAllInformationWindow = new RuntimeMemoryInformationWindow<Object>();
        private RuntimeMemoryInformationWindow<Texture> m_RuntimeMemoryTextureInformationWindow = new RuntimeMemoryInformationWindow<Texture>();
        private RuntimeMemoryInformationWindow<Mesh> m_RuntimeMemoryMeshInformationWindow = new RuntimeMemoryInformationWindow<Mesh>();
        private RuntimeMemoryInformationWindow<Material> m_RuntimeMemoryMaterialInformationWindow = new RuntimeMemoryInformationWindow<Material>();
        private RuntimeMemoryInformationWindow<Shader> m_RuntimeMemoryShaderInformationWindow = new RuntimeMemoryInformationWindow<Shader>();
        private RuntimeMemoryInformationWindow<AnimationClip> m_RuntimeMemoryAnimationClipInformationWindow = new RuntimeMemoryInformationWindow<AnimationClip>();
        private RuntimeMemoryInformationWindow<AudioClip> m_RuntimeMemoryAudioClipInformationWindow = new RuntimeMemoryInformationWindow<AudioClip>();
        private RuntimeMemoryInformationWindow<Font> m_RuntimeMemoryFontInformationWindow = new RuntimeMemoryInformationWindow<Font>();
        private RuntimeMemoryInformationWindow<TextAsset> m_RuntimeMemoryTextAssetInformationWindow = new RuntimeMemoryInformationWindow<TextAsset>();
        private RuntimeMemoryInformationWindow<ScriptableObject> m_RuntimeMemoryScriptableObjectInformationWindow = new RuntimeMemoryInformationWindow<ScriptableObject>();
        private ReferencePoolInformationWindow m_ReferencePoolInformationWindow = new ReferencePoolInformationWindow();
        private AudioInformationWindow m_AudioInformationWindow = new AudioInformationWindow();
        private TimerInformationWindow m_TimerInformationWindow = new TimerInformationWindow();
        private SettingsWindow m_SettingsWindow = new SettingsWindow();
        private FpsCounter m_FpsCounter;
        private StatsOverlay m_StatsOverlay;

        private PanelSettings _panelSettings;
        private PanelTextSettings _panelTextSettings;
        private Font _runtimeSystemFont;
        private UIDocument _uiDocument;
        private EventSystem _createdEventSystem;
        private VisualElement _root;
        private VisualElement _overlay;
        private VisualElement _window;
        private VisualElement _sidebar;
        private ScrollView _sidebarScrollView;
        private VisualElement _contentHost;
        private Label _headerTitle;
        private VisualElement _toggleButton;
        private Label _toggleFpsLabel;
        private Button _resetLayoutButton;
        private Button _closeButton;
        private VisualElement _resizeHandle;
        private IDebuggerWindow _activeWindow;
        private string _activePath;
        private VisualElement _activeSidebarElement;

        private Vector2 _dragPointerStart;
        private Vector2 _dragWindowStart;
        private bool _isResizeActive;
        private Vector2 _resizePointerStart;
        private Vector2 _resizeWindowSizeStart;
        private float _toggleOpenSuppressUntil;
        private bool _isToggleDragging;
        private bool _isToggleSnapAnimating;
        private Vector2 _toggleSnapTargetPosition;
        private Vector2 _toggleSnapVelocity;
        private float _lastToggleTapTime = -1f;
        private Vector2 _lastToggleTapPosition;
        private float _toggleFpsValue = float.NegativeInfinity;
        private Color _toggleFpsColor;
        private bool _hasToggleFpsValue;
        private bool _isWindowScaleApplyPending;
        private float _windowScaleApplyAt;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        private float _lastSavedIconX;
        private float _lastSavedIconY;
        private float _lastSavedWindowX;
        private float _lastSavedWindowY;
        private float _lastSavedWindowWidth;
        private float _lastSavedWindowHeight;
        private float _lastSavedWindowScale;
        private bool _lastSavedEnableFloatingToggleSnap;
        public static DebuggerComponent Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityObjectQuery.FindFirst<DebuggerComponent>();
                }

                return _instance;
            }
        }

        public bool ActiveWindow
        {
            get => _mDebuggerService != null && _mDebuggerService.ActiveWindow;
            set
            {
                if (_mDebuggerService == null)
                {
                    return;
                }

                _mDebuggerService.ActiveWindow = value;
                enabled = value;
                SyncWindowVisibility();
            }
        }

        public bool ShowFullWindow
        {
            get => m_ShowFullWindow;
            set
            {
                if (m_ShowFullWindow == value)
                {
                    return;
                }

                m_ShowFullWindow = value;
                ApplyWindowScale();
                RebuildRuntimeVisualTree();
                SyncWindowVisibility();
            }
        }

        public float WindowOpacity
        {
            get => m_WindowOpacity;
            set
            {
                float clampedValue = Mathf.Clamp(value, 0.2f, 1f);
                if (Mathf.Approximately(m_WindowOpacity, clampedValue))
                {
                    return;
                }

                m_WindowOpacity = clampedValue;
                ApplyWindowOpacity();
            }
        }

        public Font CustomFont
        {
            get => m_Font;
            set
            {
                if (m_Font == value)
                {
                    return;
                }

                m_Font = value;
                ApplyFont();
            }
        }

        public FontAsset CustomFontAsset
        {
            get => m_FontAsset;
            set
            {
                if (m_FontAsset == value)
                {
                    return;
                }

                m_FontAsset = value;
                ApplyFont();
            }
        }

        public bool EnableFloatingToggleSnap
        {
            get => m_EnableFloatingToggleSnap;
            set
            {
                if (m_EnableFloatingToggleSnap == value)
                {
                    return;
                }

                m_EnableFloatingToggleSnap = value;
                if (_isToggleDragging)
                {
                    return;
                }

                if (m_EnableFloatingToggleSnap)
                {
                    SnapIconToEdge();
                    SaveFloatingToggleSnapSettings();
                    return;
                }

                _isToggleSnapAnimating = false;
                _toggleSnapVelocity = Vector2.zero;
                ApplyToggleRect();
                ApplyToggleVisualOffset(Vector2.zero);
                SaveFloatingToggleSnapSettings();
            }
        }

        public Rect IconRect
        {
            get => m_IconRect;
            set
            {
                m_IconRect = ClampIconRect(value);
                ApplyToggleRect();
            }
        }

        public Rect WindowRect
        {
            get => m_WindowRect;
            set
            {
                m_WindowRect = ClampWindowRect(value);
                ApplyWindowRect();
            }
        }

        public float WindowScale
        {
            get => m_WindowScale;
            set
            {
                float clampedValue = Mathf.Max(value, MinWindowScale);
                if (Mathf.Approximately(m_WindowScale, clampedValue))
                {
                    return;
                }

                m_WindowScale = clampedValue;
                ApplyWindowScale();
                SaveLayoutScaleSettings();

                // 视觉树里所有尺寸都在构建时按 UI 缩放烘焙，改缩放必须重建整棵树；
                // 设置面板的滑杆拖动会逐帧触发，这里合并成"停止拖动后重建一次"。
                _isWindowScaleApplyPending = true;
                _windowScaleApplyAt = Time.unscaledTime + WindowScaleApplyDelay;
            }
        }

        private void Awake()
        {
            _instance = this;

            // AppServices.Register 在服务容器未创建时会抛异常，先显式判定前置条件，
            // 避免把"框架没启动"表现成 Awake 里的 InvalidOperationException。
            if (!AppServices.IsAvailable)
            {
                Log.Error("Debugger requires an initialized service registry. Component disabled.");
                enabled = false;
                return;
            }

            if (!AppServices.TryGet<IDebuggerService>(out _mDebuggerService))
            {
                _mDebuggerService = AppServices.Register<IDebuggerService>(new DebuggerService());
            }

            _debuggerService = _mDebuggerService as DebuggerService;

            m_FpsCounter = new FpsCounter(0.5f);
            m_StatsOverlay = new StatsOverlay(this);
            LoadLayoutSettings();
            BuildRuntimePanel();
        }

        private void OnValidate()
        {
            m_WindowOpacity = Mathf.Clamp(m_WindowOpacity, 0.2f, 1f);
            m_WindowScale = Mathf.Max(m_WindowScale, MinWindowScale);

            if (!Application.isPlaying)
            {
                return;
            }

            m_IconRect = ClampIconRect(m_IconRect);
            m_WindowRect = ClampWindowRect(m_WindowRect);
            ApplyWindowRect();
            ApplyWindowOpacity();
            ApplyFont();

            if (m_EnableFloatingToggleSnap && !_isToggleDragging)
            {
                SnapIconToEdge();
            }
            else
            {
                ApplyToggleRect();
                ApplyToggleVisualOffset(Vector2.zero);
            }
        }

        internal void SetActiveMode(DebuggerActiveWindowType activeWindowType)
        {
            m_ActiveWindow = activeWindowType;
        }

        private void Start()
        {
            RegisterBuiltInWindows();
            RegisterDiscoveredWindows();
            if (_sidebarDirty)
            {
                RebuildSidebar();
            }

            ApplyActiveMode();
            m_ShowFullWindow = false;
            SyncWindowVisibility();
        }

        private void Update()
        {
            if (m_FpsCounter != null)
            {
                m_FpsCounter.Update(Time.deltaTime, Time.unscaledDeltaTime);
            }

            m_StatsOverlay?.Tick(Time.unscaledDeltaTime);

            ApplyPendingWindowScale();
            if (_sidebarDirty)
            {
                RebuildSidebar();
            }

            UpdateToggleState();
            UpdateToggleSnapAnimation(Time.unscaledDeltaTime);
            HandleScreenSizeChanged();

            m_CommandWindow?.TickRuntime();

            if (_mDebuggerService == null || !_mDebuggerService.ActiveWindow)
            {
                return;
            }

            if (_activeWindow != null)
            {
                _activeWindow.OnUpdate(Time.deltaTime, Time.unscaledDeltaTime);
            }
        }

        private void OnEnable()
        {
            SyncWindowVisibility();
        }

        private void OnDisable()
        {
            SyncWindowVisibility();
        }

        private void OnDestroy()
        {
            if (_activeWindow != null)
            {
                _activeWindow.OnLeave();
                _activeWindow = null;
            }

            if (_mDebuggerService?.DebuggerWindowRoot is DebuggerWindowGroup root)
            {
                List<string> windowPaths = new List<string>(32);
                root.CollectWindowPaths(windowPaths);
                for (int i = 0; i < windowPaths.Count; i++)
                {
                    // 解除注册会一并 Shutdown 窗口。组件销毁后必须归还路径，
                    // 否则同一服务容器内重建组件时注册会抛"Debugger window has been registered."。
                    _mDebuggerService.UnregisterDebuggerWindow(windowPaths[i]);
                }
            }

            _viewByWindow.Clear();
            _sidebarRowStates.Clear();
            _sidebarChildContainers.Clear();
            _expansionByPath.Clear();

            if (m_StatsOverlay != null)
            {
                m_StatsOverlay.Dispose();
                m_StatsOverlay = null;
            }

            if (_uiDocument != null)
            {
                Destroy(_uiDocument);
                _uiDocument = null;
            }

            if (_panelSettings != null)
            {
                Destroy(_panelSettings);
                _panelSettings = null;
            }

            if (_panelTextSettings != null)
            {
                Destroy(_panelTextSettings);
                _panelTextSettings = null;
            }

            if (_runtimeSystemFont != null)
            {
                Destroy(_runtimeSystemFont);
                _runtimeSystemFont = null;
            }

            if (_createdEventSystem != null)
            {
                Destroy(_createdEventSystem.gameObject);
                _createdEventSystem = null;
            }

            if (_instance == this)
            {
                _instance = null;
            }
        }

        public void RegisterDebuggerWindow(string path, IDebuggerWindow debuggerWindow, params object[] args)
        {
            if (_mDebuggerService == null)
            {
                throw new GameFrameworkException("Debugger service is invalid.");
            }

            _mDebuggerService.RegisterDebuggerWindow(path, debuggerWindow, args);
            _sidebarDirty = true;
        }

        public bool UnregisterDebuggerWindow(string path)
        {
            IDebuggerWindow window = GetDebuggerWindow(path);
            if (window == null || _mDebuggerService == null)
            {
                return false;
            }

            if (!_mDebuggerService.UnregisterDebuggerWindow(path))
            {
                return false;
            }

            _viewByWindow.Remove(window);
            if (ReferenceEquals(_activeWindow, window))
            {
                _activeWindow = null;
                _activePath = null;
                _contentHost?.Clear();
            }

            _sidebarDirty = true;
            return true;
        }

        public IDebuggerWindow GetDebuggerWindow(string path)
        {
            return _mDebuggerService?.GetDebuggerWindow(path);
        }

        public bool SelectDebuggerWindow(string path)
        {
            IDebuggerWindow window = GetDebuggerWindow(path);
            if (window == null)
            {
                return false;
            }

            if (!_mDebuggerService.SelectDebuggerWindow(path))
            {
                return false;
            }

            return SelectWindow(window);
        }

        public void ResetLayout()
        {
            IconRect = DefaultIconRect;
            if (m_EnableFloatingToggleSnap)
            {
                SnapIconToEdge();
            }
            WindowRect = DefaultWindowRect;
            WindowScale = DefaultWindowScale;
            RebuildRuntimeVisualTree();
            SaveLayoutSettings();
        }

        private void LoadLayoutSettings()
        {
            float iconX = Utility.PlayerPrefsX.GetFloat(LayoutIconXKey, DefaultIconRect.x);
            float iconY = Utility.PlayerPrefsX.GetFloat(LayoutIconYKey, DefaultIconRect.y);
            float windowX = Utility.PlayerPrefsX.GetFloat(LayoutWindowXKey, DefaultWindowRect.x);
            float windowY = Utility.PlayerPrefsX.GetFloat(LayoutWindowYKey, DefaultWindowRect.y);
            float windowWidth = Utility.PlayerPrefsX.GetFloat(LayoutWindowWidthKey, DefaultWindowRect.width);
            float windowHeight = Utility.PlayerPrefsX.GetFloat(LayoutWindowHeightKey, DefaultWindowRect.height);
            float windowScale = Utility.PlayerPrefsX.GetFloat(LayoutWindowScaleKey, DefaultWindowScale);

            if (Utility.PlayerPrefsX.HasSetting(LayoutIconSnapKey))
            {
                m_EnableFloatingToggleSnap = Utility.PlayerPrefsX.GetBool(LayoutIconSnapKey, m_EnableFloatingToggleSnap);
            }

            m_WindowScale = Mathf.Max(windowScale, MinWindowScale);
            m_IconRect = ClampIconRect(new Rect(iconX, iconY, DefaultIconRect.width, DefaultIconRect.height));
            m_WindowRect = ClampWindowRect(new Rect(windowX, windowY, windowWidth, windowHeight));
            MarkLayoutSettingsSaved();
        }

        private void SaveLayoutSettings()
        {
            SaveIconLayoutSettings();
            SaveWindowLayoutSettings();
            SaveLayoutScaleSettings();
            SaveFloatingToggleSnapSettings();
        }

        private void SaveIconLayoutSettings()
        {
            if (!Mathf.Approximately(_lastSavedIconX, m_IconRect.x))
            {
                _lastSavedIconX = m_IconRect.x;
                Utility.PlayerPrefsX.SetFloat(LayoutIconXKey, m_IconRect.x);
            }

            if (!Mathf.Approximately(_lastSavedIconY, m_IconRect.y))
            {
                _lastSavedIconY = m_IconRect.y;
                Utility.PlayerPrefsX.SetFloat(LayoutIconYKey, m_IconRect.y);
            }
        }

        private void SaveWindowLayoutSettings()
        {
            if (!Mathf.Approximately(_lastSavedWindowX, m_WindowRect.x))
            {
                _lastSavedWindowX = m_WindowRect.x;
                Utility.PlayerPrefsX.SetFloat(LayoutWindowXKey, m_WindowRect.x);
            }

            if (!Mathf.Approximately(_lastSavedWindowY, m_WindowRect.y))
            {
                _lastSavedWindowY = m_WindowRect.y;
                Utility.PlayerPrefsX.SetFloat(LayoutWindowYKey, m_WindowRect.y);
            }

            if (!Mathf.Approximately(_lastSavedWindowWidth, m_WindowRect.width))
            {
                _lastSavedWindowWidth = m_WindowRect.width;
                Utility.PlayerPrefsX.SetFloat(LayoutWindowWidthKey, m_WindowRect.width);
            }

            if (!Mathf.Approximately(_lastSavedWindowHeight, m_WindowRect.height))
            {
                _lastSavedWindowHeight = m_WindowRect.height;
                Utility.PlayerPrefsX.SetFloat(LayoutWindowHeightKey, m_WindowRect.height);
            }
        }

        private void SaveLayoutScaleSettings()
        {
            if (Mathf.Approximately(_lastSavedWindowScale, m_WindowScale))
            {
                return;
            }

            _lastSavedWindowScale = m_WindowScale;
            Utility.PlayerPrefsX.SetFloat(LayoutWindowScaleKey, m_WindowScale);
        }

        private void SaveFloatingToggleSnapSettings()
        {
            if (_lastSavedEnableFloatingToggleSnap == m_EnableFloatingToggleSnap)
            {
                return;
            }

            _lastSavedEnableFloatingToggleSnap = m_EnableFloatingToggleSnap;
            Utility.PlayerPrefsX.SetBool(LayoutIconSnapKey, m_EnableFloatingToggleSnap);
        }

        private void MarkLayoutSettingsSaved()
        {
            _lastSavedIconX = m_IconRect.x;
            _lastSavedIconY = m_IconRect.y;
            _lastSavedWindowX = m_WindowRect.x;
            _lastSavedWindowY = m_WindowRect.y;
            _lastSavedWindowWidth = m_WindowRect.width;
            _lastSavedWindowHeight = m_WindowRect.height;
            _lastSavedWindowScale = m_WindowScale;
            _lastSavedEnableFloatingToggleSnap = m_EnableFloatingToggleSnap;
        }

        /// <summary>
        /// 获取最近的日志结点。
        /// </summary>
        /// <remarks>
        /// 返回值是 MemoryPool 复用的对象：日志被裁剪或再次入队时会被 <c>Clear()</c> 并重新使用。
        /// 调用方只能在当前帧读取，不得跨帧缓存这些引用。
        /// </remarks>
        public void GetRecentLogs(List<LogNode> results)
        {
            m_ConsoleWindow.GetRecentLogs(results);
        }

        /// <summary>
        /// 获取最近的日志结点（最多 <paramref name="count"/> 条）。
        /// </summary>
        /// <remarks>返回的是池化对象，生命周期同上：只在当前帧有效。</remarks>
        public void GetRecentLogs(List<LogNode> results, int count)
        {
            m_ConsoleWindow.GetRecentLogs(results, count);
        }

        private void RegisterBuiltInWindows()
        {
            RegisterDebuggerWindow("Console", m_ConsoleWindow);
            RegisterDebuggerWindow("Command", m_CommandWindow);
            RegisterDebuggerWindow("Information/Input", m_InputInformationWindow);
            RegisterDebuggerWindow("Information/Other/Time", m_TimeInformationWindow);
            RegisterDebuggerWindow("Profiler/Summary", m_ProfilerInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/Summary", m_RuntimeMemorySummaryWindow);
            RegisterDebuggerWindow("Profiler/Memory/All", m_RuntimeMemoryAllInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/Texture", m_RuntimeMemoryTextureInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/Mesh", m_RuntimeMemoryMeshInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/Material", m_RuntimeMemoryMaterialInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/Shader", m_RuntimeMemoryShaderInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/AnimationClip", m_RuntimeMemoryAnimationClipInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/AudioClip", m_RuntimeMemoryAudioClipInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/Font", m_RuntimeMemoryFontInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/TextAsset", m_RuntimeMemoryTextAssetInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/ScriptableObject", m_RuntimeMemoryScriptableObjectInformationWindow);
            RegisterDebuggerWindow("Profiler/Reference Pool", m_ReferencePoolInformationWindow);
            RegisterDebuggerWindow("Profiler/Audio", m_AudioInformationWindow);
            RegisterDebuggerWindow("Profiler/Timer", m_TimerInformationWindow);
            RegisterDebuggerWindow("Other/Settings", m_SettingsWindow);
        }

        /// <summary>
        /// 扫描带 <see cref="DebuggerWindowAttribute"/> 的窗口并注册；手动注册的路径优先生效。
        /// </summary>
        private void RegisterDiscoveredWindows()
        {
            if (_debuggerService == null)
            {
                Log.Warning("Debugger: auto-registration skipped, IDebuggerService is not the built-in implementation.");
                return;
            }

            List<DebuggerWindowScanner.DiscoveredWindow> discovered = new List<DebuggerWindowScanner.DiscoveredWindow>(16);
            DebuggerWindowScanner.Discover(m_AutoRegisterAssemblyNames, discovered);

            for (int i = 0; i < discovered.Count; i++)
            {
                DebuggerWindowScanner.DiscoveredWindow candidate = discovered[i];

                IDebuggerWindow window;
                try
                {
                    // 内置窗口是 DebuggerComponent 的私有嵌套类型，必须允许非公开构造。
                    window = (IDebuggerWindow)Activator.CreateInstance(candidate.Type, nonPublic: true);
                }
                catch (Exception e)
                {
                    Log.Warning(Utility.Text.Format(
                        "Debugger: failed to create window '{0}' for path '{1}': {2}",
                        candidate.Type.FullName,
                        candidate.Path,
                        e.Message));
                    continue;
                }

                if (!_debuggerService.TryRegisterDebuggerWindow(candidate.Path, window))
                {
                    Log.Warning(Utility.Text.Format(
                        "Debugger: path '{0}' is already registered, '{1}' was skipped.",
                        candidate.Path,
                        candidate.Type.FullName));
                    continue;
                }

                _sidebarDirty = true;
            }
        }

        private void ApplyActiveMode()
        {
            switch (m_ActiveWindow)
            {
                case DebuggerActiveWindowType.AlwaysOpen:
                    ActiveWindow = true;
                    break;
                case DebuggerActiveWindowType.OnlyOpenWhenDevelopment:
                    ActiveWindow = Debug.isDebugBuild;
                    break;
                case DebuggerActiveWindowType.OnlyOpenInEditor:
                    ActiveWindow = Application.isEditor;
                    break;
                default:
                    ActiveWindow = false;
                    break;
            }
        }

        private void BuildRuntimePanel()
        {
            CreateEventSystem();

            _panelSettings = CreateRuntimePanelSettings();
            ApplyRuntimePanelSettings(_panelSettings);

            _uiDocument = gameObject.GetOrAddComponent<UIDocument>();

            _uiDocument.panelSettings = _panelSettings;
            _uiDocument.sortingOrder = DefaultPanelSortingOrder;
            BuildRootVisualTree();
        }

        private PanelSettings CreateRuntimePanelSettings()
        {
            PanelSettings source = m_PanelSettings != null
                ? m_PanelSettings
                : Resources.Load<PanelSettings>(DefaultPanelSettingsResourcePath);

            if (source != null)
            {
                PanelSettings instance = Instantiate(source);
                instance.name = SettingsPanelName;
                return instance;
            }

            PanelSettings fallback = ScriptableObject.CreateInstance<PanelSettings>();
            fallback.name = SettingsPanelName;
            InitializePanelSettingsDefaults(fallback);
            return fallback;
        }

        private void ApplyRuntimePanelSettings(PanelSettings panelSettings)
        {
            if (panelSettings == null)
            {
                return;
            }

            panelSettings.sortingOrder = DefaultPanelSortingOrder;
            panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
            panelSettings.scale = 1f;
            panelSettings.referenceDpi = 96f;
            panelSettings.fallbackDpi = 96f;
            panelSettings.clearColor = false;
            panelSettings.targetTexture = null;
            panelSettings.textSettings = ResolvePanelTextSettings();
        }

#if UNITY_6000_5_OR_NEWER
        private static readonly PropertyInfo DefaultFontAssetProperty =
            typeof(PanelTextSettings).GetProperty("defaultFontAsset");
#endif

        private PanelTextSettings ResolvePanelTextSettings()
        {
            if (_panelTextSettings == null)
            {
                _panelTextSettings = ScriptableObject.CreateInstance<PanelTextSettings>();
                _panelTextSettings.name = "Debugger Runtime Panel TextSettings";
                _panelTextSettings.hideFlags = HideFlags.HideAndDontSave;
                _panelTextSettings.fallbackFontAssets = new System.Collections.Generic.List<FontAsset>();
            }

            FontAsset safeFont = ResolveSafeFontAsset();
            if (safeFont != null)
            {
#if UNITY_6000_5_OR_NEWER
                // Unity 6000.5 起 TextSettings.defaultFontAsset 标记废弃，官方未提供替代属性（只剩
                // defaultFontAssetPath 与 fallbackFontAssets）。属性本身仍可写，这里用反射写入以保持原有
                // 显示效果；同时并入 fallback 链，保证将来该属性被移除后仍有可用字体。
                DefaultFontAssetProperty?.SetValue(_panelTextSettings, safeFont);
                if (_panelTextSettings.fallbackFontAssets == null)
                {
                    _panelTextSettings.fallbackFontAssets = new System.Collections.Generic.List<FontAsset>();
                }

                if (!_panelTextSettings.fallbackFontAssets.Contains(safeFont))
                {
                    _panelTextSettings.fallbackFontAssets.Add(safeFont);
                }
#else
                _panelTextSettings.defaultFontAsset = safeFont;
#endif
            }

            return _panelTextSettings;
        }

        private FontAsset ResolveSafeFontAsset()
        {
            if (IsFontAssetUsable(m_FontAsset))
            {
                return m_FontAsset;
            }

            return null;
        }

        private Font ResolveRuntimeFont()
        {
            if (m_Font != null)
            {
                return m_Font;
            }

            if (_runtimeSystemFont != null)
            {
                return _runtimeSystemFont;
            }

            _runtimeSystemFont = CreateSystemFallbackFont();
            return _runtimeSystemFont;
        }

        private static Font CreateSystemFallbackFont()
        {
            string[] candidates;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            candidates = new[]
            {
                "Segoe UI",
                "Microsoft YaHei UI",
                "Microsoft YaHei",
                "Arial",
                "Tahoma",
            };
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IOS
            candidates = new[]
            {
                "PingFang SC",
                "Hiragino Sans GB",
                "Helvetica Neue",
                "Helvetica",
                "Arial",
            };
#elif UNITY_ANDROID
            candidates = new[]
            {
                "Roboto",
                "Noto Sans CJK SC",
                "Droid Sans Fallback",
                "Arial",
            };
#else
            candidates = new[]
            {
                "Arial",
                "Helvetica",
                "Roboto",
                "Sans-Serif",
            };
#endif

            Exception fontCreationError = null;
            for (int i = 0; i < candidates.Length; i++)
            {
                try
                {
                    Font font = Font.CreateDynamicFontFromOSFont(candidates[i], 16);
                    if (font != null)
                    {
                        font.hideFlags = HideFlags.HideAndDontSave;
                        font.name = "Debugger System Font (" + candidates[i] + ")";
                        return font;
                    }
                }
                catch (Exception e)
                {
                    fontCreationError = e;
                }
            }

            try
            {
                string[] names = Font.GetOSInstalledFontNames();
                if (names != null)
                {
                    for (int i = 0; i < names.Length; i++)
                    {
                        string name = names[i];
                        if (string.IsNullOrEmpty(name))
                        {
                            continue;
                        }

                        try
                        {
                            Font font = Font.CreateDynamicFontFromOSFont(name, 16);
                            if (font != null)
                            {
                                font.hideFlags = HideFlags.HideAndDontSave;
                                font.name = "Debugger System Font (" + name + ")";
                                return font;
                            }
                        }
                        catch (Exception e)
                        {
                            fontCreationError = e;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                fontCreationError = e;
            }

            if (fontCreationError != null)
            {
                Log.Warning(Utility.Text.Format("Debugger could not create an OS font, falling back to builtin Arial: {0}", fontCreationError.Message));
            }

            Font builtin = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return builtin;
        }

        internal static bool IsFontAssetUsable(FontAsset fontAsset)
        {
            if (fontAsset == null)
            {
                return false;
            }

            try
            {
                Texture2D[] atlases = fontAsset.atlasTextures;
                if (atlases == null || atlases.Length == 0)
                {
                    return false;
                }

                for (int i = 0; i < atlases.Length; i++)
                {
                    Texture2D atlas = atlases[i];
                    if (atlas == null)
                    {
                        return false;
                    }

                    _ = atlas.name;
                    if (atlas.width <= 0 || atlas.height <= 0)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (MissingReferenceException)
            {
                return false;
            }
            catch (UnityException)
            {
                return false;
            }
        }

        internal StyleFontDefinition ResolveFontDefinition()
        {
            FontAsset safeFontAsset = ResolveSafeFontAsset();
            if (safeFontAsset != null)
            {
                return new StyleFontDefinition(safeFontAsset);
            }

            Font runtimeFont = ResolveRuntimeFont();
            if (runtimeFont != null)
            {
                return new StyleFontDefinition(FontDefinition.FromFont(runtimeFont));
            }

            return new StyleFontDefinition(StyleKeyword.Null);
        }

        private void CreateEventSystem()
        {
            EventSystem existing = UnityObjectQuery.FindFirst<EventSystem>();
            if (existing != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject(SettingsEventSystemName);
            _createdEventSystem = eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        private void BuildRootVisualTree()
        {
            _root = _uiDocument.rootVisualElement;
            _root.Clear();
            _root.name = DebuggerRootName;
            _root.style.position = Position.Absolute;
            _root.style.left = 0f;
            _root.style.top = 0f;
            _root.style.right = 0f;
            _root.style.bottom = 0f;
            _root.pickingMode = PickingMode.Ignore;

            _toggleButton = BuildToggleButton();
            _overlay = BuildWindowOverlay();

            _root.Add(_toggleButton);
            _root.Add(_overlay);

            if (m_StatsOverlay != null)
            {
                m_StatsOverlay.Attach(_root);
            }

            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            ApplyToggleRect();
            ApplyWindowRect();
            ApplyWindowScale();
            ApplyWindowOpacity();
            ApplyFont();
            UpdateToggleState();
        }

        public void SetStatsOverlayVisible(bool visible)
        {
            if (m_StatsOverlay == null)
            {
                m_StatsOverlay = new StatsOverlay(this);
            }

            if (visible && _root != null)
            {
                m_StatsOverlay.Attach(_root);
            }

            m_StatsOverlay.Visible = visible;
        }

        public bool IsStatsOverlayVisible => m_StatsOverlay != null && m_StatsOverlay.Visible;

        private static void InitializePanelSettingsDefaults(PanelSettings panelSettings)
        {
            if (panelSettings == null)
            {
                return;
            }

            MethodInfo resetMethod = typeof(PanelSettings).GetMethod("Reset", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            resetMethod?.Invoke(panelSettings, null);
        }

        private void RebuildRuntimeVisualTree()
        {
            if (_uiDocument == null)
            {
                return;
            }

            // 重建后当前缩放已经烘焙进样式，挂起的缩放变更不再需要单独应用。
            _isWindowScaleApplyPending = false;
            _viewByWindow.Clear();
            BuildRootVisualTree();
            UpdateHeaderTitle();
            RebuildSidebar();
            RefreshActiveWindowView();
            SyncWindowVisibility();
        }

        private void RefreshActiveWindowView()
        {
            if (_contentHost == null)
            {
                return;
            }

            _contentHost.Clear();
            if (_activeWindow == null)
            {
                return;
            }

            VisualElement view = _activeWindow.CreateView();
            if (view != null)
            {
                _viewByWindow[_activeWindow] = view;
                _contentHost.Add(view);
            }
        }

        private VisualElement BuildToggleButton()
        {
            float scale = GetUiScale();
            VisualElement button = new VisualElement
            {
                name = "debugger-toggle-button"
            };
            button.style.position = Position.Absolute;
            button.style.width = m_IconRect.width;
            button.style.height = m_IconRect.height;
            button.style.paddingLeft = 12f * scale;
            button.style.paddingRight = 12f * scale;
            button.style.paddingTop = 8f * scale;
            button.style.paddingBottom = 8f * scale;
            button.style.flexDirection = FlexDirection.Column;
            button.style.alignItems = Align.Center;
            button.style.justifyContent = Justify.Center;
            button.style.translate = new Translate(0f, 0f);
            button.style.backgroundColor = DebuggerTheme.Background;
            button.style.borderTopLeftRadius = 0f;
            button.style.borderTopRightRadius = 0f;
            button.style.borderBottomLeftRadius = 0f;
            button.style.borderBottomRightRadius = 0f;
            button.style.borderTopWidth = 1f;
            button.style.borderRightWidth = 1f;
            button.style.borderBottomWidth = 1f;
            button.style.borderLeftWidth = 1f;
            button.style.borderTopColor = DebuggerTheme.Border;
            button.style.borderRightColor = DebuggerTheme.Border;
            button.style.borderBottomColor = DebuggerTheme.Border;
            button.style.borderLeftColor = DebuggerTheme.Border;
            button.pickingMode = PickingMode.Position;
            button.usageHints = UsageHints.DynamicTransform;

            _toggleFpsLabel = new Label();
            _toggleFpsLabel.style.fontSize = 22f * scale;
            _toggleFpsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _toggleFpsLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _toggleFpsLabel.style.color = DebuggerTheme.PrimaryText;
            _toggleFpsLabel.style.letterSpacing = 0.2f;
            _toggleFpsLabel.pickingMode = PickingMode.Ignore;
            _hasToggleFpsValue = false;

            button.Add(_toggleFpsLabel);
            ScrollableDebuggerWindowBase.ApplyButtonStateStyles(
                button,
                DebuggerTheme.Background,
                DebuggerTheme.PanelSurfaceAlt,
                DebuggerTheme.PanelSurfaceStrong,
                DebuggerTheme.PrimaryText,
                DebuggerTheme.PrimaryText,
                DebuggerTheme.PrimaryText);
            RegisterFloatingToggleManipulator(button);
            return button;
        }

        private void OpenWindow()
        {
            if (_activeWindow == null)
            {
                SelectDebuggerWindow(DefaultWindowPath);
                return;
            }

            ShowFullWindow = true;
        }

        private VisualElement BuildWindowOverlay()
        {
            float scale = GetUiScale();
            VisualElement overlay = new VisualElement
            {
                name = "debugger-overlay"
            };
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0f;
            overlay.style.top = 0f;
            overlay.style.right = 0f;
            overlay.style.bottom = 0f;
            overlay.style.backgroundColor = Color.clear;
            overlay.pickingMode = PickingMode.Ignore;

            _window = new VisualElement
            {
                name = "debugger-window"
            };
            _window.style.position = Position.Absolute;
            _window.style.width = m_WindowRect.width;
            _window.style.height = m_WindowRect.height;
            _window.style.backgroundColor = DebuggerTheme.Background;
            _window.style.borderTopLeftRadius = 0f;
            _window.style.borderTopRightRadius = 0f;
            _window.style.borderBottomLeftRadius = 0f;
            _window.style.borderBottomRightRadius = 0f;
            _window.style.borderTopWidth = 1f;
            _window.style.borderRightWidth = 1f;
            _window.style.borderBottomWidth = 1f;
            _window.style.borderLeftWidth = 1f;
            _window.style.borderTopColor = DebuggerTheme.Border;
            _window.style.borderRightColor = DebuggerTheme.Border;
            _window.style.borderBottomColor = DebuggerTheme.Border;
            _window.style.borderLeftColor = DebuggerTheme.Border;
            _window.style.flexDirection = FlexDirection.Column;
            _window.pickingMode = PickingMode.Position;
            _window.usageHints = UsageHints.DynamicTransform;
            float resolutionScale = GetResolutionScale();
            _window.style.minWidth = MinWindowWidth * resolutionScale;
            _window.style.minHeight = MinWindowHeight * resolutionScale;

            VisualElement header = new VisualElement();
            header.style.height = 54f * scale;
            header.style.minHeight = 54f * scale;
            header.style.paddingLeft = 16f * scale;
            header.style.paddingRight = 10f * scale;
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.backgroundColor = DebuggerTheme.PanelSurfaceAlt;
            header.style.borderBottomWidth = 1f;
            header.style.borderBottomColor = DebuggerTheme.Border;

            VisualElement headerLeft = new VisualElement();
            headerLeft.style.flexGrow = 1f;
            headerLeft.style.minWidth = 0f;

            _headerTitle = new Label(RootWindowTitle);
            _headerTitle.style.color = DebuggerTheme.PrimaryText;
            _headerTitle.style.fontSize = 22f * scale;
            _headerTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerLeft.Add(_headerTitle);

            VisualElement headerActions = new VisualElement();
            headerActions.style.flexDirection = FlexDirection.Row;
            _resetLayoutButton = CreateIconChromeButton("R", "Reset Layout", ResetLayout, DebuggerTheme.ButtonSurface);
            _resetLayoutButton.style.marginRight = 8f * scale;
            _closeButton = CreateIconChromeButton("-", "Close", CloseToFloatingEntry, DebuggerTheme.Danger);
            headerActions.Add(_resetLayoutButton);
            headerActions.Add(_closeButton);

            header.Add(headerLeft);
            header.Add(headerActions);

            VisualElement body = new VisualElement();
            body.style.flexGrow = 1f;
            body.style.flexDirection = FlexDirection.Row;
            body.style.minWidth = 0f;
            body.style.minHeight = 0f;

            _sidebar = new VisualElement();
            _sidebar.style.width = 266f * scale;
            _sidebar.style.minWidth = 246f * scale;
            _sidebar.style.backgroundColor = DebuggerTheme.SidebarBackground;
            _sidebar.style.borderRightWidth = 1f;
            _sidebar.style.borderRightColor = DebuggerTheme.Border;
            _sidebar.style.flexShrink = 0f;
            _sidebar.style.flexDirection = FlexDirection.Column;
            _sidebar.style.minHeight = 0f;

            _sidebarScrollView = new ScrollView(ScrollViewMode.Vertical);
            _sidebarScrollView.style.flexGrow = 1f;
            _sidebarScrollView.style.minHeight = 0f;
            _sidebarScrollView.style.paddingLeft = 10f * scale;
            _sidebarScrollView.style.paddingRight = 8f * scale;
            _sidebarScrollView.style.paddingTop = 10f * scale;
            _sidebarScrollView.style.paddingBottom = 12f * scale;
            _sidebarScrollView.contentContainer.style.flexDirection = FlexDirection.Column;
            _sidebarScrollView.mouseWheelScrollSize = 240f * scale;
            ScrollableDebuggerWindowBase.StyleScrollView(_sidebarScrollView, scale);
            _sidebar.Add(_sidebarScrollView);

            _contentHost = new VisualElement();
            _contentHost.style.flexGrow = 1f;
            _contentHost.style.flexShrink = 1f;
            _contentHost.style.flexBasis = 0f;
            _contentHost.style.minWidth = 0f;
            _contentHost.style.minHeight = 0f;
            _contentHost.style.backgroundColor = DebuggerTheme.OverlayBackground;
            _contentHost.pickingMode = PickingMode.Position;

            body.Add(_sidebar);
            body.Add(_contentHost);

            _resizeHandle = new VisualElement();
            _resizeHandle.style.position = Position.Absolute;
            _resizeHandle.style.right = 0f;
            _resizeHandle.style.bottom = 0f;
            _resizeHandle.style.width = 22f * scale;
            _resizeHandle.style.height = 22f * scale;
            _resizeHandle.style.backgroundColor = DebuggerTheme.PanelSurfaceAlt;
            _resizeHandle.style.borderTopLeftRadius = 0f;
            _resizeHandle.style.borderBottomRightRadius = 0f;
            _resizeHandle.style.borderTopWidth = 1f;
            _resizeHandle.style.borderLeftWidth = 1f;
            _resizeHandle.style.borderTopColor = DebuggerTheme.Border;
            _resizeHandle.style.borderLeftColor = DebuggerTheme.Border;
            _resizeHandle.pickingMode = PickingMode.Position;
            _resizeHandle.style.justifyContent = Justify.Center;
            _resizeHandle.style.alignItems = Align.Center;

            Label resizeGlyph = new Label("//");
            resizeGlyph.style.color = DebuggerTheme.SecondaryText;
            resizeGlyph.style.fontSize = 10f * scale;
            resizeGlyph.style.unityFontStyleAndWeight = FontStyle.Bold;
            resizeGlyph.style.rotate = new Rotate(45f);
            resizeGlyph.pickingMode = PickingMode.Ignore;
            _resizeHandle.Add(resizeGlyph);
            ScrollableDebuggerWindowBase.ApplyButtonStateStyles(
                _resizeHandle,
                DebuggerTheme.PanelSurfaceAlt,
                DebuggerTheme.ButtonSurfaceHover,
                DebuggerTheme.ButtonSurfacePressed,
                DebuggerTheme.SecondaryText,
                DebuggerTheme.PrimaryText,
                DebuggerTheme.PrimaryText);
            RegisterResizeManipulator(_resizeHandle);

            _window.Add(header);
            _window.Add(body);
            _window.Add(_resizeHandle);
            overlay.Add(_window);

            RegisterMoveManipulator(header, true);
            return overlay;
        }

        private Button CreateChromeButton(string text, Action onClick, Color? background = null)
        {
            float scale = GetUiScale();
            Button button = new Button(onClick)
            {
                text = text
            };
            button.style.height = 34f * scale;
            button.style.minWidth = 94f * scale;
            button.style.paddingLeft = 12f * scale;
            button.style.paddingRight = 12f * scale;
            button.style.backgroundColor = background ?? DebuggerTheme.ButtonSurface;
            button.style.color = DebuggerTheme.PrimaryText;
            button.style.borderTopLeftRadius = 0f;
            button.style.borderTopRightRadius = 0f;
            button.style.borderBottomLeftRadius = 0f;
            button.style.borderBottomRightRadius = 0f;
            button.style.borderTopWidth = 0f;
            button.style.borderRightWidth = 0f;
            button.style.borderBottomWidth = 0f;
            button.style.borderLeftWidth = 0f;
            button.style.fontSize = 16f * scale;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            Color defaultBackground = background ?? DebuggerTheme.ButtonSurface;
            Color hoverBackground = ColorsEqual(defaultBackground, DebuggerTheme.ButtonSurface)
                ? DebuggerTheme.ButtonSurfaceHover
                : TintColor(defaultBackground, 0.12f);
            Color pressedBackground = ColorsEqual(defaultBackground, DebuggerTheme.ButtonSurface)
                ? DebuggerTheme.ButtonSurfacePressed
                : TintColor(defaultBackground, -0.12f);
            ScrollableDebuggerWindowBase.ApplyButtonStateStyles(
                button,
                defaultBackground,
                hoverBackground,
                pressedBackground,
                DebuggerTheme.PrimaryText,
                DebuggerTheme.PrimaryText,
                DebuggerTheme.PrimaryText);
            return button;
        }

        private Button CreateIconChromeButton(string icon, string tooltip, Action onClick, Color? background = null)
        {
            float scale = GetUiScale();
            Button button = CreateChromeButton(icon, onClick, background);
            button.tooltip = tooltip;
            button.style.minWidth = 38f * scale;
            button.style.width = 38f * scale;
            button.style.height = 38f * scale;
            button.style.paddingLeft = 0f;
            button.style.paddingRight = 0f;
            button.style.fontSize = 18f * scale;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
            button.style.borderTopWidth = 1f;
            button.style.borderRightWidth = 1f;
            button.style.borderBottomWidth = 1f;
            button.style.borderLeftWidth = 1f;
            button.style.borderTopColor = DebuggerTheme.Border;
            button.style.borderRightColor = DebuggerTheme.Border;
            button.style.borderBottomColor = DebuggerTheme.Border;
            button.style.borderLeftColor = DebuggerTheme.Border;
            return button;
        }

        internal float GetUiScale()
        {
            float resolutionScale = GetResolutionScale();
            return m_ShowFullWindow ? m_WindowScale * resolutionScale : resolutionScale;
        }

        /// <summary>
        /// 窗口工厂使用的 UI 缩放。组件尚未创建时按 1 倍处理（编辑器预览/单元测试路径）。
        /// </summary>
        internal static float UiScale
        {
            get
            {
                DebuggerComponent instance = Instance;
                return instance != null ? instance.GetUiScale() : 1f;
            }
        }

        private static float GetResolutionScale()
        {
            float widthScale = Screen.width > 0 ? Screen.width / ReferenceScreenWidth : 1f;
            float heightScale = Screen.height > 0 ? Screen.height / ReferenceScreenHeight : 1f;
            return Mathf.Max(0.001f, Mathf.Min(widthScale, heightScale));
        }

        internal static float GetLayoutScreenWidth()
        {
            return Screen.width / GetResolutionScale();
        }

        internal static float GetLayoutScreenHeight()
        {
            return Screen.height / GetResolutionScale();
        }

        private void RebuildSidebar()
        {
            if (_sidebarScrollView == null)
            {
                return;
            }

            _sidebarDirty = false;
            _sidebarScrollView.contentContainer.Clear();
            _sidebarRowStates.Clear();
            _sidebarChildContainers.Clear();
            _activeSidebarElement = null;

            if (_mDebuggerService?.DebuggerWindowRoot is DebuggerWindowGroup root)
            {
                AddMenuEntries(root, 0, _sidebarScrollView.contentContainer);
            }

            ScrollSidebarToActive();
        }

        private void AddMenuEntries(DebuggerWindowGroup group, int depth, VisualElement parent)
        {
            // 树按注册顺序保存，渲染时按名称排序，保证扩展窗口与内置窗口的展示顺序稳定。
            List<DebuggerWindowGroup.Entry> entries = new List<DebuggerWindowGroup.Entry>(group.Entries);
            entries.Sort(CompareMenuEntries);

            for (int i = 0; i < entries.Count; i++)
            {
                AddMenuEntry(entries[i], depth, parent);
            }
        }

        private static int CompareMenuEntries(DebuggerWindowGroup.Entry left, DebuggerWindowGroup.Entry right)
        {
            return string.CompareOrdinal(left.Name, right.Name);
        }

        private void AddMenuEntry(DebuggerWindowGroup.Entry entry, int depth, VisualElement parent)
        {
            float scale = GetUiScale();
            DebuggerWindowGroup childGroup = entry.Window as DebuggerWindowGroup;
            bool hasChildren = childGroup != null && childGroup.DebuggerWindowCount > 0;
            bool isGroup = hasChildren;
            bool isSelected = string.Equals(_activePath, entry.Path, StringComparison.Ordinal);
            bool isAncestorOfActive = !isSelected && IsPathActive(entry.Path);
            bool isExpanded = hasChildren && (isSelected || isAncestorOfActive || GetPathExpanded(entry.Path, depth));

            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.height = (isGroup && depth == 0 ? 34f : 32f) * scale;
            row.style.minHeight = row.style.height;
            row.style.paddingLeft = 8f * scale;
            row.style.paddingRight = 8f * scale;
            row.style.marginTop = depth == 0 && parent.childCount > 0 ? SidebarSectionGap * scale : 0f;
            row.style.marginBottom = SidebarRowGap * scale;
            row.style.borderTopLeftRadius = SidebarRowRadius * scale;
            row.style.borderTopRightRadius = SidebarRowRadius * scale;
            row.style.borderBottomLeftRadius = SidebarRowRadius * scale;
            row.style.borderBottomRightRadius = SidebarRowRadius * scale;
            row.style.backgroundColor = isSelected ? DebuggerTheme.SidebarRowSelected : DebuggerTheme.SidebarRow;
            row.style.borderLeftWidth = 3f * scale;
            row.style.borderLeftColor = isSelected || isAncestorOfActive ? DebuggerTheme.Accent : Color.clear;
            row.style.borderTopWidth = 0f;
            row.style.borderRightWidth = 0f;
            row.style.borderBottomWidth = 0f;
            row.style.flexShrink = 0f;
            row.pickingMode = PickingMode.Position;

            Button expander = null;
            if (hasChildren)
            {
                expander = CreateSidebarExpander(scale, isExpanded);
                expander.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
                expander.RegisterCallback<PointerUpEvent>(evt => evt.StopPropagation());
                row.Add(expander);
            }
            else
            {
                VisualElement spacer = new VisualElement();
                spacer.style.width = 22f * scale;
                spacer.style.minWidth = 22f * scale;
                spacer.style.height = 1f;
                spacer.pickingMode = PickingMode.Ignore;
                row.Add(spacer);
            }

            Label titleLabel = new Label(entry.Name);
            titleLabel.style.flexGrow = 1f;
            titleLabel.style.minWidth = 0f;
            titleLabel.style.fontSize = (isGroup && depth == 0 ? 14f : 15f) * scale;
            titleLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            titleLabel.style.whiteSpace = WhiteSpace.NoWrap;
            titleLabel.style.overflow = Overflow.Hidden;
            titleLabel.style.textOverflow = TextOverflow.Ellipsis;
            titleLabel.style.color = isSelected
                ? DebuggerTheme.PrimaryText
                : isGroup
                    ? DebuggerTheme.SidebarGroupText
                    : DebuggerTheme.SecondaryText;
            titleLabel.style.unityFontStyleAndWeight = isGroup || isSelected ? FontStyle.Bold : FontStyle.Normal;
            titleLabel.style.letterSpacing = isGroup && depth == 0 ? 0.4f : 0f;
            titleLabel.pickingMode = PickingMode.Ignore;
            row.Add(titleLabel);

            string path = entry.Path;
            IDebuggerWindow window = entry.Window;
            row.AddManipulator(new Clickable(() => SelectSidebarPath(path, window)));
            SidebarRowState state = new SidebarRowState(row, titleLabel, expander, scale, isSelected, isAncestorOfActive, isGroup, isExpanded);
            _sidebarRowStates[path] = state;

            parent.Add(row);
            if (isSelected)
            {
                _activeSidebarElement = row;
            }

            if (!hasChildren)
            {
                return;
            }

            VisualElement childContainer = new VisualElement();
            childContainer.style.flexDirection = FlexDirection.Column;
            childContainer.style.display = isExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            childContainer.style.marginLeft = SidebarTreeGuideInset * scale;
            childContainer.style.paddingLeft = 8f * scale;
            childContainer.style.borderLeftWidth = 1f * scale;
            childContainer.style.borderLeftColor = DebuggerTheme.SidebarTreeGuide;
            childContainer.style.marginBottom = SidebarRowGap * scale;
            _sidebarChildContainers[path] = childContainer;
            parent.Add(childContainer);

            expander.clicked += () => ToggleSidebarPath(path, depth);
            AddMenuEntries(childGroup, depth + 1, childContainer);
        }

        private bool GetPathExpanded(string path, int depth)
        {
            return _expansionByPath.TryGetValue(path, out bool expanded) ? expanded : depth == 0;
        }

        private static Button CreateSidebarExpander(float scale, bool expanded)
        {
            Button expander = new Button();
            expander.text = string.Empty;
            expander.style.width = 18f * scale;
            expander.style.minWidth = 18f * scale;
            expander.style.height = 24f * scale;
            expander.style.minHeight = 24f * scale;
            expander.style.paddingLeft = 0f;
            expander.style.paddingRight = 0f;
            expander.style.paddingTop = 0f;
            expander.style.paddingBottom = 0f;
            expander.style.marginRight = 4f * scale;
            expander.style.marginLeft = 0f;
            expander.style.backgroundColor = Color.clear;
            expander.style.borderTopWidth = 0f;
            expander.style.borderRightWidth = 0f;
            expander.style.borderBottomWidth = 0f;
            expander.style.borderLeftWidth = 0f;
            expander.style.borderTopLeftRadius = 4f * scale;
            expander.style.borderTopRightRadius = 4f * scale;
            expander.style.borderBottomLeftRadius = 4f * scale;
            expander.style.borderBottomRightRadius = 4f * scale;
            expander.style.alignItems = Align.Center;
            expander.style.justifyContent = Justify.Center;
            expander.focusable = false;

            VisualElement icon = new VisualElement();
            icon.name = "sidebar-chevron";
            icon.pickingMode = PickingMode.Ignore;
            expander.Add(icon);
            ApplySidebarChevronIcon(icon, scale, expanded, DebuggerTheme.SecondaryText);

            ScrollableDebuggerWindowBase.ApplyButtonStateStyles(
                expander,
                Color.clear,
                DebuggerTheme.GhostHover,
                DebuggerTheme.GhostPressed,
                DebuggerTheme.PrimaryText,
                DebuggerTheme.PrimaryText,
                DebuggerTheme.PrimaryText);

            return expander;
        }

        private static void ApplySidebarChevronIcon(VisualElement icon, float scale, bool expanded, Color color)
        {
            if (icon == null)
            {
                return;
            }

            float half = 3.5f * scale;
            float full = 5.5f * scale;
            icon.style.width = 0f;
            icon.style.height = 0f;
            icon.style.backgroundColor = Color.clear;
            icon.style.marginLeft = 0f;
            icon.style.marginRight = 0f;
            icon.style.marginTop = 0f;
            icon.style.marginBottom = 0f;

            if (expanded)
            {
                icon.style.borderLeftWidth = half;
                icon.style.borderRightWidth = half;
                icon.style.borderTopWidth = full;
                icon.style.borderBottomWidth = 0f;
                icon.style.borderLeftColor = Color.clear;
                icon.style.borderRightColor = Color.clear;
                icon.style.borderTopColor = color;
                icon.style.borderBottomColor = Color.clear;
            }
            else
            {
                icon.style.borderLeftWidth = full;
                icon.style.borderRightWidth = 0f;
                icon.style.borderTopWidth = half;
                icon.style.borderBottomWidth = half;
                icon.style.borderLeftColor = color;
                icon.style.borderRightColor = Color.clear;
                icon.style.borderTopColor = Color.clear;
                icon.style.borderBottomColor = Color.clear;
            }
        }

        private void ToggleSidebarPath(string path, int depth)
        {
            if (!_sidebarChildContainers.TryGetValue(path, out VisualElement container))
            {
                return;
            }

            bool expanded = !GetPathExpanded(path, depth);
            _expansionByPath[path] = expanded;
            container.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;

            if (_sidebarRowStates.TryGetValue(path, out SidebarRowState state))
            {
                state.SetExpanded(expanded);
            }
        }

        private void RefreshSidebarSelectionState(bool scrollToActive)
        {
            if (_sidebarRowStates.Count <= 0)
            {
                return;
            }

            _activeSidebarElement = null;
            foreach (KeyValuePair<string, SidebarRowState> pair in _sidebarRowStates)
            {
                bool isSelected = string.Equals(_activePath, pair.Key, StringComparison.Ordinal);
                bool isAncestorOfActive = !isSelected && IsPathActive(pair.Key);
                pair.Value.SetActive(isSelected, isAncestorOfActive);
                if (isSelected)
                {
                    _activeSidebarElement = pair.Value.Row;
                }
            }

            if (scrollToActive)
            {
                ScrollSidebarToActive();
            }
        }

        private void ScrollSidebarToActive()
        {
            if (_activeSidebarElement == null || _sidebarScrollView == null)
            {
                return;
            }

            _activeSidebarElement.schedule.Execute(() => _sidebarScrollView.ScrollTo(_activeSidebarElement)).ExecuteLater(0);
        }

        private bool IsPathActive(string path)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(_activePath))
            {
                return false;
            }

            if (string.Equals(_activePath, path, StringComparison.Ordinal))
            {
                return true;
            }

            // "A/B" 是 "A/B/C" 的祖先；避免 "A/B" 误判 "A/BC"。
            return _activePath.Length > path.Length
                && _activePath[path.Length] == '/'
                && _activePath.StartsWith(path, StringComparison.Ordinal);
        }

        private bool SelectSidebarPath(string path, IDebuggerWindow window)
        {
            if (window is DebuggerWindowGroup)
            {
                ToggleSidebarPath(path, CountPathDepth(path));
                return true;
            }

            return SelectWindow(window);
        }

        private static int CountPathDepth(string path)
        {
            int depth = 0;
            for (int i = 0; i < path.Length; i++)
            {
                if (path[i] == '/')
                {
                    depth++;
                }
            }

            return depth;
        }

        private bool SelectWindow(IDebuggerWindow window)
        {
            if (window == null)
            {
                return false;
            }

            if (ReferenceEquals(_activeWindow, window))
            {
                ShowFullWindow = true;
                return true;
            }

            if (_activeWindow != null)
            {
                _activeWindow.OnLeave();
            }

            _activeWindow = window;
            _activePath = ResolveWindowPath(window);
            ExpandAncestors(_activePath);
            _activeWindow.OnEnter();
            UpdateHeaderTitle();

            if (!m_ShowFullWindow)
            {
                ShowFullWindow = true;
                return true;
            }

            _contentHost.Clear();
            if (!_viewByWindow.TryGetValue(window, out VisualElement view) || view == null)
            {
                view = window.CreateView();
                _viewByWindow[window] = view;
            }

            if (view != null)
            {
                _contentHost.Add(view);
            }

            RefreshSidebarSelectionState(true);

            return true;
        }

        private string ResolveWindowPath(IDebuggerWindow window)
        {
            return _mDebuggerService?.DebuggerWindowRoot is DebuggerWindowGroup root && root.TryGetPath(window, out string path)
                ? path
                : null;
        }

        private void UpdateHeaderTitle()
        {
            if (_headerTitle == null)
            {
                return;
            }

            _headerTitle.text = string.IsNullOrEmpty(_activePath) ? RootWindowTitle : _activePath;
        }

        private void ExpandAncestors(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            int index = path.LastIndexOf('/');
            while (index > 0)
            {
                string ancestor = path.Substring(0, index);
                _expansionByPath[ancestor] = true;
                if (_sidebarChildContainers.TryGetValue(ancestor, out VisualElement container))
                {
                    container.style.display = DisplayStyle.Flex;
                }

                if (_sidebarRowStates.TryGetValue(ancestor, out SidebarRowState state))
                {
                    state.SetExpanded(true);
                }

                index = ancestor.LastIndexOf('/');
            }
        }

        private void UpdateToggleState()
        {
            if (_toggleFpsLabel == null || m_FpsCounter == null)
            {
                return;
            }

            // FPS 每 0.5 秒才刷新一次，颜色只在日志计数变化时变化；
            // 两者都没变就不重新格式化，避免每帧一个字符串。
            float fps = m_FpsCounter.CurrentFps;
            Color color = ResolveToggleFpsColor();
            if (_hasToggleFpsValue && fps == _toggleFpsValue && ColorsEqual(color, _toggleFpsColor))
            {
                return;
            }

            _hasToggleFpsValue = true;
            _toggleFpsValue = fps;
            _toggleFpsColor = color;
            _toggleFpsLabel.text = Utility.Text.Format("FPS:{0:F0}", fps);
            _toggleFpsLabel.style.color = color;
        }

        private Color ResolveToggleFpsColor()
        {
            if (m_ConsoleWindow.FatalCount > 0)
            {
                return m_ConsoleWindow.GetLogStringColor(LogType.Exception);
            }

            if (m_ConsoleWindow.ErrorCount > 0)
            {
                return m_ConsoleWindow.GetLogStringColor(LogType.Error);
            }

            if (m_ConsoleWindow.WarningCount > 0)
            {
                return m_ConsoleWindow.GetLogStringColor(LogType.Warning);
            }

            return m_ConsoleWindow.GetLogStringColor(LogType.Log);
        }

        private void ApplyPendingWindowScale()
        {
            if (!_isWindowScaleApplyPending || Time.unscaledTime < _windowScaleApplyAt)
            {
                return;
            }

            RebuildRuntimeVisualTree();
        }

        private void SyncWindowVisibility()
        {
            bool active = isActiveAndEnabled && _mDebuggerService != null && _mDebuggerService.ActiveWindow;
            if (_toggleButton != null)
            {
                _toggleButton.style.display = active && !m_ShowFullWindow ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_overlay != null)
            {
                _overlay.style.display = active && m_ShowFullWindow ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void ApplyToggleRect()
        {
            if (_toggleButton == null)
            {
                return;
            }

            float resolutionScale = GetResolutionScale();
            _toggleButton.style.left = m_IconRect.x * resolutionScale;
            _toggleButton.style.top = m_IconRect.y * resolutionScale;
            _toggleButton.style.width = m_IconRect.width * resolutionScale;
            _toggleButton.style.height = m_IconRect.height * resolutionScale;
        }

        private void ApplyToggleVisualOffset(Vector2 offset)
        {
            if (_toggleButton == null)
            {
                return;
            }

            _toggleButton.style.translate = new Translate(offset.x, offset.y);
        }

        private void ApplyWindowRect()
        {
            if (_window == null)
            {
                return;
            }

            float resolutionScale = GetResolutionScale();
            _window.style.left = m_WindowRect.x * resolutionScale;
            _window.style.top = m_WindowRect.y * resolutionScale;
            _window.style.width = m_WindowRect.width * resolutionScale;
            _window.style.height = m_WindowRect.height * resolutionScale;
        }

        private void ApplyWindowScale()
        {
            if (_window == null || _panelSettings == null)
            {
                return;
            }

            // VisualElement.transform 的写入在 Unity 6.x 已废弃，改用 style.scale（2021.2+ 全区间可用）。
            _window.style.scale = new StyleScale(new Scale(Vector3.one));
            _panelSettings.scale = 1f;
        }

        private void ApplyWindowOpacity()
        {
            if (_window != null)
            {
                _window.style.opacity = m_WindowOpacity;
            }

            if (_toggleButton != null)
            {
                _toggleButton.style.opacity = Mathf.Clamp(m_WindowOpacity + 0.1f, 0.35f, 1f);
            }
        }

        private void ApplyFont()
        {
            if (_root == null)
            {
                return;
            }

            StyleFontDefinition fontDefinition = ResolveFontDefinition();
            _root.style.unityFontDefinition = fontDefinition;
            if (fontDefinition.keyword == StyleKeyword.Null)
            {
                _root.style.unityFont = new StyleFont(StyleKeyword.Null);
            }
        }

        private void CloseToFloatingEntry()
        {
            ShowFullWindow = false;
        }

        private void RegisterMoveManipulator(VisualElement target, bool moveWindow)
        {
            target.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0)
                {
                    return;
                }

                target.CapturePointer(evt.pointerId);
                _dragPointerStart = new Vector2(evt.position.x, evt.position.y);
                _dragWindowStart = moveWindow ? new Vector2(m_WindowRect.x, m_WindowRect.y) : new Vector2(m_IconRect.x, m_IconRect.y);
                evt.StopPropagation();
            });

            target.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!target.HasPointerCapture(evt.pointerId))
                {
                    return;
                }

                Vector2 delta = (new Vector2(evt.position.x, evt.position.y) - _dragPointerStart) / GetResolutionScale();
                if (moveWindow)
                {
                    WindowRect = new Rect(_dragWindowStart.x + delta.x, _dragWindowStart.y + delta.y, m_WindowRect.width, m_WindowRect.height);
                }
                else
                {
                    IconRect = new Rect(_dragWindowStart.x + delta.x, _dragWindowStart.y + delta.y, m_IconRect.width, m_IconRect.height);
                }

                evt.StopPropagation();
            });

            target.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!target.HasPointerCapture(evt.pointerId))
                {
                    return;
                }

                target.ReleasePointer(evt.pointerId);
                if (moveWindow)
                {
                    SaveWindowLayoutSettings();
                }
                else
                {
                    SaveIconLayoutSettings();
                }

                evt.StopPropagation();
            });
        }

        private void RegisterFloatingToggleManipulator(VisualElement target)
        {
            target.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0)
                {
                    return;
                }

                target.CapturePointer(evt.pointerId);
                _isToggleDragging = false;
                _isToggleSnapAnimating = false;
                _toggleSnapVelocity = Vector2.zero;
                _dragPointerStart = new Vector2(evt.position.x, evt.position.y);
                _dragWindowStart = new Vector2(m_IconRect.x, m_IconRect.y);
                ApplyToggleVisualOffset(Vector2.zero);

                evt.StopPropagation();
            });

            target.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!target.HasPointerCapture(evt.pointerId))
                {
                    return;
                }

                Vector2 currentPosition = new Vector2(evt.position.x, evt.position.y);
                Vector2 delta = (currentPosition - _dragPointerStart) / GetResolutionScale();
                if (!_isToggleDragging && delta.sqrMagnitude > ToggleClickMoveThreshold * ToggleClickMoveThreshold)
                {
                    _isToggleDragging = true;
                }

                if (_isToggleDragging)
                {
                    Vector2 targetPosition = ClampIconPosition(new Vector2(_dragWindowStart.x + delta.x, _dragWindowStart.y + delta.y));
                    m_IconRect = new Rect(targetPosition.x, targetPosition.y, m_IconRect.width, m_IconRect.height);
                    ApplyToggleRect();
                    ApplyToggleVisualOffset(Vector2.zero);
                }

                evt.StopPropagation();
            });

            target.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!target.HasPointerCapture(evt.pointerId))
                {
                    return;
                }

                target.ReleasePointer(evt.pointerId);
                if (_isToggleDragging)
                {
                    if (m_EnableFloatingToggleSnap)
                    {
                        StartToggleSnapAnimation(GetSnappedIconPosition(new Vector2(m_IconRect.x, m_IconRect.y)));
                    }
                    else
                    {
                        _isToggleSnapAnimating = false;
                        _toggleSnapVelocity = Vector2.zero;
                        ApplyToggleRect();
                        SaveIconLayoutSettings();
                    }

                    _toggleOpenSuppressUntil = Time.unscaledTime + ToggleClickSuppressAfterDrag;
                }
                else if (Time.unscaledTime >= _toggleOpenSuppressUntil && IsToggleDoubleTap(evt.position))
                {
                    OpenWindow();
                }

                _isToggleDragging = false;
                evt.StopPropagation();
            });
        }

        private void SnapIconToEdge()
        {
            if (!m_EnableFloatingToggleSnap)
            {
                ApplyToggleRect();
                ApplyToggleVisualOffset(Vector2.zero);
                SaveIconLayoutSettings();
                return;
            }

            Vector2 targetPosition = GetSnappedIconPosition(new Vector2(m_IconRect.x, m_IconRect.y));
            IconRect = new Rect(targetPosition.x, targetPosition.y, m_IconRect.width, m_IconRect.height);
            ApplyToggleVisualOffset(Vector2.zero);
            SaveIconLayoutSettings();
        }

        private void RegisterResizeManipulator(VisualElement target)
        {
            target.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0)
                {
                    return;
                }

                target.CapturePointer(evt.pointerId);
                _isResizeActive = true;
                _resizePointerStart = new Vector2(evt.position.x, evt.position.y);
                _resizeWindowSizeStart = new Vector2(m_WindowRect.width, m_WindowRect.height);
                evt.StopPropagation();
            });

            target.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!_isResizeActive || !target.HasPointerCapture(evt.pointerId))
                {
                    return;
                }

                Vector2 delta = (new Vector2(evt.position.x, evt.position.y) - _resizePointerStart) / GetResolutionScale();
                WindowRect = new Rect(m_WindowRect.x, m_WindowRect.y, _resizeWindowSizeStart.x + delta.x, _resizeWindowSizeStart.y + delta.y);
                evt.StopPropagation();
            });

            target.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!target.HasPointerCapture(evt.pointerId))
                {
                    return;
                }

                target.ReleasePointer(evt.pointerId);
                _isResizeActive = false;
                SaveWindowLayoutSettings();
                evt.StopPropagation();
            });
        }

        private bool IsToggleDoubleTap(Vector2 pointerPosition)
        {
            float now = Time.unscaledTime;
            Vector2 currentPosition = new Vector2(pointerPosition.x, pointerPosition.y);
            bool isDoubleTap = now - _lastToggleTapTime <= ToggleDoubleClickInterval
                && (currentPosition - _lastToggleTapPosition).sqrMagnitude <= ToggleDoubleClickMaxDistance * ToggleDoubleClickMaxDistance;

            _lastToggleTapTime = now;
            _lastToggleTapPosition = currentPosition;
            return isDoubleTap;
        }

        private void StartToggleSnapAnimation(Vector2 targetPosition)
        {
            _toggleSnapTargetPosition = ClampIconPosition(targetPosition);
            _isToggleSnapAnimating = true;
            _toggleSnapVelocity = Vector2.zero;
        }

        private void UpdateToggleSnapAnimation(float deltaTime)
        {
            if (!m_EnableFloatingToggleSnap)
            {
                _isToggleSnapAnimating = false;
                _toggleSnapVelocity = Vector2.zero;
                return;
            }

            if (!_isToggleSnapAnimating || _isToggleDragging)
            {
                return;
            }

            Vector2 currentPosition = new Vector2(m_IconRect.x, m_IconRect.y);
            Vector2 nextPosition = Vector2.SmoothDamp(currentPosition, _toggleSnapTargetPosition, ref _toggleSnapVelocity, ToggleSnapSmoothTime, Mathf.Infinity, Mathf.Max(0.0001f, deltaTime));
            m_IconRect = new Rect(nextPosition.x, nextPosition.y, m_IconRect.width, m_IconRect.height);
            ApplyToggleRect();

            if ((nextPosition - _toggleSnapTargetPosition).sqrMagnitude <= ToggleSnapStopDistance * ToggleSnapStopDistance)
            {
                _isToggleSnapAnimating = false;
                m_IconRect = new Rect(_toggleSnapTargetPosition.x, _toggleSnapTargetPosition.y, m_IconRect.width, m_IconRect.height);
                ApplyToggleRect();
                ApplyToggleVisualOffset(Vector2.zero);
                SaveIconLayoutSettings();
            }
        }

        private void HandleScreenSizeChanged()
        {
            if (_lastScreenWidth == Screen.width && _lastScreenHeight == Screen.height)
            {
                return;
            }

            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            IconRect = new Rect(m_IconRect.x, m_IconRect.y, m_IconRect.width, m_IconRect.height);
            if (m_EnableFloatingToggleSnap && !_isToggleDragging)
            {
                SnapIconToEdge();
            }
            else
            {
                ApplyToggleRect();
                ApplyToggleVisualOffset(Vector2.zero);
            }

            WindowRect = new Rect(m_WindowRect.x, m_WindowRect.y, m_WindowRect.width, m_WindowRect.height);
            RebuildRuntimeVisualTree();
        }

        private Vector2 ClampIconPosition(Vector2 position)
        {
            float maxX = Mathf.Max(0f, GetLayoutScreenWidth() - m_IconRect.width);
            float maxY = Mathf.Max(0f, GetLayoutScreenHeight() - m_IconRect.height);
            position.x = Mathf.Clamp(position.x, 0f, maxX);
            position.y = Mathf.Clamp(position.y, 0f, maxY);
            return position;
        }

        private Vector2 GetSnappedIconPosition(Vector2 position)
        {
            position = ClampIconPosition(position);

            float leftDistance = position.x;
            float rightDistance = Mathf.Max(0f, GetLayoutScreenWidth() - (position.x + m_IconRect.width));
            float topDistance = position.y;
            float bottomDistance = Mathf.Max(0f, GetLayoutScreenHeight() - (position.y + m_IconRect.height));

            float minDistance = leftDistance;
            Vector2 snappedPosition = new Vector2(0f, position.y);

            if (rightDistance < minDistance)
            {
                minDistance = rightDistance;
                snappedPosition = new Vector2(Mathf.Max(0f, GetLayoutScreenWidth() - m_IconRect.width), position.y);
            }

            if (topDistance < minDistance)
            {
                minDistance = topDistance;
                snappedPosition = new Vector2(position.x, 0f);
            }

            if (bottomDistance < minDistance)
            {
                snappedPosition = new Vector2(position.x, Mathf.Max(0f, GetLayoutScreenHeight() - m_IconRect.height));
            }

            return ClampIconPosition(snappedPosition);
        }

        private Rect ClampIconRect(Rect rect)
        {
            rect.width = DefaultIconRect.width;
            rect.height = DefaultIconRect.height;
            Vector2 clampedPosition = ClampIconPosition(new Vector2(rect.x, rect.y));
            rect.x = clampedPosition.x;
            rect.y = clampedPosition.y;
            return rect;
        }

        private Rect ClampWindowRect(Rect rect)
        {
            float screenW = GetLayoutScreenWidth();
            float screenH = GetLayoutScreenHeight();
            rect.width = Mathf.Max(rect.width, MinWindowWidth);
            rect.height = Mathf.Max(rect.height, MinWindowHeight);

            const float edgeMargin = 60f;
            float minX = -(rect.width - edgeMargin);
            float minY = 0f;
            float maxX = screenW - edgeMargin;
            float maxY = screenH - edgeMargin;
            rect.x = Mathf.Clamp(rect.x, minX, maxX);
            rect.y = Mathf.Clamp(rect.y, minY, maxY);
            return rect;
        }

        private static void CopyToClipboard(string content)
        {
            GUIUtility.systemCopyBuffer = content ?? string.Empty;
        }

        private static Color TintColor(Color color, float amount)
        {
            return new Color(
                Mathf.Clamp01(color.r + amount),
                Mathf.Clamp01(color.g + amount),
                Mathf.Clamp01(color.b + amount),
                color.a);
        }

        private static bool ColorsEqual(Color lhs, Color rhs)
        {
            return Mathf.Approximately(lhs.r, rhs.r)
                && Mathf.Approximately(lhs.g, rhs.g)
                && Mathf.Approximately(lhs.b, rhs.b)
                && Mathf.Approximately(lhs.a, rhs.a);
        }

        private sealed class SidebarRowState
        {
            private readonly Label _titleLabel;
            private readonly VisualElement _chevronIcon;
            private readonly float _scale;
            private readonly bool _isGroup;
            private bool _isSelected;
            private bool _isAncestorOfActive;
            private bool _isExpanded;
            private bool _isHovered;
            private bool _isPressed;

            public SidebarRowState(
                VisualElement row,
                Label titleLabel,
                Button expander,
                float scale,
                bool isSelected,
                bool isAncestorOfActive,
                bool isGroup,
                bool isExpanded)
            {
                Row = row;
                _titleLabel = titleLabel;
                Expander = expander;
                _chevronIcon = expander != null ? expander.Q("sidebar-chevron") : null;
                _scale = scale;
                _isSelected = isSelected;
                _isAncestorOfActive = isAncestorOfActive;
                _isGroup = isGroup;
                _isExpanded = isExpanded;
                RegisterCallbacks();
                Apply();
            }

            public VisualElement Row { get; }

            public Button Expander { get; }

            public void SetActive(bool isSelected, bool isAncestorOfActive)
            {
                _isSelected = isSelected;
                _isAncestorOfActive = isAncestorOfActive;
                Apply();
            }

            public void SetExpanded(bool expanded)
            {
                _isExpanded = expanded;
                Apply();
            }

            private void RegisterCallbacks()
            {
                Row.RegisterCallback<PointerEnterEvent>(_ =>
                {
                    _isHovered = true;
                    Apply();
                });
                Row.RegisterCallback<PointerLeaveEvent>(_ =>
                {
                    _isHovered = false;
                    _isPressed = false;
                    Apply();
                });
                Row.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button != 0)
                    {
                        return;
                    }

                    _isPressed = true;
                    Apply();
                });
                Row.RegisterCallback<PointerUpEvent>(_ =>
                {
                    _isPressed = false;
                    Apply();
                });
                Row.RegisterCallback<PointerCancelEvent>(_ =>
                {
                    _isPressed = false;
                    _isHovered = false;
                    Apply();
                });
            }

            private void Apply()
            {
                bool showSelectedFill = _isSelected;
                Row.style.backgroundColor = _isPressed
                    ? showSelectedFill ? DebuggerTheme.SidebarRowSelectedPressed : DebuggerTheme.SidebarRowPressed
                    : _isHovered
                        ? showSelectedFill ? DebuggerTheme.SidebarRowSelectedHover : DebuggerTheme.SidebarRowHover
                        : showSelectedFill ? DebuggerTheme.SidebarRowSelected : DebuggerTheme.SidebarRow;
                Row.style.borderLeftWidth = 3f * _scale;
                Row.style.borderLeftColor = _isSelected || _isAncestorOfActive
                    ? DebuggerTheme.Accent
                    : Color.clear;

                Color textColor;
                if (_isSelected || _isHovered)
                {
                    textColor = DebuggerTheme.PrimaryText;
                }
                else if (_isAncestorOfActive)
                {
                    textColor = DebuggerTheme.PrimaryText;
                }
                else if (_isGroup)
                {
                    textColor = DebuggerTheme.SidebarGroupText;
                }
                else
                {
                    textColor = DebuggerTheme.SecondaryText;
                }

                _titleLabel.style.color = textColor;
                _titleLabel.style.unityFontStyleAndWeight = _isGroup || _isSelected || _isAncestorOfActive
                    ? FontStyle.Bold
                    : FontStyle.Normal;

                if (_chevronIcon != null)
                {
                    ApplySidebarChevronIcon(_chevronIcon, _scale, _isExpanded, textColor);
                }
            }
        }

    }
}
