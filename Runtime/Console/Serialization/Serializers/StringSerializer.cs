namespace AlicizaX.Console.Serializers
{
    public class StringSerializer : BasicAlicizaXConsoleSerializer<string>
    {
        public override int Priority => int.MaxValue;

        public override string SerializeFormatted(string value, AlicizaXConsoleTheme theme)
        {
            return value;
        }
    }
}
