using System;
using System.Linq;
using UnityEngine;

namespace AlicizaX.Console.Actions
{
    /// <summary>
    /// 等待任意按键按下，并通过给定委托返回按键。
    /// </summary>
    public class GetKey : ICommandAction
    {
        private KeyCode _key;
        private readonly Action<KeyCode> _onKey;
        private static readonly KeyCode[] KeyCodes = Enum.GetValues(typeof(KeyCode))
            .Cast<KeyCode>()
            .Where(k => (int)k < (int)KeyCode.Mouse0)
            .ToArray();

        public bool IsFinished
        {
            get
            {
                _key = GetCurrentKeyDown();
                return _key != KeyCode.None;
            }
        }

        public bool StartsIdle => true;

        /// <param name="onKey">按下按键时要执行的动作。</param>
        public GetKey(Action<KeyCode> onKey)
        {
            _onKey = onKey;
        }

        private KeyCode GetCurrentKeyDown()
        {
            return KeyCodes.FirstOrDefault(InputHelper.GetKeyDown);
        }

        public void Start(ActionContext context) { }

        public void Finalize(ActionContext context)
        {
            _onKey(_key);
        }
    }
}