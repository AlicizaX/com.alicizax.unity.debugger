using AlicizaX.Console.Utilities;
using System;
using UnityEngine;

namespace AlicizaX.Console
{
    // 说明见当前实现。
    public class AlicizaXConsoleTheme : ScriptableObject
    {
        private static readonly Color DefaultReturnColor = Color.white;

        public string ColorizeReturn(string data, Type type)
        {
            return data.ColorText(DefaultReturnColor);
        }

        public void GetCollectionFormatting(Type type, out string leftScoper, out string seperator, out string rightScoper)
        {
            leftScoper = "[";
            seperator = ",";
            rightScoper = "]";
        }
    }
}
