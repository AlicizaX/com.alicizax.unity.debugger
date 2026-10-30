using AlicizaX.Editor.UIToolkit;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AlicizaX.Debugger.Editor
{
    [CustomEditor(typeof(DebuggerComponent))]
    internal sealed class DebuggerComponentInspector : AlicizaUIEditor
    {
        private SerializedProperty _activeWindow;
        private SerializedProperty _enableFloatingToggleSnap;
        private SerializedProperty _windowOpacity;
        private SerializedProperty _panelSettings;
        private SerializedProperty _font;
        private SerializedProperty _commandWindow;
        private SerializedProperty _commandAssemblyNames;
        private SerializedProperty _commandEnableAutocomplete;
        private SerializedProperty _commandShowPopupDisplay;
        private SerializedProperty _commandStoreCommandHistory;
        private SerializedProperty _commandMaxStoredLogs;
        private SerializedProperty _commandSubmitKey;
        private SerializedProperty _commandNextSuggestionKey;
        private SerializedProperty _commandPreviousSuggestionKey;
        private SerializedProperty _commandNextHistoryKey;
        private SerializedProperty _commandPreviousHistoryKey;
        private SerializedProperty _commandCancelActionsKey;

        private void OnEnable()
        {
            CacheProperties();
        }

        protected override void BuildInspector(VisualElement root)
        {
            if (_activeWindow == null)
            {
                CacheProperties();
            }

            AlicizaPanel panel = AlicizaEditorUI.CreatePanel("Debugger Component");
            root.Add(panel);
            BuildComponentRows(panel.Body);
            BuildCommandSection(panel.Body);
        }

        private void CacheProperties()
        {
            _activeWindow = Find("m_ActiveWindow");
            _enableFloatingToggleSnap = Find("m_EnableFloatingToggleSnap");
            _windowOpacity = Find("m_WindowOpacity");
            _panelSettings = Find("m_PanelSettings");
            _font = Find("m_Font");
            _commandWindow = Find("m_CommandWindow");

            if (_commandWindow == null)
            {
                return;
            }

            _commandAssemblyNames = _commandWindow.FindPropertyRelative("m_CommandAssemblyNames");
            _commandEnableAutocomplete = _commandWindow.FindPropertyRelative("m_EnableAutocomplete");
            _commandShowPopupDisplay = _commandWindow.FindPropertyRelative("m_ShowPopupDisplay");
            _commandStoreCommandHistory = _commandWindow.FindPropertyRelative("m_StoreCommandHistory");
            _commandMaxStoredLogs = _commandWindow.FindPropertyRelative("m_MaxStoredLogs");
            _commandSubmitKey = _commandWindow.FindPropertyRelative("m_SubmitCommandKey");
            _commandNextSuggestionKey = _commandWindow.FindPropertyRelative("m_SelectNextSuggestionKey");
            _commandPreviousSuggestionKey = _commandWindow.FindPropertyRelative("m_SelectPreviousSuggestionKey");
            _commandNextHistoryKey = _commandWindow.FindPropertyRelative("m_NextCommandKey");
            _commandPreviousHistoryKey = _commandWindow.FindPropertyRelative("m_PreviousCommandKey");
            _commandCancelActionsKey = _commandWindow.FindPropertyRelative("m_CancelActionsKey");
        }

        private void BuildComponentRows(VisualElement parent)
        {
            AddActiveWindowRow(parent);
            AddObjectRow(parent, "Panel Settings", _panelSettings, typeof(PanelSettings));
            AddObjectRow(parent, "Font", _font, typeof(Font));
            AddToggleRow(parent, "Enable Snap", _enableFloatingToggleSnap);
            AddOpacityRow(parent);
        }

        private void BuildCommandSection(VisualElement parent)
        {
            if (_commandWindow == null)
            {
                return;
            }

            if (_commandAssemblyNames != null)
            {
                parent.Add(new AlicizaStringListField(_commandAssemblyNames, "Command Assemblies"));
            }

            AddToggleRow(parent, "Autocomplete", _commandEnableAutocomplete);
            AddToggleRow(parent, "Suggestion Popup", _commandShowPopupDisplay);
            AddToggleRow(parent, "Command History", _commandStoreCommandHistory);
            AddIntegerRow(parent, "Max Stored Logs", _commandMaxStoredLogs);
            AddKeyRow(parent, "Submit Key", _commandSubmitKey);
            AddComboRow(parent, "Next Suggestion", _commandNextSuggestionKey);
            AddComboRow(parent, "Prev Suggestion", _commandPreviousSuggestionKey);
            AddKeyRow(parent, "Next History", _commandNextHistoryKey);
            AddKeyRow(parent, "Prev History", _commandPreviousHistoryKey);
            AddComboRow(parent, "Cancel Actions", _commandCancelActionsKey);
        }

        private void AddActiveWindowRow(VisualElement parent)
        {
            if (_activeWindow == null)
            {
                return;
            }

            var field = new AlicizaEnumField(typeof(DebuggerActiveWindowType), _activeWindow);
            field.AddToClassList("alicia-control--grow");
            AlicizaEditorUI.AddRow(parent, "Active Window", field);
        }

        private void AddObjectRow(VisualElement parent, string caption, SerializedProperty property, System.Type objectType)
        {
            if (property == null)
            {
                return;
            }

            var field = new ObjectField(string.Empty)
            {
                objectType = objectType,
                allowSceneObjects = false
            };
            field.AddToClassList("alicia-control");
            field.AddToClassList("alicia-control--grow");
            field.BindProperty(property);
            AlicizaEditorUI.AddRow(parent, caption, field);
        }

        private void AddToggleRow(VisualElement parent, string caption, SerializedProperty property)
        {
            if (property == null)
            {
                return;
            }

            var pill = new AlicizaPillToggle();
            pill.BindProperty(property);
            AlicizaEditorUI.AddRow(parent, caption, pill);
        }

        private void AddOpacityRow(VisualElement parent)
        {
            if (_windowOpacity == null)
            {
                return;
            }

            var slider = new AlicizaSliderField(0.2f, 1f);
            slider.BindProperty(_windowOpacity);
            AlicizaEditorUI.AddRow(parent, "Window Opacity", slider);
        }

        private void AddIntegerRow(VisualElement parent, string caption, SerializedProperty property)
        {
            if (property == null)
            {
                return;
            }

            var field = new IntegerField { label = string.Empty };
            field.AddToClassList("alicia-control");
            field.AddToClassList("alicia-control--narrow");
            field.BindProperty(property);
            AlicizaEditorUI.AddRow(parent, caption, field);
        }

        private void AddKeyRow(VisualElement parent, string caption, SerializedProperty property)
        {
            if (property == null)
            {
                return;
            }

            var field = new AlicizaEnumField(typeof(KeyCode), property);
            field.AddToClassList("alicia-control--key");
            AlicizaEditorUI.AddRow(parent, caption, field);
        }

        private void AddComboRow(VisualElement parent, string caption, SerializedProperty property)
        {
            if (property == null)
            {
                return;
            }

            AlicizaEditorUI.AddRow(parent, caption, new ModifierKeyComboField(property));
        }
    }
}
