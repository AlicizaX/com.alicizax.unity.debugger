using Cysharp.Text;
using System;
using System.Collections.Generic;

namespace AlicizaX.Console.Parsers
{
    public class TupleParser : MassGenericAlicizaXConsoleParser
    {
        private const int MaxFlatTupleSize = 8;

        protected override HashSet<Type> GenericTypes { get; } = new HashSet<Type>
        {
            typeof(ValueTuple<>),
            typeof(ValueTuple<,>),
            typeof(ValueTuple<,,>),
            typeof(ValueTuple<,,,>),
            typeof(ValueTuple<,,,,>),
            typeof(ValueTuple<,,,,,>),
            typeof(ValueTuple<,,,,,,>),
            typeof(ValueTuple<,,,,,,,>),
            typeof(Tuple<>),
            typeof(Tuple<,>),
            typeof(Tuple<,,>),
            typeof(Tuple<,,,>),
            typeof(Tuple<,,,,>),
            typeof(Tuple<,,,,,>),
            typeof(Tuple<,,,,,,>),
            typeof(Tuple<,,,,,,,>)
        };

        public override object Parse(string value, Type type)
        {
            TextProcessing.ScopedSplitOptions options = TextProcessing.ScopedSplitOptions.Default;
            options.MaxCount = MaxFlatTupleSize;

            string[] inputParts = value.ReduceScope('(', ')').SplitScoped(',', options);
            Type[] elementTypes = type.GetGenericArguments();

            if (elementTypes.Length != inputParts.Length)
            {
                throw new ParserInputException(ZString.Format("Desired tuple type {0} has {1} elements but input contained {2}.", type, elementTypes.Length, inputParts.Length));
            }

            object[] tupleParts = new object[inputParts.Length];
            for (int i = 0; i < tupleParts.Length; i++)
            {
                tupleParts[i] = ParseRecursive(inputParts[i], elementTypes[i]);
            }

            return Activator.CreateInstance(type, tupleParts);
        }
    }
}
