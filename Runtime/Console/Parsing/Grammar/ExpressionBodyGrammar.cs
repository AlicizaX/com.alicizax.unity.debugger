using AlicizaX.Console.Utilities;
using Cysharp.Text;
using System;
using System.Text.RegularExpressions;

namespace AlicizaX.Console.Grammar
{
    public class ExpressionBodyGrammar : IAlicizaXConsoleGrammarConstruct
    {
        private readonly Regex _expressionBodyRegex = new Regex(@"^{.+}\??$");

        public int Precedence => 0;

        public bool Match(string value, Type type)
        {
            return _expressionBodyRegex.IsMatch(value);
        }

        public object Parse(string value, Type type, Func<string, Type, object> recursiveParser)
        {
            bool nullable = false;
            if (value.EndsWith("?"))
            {
                nullable = true;
                value = value.Substring(0, value.Length - 1);
            }

            value = value.ReduceScope('{', '}');
            object result = AlicizaXConsoleProcessor.InvokeCommand(value);

            if (result is null)
            {
                if (nullable)
                {
                    if (type.IsClass)
                    {
                        return result;
                    }
                    else
                    {
                        throw new ParserInputException(ZString.Format("Expression body {{{0}}} evaluated to null which is incompatible with the expected type '{1}'.", value, type.GetDisplayName()));
                    }
                }
                else
                {
                    throw new ParserInputException(ZString.Format("Expression body {{{0}}} evaluated to null. If this is intended, please use nullable expression bodies, {{expr}}?", value));
                }
            }
            else if (result.GetType().IsCastableTo(type, true))
            {
                return type.Cast(result);
            }
            else
            {
                throw new ParserInputException(ZString.Format("Expression body {{{0}}} evaluated to an object of type '{1}', which is incompatible with the expected type '{2}'.",
                    value,
                    result.GetType().GetDisplayName(),
                    type.GetDisplayName()));
            }
        }
    }
}
