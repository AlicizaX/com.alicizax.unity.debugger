using System.Collections;

namespace AlicizaX.Console.Serializers
{
    public class DictionaryEntrySerializer : BasicAlicizaXConsoleSerializer<DictionaryEntry>
    {
        public override string SerializeFormatted(DictionaryEntry value, AlicizaXConsoleTheme theme)
        {
            string innerKey = SerializeRecursive(value.Key, theme);
            string innerValue = SerializeRecursive(value.Value, theme);

            return $"{innerKey}: {innerValue}";
        }
    }
}
