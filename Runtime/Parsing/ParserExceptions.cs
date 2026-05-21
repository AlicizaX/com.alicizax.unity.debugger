using System;

namespace AlicizaX.Console
{
    public class ParserException : Exception
    {
        public ParserException(string message) : base(message) { }
        public ParserException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class ParserInputException : ParserException
    {
        public ParserInputException(string message) : base(message) { }
        public ParserInputException(string message, Exception innerException) : base(message, innerException) { }
    }
}
