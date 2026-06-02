using System;

namespace AlicizaX.Console.Parsers
{
    public class TypeParser : BasicCachedAlicizaXConsoleParser<Type>
    {
        public override Type Parse(string value)
        {
            return AlicizaXConsoleParser.ParseType(value);
        }
    }
}
