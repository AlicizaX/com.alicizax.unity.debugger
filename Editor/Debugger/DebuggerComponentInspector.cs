using AlicizaX.Debugger;
using AlicizaX.Editor;
using UnityEditor;
using UnityEngine;

namespace AlicizaX.Debugger.Editor
{
    [CustomEditor(typeof(DebuggerComponent))]
    internal sealed class DebuggerComponentInspector : UnityEditor.Editor
    {
        private const float ToolbarHeight = 30f;
        private const float RowLabelWidth = 132f;
        private const float SliderValueWidth = 46f;
        private const float SliderMinValue = 0.2f;
        private const float SliderMaxValue = 1f;

        private SerializedProperty _activeWindowProperty;
        private SerializedProperty _enableFloatingToggleSnapProperty;
        private SerializedProperty _windowOpacityProperty;
        private SerializedProperty _panelSettingsProperty;
        private SerializedProperty _fontProperty;
        private SerializedProperty _commandWindowProperty;
        private SerializedProperty _commandAssemblyNamesProperty;
        private SerializedProperty _commandEnableAutocompleteProperty;
        private SerializedProperty _commandShowPopupDisplayProperty;
        private SerializedProperty _commandStoreCommandHistoryProperty;
        private SerializedProperty _commandMaxStoredLogsProperty;
        private SerializedProperty _commandSubmitKeyProperty;
        private SerializedProperty _commandNextSuggestionKeyProperty;
        private SerializedProperty _commandPreviousSuggestionKeyProperty;
        private SerializedProperty _commandNextHistoryKeyProperty;
        private SerializedProperty _commandPreviousHistoryKeyProperty;
        private SerializedProperty _commandCancelActionsKeyProperty;
        private GUIStyle _panelStyle;
        private GUIStyle _fieldRowStyle;
        private GUIStyle _fieldLabelStyle;
        private GUIStyle _rowLabelStyle;
        private string[] _activeWindowOptions;
        private bool _commandFoldout = true;

        private void OnEnable()
        {
            _activeWindowProperty = serializedObject.FindProperty("m_ActiveWindow");
            _enableFloatingToggleSnapProperty = serializedObject.FindProperty("m_EnableFloatingToggleSnap");
            _windowOpacityProperty = serializedObject.FindProperty("m_WindowOpacity");
            _panelSettingsProperty = serializedObject.FindProperty("m_PanelSettings");
            _fontProperty = serializedObject.FindProperty("m_Font");
            _commandWindowProperty = serializedObject.FindProperty("m_CommandWindow");
            if (_commandWindowProperty != null)
            {
                _commandAssemblyNamesProperty = _commandWindowProperty.FindPropertyRelative("m_CommandAssemblyNames");
                _commandEnableAutocompleteProperty = _commandWindowProperty.FindPropertyRelative("m_EnableAutocomplete");
                _commandShowPopupDisplayProperty = _commandWindowProperty.FindPropertyRelative("m_ShowPopupDisplay");
                _commandStoreCommandHistoryProperty = _commandWindowProperty.FindPropertyRelative("m_StoreCommandHistory");
                _commandMaxStoredLogsProperty = _commandWindowProperty.FindPropertyRelative("m_MaxStoredLogs");
                _commandSubmitKeyProperty = _commandWindowProperty.FindPropertyRelative("m_SubmitCommandKey");
                _commandNextSuggestionKeyProperty = _commandWindowProperty.FindPropertyRelative("m_SelectNextSuggestionKey");
                _commandPreviousSuggestionKeyProperty = _commandWindowProperty.FindPropertyRelative("m_SelectPreviousSuggestionKey");
                _commandNextHistoryKeyProperty = _commandWindowProperty.FindPropertyRelative("m_NextCommandKey");
                _commandPreviousHistoryKeyProperty = _commandWindowProperty.FindPropertyRelative("m_PreviousCommandKey");
                _commandCancelActionsKeyProperty = _commandWindowProperty.FindPropertyRelative("m_CancelActionsKey");
            }

            _activeWindowOptions = _activeWindowProperty.enumDisplayNames;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EnsureStyles();

            DrawComponentPanel();

            serializedObject.ApplyModifiedProperties();
        }

        private void EnsureStyles()
        {
            if (_panelStyle != null)
            {
                return;
            }

            _panelStyle = AlicizaEditorGUI.Styles.Panel;
            _fieldRowStyle = AlicizaEditorGUI.Styles.FieldRow;
            _fieldLabelStyle = AlicizaEditorGUI.Styles.FieldLabel;
            _rowLabelStyle = AlicizaEditorGUI.Styles.RowLabel;
        }

        private void DrawComponentPanel()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginVertical(_panelStyle);
            DrawToolbar("Debugger Component");

            DrawActiveWindowRow();
            DrawPanelSettingsRow();
            DrawFontRow();
            DrawToggleRow("Enable Snap", _enableFloatingToggleSnapProperty);
            DrawOpacityRow();
            DrawCommandSettings();

            EditorGUILayout.EndVertical();
        }

        private void DrawCommandSettings()
        {
            if (_commandWindowProperty == null)
            {
                return;
            }

            EditorGUILayout.Space(4f);
            _commandFoldout = EditorGUILayout.Foldout(_commandFoldout, "Command Tab", true);
            if (!_commandFoldout)
            {
                return;
            }

            EditorGUI.indentLevel++;
            if (_commandAssemblyNamesProperty != null)
            {
                EditorGUILayout.PropertyField(_commandAssemblyNamesProperty, new GUIContent("Command Assemblies"), true);
            }

            DrawNestedToggle("Autocomplete", _commandEnableAutocompleteProperty);
            DrawNestedToggle("Suggestion Popup", _commandShowPopupDisplayProperty);
            DrawNestedToggle("Command History", _commandStoreCommandHistoryProperty);

            if (_commandMaxStoredLogsProperty != null)
            {
                EditorGUILayout.BeginHorizontal(_fieldRowStyle);
                EditorGUILayout.LabelField("Max Stored Logs", _fieldLabelStyle, GUILayout.Width(RowLabelWidth));
                _commandMaxStoredLogsProperty.intValue = EditorGUILayout.IntField(_commandMaxStoredLogsProperty.intValue);
                EditorGUILayout.EndHorizontal();
            }

            DrawNestedProperty("Submit Key", _commandSubmitKeyProperty);
            DrawNestedProperty("Next Suggestion", _commandNextSuggestionKeyProperty);
            DrawNestedProperty("Prev Suggestion", _commandPreviousSuggestionKeyProperty);
            DrawNestedProperty("Next History", _commandNextHistoryKeyProperty);
            DrawNestedProperty("Prev History", _commandPreviousHistoryKeyProperty);
            DrawNestedProperty("Cancel Actions", _commandCancelActionsKeyProperty);
            EditorGUI.indentLevel--;
        }

        private void DrawNestedToggle(string label, SerializedProperty property)
        {
            if (property == null)
            {
                return;
            }

            DrawToggleRow(label, property);
        }

        private void DrawNestedProperty(string label, SerializedProperty property)
        {
            if (property == null)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal(_fieldRowStyle);
            EditorGUILayout.LabelField(label, _fieldLabelStyle, GUILayout.Width(RowLabelWidth));
            EditorGUILayout.PropertyField(property, GUIContent.none, true);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar(string title)
        {
            Rect toolbarRect = GUILayoutUtility.GetRect(1f, ToolbarHeight, GUILayout.ExpandWidth(true));
            AlicizaEditorGUI.DrawToolbarBackground(toolbarRect);

            Rect labelRect = new Rect(toolbarRect.x + 8f, toolbarRect.y + 5f, toolbarRect.width - 16f, 20f);
            GUI.Label(labelRect, title, _rowLabelStyle);
        }

        private void DrawActiveWindowRow()
        {
            EditorGUILayout.BeginHorizontal(_fieldRowStyle);
            EditorGUILayout.LabelField("Active Window", _fieldLabelStyle, GUILayout.Width(RowLabelWidth));

            Rect popupRect = GUILayoutUtility.GetRect(90f, 20f, GUILayout.MinWidth(90f), GUILayout.ExpandWidth(true));
            _activeWindowProperty.enumValueIndex = AlicizaEditorGUI.DrawStyledPopup(
                popupRect,
                _activeWindowProperty.enumValueIndex,
                _activeWindowOptions);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPanelSettingsRow()
        {
            EditorGUILayout.BeginHorizontal(_fieldRowStyle);
            EditorGUILayout.LabelField("Panel Settings", _fieldLabelStyle, GUILayout.Width(RowLabelWidth));
            EditorGUILayout.PropertyField(_panelSettingsProperty, GUIContent.none);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFontRow()
        {
            EditorGUILayout.BeginHorizontal(_fieldRowStyle);
            EditorGUILayout.LabelField("Font", _fieldLabelStyle, GUILayout.Width(RowLabelWidth));
            EditorGUILayout.PropertyField(_fontProperty, GUIContent.none);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToggleRow(string label, SerializedProperty property)
        {
            EditorGUILayout.BeginHorizontal(_fieldRowStyle);
            EditorGUILayout.LabelField(label, _fieldLabelStyle, GUILayout.Width(RowLabelWidth));
            property.boolValue = GUILayout.Toggle(
                property.boolValue,
                property.boolValue ? "Enabled" : "Disabled",
                property.boolValue ? AlicizaEditorGUI.Styles.PillOn : AlicizaEditorGUI.Styles.PillOff,
                GUILayout.Width(78f));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawOpacityRow()
        {
            EditorGUILayout.BeginHorizontal(_fieldRowStyle);
            EditorGUILayout.LabelField("Window Opacity", _fieldLabelStyle, GUILayout.Width(RowLabelWidth));

            float value = Mathf.Clamp(_windowOpacityProperty.floatValue, SliderMinValue, SliderMaxValue);
            value = GUILayout.HorizontalSlider(value, SliderMinValue, SliderMaxValue, GUILayout.MinWidth(90f));
            value = Mathf.Clamp(EditorGUILayout.FloatField(value, GUILayout.Width(SliderValueWidth)), SliderMinValue, SliderMaxValue);
            _windowOpacityProperty.floatValue = value;

            EditorGUILayout.EndHorizontal();
        }
    }
}
