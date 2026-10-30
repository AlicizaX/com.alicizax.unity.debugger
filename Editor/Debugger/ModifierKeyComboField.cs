using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AlicizaX.Debugger.Editor
{

    internal sealed class ModifierKeyComboField : VisualElement
    {
        private AlicizaX.Editor.UIToolkit.AlicizaEnumField _keyField;
        private Toggle _ctrlToggle;
        private Toggle _altToggle;
        private Toggle _shiftToggle;

        public ModifierKeyComboField(SerializedProperty property)
        {
            AddToClassList("alicia-keycombo");

            if (property == null)
            {
                Add(AlicizaX.Editor.UIToolkit.AlicizaEditorUI.CreateHint("Modifier key combo not found."));
                return;
            }

            SerializedProperty keyProperty = property.FindPropertyRelative("Key");
            SerializedProperty ctrlProperty = property.FindPropertyRelative("Ctrl");
            SerializedProperty altProperty = property.FindPropertyRelative("Alt");
            SerializedProperty shiftProperty = property.FindPropertyRelative("Shift");

            if (keyProperty == null)
            {
                Add(AlicizaX.Editor.UIToolkit.AlicizaEditorUI.CreateHint("Key property not found."));
                return;
            }

            _keyField = new AlicizaX.Editor.UIToolkit.AlicizaEnumField(keyProperty);
            _keyField.AddToClassList("alicia-keycombo__key");

            _ctrlToggle = CreateToggle("Ctrl", ctrlProperty);
            _altToggle = CreateToggle("Alt", altProperty);
            _shiftToggle = CreateToggle("Shift", shiftProperty);

            var modifiers = new VisualElement { name = "alicia-keycombo-modifiers" };
            modifiers.AddToClassList("alicia-keycombo__modifiers");
            modifiers.Add(_ctrlToggle);
            modifiers.Add(_altToggle);
            modifiers.Add(_shiftToggle);

            Add(_keyField);
            Add(modifiers);

            _keyField.RegisterValueChangedCallback(evt => UpdateModifierState(ToKeyCode(evt.newValue)));
            schedule.Execute(() => UpdateModifierState((KeyCode)keyProperty.enumValueIndex)).Every(200);
            UpdateModifierState((KeyCode)keyProperty.enumValueIndex);
        }

        private static Toggle CreateToggle(string label, SerializedProperty property)
        {
            var toggle = new Toggle(label);
            toggle.AddToClassList("alicia-keycombo__modifier");
            toggle.style.flexShrink = 0f;
            toggle.style.minWidth = 0f;

            VisualElement input = toggle.Q(className: BaseField<bool>.inputUssClassName);
            if (input != null)
            {
                toggle.Insert(0, input);
            }

            toggle.labelElement.AddToClassList("alicia-keycombo__modifier-label");
            toggle.labelElement.style.width = 32f;
            toggle.labelElement.style.minWidth = 0f;
            toggle.labelElement.style.flexGrow = 0f;

            if (property != null)
            {
                toggle.BindProperty(property);
            }
            else
            {
                toggle.SetEnabled(false);
            }

            return toggle;
        }

        private void UpdateModifierState(KeyCode key)
        {
            bool enabled = key != KeyCode.None;
            _ctrlToggle?.SetEnabled(enabled);
            _altToggle?.SetEnabled(enabled);
            _shiftToggle?.SetEnabled(enabled);
        }

        private static KeyCode ToKeyCode(int value)
        {
            return (KeyCode)value;
        }
    }
}
