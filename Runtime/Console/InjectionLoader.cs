using Cysharp.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AlicizaX.Console
{
    /// <summary>
    /// 防止该类型被 InjectionLoader 加载。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class NoInjectAttribute : Attribute { }

    /// <summary>
    /// 加载可注入类型并创建实例。
    /// </summary>
    /// <typeparam name="T">被注入实例的基类。</typeparam>
    public class InjectionLoader<T>
    {
        private Type[] _injectableTypes;

        /// <summary>
        /// 获取所有可注入类型。
        /// </summary>
        /// <param name="forceReload">强制重新加载类型，而不是使用缓存。</param>
        /// <returns>可注入类型。</returns>
        public Type[] GetInjectableTypes(bool forceReload = false)
        {
            if (_injectableTypes == null || forceReload)
            {
#if UNITY_2019_2_OR_NEWER && UNITY_EDITOR
                _injectableTypes = UnityEditor.TypeCache.GetTypesDerivedFrom<T>()
                                                        .Where(type => !type.IsAbstract)
                                                        .Where(type => !type.IsDefined(typeof(NoInjectAttribute), false))
                                                        .ToArray();
#else
                _injectableTypes = AppDomain.CurrentDomain.GetAssemblies()
                                                          .SelectMany(assembly => assembly.GetTypes())
                                                          .Where(type => typeof(T).IsAssignableFrom(type))
                                                          .Where(type => !type.IsAbstract)
                                                          .Where(type => !type.IsDefined(typeof(NoInjectAttribute), false))
                                                          .ToArray();
#endif
            }

            return _injectableTypes;
        }

        /// <summary>
        /// 为所有可注入类型创建实例。
        /// </summary>
        /// <param name="forceReload">强制重新加载类型，而不是使用缓存。</param>
        /// <returns>可注入实例。</returns>
        public IEnumerable<T> GetInjectedInstances(bool forceReload = false)
        {
            IEnumerable<Type> injectableTypes = GetInjectableTypes(forceReload);
            return GetInjectedInstances(injectableTypes);
        }

        /// <summary>
        /// 根据自定义的可注入类型序列创建实例。
        /// </summary>
        /// <param name="injectableTypes">要创建实例的类型。</param>
        /// <returns>可注入实例。</returns>
        public IEnumerable<T> GetInjectedInstances(IEnumerable<Type> injectableTypes)
        {
            foreach (Type type in injectableTypes)
            {
                T instance = default;
                bool success = false;

                try
                {
                    instance = (T)Activator.CreateInstance(type);
                    success = true;
                }
                catch (MissingMethodException)
                {
                    Debug.LogError(ZString.Format("Could not load {0} {1} as it is missing a public parameterless constructor.", typeof(T), type));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                if (success)
                {
                    yield return instance;
                }
            }
        }
    }
}
