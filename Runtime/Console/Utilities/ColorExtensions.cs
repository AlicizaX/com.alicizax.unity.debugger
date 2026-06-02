using Cysharp.Text;
using System.Collections.Concurrent;
using UnityEngine;

namespace AlicizaX.Console.Utilities
{
    public static class ColorExtensions
    {
        /// <summary>用富文本给字符串上色。</summary>
        /// <returns>格式化后的文本。</returns>
        /// <param name="text">要上色的文本。</param>
        /// <param name="color">要添加到文本上的颜色。</param>
        public static string ColorText(this string text, Color color)
        {
            Utf16ValueStringBuilder buffer = ZString.CreateStringBuilder();
            buffer.AppendColoredText(text, color);
            return buffer.ToStringAndDispose();
        }

        /// <summary>用富文本给字符串上色，并把结果写入 StringBuilder。</summary>
        /// <returns>格式化后的文本。</returns>
        /// <param name="stringBuilder">用于写入结果的 StringBuilder。</param>
        /// <param name="text">要上色的文本。</param>
        /// <param name="color">要添加到文本上的颜色。</param>
        public static void AppendColoredText(this ref Utf16ValueStringBuilder stringBuilder, string text, Color color)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                stringBuilder.Append(text);
                return;
            }

            stringBuilder.Append("<#");
            stringBuilder.Append(Color32ToStringNonAlloc(color));
            stringBuilder.Append('>');
            stringBuilder.Append(text);
            stringBuilder.Append("</color>");
        }

        private static readonly ConcurrentDictionary<int, string> _colorLookupTable = new ConcurrentDictionary<int, string>();
        public static unsafe string Color32ToStringNonAlloc(Color32 color)
        {
            int colorKey = color.r << 24 | color.g << 16 | color.b << 8 | color.a;
            if (_colorLookupTable.ContainsKey(colorKey))
            {
                return _colorLookupTable[colorKey];
            }

            char* buffer = stackalloc char[8];
            Color32ToHexNonAlloc(color, buffer);

            int bufferLength = color.a < 0xFF ? 8 : 6;
            string colorText = new string(buffer, 0, bufferLength);

            _colorLookupTable[colorKey] = colorText;
            return colorText;
        }

        private static unsafe void Color32ToHexNonAlloc(Color32 color, char* buffer)
        {
            ByteToHex(color.r, out buffer[0], out buffer[1]);
            ByteToHex(color.g, out buffer[2], out buffer[3]);
            ByteToHex(color.b, out buffer[4], out buffer[5]);
            ByteToHex(color.a, out buffer[6], out buffer[7]);
        }

        private static void ByteToHex(byte value, out char dig1, out char dig2)
        {
            dig1 = NibbleToHex((byte)(value >> 4));
            dig2 = NibbleToHex((byte)(value & 0xF));
        }

        private static char NibbleToHex(byte nibble)
        {
            if (nibble < 10) { return (char)('0' + nibble); }
            else { return (char)('A' + nibble - 10); }
        }
    }
}
