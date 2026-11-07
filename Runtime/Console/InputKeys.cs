// INPUTSYSTEM_SUPPORT 由 asmdef 的 versionDefines 提供：com.unity.inputsystem 是可选依赖，
// 没装这个包时符号不会定义，整段新输入 API 不参与编译，自动回退旧 Input。
// 只有在"包存在"且"当前激活的是新输入系统"时才走新 API；Both 模式仍用旧 Input。
#if INPUTSYSTEM_SUPPORT && !ENABLE_LEGACY_INPUT_MANAGER
#define NEW_INPUT
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

using UnityEngine;

namespace AlicizaX.Console
{
    public static class InputKeys
    {
        public static bool GetKey(KeyCode key)
        {
#if NEW_INPUT
            return TryGetKeyControl(key, out KeyControl control) && control.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(key);
#else
            // Active Input Handling 设为 New 却没装 Input System 包：两个 API 都不可用。
            return false;
#endif
        }

        public static bool GetKeyDown(KeyCode key)
        {
#if NEW_INPUT
            return TryGetKeyControl(key, out KeyControl control) && control.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(key);
#else
            return false;
#endif
        }

        public static bool GetKeyUp(KeyCode key)
        {
#if NEW_INPUT
            return TryGetKeyControl(key, out KeyControl control) && control.wasReleasedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyUp(key);
#else
            return false;
#endif
        }

        public static Vector2 GetMousePosition()
        {
#if NEW_INPUT
            Mouse mouse = Mouse.current;
            return mouse != null ? mouse.position.ReadValue() : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.mousePosition;
#else
            return Vector2.zero;
#endif
        }

#if NEW_INPUT
        /// <summary>
        /// KeyCode → KeyControl 的查找缓存。原来每次查询都线性遍历 Keyboard.allKeys（100+ 项），
        /// 而 ModifierKeyCombo 一次按键判定要查 8 次。
        /// </summary>
        private static readonly Dictionary<KeyCode, KeyControl> KeyControls = new Dictionary<KeyCode, KeyControl>();
        private static Keyboard _cachedKeyboard;

        private static bool TryGetKeyControl(KeyCode key, out KeyControl control)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || key == KeyCode.None)
            {
                control = null;
                return false;
            }

            // 设备重连/重初始化后 KeyControl 会换成新实例，缓存必须整体失效。
            if (!ReferenceEquals(_cachedKeyboard, keyboard))
            {
                _cachedKeyboard = keyboard;
                KeyControls.Clear();
            }

            if (KeyControls.TryGetValue(key, out control))
            {
                return control != null;
            }

            Key keyConverted = key.ToKey();
            control = keyConverted != Key.None ? keyboard[keyConverted] : null;
            KeyControls[key] = control;
            return control != null;
        }

        private static Key ToKey(this KeyCode key)
        {
            switch (key)
            {
                case KeyCode.None: return Key.None;
                case KeyCode.Space: return Key.Space;
                case KeyCode.Return: return Key.Enter;
                case KeyCode.Tab: return Key.Tab;
                case KeyCode.BackQuote: return Key.Backquote;
                case KeyCode.Quote: return Key.Quote;
                case KeyCode.Semicolon: return Key.Semicolon;
                case KeyCode.Comma: return Key.Comma;
                case KeyCode.Period: return Key.Period;
                case KeyCode.Slash: return Key.Slash;
                case KeyCode.Backslash: return Key.Backslash;
                case KeyCode.LeftBracket: return Key.LeftBracket;
                case KeyCode.RightBracket: return Key.RightBracket;
                case KeyCode.Minus: return Key.Minus;
                case KeyCode.Equals: return Key.Equals;
                case KeyCode.A: return Key.A;
                case KeyCode.B: return Key.B;
                case KeyCode.C: return Key.C;
                case KeyCode.D: return Key.D;
                case KeyCode.E: return Key.E;
                case KeyCode.F: return Key.F;
                case KeyCode.G: return Key.G;
                case KeyCode.H: return Key.H;
                case KeyCode.I: return Key.I;
                case KeyCode.J: return Key.J;
                case KeyCode.K: return Key.K;
                case KeyCode.L: return Key.L;
                case KeyCode.M: return Key.M;
                case KeyCode.N: return Key.N;
                case KeyCode.O: return Key.O;
                case KeyCode.P: return Key.P;
                case KeyCode.Q: return Key.Q;
                case KeyCode.R: return Key.R;
                case KeyCode.S: return Key.S;
                case KeyCode.T: return Key.T;
                case KeyCode.U: return Key.U;
                case KeyCode.V: return Key.V;
                case KeyCode.W: return Key.W;
                case KeyCode.X: return Key.X;
                case KeyCode.Y: return Key.Y;
                case KeyCode.Z: return Key.Z;
                case KeyCode.Alpha1: return Key.Digit1;
                case KeyCode.Alpha2: return Key.Digit2;
                case KeyCode.Alpha3: return Key.Digit3;
                case KeyCode.Alpha4: return Key.Digit4;
                case KeyCode.Alpha5: return Key.Digit5;
                case KeyCode.Alpha6: return Key.Digit6;
                case KeyCode.Alpha7: return Key.Digit7;
                case KeyCode.Alpha8: return Key.Digit8;
                case KeyCode.Alpha9: return Key.Digit9;
                case KeyCode.Alpha0: return Key.Digit0;
                case KeyCode.LeftShift: return Key.LeftShift;
                case KeyCode.RightShift: return Key.RightShift;
                case KeyCode.LeftAlt: return Key.LeftAlt;
                case KeyCode.RightAlt: return Key.RightAlt;
                case KeyCode.AltGr: return Key.AltGr;
                case KeyCode.LeftControl: return Key.LeftCtrl;
                case KeyCode.RightControl: return Key.RightCtrl;
                case KeyCode.LeftWindows: return Key.LeftWindows;
                case KeyCode.RightWindows: return Key.RightWindows;
                case KeyCode.LeftCommand: return Key.LeftCommand;
                case KeyCode.RightCommand: return Key.RightCommand;
                case KeyCode.Escape: return Key.Escape;
                case KeyCode.LeftArrow: return Key.LeftArrow;
                case KeyCode.RightArrow: return Key.RightArrow;
                case KeyCode.UpArrow: return Key.UpArrow;
                case KeyCode.DownArrow: return Key.DownArrow;
                case KeyCode.Backspace: return Key.Backspace;
                case KeyCode.PageDown: return Key.PageDown;
                case KeyCode.PageUp: return Key.PageUp;
                case KeyCode.Home: return Key.Home;
                case KeyCode.End: return Key.End;
                case KeyCode.Insert: return Key.Insert;
                case KeyCode.Delete: return Key.Delete;
                case KeyCode.CapsLock: return Key.CapsLock;
                case KeyCode.Numlock: return Key.NumLock;
                case KeyCode.Print: return Key.PrintScreen;
                case KeyCode.ScrollLock: return Key.ScrollLock;
                case KeyCode.Pause: return Key.Pause;
                case KeyCode.KeypadEnter: return Key.NumpadEnter;
                case KeyCode.KeypadDivide: return Key.NumpadDivide;
                case KeyCode.KeypadMultiply: return Key.NumpadMultiply;
                case KeyCode.KeypadPlus: return Key.NumpadPlus;
                case KeyCode.KeypadMinus: return Key.NumpadMinus;
                case KeyCode.KeypadPeriod: return Key.NumpadPeriod;
                case KeyCode.KeypadEquals: return Key.NumpadEquals;
                case KeyCode.Keypad0: return Key.Numpad0;
                case KeyCode.Keypad1: return Key.Numpad1;
                case KeyCode.Keypad2: return Key.Numpad2;
                case KeyCode.Keypad3: return Key.Numpad3;
                case KeyCode.Keypad4: return Key.Numpad4;
                case KeyCode.Keypad5: return Key.Numpad5;
                case KeyCode.Keypad6: return Key.Numpad6;
                case KeyCode.Keypad7: return Key.Numpad7;
                case KeyCode.Keypad8: return Key.Numpad8;
                case KeyCode.Keypad9: return Key.Numpad9;
                case KeyCode.F1: return Key.F1;
                case KeyCode.F2: return Key.F2;
                case KeyCode.F3: return Key.F3;
                case KeyCode.F4: return Key.F4;
                case KeyCode.F5: return Key.F5;
                case KeyCode.F6: return Key.F6;
                case KeyCode.F7: return Key.F7;
                case KeyCode.F8: return Key.F8;
                case KeyCode.F9: return Key.F9;
                case KeyCode.F10: return Key.F10;
                case KeyCode.F11: return Key.F11;
                case KeyCode.F12: return Key.F12;
                default: return Key.None;
            }
        }
#endif
    }
}
