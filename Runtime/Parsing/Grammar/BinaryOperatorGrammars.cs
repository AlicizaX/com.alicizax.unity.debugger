using System;
using System.Linq.Expressions;

namespace AlicizaX.Console.Grammar
{
    public class AdditionOperatorGrammar : BinaryAndUnaryOperatorGrammar
    {
        public override int Precedence => 0;

        protected override char OperatorToken => '+';
        protected override string OperatorMethodName => "op_Addition";

        protected override Func<Expression, Expression, BinaryExpression> PrimitiveExpressionGenerator => Expression.Add;
    }

    public class SubtractionOperatorGrammar : BinaryAndUnaryOperatorGrammar
    {
        public override int Precedence => 1;

        protected override char OperatorToken => '-';
        protected override string OperatorMethodName => "op_Subtraction";

        protected override Func<Expression, Expression, BinaryExpression> PrimitiveExpressionGenerator => Expression.Subtract;
    }

    public class MultiplyOperatorGrammar : BinaryOperatorGrammar
    {
        public override int Precedence => 2;

        protected override char OperatorToken => '*';
        protected override string OperatorMethodName => "op_Multiply";

        protected override Func<Expression, Expression, BinaryExpression> PrimitiveExpressionGenerator => Expression.Multiply;
    }

    public class DivisionOperatorGrammar : BinaryOperatorGrammar
    {
        public override int Precedence => 3;

        protected override char OperatorToken => '/';
        protected override string OperatorMethodName => "op_Division";

        protected override Func<Expression, Expression, BinaryExpression> PrimitiveExpressionGenerator => Expression.Divide;
    }

    public class ModulusOperatorGrammar : BinaryOperatorGrammar
    {
        public override int Precedence => 4;

        protected override char OperatorToken => '%';
        protected override string OperatorMethodName => "op_Modulus";

        protected override Func<Expression, Expression, BinaryExpression> PrimitiveExpressionGenerator => Expression.Modulo;
    }

    public class BitwiseOrOperatorGrammar : BinaryOperatorGrammar
    {
        public override int Precedence => 5;

        protected override char OperatorToken => '|';
        protected override string OperatorMethodName => "op_bitwiseOr";

        protected override Func<Expression, Expression, BinaryExpression> PrimitiveExpressionGenerator => Expression.Or;
    }

    public class BitwiseAndOperatorGrammar : BinaryOperatorGrammar
    {
        public override int Precedence => 6;

        protected override char OperatorToken => '&';
        protected override string OperatorMethodName => "op_bitwiseAnd";

        protected override Func<Expression, Expression, BinaryExpression> PrimitiveExpressionGenerator => Expression.And;
    }

    public class ExclusiveOrOperatorGrammar : BinaryOperatorGrammar
    {
        public override int Precedence => 7;

        protected override char OperatorToken => '^';
        protected override string OperatorMethodName => "op_ExclusiveOr";

        protected override Func<Expression, Expression, BinaryExpression> PrimitiveExpressionGenerator => Expression.ExclusiveOr;
    }
}
