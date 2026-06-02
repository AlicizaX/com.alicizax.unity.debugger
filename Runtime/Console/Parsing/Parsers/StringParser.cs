namespace AlicizaX.Console.Parsers
{
    public class StringParser : BasicCachedAlicizaXConsoleParser<string>
    {
        public override int Priority => int.MaxValue;

        public override string Parse(string value)
        {
            return value
                .ReduceScope('"', '"')
                .UnescapeText('"');
        }
    }
}
