using AlicizaX.Console.Utilities;
using Cysharp.Text;
using System;

namespace AlicizaX.Console.Parsers
{
    public class EnumParser : PolymorphicCachedAlicizaXConsoleParser<Enum>
    {
        public override Enum Parse(string value, Type type)
        {
            try
            {
                return (Enum)Enum.Parse(type, value);
            }
            catch (Exception e)
            {
                throw new ParserInputException(ZString.Format("Cannot parse '{0}' to the type '{1}'. To see the supported values, use the command `enum-info {2}`", value, type.GetDisplayName(), type), e);
            }
        }
    }
}
