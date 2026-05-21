using AlicizaX.Console.Utilities;
using System;

namespace AlicizaX.Console.Serializers
{
    public class TypeSerialiazer : PolymorphicAlicizaXConsoleSerializer<Type>
    {
        public override string SerializeFormatted(Type value, AlicizaXConsoleTheme theme)
        {
            return value.GetDisplayName();
        }
    }
}
