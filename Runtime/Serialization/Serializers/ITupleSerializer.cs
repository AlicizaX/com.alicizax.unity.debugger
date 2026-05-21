#if !NET_STANDARD_2_0

using System.Runtime.CompilerServices;

namespace AlicizaX.Console.Serializers
{
    public class ITupleSerializer : PolymorphicAlicizaXConsoleSerializer<ITuple>
    {
        public override string SerializeFormatted(ITuple value, AlicizaXConsoleTheme theme)
        {
            string[] serializedItems = new string[value.Length];
            for (int i = 0; i < value.Length; i++)
            {
                serializedItems[i] = SerializeRecursive(value[i], theme);
            }

            return $"({string.Join(", ", serializedItems)})";
        }
    }
}
#endif
