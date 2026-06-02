using AlicizaX.Console.Comparators;
using AlicizaX.Console.Utilities;
using Cysharp.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AlicizaX.Console
{
    public static class InvocationTargetFactory
    {
        private static readonly Dictionary<(MonoTargetType, Type), object> TargetCache = new Dictionary<(MonoTargetType, Type), object>();

        public static IEnumerable<T> FindTargets<T>(MonoTargetType method) where T : MonoBehaviour
        {
            foreach (object target in FindTargets(typeof(T), method))
            {
                yield return target as T;
            }
        }

        public static IEnumerable<object> FindTargets(Type classType, MonoTargetType method)
        {
            switch (method)
            {
                case MonoTargetType.Single:
                {
#if UNITY_6000_0_OR_NEWER
                    Object target = Object.FindAnyObjectByType(classType);
#else
                    Object target = Object.FindObjectOfType(classType);
#endif
                    return target == null ? Enumerable.Empty<object>() : target.Yield();
                }
                case MonoTargetType.SingleInactive:
                {
                    return WrapSingleCached(classType, method, type =>
                    {
                        return Resources.FindObjectsOfTypeAll(type)
                            .FirstOrDefault(x => !x.hideFlags.HasFlag(HideFlags.HideInHierarchy));
                    });
                }
                case MonoTargetType.All:
                {
#if UNITY_6000_0_OR_NEWER
                    return Object.FindObjectsByType(classType)
                        .OrderBy(x => x.name, new AlphanumComparator());
#else
                    return Object.FindObjectsOfType(classType)
                        .OrderBy(x => x.name, new AlphanumComparator());
#endif
                }
                case MonoTargetType.AllInactive:
                {
                    return Resources.FindObjectsOfTypeAll(classType)
                        .Where(x => !x.hideFlags.HasFlag(HideFlags.HideInHierarchy))
                        .OrderBy(x => x.name, new AlphanumComparator());
                }
                case MonoTargetType.Registry:
                {
                    return AlicizaXConsoleRegistry.GetRegistryContents(classType);
                }
                case MonoTargetType.Singleton:
                {
                    return GetSingletonInstance(classType).Yield();
                }
                default:
                {
                    throw new ArgumentException(ZString.Format("Unsupported MonoTargetType {0}", method));
                }
            }
        }

        private static IEnumerable<object> WrapSingleCached(Type classType, MonoTargetType method, Func<Type, object> targetFinder)
        {
            if (!TargetCache.TryGetValue((method, classType), out object target) || target as Object == null)
            {
                target = targetFinder(classType);
                TargetCache[(method, classType)] = target;
            }

            return target == null ? Enumerable.Empty<object>() : target.Yield();
        }

        public static object InvokeOnTargets(MethodInfo invokingMethod, IEnumerable<object> targets, object[] arguments)
        {
            int returnCount = 0;
            int invokeCount = 0;
            Dictionary<object, object> resultsParts = new Dictionary<object, object>();

            foreach (object target in targets)
            {
                invokeCount++;
                object result = invokingMethod.Invoke(target, arguments);

                if (result != null)
                {
                    resultsParts.Add(target, result);
                    returnCount++;
                }
            }

            if (returnCount > 1)
            {
                return resultsParts;
            }

            if (returnCount == 1)
            {
                return resultsParts.Values.First();
            }

            if (invokeCount == 0)
            {
                string typeName = invokingMethod.DeclaringType.GetDisplayName();
                throw new Exception(ZString.Format("Could not invoke the command because no objects of type {0} could be found.", typeName));
            }

            return null;
        }

        private static string FormatInvocationMessage(int invocationCount, object lastTarget = null)
        {
            switch (invocationCount)
            {
                case 0:
                    throw new Exception("No targets could be found");
                case 1:
                {
                    string name;
                    if (lastTarget is Object obj)
                    {
                        name = obj.name;
                    }
                    else
                    {
                        name = lastTarget?.ToString();
                    }

                    return ZString.Format("> Invoked on {0}", name);
                }
                default:
                    return ZString.Format("> Invoked on {0} targets", invocationCount);
            }
        }

        private static object GetSingletonInstance(Type classType)
        {
            if (AlicizaXConsoleRegistry.GetRegistrySize(classType) > 0)
            {
                return AlicizaXConsoleRegistry.GetRegistryContents(classType).First();
            }

            object target = CreateCommandSingletonInstance(classType);
            AlicizaXConsoleRegistry.RegisterObject(classType, target);

            return target;
        }

        private static Component CreateCommandSingletonInstance(Type classType)
        {
            GameObject obj = new GameObject(ZString.Format("{0}Singleton", classType));
            Object.DontDestroyOnLoad(obj);
            return obj.AddComponent(classType);
        }
    }
}
