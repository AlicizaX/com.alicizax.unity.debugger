using UnityEngine;
using UnityEngine.Serialization;

namespace AlicizaX.Console
{
    [System.Serializable]
    public struct ModifierKeyCombo
    {
        [FormerlySerializedAs("key")]
        public KeyCode Key;
        [FormerlySerializedAs("ctrl")]
        public bool Ctrl;
        [FormerlySerializedAs("alt")]
        public bool Alt;
        [FormerlySerializedAs("shift")]
        public bool Shift;

        public bool ModifiersActive
        {
            get
            {
                bool ctrlDown = !Ctrl ^
                                (InputKeys.GetKey(KeyCode.LeftControl)  ||
                                 InputKeys.GetKey(KeyCode.RightControl) ||
                                 InputKeys.GetKey(KeyCode.LeftCommand)  ||
                                 InputKeys.GetKey(KeyCode.RightCommand));

                bool altDown = !Alt ^ (InputKeys.GetKey(KeyCode.LeftAlt) || InputKeys.GetKey(KeyCode.RightAlt));
                bool shiftDown =
                    !Shift ^ (InputKeys.GetKey(KeyCode.LeftShift) || InputKeys.GetKey(KeyCode.RightShift));

                return ctrlDown && altDown && shiftDown;
            }
        }

        public bool IsHeld()
        {
            return ModifiersActive && InputKeys.GetKey(Key);
        }

        public bool IsPressed()
        {
            return ModifiersActive && InputKeys.GetKeyDown(Key);
        }

        public static implicit operator ModifierKeyCombo(KeyCode key)
        {
            return new ModifierKeyCombo { Key = key };
        }
    }
}
