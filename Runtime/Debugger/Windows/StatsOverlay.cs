using System;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
#if UNITY_2020_2_OR_NEWER
using Unity.Profiling;
#endif

namespace AlicizaX.Debugger
{
    public sealed partial class DebuggerComponent
    {
        private sealed class StatsOverlay
        {
            private const float RefreshInterval = 0.25f;
            private const float LandscapePanelWidth = 236f;
            private const float PortraitPanelWidth = 188f;
            private const float Margin = 12f;

            private readonly DebuggerComponent _owner;
            private VisualElement _root;
            private VisualElement _panel;
            private Label _titleLabel;
            private Label _bodyLabel;
            private bool _visible;
            private float _timeLeft;
            private int _layoutWidth;
            private int _layoutHeight;
            private readonly StringBuilder _builder = new StringBuilder(256);

#if UNITY_2020_2_OR_NEWER
            private ProfilerRecorder _trianglesRecorder;
            private ProfilerRecorder _drawCallsRecorder;
            private ProfilerRecorder _batchesRecorder;
            private ProfilerRecorder _setPassRecorder;
            private bool _recordersStarted;
#endif

            public StatsOverlay(DebuggerComponent owner)
            {
                _owner = owner;
            }

            public bool Visible
            {
                get => _visible;
                set
                {
                    if (_visible == value)
                    {
                        if (value)
                        {
                            EnsureAttached();
                            ApplyLayout();
                            RefreshNow();
                        }

                        return;
                    }

                    _visible = value;
                    if (_visible)
                    {
                        EnsureAttached();
                        StartRecorders();
                        ApplyLayout();
                        RefreshNow();
                    }
                    else
                    {
                        StopRecorders();
                    }

                    ApplyVisibility();
                }
            }

            public void Attach(VisualElement host)
            {
                if (host == null)
                {
                    return;
                }

                if (_root != null && _root.parent == host)
                {
                    ApplyVisibility();
                    ApplyLayout();
                    return;
                }

                Detach();
                Build(host);
                ApplyVisibility();
                ApplyLayout();
                if (_visible)
                {
                    StartRecorders();
                    RefreshNow();
                }
            }

            public void Detach()
            {
                if (_root != null)
                {
                    _root.RemoveFromHierarchy();
                    _root = null;
                    _panel = null;
                    _titleLabel = null;
                    _bodyLabel = null;
                }

                _layoutWidth = 0;
                _layoutHeight = 0;
            }

            public void Tick(float unscaledDeltaTime)
            {
                if (!_visible || _bodyLabel == null)
                {
                    return;
                }

                if (_layoutWidth != Screen.width || _layoutHeight != Screen.height)
                {
                    ApplyLayout();
                }

                _timeLeft -= unscaledDeltaTime;
                if (_timeLeft > 0f)
                {
                    return;
                }

                _timeLeft = RefreshInterval;
                RefreshNow();
            }

            public void Dispose()
            {
                Visible = false;
                StopRecorders();
                Detach();
            }

            private void EnsureAttached()
            {
                if (_owner == null || _owner._root == null)
                {
                    return;
                }

                if (_root == null || _root.parent != _owner._root)
                {
                    Attach(_owner._root);
                }
            }

            private void Build(VisualElement host)
            {
                _root = new VisualElement();
                _root.name = "debugger-stats-overlay";
                _root.pickingMode = PickingMode.Ignore;
                _root.style.position = Position.Absolute;
                _root.style.left = 0f;
                _root.style.top = 0f;
                _root.style.right = 0f;
                _root.style.bottom = 0f;
                _root.style.flexGrow = 1f;

                _panel = new VisualElement();
                _panel.pickingMode = PickingMode.Ignore;
                _panel.style.position = Position.Absolute;
                _panel.style.backgroundColor = new Color(8f / 255f, 10f / 255f, 14f / 255f, 0.62f);
                _panel.style.borderTopWidth = 1f;
                _panel.style.borderRightWidth = 1f;
                _panel.style.borderBottomWidth = 1f;
                _panel.style.borderLeftWidth = 1f;
                _panel.style.borderTopColor = new Color(1f, 1f, 1f, 0.08f);
                _panel.style.borderRightColor = new Color(1f, 1f, 1f, 0.08f);
                _panel.style.borderBottomColor = new Color(1f, 1f, 1f, 0.08f);
                _panel.style.borderLeftColor = new Color(1f, 1f, 1f, 0.08f);
                _panel.style.flexDirection = FlexDirection.Column;

                _titleLabel = new Label("STATS");
                _titleLabel.pickingMode = PickingMode.Ignore;
                _titleLabel.style.color = DebuggerTheme.SecondaryText;
                _titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                ApplyOverlayFont(_titleLabel);

                _bodyLabel = new Label();
                _bodyLabel.pickingMode = PickingMode.Ignore;
                _bodyLabel.style.color = DebuggerTheme.PrimaryText;
                _bodyLabel.style.whiteSpace = WhiteSpace.Normal;
                _bodyLabel.style.unityTextAlign = TextAnchor.UpperLeft;
                ApplyOverlayFont(_bodyLabel);

                _panel.Add(_titleLabel);
                _panel.Add(_bodyLabel);
                _root.Add(_panel);
                host.Add(_root);
                _root.BringToFront();
            }

            private void ApplyLayout()
            {
                if (_panel == null)
                {
                    return;
                }

                _layoutWidth = Screen.width;
                _layoutHeight = Screen.height;

                bool portrait = _layoutHeight > _layoutWidth;
                float shortSide = Mathf.Min(_layoutWidth, _layoutHeight);
                float longSide = Mathf.Max(_layoutWidth, _layoutHeight);
                float scale = portrait
                    ? Mathf.Clamp(shortSide > 0f ? shortSide / 1080f : 1f, 0.7f, 1.8f)
                    : Mathf.Min(
                        Mathf.Clamp(longSide > 0f ? longSide / 1920f : 1f, 0.7f, 1.8f),
                        Mathf.Clamp(shortSide > 0f ? shortSide / 1080f : 1f, 0.7f, 1.8f));

                float panelWidth = (portrait ? PortraitPanelWidth : LandscapePanelWidth) * scale;
                float maxWidth = Mathf.Max(120f, _layoutWidth - Margin * scale * 2f);
                panelWidth = Mathf.Min(panelWidth, maxWidth);

                float margin = Margin * scale;
                float safeTop = margin;
                float safeRight = margin;
#if UNITY_2019_1_OR_NEWER
                Rect safe = Screen.safeArea;
                if (safe.width > 1f && safe.height > 1f)
                {
                    safeTop = Mathf.Max(margin, _layoutHeight - safe.yMax + margin * 0.25f);
                    safeRight = Mathf.Max(margin, _layoutWidth - safe.xMax + margin * 0.25f);
                }
#endif

                _panel.style.top = safeTop;
                _panel.style.right = safeRight;
                _panel.style.width = panelWidth;
                _panel.style.borderTopLeftRadius = 8f * scale;
                _panel.style.borderTopRightRadius = 8f * scale;
                _panel.style.borderBottomLeftRadius = 8f * scale;
                _panel.style.borderBottomRightRadius = 8f * scale;
                _panel.style.paddingLeft = (portrait ? 8f : 10f) * scale;
                _panel.style.paddingRight = (portrait ? 8f : 10f) * scale;
                _panel.style.paddingTop = (portrait ? 6f : 8f) * scale;
                _panel.style.paddingBottom = (portrait ? 6f : 8f) * scale;

                if (_titleLabel != null)
                {
                    _titleLabel.style.fontSize = (portrait ? 10f : 11f) * scale;
                    _titleLabel.style.letterSpacing = 0.8f * scale;
                    _titleLabel.style.marginBottom = (portrait ? 2f : 4f) * scale;
                }

                if (_bodyLabel != null)
                {
                    _bodyLabel.style.fontSize = (portrait ? 11.5f : 13f) * scale;
                }
            }

            private void ApplyOverlayFont(VisualElement element)
            {
                if (element == null || _owner == null)
                {
                    return;
                }

                element.style.unityFontDefinition = _owner.ResolveFontDefinition();
            }

            private void ApplyVisibility()
            {
                if (_root == null)
                {
                    return;
                }

                _root.style.display = _visible ? DisplayStyle.Flex : DisplayStyle.None;
            }

            private void RefreshNow()
            {
                if (_bodyLabel == null)
                {
                    return;
                }

                float fps = _owner != null && _owner.m_FpsCounter != null
                    ? _owner.m_FpsCounter.CurrentFps
                    : 0f;
                float ms = fps > 0.01f ? 1000f / fps : 0f;

                long triangles;
                long drawCalls;
                long batches;
                long setPass;
#if UNITY_2020_2_OR_NEWER
                triangles = ReadRecorder(_trianglesRecorder);
                drawCalls = ReadRecorder(_drawCallsRecorder);
                batches = ReadRecorder(_batchesRecorder);
                setPass = ReadRecorder(_setPassRecorder);
#else
                triangles = -1L;
                drawCalls = -1L;
                batches = -1L;
                setPass = -1L;
#endif

                long monoUsed = Profiler.GetMonoUsedSizeLong();
                long monoHeap = Profiler.GetMonoHeapSizeLong();
                long totalAlloc = Profiler.GetTotalAllocatedMemoryLong();
                long totalReserved = Profiler.GetTotalReservedMemoryLong();
                long gfxDriver = Profiler.GetAllocatedMemoryForGraphicsDriver();

                bool portrait = Screen.height > Screen.width;
                _builder.Clear();
                if (portrait)
                {
                    _builder.Append(fps.ToString("F0")).Append(" FPS  ").Append(ms.ToString("F1")).Append("ms\n");
                    _builder.Append("Tris ").Append(FormatCount(triangles)).Append("  DC ").Append(FormatCount(drawCalls)).Append('\n');
                    _builder.Append("Bat ").Append(FormatCount(batches)).Append("  SP ").Append(FormatCount(setPass)).Append('\n');
                    _builder.Append("Mono ").Append(FormatBytes(monoUsed)).Append('\n');
                    _builder.Append("Alloc ").Append(FormatBytes(totalAlloc)).Append('\n');
                    _builder.Append("Gfx ").Append(FormatBytes(gfxDriver));
                }
                else
                {
                    _builder.Append("FPS      ").Append(fps.ToString("F1")).Append("  (").Append(ms.ToString("F1")).Append(" ms)\n");
                    _builder.Append("Tris     ").Append(FormatCount(triangles)).Append('\n');
                    _builder.Append("Batches  ").Append(FormatCount(batches)).Append('\n');
                    _builder.Append("DrawCall ").Append(FormatCount(drawCalls)).Append('\n');
                    _builder.Append("SetPass  ").Append(FormatCount(setPass)).Append('\n');
                    _builder.Append("Mono     ").Append(FormatBytes(monoUsed)).Append(" / ").Append(FormatBytes(monoHeap)).Append('\n');
                    _builder.Append("Alloc    ").Append(FormatBytes(totalAlloc)).Append('\n');
                    _builder.Append("Reserve  ").Append(FormatBytes(totalReserved)).Append('\n');
                    _builder.Append("GfxDrv   ").Append(FormatBytes(gfxDriver));
                }

                _bodyLabel.text = _builder.ToString();
            }

            private void StartRecorders()
            {
#if UNITY_2020_2_OR_NEWER
                if (_recordersStarted)
                {
                    return;
                }

                _trianglesRecorder = CreateRecorder("Triangles Count");
                _drawCallsRecorder = CreateRecorder("Draw Calls Count");
                _batchesRecorder = CreateRecorder("Batches Count");
                _setPassRecorder = CreateRecorder("SetPass Calls Count");
                _recordersStarted = true;
#endif
            }

            private void StopRecorders()
            {
#if UNITY_2020_2_OR_NEWER
                DisposeRecorder(ref _trianglesRecorder);
                DisposeRecorder(ref _drawCallsRecorder);
                DisposeRecorder(ref _batchesRecorder);
                DisposeRecorder(ref _setPassRecorder);
                _recordersStarted = false;
#endif
            }

#if UNITY_2020_2_OR_NEWER
            private static ProfilerRecorder CreateRecorder(string markerName)
            {
                try
                {
                    return ProfilerRecorder.StartNew(ProfilerCategory.Render, markerName);
                }
                catch (Exception)
                {
                    return default;
                }
            }

            private static void DisposeRecorder(ref ProfilerRecorder recorder)
            {
                if (recorder.Valid)
                {
                    recorder.Dispose();
                }

                recorder = default;
            }

            private static long ReadRecorder(ProfilerRecorder recorder)
            {
                if (!recorder.Valid || !recorder.IsRunning)
                {
                    return -1L;
                }

                return Math.Max(0L, recorder.LastValue);
            }
#endif

            private static string FormatCount(long value)
            {
                if (value < 0L)
                {
                    return "n/a";
                }

                if (value >= 1000000L)
                {
                    return (value / 1000000f).ToString("F2") + "M";
                }

                if (value >= 1000L)
                {
                    return (value / 1000f).ToString("F1") + "K";
                }

                return value.ToString();
            }

            private static string FormatBytes(long byteLength)
            {
                if (byteLength < 1024L)
                {
                    return byteLength + " B";
                }

                if (byteLength < 1048576L)
                {
                    return (byteLength / 1024f).ToString("F1") + " KB";
                }

                if (byteLength < 1073741824L)
                {
                    return (byteLength / 1048576f).ToString("F1") + " MB";
                }

                return (byteLength / 1073741824f).ToString("F2") + " GB";
            }
        }
    }
}
