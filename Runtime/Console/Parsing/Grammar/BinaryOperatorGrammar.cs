using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using AlicizaX.Console.Utilities;
using Cysharp.Text;

namespace AlicizaX.Console.Grammar
{
    internal abstract class BinaryOperator
    {
        public abstract Type LArg { get; }
        public abstract Type RArg { get; }
        public abstract Type Ret { get; }

        public abstract object Invoke(object left, object right);
    }

    internal class BinaryOperatorData : BinaryOperator
    {
        public override Type LArg { get; }
        public override Type RArg { get; }
        public override Type Ret { get; }

        private readonly MethodInfo _method;

        public BinaryOperatorData(MethodInfo OperatorMethod)
        {
            _method = OperatorMethod;
            Ret = OperatorMethod.ReturnType;

            ParameterInfo[] paramData = _method.GetParameters();
            if (paramData.Length != 2)
            {
                throw new ArgumentException(ZString.Format("Cannot create a binary operator from a method with {0} parameters", paramData.Length));
            }

            LArg = paramData[0].ParameterType;
            RArg = paramData[1].ParameterType;
        }

        public override object Invoke(object left, object right)
        {
            return _method.Invoke(null, new[] { left, right });
        }
    }

    internal class DynamicBinaryOperator : BinaryOperator
    {
        public override Type LArg { get; }
        public override Type RArg { get; }
        public override Type Ret { get; }

        private readonly Delegate _del;

        public DynamicBinaryOperator(Delegate del, Type lArg, Type rArg, Type ret)
        {
            _del = del;
            LArg = lArg;
            RArg = rArg;
            Ret = ret;
        }

        public override object Invoke(object left, object right)
        {
            return _del.DynamicInvoke(left, right);
        }
    }

    public abstract class BinaryOperatorGrammar : IAlicizaXConsoleGrammarConstruct
    {
        public abstract int Precedence { get; }
        protected abstract char OperatorToken { get; }
        protected abstract string OperatorMethodName { get; }
        protected abstract Func<Expression, Expression, BinaryExpression> PrimitiveExpressionGenerator { get; }

        private Regex _operatorRegex;

        private readonly HashSet<Type> _missingOperatorTable = new HashSet<Type>();
        private readonly Dictionary<Type, BinaryOperator> _foundOperatorTable = new Dictionary<Type, BinaryOperator>();

        public bool Match(string value, Type type)
        {
            if (_missingOperatorTable.Contains(type))
            {
                return false;
            }

            if (!IsSyntaxMatch(value))
            {
                return false;
            }

            if (_foundOperatorTable.ContainsKey(type))
            {
                return true;
            }

            BinaryOperator operatorData = GetOperatorData(type);

            if (operatorData != null)
            {
                _foundOperatorTable.Add(type, operatorData);
                return true;
            }

            _missingOperatorTable.Add(type);
            return false;
        }

        private bool IsSyntaxMatch(string value)
        {
            if (_operatorRegex == null)
            {
                _operatorRegex = new Regex(ZString.Format(@"^.+\{0}.+$", OperatorToken));
            }

            if (!_operatorRegex.IsMatch(value))
            {
                return false;
            }

            int operatorPos = GetOperatorPosition(value);
            return operatorPos > 0 && operatorPos < value.Length;
        }

        private BinaryOperator GetOperatorData(Type type)
        {
            if (type.IsPrimitive)
            {
#if !UNITY_EDITOR && ENABLE_IL2CPP && !UNITY_2022_2_OR_NEWER
                string typeName = AlicizaX.Console.Utilities.ReflectionExtensions.GetDisplayName(type);
                UnityEngine.Debug.LogWarning(ZString.Format("{0} {1} {0} is not supported as IL2CPP does not support dynamic value typed generics before Unity 2022.2", typeName, OperatorToken));
#else
                return GeneratePrimitiveOperator(type);
#endif

            }

            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
            BinaryOperatorData[] candidates = methods.Where(x => x.Name == OperatorMethodName)
                                                     .Where(x => x.ReturnType == type)
                                                     .Where(x => x.GetParameters().Length == 2)
                                                     .Select(x => new BinaryOperatorData(x))
                                                     .ToArray();

            BinaryOperatorData idealCandidate = candidates.FirstOrDefault(x => x.LArg == type && x.RArg == type)
                                             ?? candidates.FirstOrDefault(x => x.LArg == type)
                                             ?? candidates.FirstOrDefault(x => x.RArg == type)
                                             ?? candidates.FirstOrDefault();

            return idealCandidate;
        }

        private BinaryOperator GeneratePrimitiveOperator(Type type)
        {
            ParameterExpression leftParam = Expression.Parameter(type, "left");
            ParameterExpression rightParam = Expression.Parameter(type, "right");
            BinaryExpression body;

            try
            {
                body = PrimitiveExpressionGenerator(leftParam, rightParam);
            }
            catch (InvalidOperationException)
            {
                return null;
            }

            Delegate expr = Expression.Lambda(body, true, leftParam, rightParam).Compile();

            return new DynamicBinaryOperator(expr, type, type, type);
        }

        /// <summary>
        /// 获取最右侧有效操作符的位置。
        /// </summary>
        /// <param name="value">要从中查找操作符的字符串。</param>
        /// <returns>操作符的位置；找不到时返回 -1。</returns>
        protected virtual int GetOperatorPosition(string value)
        {
            return TextProcessing.GetScopedSplitPoints(value, OperatorToken, TextProcessing.DefaultLeftScopers, TextProcessing.DefaultRightScopers).LastOr(-1);
        }

        public object Parse(string value, Type type, Func<string, Type, object> recursiveParser)
        {
            BinaryOperator operatorData = _foundOperatorTable[type];

            int splitIndex = GetOperatorPosition(value);
            string left = value.Substring(0, splitIndex);
            string right = value.Substring(splitIndex + 1);

            object leftVal = recursiveParser(left, operatorData.LArg);
            object rightVal = recursiveParser(right, operatorData.RArg);

            try
            {
                return operatorData.Invoke(leftVal, rightVal);
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException ?? e;
            }
        }
    }
}
