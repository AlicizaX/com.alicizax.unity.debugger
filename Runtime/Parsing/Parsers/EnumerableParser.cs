using System;
using System.Collections.Generic;

namespace AlicizaX.Console.Parsers
{
    public class EnumerableParser : MassGenericAlicizaXConsoleParser
    {
        protected override HashSet<Type> GenericTypes { get; } = new HashSet<Type>()
        {
            typeof(IEnumerable<>),
            typeof(ICollection<>),
            typeof(IReadOnlyCollection<>),
            typeof(IList<>),
            typeof(IReadOnlyList<>)
        };

        public override object Parse(string value, Type type)
        {
            Type arrayType = type.GetGenericArguments()[0].MakeArrayType();
            return ParseRecursive(value, arrayType);
        }
    }
}
