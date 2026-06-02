using AlicizaX.Editor;
using UnityEditor;
using UnityEngine;

namespace AlicizaX.Console.Editor
{
    [CustomEditor(typeof(AlicizaXConsoleUITK), true)]
    public sealed class AlicizaXConsoleUITKInspector : UnityEditor.Editor
    {
        private const float LabelWidth = 148f;

        // 外观。
        private SerializedProperty _fontProperty;
        private SerializedProperty _panelColorProperty;

        // 按键绑定。
        private SerializedProperty _submitCommandKeyProperty;
        private SerializedProperty _showConsoleKeyProperty;
        private SerializedProperty _hideConsoleKeyProperty;
        private SerializedProperty _toggleConsoleKeyProperty;
        private SerializedProperty _zoomInKeyProperty;
        private SerializedProperty _zoomOutKeyProperty;
        private SerializedProperty _dragConsoleKeyProperty;
        private SerializedProperty _selectNextSuggestionKeyProperty;
        private SerializedProperty _selectPreviousSuggestionKeyProperty;
        private SerializedProperty _nextCommandKeyProperty;
        private SerializedProperty _previousCommandKeyProperty;
        private SerializedProperty _cancelActionsKeyProperty;

        // 核心。
        private SerializedProperty _documentProperty;
        private SerializedProperty _visualTreeProperty;
        private SerializedProperty _styleSheetProperty;
        private SerializedProperty _supportedStateProperty;
        private SerializedProperty _activateOnStartupProperty;
        private SerializedProperty _initialiseOnStartupProperty;
        private SerializedProperty _focusOnActivateProperty;
        private SerializedProperty _closeOnSubmitProperty;
        private SerializedProperty _autoScrollProperty;
        private SerializedProperty _commandAssemblyNamesProperty;

        // 窗口。
        private SerializedProperty _allowDragProperty;
        private SerializedProperty _allowResizeProperty;
        private SerializedProperty _allowZoomProperty;
        private SerializedProperty _defaultSizeProperty;
        private SerializedProperty _minimumSizeProperty;
        private SerializedProperty _zoomStepProperty;
        private SerializedProperty _minimumZoomProperty;
        private SerializedProperty _maximumZoomProperty;

        // 日志。
        private SerializedProperty _verboseErrorsProperty;
        private SerializedProperty _verboseLoggingProperty;
        private SerializedProperty _loggingLevelProperty;
        private SerializedProperty _openOnLogLevelProperty;
        private SerializedProperty _interceptDebugProperty;
        private SerializedProperty _interceptInactiveProperty;
        private SerializedProperty _prependTimestampsProperty;
        private SerializedProperty _maxStoredLogsProperty;
        private SerializedProperty _maxLogSizeProperty;
        private SerializedProperty _showInitLogsProperty;

        // 自动补全。
        private SerializedProperty _enableAutocompleteProperty;
        private SerializedProperty _showPopupProperty;
        private SerializedProperty _suggestionOrderProperty;
        private SerializedProperty _maxSuggestionDisplaySizeProperty;
        private SerializedProperty _useFuzzySearchProperty;
        private SerializedProperty _caseSensitiveSearchProperty;
        private SerializedProperty _collapseSuggestionOverloadsProperty;

        // 异步。
        private SerializedProperty _showCurrentJobsProperty;
        private SerializedProperty _blockOnAsyncProperty;

        // 历史记录。
        private SerializedProperty _storeCommandHistoryProperty;
        private SerializedProperty _storeDuplicateCommandsProperty;
        private SerializedProperty _storeAdjacentDuplicateCommandsProperty;
        private SerializedProperty _commandHistorySizeProperty;

        private bool _appearanceExpanded;
        private bool _keyBindingsExpanded;
        private bool _loggingExpanded;
        private bool _autocompleteExpanded;
        private bool _historyExpanded;
        private bool _asyncExpanded;
        private bool _referencesExpanded;

        private GUIStyle _panelStyle;
        private GUIStyle _fieldRowStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _mutedLabelStyle;

        private void OnEnable()
        {
            _fontProperty = serializedObject.FindProperty("_font");
            _panelColorProperty = serializedObject.FindProperty("_panelColor");

            _submitCommandKeyProperty = serializedObject.FindProperty("_submitCommandKey");
            _showConsoleKeyProperty = serializedObject.FindProperty("_showConsoleKey");
            _hideConsoleKeyProperty = serializedObject.FindProperty("_hideConsoleKey");
            _toggleConsoleKeyProperty = serializedObject.FindProperty("_toggleConsoleKey");
            _zoomInKeyProperty = serializedObject.FindProperty("_zoomInKey");
            _zoomOutKeyProperty = serializedObject.FindProperty("_zoomOutKey");
            _dragConsoleKeyProperty = serializedObject.FindProperty("_dragConsoleKey");
            _selectNextSuggestionKeyProperty = serializedObject.FindProperty("_selectNextSuggestionKey");
            _selectPreviousSuggestionKeyProperty = serializedObject.FindProperty("_selectPreviousSuggestionKey");
            _nextCommandKeyProperty = serializedObject.FindProperty("_nextCommandKey");
            _previousCommandKeyProperty = serializedObject.FindProperty("_previousCommandKey");
            _cancelActionsKeyProperty = serializedObject.FindProperty("_cancelActionsKey");

            _documentProperty = serializedObject.FindProperty("_document");
            _visualTreeProperty = serializedObject.FindProperty("_visualTree");
            _styleSheetProperty = serializedObject.FindProperty("_styleSheet");
            _supportedStateProperty = serializedObject.FindProperty("_supportedState");
            _activateOnStartupProperty = serializedObject.FindProperty("_activateOnStartup");
            _initialiseOnStartupProperty = serializedObject.FindProperty("_initialiseOnStartup");
            _focusOnActivateProperty = serializedObject.FindProperty("_focusOnActivate");
            _closeOnSubmitProperty = serializedObject.FindProperty("_closeOnSubmit");
            _autoScrollProperty = serializedObject.FindProperty("_autoScroll");
            _commandAssemblyNamesProperty = serializedObject.FindProperty("_commandAssemblyNames");

            _allowDragProperty = serializedObject.FindProperty("_allowDrag");
            _allowResizeProperty = serializedObject.FindProperty("_allowResize");
            _allowZoomProperty = serializedObject.FindProperty("_allowZoom");
            _defaultSizeProperty = serializedObject.FindProperty("_defaultSize");
            _minimumSizeProperty = serializedObject.FindProperty("_minimumSize");
            _zoomStepProperty = serializedObject.FindProperty("_zoomStep");
            _minimumZoomProperty = serializedObject.FindProperty("_minimumZoom");
            _maximumZoomProperty = serializedObject.FindProperty("_maximumZoom");

            _verboseErrorsProperty = serializedObject.FindProperty("_verboseErrors");
            _verboseLoggingProperty = serializedObject.FindProperty("_verboseLogging");
            _loggingLevelProperty = serializedObject.FindProperty("_loggingLevel");
            _openOnLogLevelProperty = serializedObject.FindProperty("_openOnLogLevel");
            _interceptDebugProperty = serializedObject.FindProperty("_interceptDebugLogger");
            _interceptInactiveProperty = serializedObject.FindProperty("_interceptWhilstInactive");
            _prependTimestampsProperty = serializedObject.FindProperty("_prependTimestamps");
            _maxStoredLogsProperty = serializedObject.FindProperty("_maxStoredLogs");
            _maxLogSizeProperty = serializedObject.FindProperty("_maxLogSize");
            _showInitLogsProperty = serializedObject.FindProperty("_showInitLogs");

            _enableAutocompleteProperty = serializedObject.FindProperty("_enableAutocomplete");
            _showPopupProperty = serializedObject.FindProperty("_showPopupDisplay");
            _suggestionOrderProperty = serializedObject.FindProperty("_suggestionDisplayOrder");
            _maxSuggestionDisplaySizeProperty = serializedObject.FindProperty("_maxSuggestionDisplaySize");
            _useFuzzySearchProperty = serializedObject.FindProperty("_useFuzzySearch");
            _caseSensitiveSearchProperty = serializedObject.FindProperty("_caseSensitiveSearch");
            _collapseSuggestionOverloadsProperty = serializedObject.FindProperty("_collapseSuggestionOverloads");

            _showCurrentJobsProperty = serializedObject.FindProperty("_showCurrentJobs");
            _blockOnAsyncProperty = serializedObject.FindProperty("_blockOnAsync");

            _storeCommandHistoryProperty = serializedObject.FindProperty("_storeCommandHistory");
            _storeDuplicateCommandsProperty = serializedObject.FindProperty("_storeDuplicateCommands");
            _storeAdjacentDuplicateCommandsProperty = serializedObject.FindProperty("_storeAdjacentDuplicateCommands");
            _commandHistorySizeProperty = serializedObject.FindProperty("_commandHistorySize");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EnsureStyles();

            EditorGUILayout.Space(4f);
            DrawCoreSettings();
            DrawStartupSettings();
            DrawWindowSettings();
            DrawAppearanceSettings();
            DrawKeyBindingSettings();
            DrawLoggingSettings();
            DrawAutocompleteSettings();
            DrawHistorySettings();
            DrawAsyncSettings();
            DrawReferenceSettings();

            serializedObject.ApplyModifiedProperties();
        }


        private void DrawCoreSettings()
        {
            BeginPanel("Core");
            DrawPropertyRow(_supportedStateProperty, "Supported State");
            DrawPropertyRow(_autoScrollProperty, "Auto Scroll");
            DrawPropertyRow(_commandAssemblyNamesProperty, "Command Assemblies");
            EndPanel();
        }

        private void DrawStartupSettings()
        {
            BeginPanel("Startup");
            DrawPropertyRow(_activateOnStartupProperty, "Show On Startup");
            DrawPropertyRow(_initialiseOnStartupProperty, "Initialize On Startup");
            DrawPropertyRow(_focusOnActivateProperty, "Focus On Activate");
            DrawPropertyRow(_closeOnSubmitProperty, "Close On Submit");
            EndPanel();
        }

        private void DrawWindowSettings()
        {
            BeginPanel("Window");
            DrawPropertyRow(_defaultSizeProperty, "Default Size");
            DrawPropertyRow(_minimumSizeProperty, "Minimum Size");
            DrawPropertyRow(_allowDragProperty, "Allow Drag");
            DrawPropertyRow(_allowResizeProperty, "Allow Resize");
            DrawPropertyRow(_allowZoomProperty, "Allow Zoom");
            if (_allowZoomProperty.boolValue)
            {
                DrawPropertyRow(_zoomStepProperty, "Zoom Step");
                DrawPropertyRow(_minimumZoomProperty, "Minimum Zoom");
                DrawPropertyRow(_maximumZoomProperty, "Maximum Zoom");
            }
            EndPanel();
        }

        private void DrawAppearanceSettings()
        {
            _appearanceExpanded = DrawFoldoutPanelHeader("Appearance", _appearanceExpanded);
            if (!_appearanceExpanded) { return; }

            EditorGUILayout.BeginVertical(_panelStyle);
            DrawPropertyRow(_fontProperty, "Font");
            DrawPropertyRow(_panelColorProperty, "Panel Color");
            EditorGUILayout.EndVertical();
        }

        private void DrawKeyBindingSettings()
        {
            _keyBindingsExpanded = DrawFoldoutPanelHeader("Key Bindings", _keyBindingsExpanded);
            if (!_keyBindingsExpanded) { return; }

            EditorGUILayout.BeginVertical(_panelStyle);
            DrawPropertyRow(_submitCommandKeyProperty, "Submit");
            DrawPropertyRow(_showConsoleKeyProperty, "Show Console");
            DrawPropertyRow(_hideConsoleKeyProperty, "Hide Console");
            DrawPropertyRow(_toggleConsoleKeyProperty, "Toggle Console");
            DrawPropertyRow(_zoomInKeyProperty, "Zoom In");
            DrawPropertyRow(_zoomOutKeyProperty, "Zoom Out");
            DrawPropertyRow(_dragConsoleKeyProperty, "Drag Console");
            DrawPropertyRow(_selectNextSuggestionKeyProperty, "Next Suggestion");
            DrawPropertyRow(_selectPreviousSuggestionKeyProperty, "Prev Suggestion");
            DrawPropertyRow(_nextCommandKeyProperty, "History Up");
            DrawPropertyRow(_previousCommandKeyProperty, "History Down");
            DrawPropertyRow(_cancelActionsKeyProperty, "Cancel Actions");
            EditorGUILayout.EndVertical();
        }

        private void DrawLoggingSettings()
        {
            _loggingExpanded = DrawFoldoutPanelHeader("Logging", _loggingExpanded);
            if (!_loggingExpanded) { return; }

            EditorGUILayout.BeginVertical(_panelStyle);
            DrawPropertyRow(_interceptDebugProperty, "Intercept Debug");
            if (_interceptDebugProperty.boolValue)
            {
                DrawPropertyRow(_interceptInactiveProperty, "Inactive Capture");
                DrawPropertyRow(_prependTimestampsProperty, "Timestamps");
                DrawPropertyRow(_loggingLevelProperty, "Logging Level");
                DrawPropertyRow(_verboseLoggingProperty, "Verbose Logging");
                DrawPropertyRow(_openOnLogLevelProperty, "Open On Log");
            }
            DrawPropertyRow(_verboseErrorsProperty, "Verbose Errors");
            DrawPropertyRow(_maxStoredLogsProperty, "Max Stored Logs");
            DrawPropertyRow(_maxLogSizeProperty, "Max Log Size");
            DrawPropertyRow(_showInitLogsProperty, "Show Init Logs");
            EditorGUILayout.EndVertical();
        }

        private void DrawAutocompleteSettings()
        {
            _autocompleteExpanded = DrawFoldoutPanelHeader("Autocomplete", _autocompleteExpanded);
            if (!_autocompleteExpanded) { return; }

            EditorGUILayout.BeginVertical(_panelStyle);
            DrawPropertyRow(_enableAutocompleteProperty, "Enabled");
            if (_enableAutocompleteProperty.boolValue)
            {
                DrawPropertyRow(_useFuzzySearchProperty, "Fuzzy Search");
                DrawPropertyRow(_caseSensitiveSearchProperty, "Case Sensitive");
                DrawPropertyRow(_collapseSuggestionOverloadsProperty, "Collapse Overloads");
                DrawPropertyRow(_showPopupProperty, "Popup Display");
                if (_showPopupProperty.boolValue)
                {
                    DrawPropertyRow(_maxSuggestionDisplaySizeProperty, "Max Suggestions");
                    DrawPropertyRow(_suggestionOrderProperty, "Display Order");
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawHistorySettings()
        {
            _historyExpanded = DrawFoldoutPanelHeader("Command History", _historyExpanded);
            if (!_historyExpanded) { return; }

            EditorGUILayout.BeginVertical(_panelStyle);
            DrawPropertyRow(_storeCommandHistoryProperty, "Enabled");
            if (_storeCommandHistoryProperty.boolValue)
            {
                DrawPropertyRow(_storeDuplicateCommandsProperty, "Store Duplicates");
                if (_storeDuplicateCommandsProperty.boolValue)
                {
                    DrawPropertyRow(_storeAdjacentDuplicateCommandsProperty, "Adjacent Duplicates");
                }
                DrawPropertyRow(_commandHistorySizeProperty, "History Size");
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawAsyncSettings()
        {
            _asyncExpanded = DrawFoldoutPanelHeader("Async Commands", _asyncExpanded);
            if (!_asyncExpanded) { return; }

            EditorGUILayout.BeginVertical(_panelStyle);
            DrawPropertyRow(_showCurrentJobsProperty, "Show Current Jobs");
            DrawPropertyRow(_blockOnAsyncProperty, "Block On Async");
            EditorGUILayout.EndVertical();
        }

        private void DrawReferenceSettings()
        {
            _referencesExpanded = DrawFoldoutPanelHeader("References", _referencesExpanded);
            if (!_referencesExpanded) { return; }

            EditorGUILayout.BeginVertical(_panelStyle);
            using (new EditorGUI.DisabledScope(true))
            {
                DrawPropertyRow(serializedObject.FindProperty("m_Script"), "Script");
            }
            DrawPropertyRow(_documentProperty, "Document");
            DrawPropertyRow(_visualTreeProperty, "Visual Tree");
            DrawPropertyRow(_styleSheetProperty, "Style Sheet");
            EditorGUILayout.EndVertical();
        }

        private void BeginPanel(string title)
        {
            EditorGUILayout.BeginVertical(_panelStyle);
            Rect rect = GUILayoutUtility.GetRect(1f, 22f);
            AlicizaEditorGUI.DrawToolbarBackground(rect);
            GUI.Label(new Rect(rect.x + 7f, rect.y + 3f, rect.width - 14f, 16f), title, EditorStyles.boldLabel);
        }

        private void EndPanel()
        {
            EditorGUILayout.EndVertical();
        }

        private bool DrawFoldoutPanelHeader(string title, bool expanded)
        {
            Rect rect = GUILayoutUtility.GetRect(1f, 24f);
            bool hovered = rect.Contains(Event.current.mousePosition);
            AlicizaEditorGUI.DrawListItemBackground(rect, expanded, hovered);
            AlicizaEditorGUI.DrawFoldoutIcon(new Rect(rect.x + 7f, rect.y + 4f, 14f, 16f), expanded);
            GUI.Label(new Rect(rect.x + 24f, rect.y + 4f, rect.width - 32f, 16f), title, _labelStyle);

            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && rect.Contains(currentEvent.mousePosition))
            {
                expanded = !expanded;
                currentEvent.Use();
            }

            return expanded;
        }

        private void DrawPropertyRow(SerializedProperty property, string label)
        {
            if (property == null)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal(_fieldRowStyle);
            EditorGUILayout.LabelField(label, _mutedLabelStyle, GUILayout.Width(LabelWidth));
            EditorGUILayout.PropertyField(property, GUIContent.none, true);
            EditorGUILayout.EndHorizontal();
        }

        private void EnsureStyles()
        {
            if (_panelStyle != null)
            {
                return;
            }

            _panelStyle = AlicizaEditorGUI.Styles.Panel;
            _fieldRowStyle = AlicizaEditorGUI.Styles.FieldRow;
            _labelStyle = AlicizaEditorGUI.Styles.RowLabel;
            _mutedLabelStyle = AlicizaEditorGUI.Styles.MutedLabel;
        }
    }
}
