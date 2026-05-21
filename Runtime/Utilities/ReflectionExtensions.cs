using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AlicizaX.Console.Utilities
{
    public static class ReflectionExtensions
    {
        #region Lookup Tables
        private static readonly Dictionary<Type, string> _typeDisplayNames = new Dictionary<Type, string>
        {
            { typeof(int), "int" },
            { typeof(float), "float" },
            { typeof(decimal), "decimal" },
            { typeof(double), "double" },
            { typeof(string), "string" },
            { typeof(bool), "bool" },
            { typeof(byte), "byte" },
            { typeof(sbyte), "sbyte" },
            { typeof(uint), "uint" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(long), "decimal" },
            { typeof(ulong), "ulong" },
            { typeof(char), "char" },
            { typeof(object), "object" }
        };

        private static readonly Type[] _valueTupleTypes =
        {
            typeof(ValueTuple<>),
            typeof(ValueTuple<,>),
            typeof(ValueTuple<,,>),
            typeof(ValueTuple<,,,>),
            typeof(ValueTuple<,,,,>),
            typeof(ValueTuple<,,,,,>),
            typeof(ValueTuple<,,,,,,>),
            typeof(ValueTuple<,,,,,,,>)
        };

        private static readonly Type[][] _primitiveTypeCastHierarchy =
        {
            new[] { typeof(byte),  typeof(sbyte), typeof(char) },
            new[] { typeof(short), typeof(ushort) },
            new[] { typeof(int), typeof(uint) },
            new[] { typeof(long), typeof(ulong) },
            new[] { typeof(float) },
            new[] { typeof(double) }
        };
        #endregion

        /// <summary>判断类型是不是委托。</summary>
        /// <returns>类型是否是委托。</returns>
        public static bool IsDelegate(this Type type)
        {
            if (!typeof(Delegate).IsAssignableFrom(type)) { return false; }
            return true;
        }

        /// <summary>判断类型是不是强类型委托。</summary>
        /// <returns>类型是否是强类型委托。</returns>
        public static bool IsStrongDelegate(this Type type)
        {
            if (!type.IsDelegate()) { return false; }
            if (type.IsAbstract) { return false; }
            return true;
        }

        /// <summary>判断字段是不是委托。</summary>
        /// <returns>字段是否是委托。</returns>
        public static bool IsDelegate(this FieldInfo fieldInfo)
        {
            return fieldInfo.FieldType.IsDelegate();
        }

        /// <summary>判断字段是不是强类型委托。</summary>
        /// <param name="fieldInfo">要查询的字段。</param>
        /// <returns>字段是否是强类型委托。</returns>
        public static bool IsStrongDelegate(this FieldInfo fieldInfo)
        {
            return fieldInfo.FieldType.IsStrongDelegate();
        }

        /// <summary>
        /// 判断类型是否是给定非泛型类型的泛型构造。
        /// </summary>
        /// <param name="nonGenericType">用于判断的非泛型类型。</param>
        /// <returns>类型是否是该非泛型类型的泛型构造。</returns>
        public static bool IsGenericTypeOf(this Type genericType, Type nonGenericType)
        {
            if (!genericType.IsGenericType) { return false; }
            return genericType.GetGenericTypeDefinition() == nonGenericType;
        }

        /// <summary>
        /// 判断类型是否派生自给定基类。
        /// </summary>
        /// <param name="baseType">用于判断的基类。</param>
        /// <returns>类型是否派生自基类。</returns>
        public static bool IsDerivedTypeOf(this Type type, Type baseType)
        {
            return baseType.IsAssignableFrom(type);
        }

        /// <summary>
        /// 判断给定类型的对象能否转换成指定类型。
        /// </summary>
        /// <param name="to">转换目标类型。</param>
        /// <param name="implicitly">是否只考虑隐式转换。</param>
        /// <returns>是否可以执行转换。</returns>
        public static bool IsCastableTo(this Type from, Type to, bool implicitly = false)
        {
            return to.IsAssignableFrom(from) || from.HasCastDefined(to, implicitly);
        }

        private static bool HasCastDefined(this Type from, Type to, bool implicitly)
        {
            if ((from.IsPrimitive || from.IsEnum) && (to.IsPrimitive || to.IsEnum))
            {
                if (!implicitly)
                {
                    return from == to || (from != typeof(bool) && to != typeof(bool));
                }

                IEnumerable<Type> lowerTypes = Enumerable.Empty<Type>();
                foreach (Type[] types in _primitiveTypeCastHierarchy)
                {
                    if (types.Any(t => t == to))
                    {
                        return lowerTypes.Any(t => t == from);
                    }

                    lowerTypes = lowerTypes.Concat(types);
                }

                return false;   // IntPtr, UIntPtr, Enum, Boolean
            }

            return IsCastDefined(to, m => m.GetParameters()[0].ParameterType, _ => from, implicitly, false)
                || IsCastDefined(from, _ => to, m => m.ReturnType, implicitly, true);
        }

        private static bool IsCastDefined(Type type, Func<MethodInfo, Type> baseType, Func<MethodInfo, Type> derivedType, bool implicitly, bool lookInBase)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.Static | (lookInBase ? BindingFlags.FlattenHierarchy : BindingFlags.DeclaredOnly);
            MethodInfo[] methods = type.GetMethods(flags);

            return methods.Where(m => m.Name == "op_Implicit" || (!implicitly && m.Name == "op_Explicit"))
                          .Any(m => baseType(m).IsAssignableFrom(derivedType(m)));
        }

        /// <summary>
        /// 把对象动态转换成指定类型。
        /// </summary>
        /// <param name="type">转换目标类型。</param>
        /// <param name="data">要转换的对象。</param>
        /// <returns>动态转换后的对象。</returns>
        public static object Cast(this Type type, object data)
        {
            if (type.IsInstanceOfType(data))
            {
                return data;
            }

            try
            {
                return Convert.ChangeType(data, type);
            }
            catch (InvalidCastException)
            {
                Type srcType = data.GetType();
                ParameterExpression dataParam = Expression.Parameter(srcType, "data");
                Expression body = Expression.Convert(Expression.Convert(dataParam, srcType), type);

                Delegate run = Expression.Lambda(body, dataParam).Compile();
                return run.DynamicInvoke(data);
            }
        }

        /// <summary>判断给定方法是否是重写方法。</summary>
        /// <returns>方法是否是重写方法。</returns>
        public static bool IsOverride(this MethodInfo methodInfo)
        {
            return methodInfo.GetBaseDefinition().DeclaringType != methodInfo.DeclaringType;
        }

        /// <summary>
        /// 检查提供者是否带有指定特性。
        /// </summary>
        /// <typeparam name="T">要检查的特性。</typeparam>
        /// <param name="provider">特性提供者。</param>
        /// <param name="searchInherited">是否搜索基类声明。</param>
        /// <returns>特性是否存在。</returns>
        public static bool HasAttribute<T>(this ICustomAttributeProvider provider, bool searchInherited = true) where T : Attribute
        {
            try
            {
                return provider.IsDefined(typeof(T), searchInherited);
            }
            catch (MissingMethodException)
            {
                return false;
            }
        }

        /// <summary>
        /// 获取类型的格式化显示名。
        /// </summary>
        /// <param name="type">要生成显示名的类型。</param>
        /// <param name="includeNamespace">生成类型名时是否包含命名空间。</param>
        /// <returns>生成的显示名。</returns>
        public static string GetDisplayName(this Type type, bool includeNamespace = false)
        {
            if (type.IsGenericParameter)
            {
                return type.Name;
            }

            if (type.IsArray)
            {
                int rank = type.GetArrayRank();
                string innerTypeName = GetDisplayName(type.GetElementType(), includeNamespace);
                return $"{innerTypeName}[{new string(',', rank - 1)}]";
            }

            if (_typeDisplayNames.ContainsKey(type))
            {
                string baseName = _typeDisplayNames[type];
                if (type.IsGenericType && !type.IsConstructedGenericType)
                {
                    Type[] genericArgs = type.GetGenericArguments();
                    return $"{baseName}<{new string(',', genericArgs.Length - 1)}>";
                }

                return baseName;
            }

            if (type.IsGenericTypeOf(typeof(Nullable<>)))
            {
                Type innerType = type.GetGenericArguments()[0];
                return $"{innerType.GetDisplayName()}?";
            }

            if (type.IsGenericType)
            {
                Type baseType = type.GetGenericTypeDefinition();
                Type[] genericArgs = type.GetGenericArguments();

                if (_valueTupleTypes.Contains(baseType))
                {
                    return GetTupleDisplayName(type, includeNamespace);
                }

                if (type.IsConstructedGenericType)
                {
                    string[] genericNames = new string[genericArgs.Length];
                    for (int i = 0; i < genericArgs.Length; i++)
                    {
                        genericNames[i] = GetDisplayName(genericArgs[i], includeNamespace);
                    }

                    string baseName = GetDisplayName(baseType, includeNamespace).Split('<')[0];
                    return $"{baseName}<{string.Join(", ", genericNames)}>";
                }

                string typeName = includeNamespace
                    ? type.FullName
                    : type.Name;

                return $"{typeName.Split('`')[0]}<{new string(',', genericArgs.Length - 1)}>";
            }

            Type declaringType = type.DeclaringType;
            if (declaringType != null)
            {
                string declaringName = GetDisplayName(declaringType, includeNamespace);
                return $"{declaringName}.{type.Name}";
            }

            return includeNamespace
                ? type.FullName
                : type.Name;
        }

        private static string GetTupleDisplayName(this Type type, bool includeNamespace = false)
        {
            IEnumerable<string> parts = type
                .GetGenericArguments()
                .Select(x => x.GetDisplayName(includeNamespace));

            return $"({string.Join(", ", parts)})";
        }

        /// <summary>
        /// 判断不同类型里的两个方法是否有相同签名。
        /// </summary>
        /// <param name="a">第一个方法。</param>
        /// <param name="b">第二个方法。</param>
        /// <returns>两者相等时返回 <c>true</c>。</returns>
        public static bool AreMethodsEqual(MethodInfo a, MethodInfo b)
        {
            if (a.Name != b.Name) return false;

            ParameterInfo[] paramsA = a.GetParameters();
            ParameterInfo[] paramsB = b.GetParameters();

            if (paramsA.Length != paramsB.Length) return false;
            for (int i = 0; i < paramsA.Length; i++)
            {
                ParameterInfo pa = paramsA[i];
                ParameterInfo pb = paramsB[i];

                if (pa.Name != pb.Name) return false;
                if (pa.HasDefaultValue != pb.HasDefaultValue) return false;

                Type ta = pa.ParameterType;
                Type tb = pb.ParameterType;

                if (!ta.ContainsGenericParameters && !tb.ContainsGenericParameters)
                {
                    if (ta != tb) return false;
                }
            }

            if (a.IsGenericMethod != b.IsGenericMethod) return false;
            if (a.IsGenericMethod && b.IsGenericMethod)
            {
                Type[] genericA = a.GetGenericArguments();
                Type[] genericB = b.GetGenericArguments();

                if (genericA.Length != genericB.Length) return false;
                for (int i = 0; i < genericA.Length; i++)
                {
                    Type ga = genericA[i];
                    Type gb = genericB[i];

                    if (ga.Name != gb.Name) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 通过查找相同签名的方法，把方法重新绑定到新类型上。
        /// </summary>
        /// <param name="method">要重新绑定的方法。</param>
        /// <param name="newBase">方法要重新绑定到的新类型。</param>
        /// <returns>重新绑定后的方法。</returns>
        public static MethodInfo RebaseMethod(this MethodInfo method, Type newBase)
        {
            BindingFlags flags = BindingFlags.Default;

            flags |= method.IsStatic
                ? BindingFlags.Static
                : BindingFlags.Instance;

            flags |= method.IsPublic
                ? BindingFlags.Public
                : BindingFlags.NonPublic;

            MethodInfo[] candidates = newBase.GetMethods(flags)
                .Where(x => AreMethodsEqual(x, method))
                .ToArray();

            if (candidates.Length == 0)
            {
                throw new ArgumentException($"Could not rebase method {method} onto type {newBase} as no matching candidates were found");
            }

            if (candidates.Length > 1)
            {
                throw new ArgumentException($"Could not rebase method {method} onto type {newBase} as too many matching candidates were found");
            }

            return candidates[0];
        }
    }
}