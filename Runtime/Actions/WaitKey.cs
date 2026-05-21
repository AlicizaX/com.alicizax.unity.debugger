using UnityEngine;

namespace AlicizaX.Console.Actions
{
    /// <summary>
    /// 等待到指定按键按下。
    /// </summary>
    public class WaitKey : WaitUntil
    {
        /// <param name="key">要等待的按键。</param>
        public WaitKey(KeyCode key) : base(() => InputHelper.GetKeyDown(key))
        {

        }
    }
}