using AlicizaX.Console.Utilities;
using Cysharp.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AlicizaX.Console
{
    public static class AlicizaXConsoleRegistry
    {
        private static readonly Dictionary<Type, List<object>> _objectRegistry = new Dictionary<Type, List<object>>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            _objectRegistry.Clear();
        }

        private static bool IsNull(object x)
        {
            if (x is UnityEngine.Object u)
            {
                return !u;
            }

            return x is null;
        }

        /// <summary>把对象加入注册表。</summary>
        /// <param name="obj">要加入注册表的对象。</param>
        /// <typeparam name="T">要加入注册表的对象类型。</typeparam>
        [Command("register-object", "Adds the object to the registry to be used by commands with MonoTargetType = Registry")]
        public static void RegisterObject<T>(T obj) where T : class { RegisterObject(typeof(T), obj); }

        /// <summary>把对象加入注册表。</summary>
        /// <param name="type">要加入注册表的对象类型。</param>
        /// <param name="obj">要加入注册表的对象。</param>
        public static void RegisterObject(Type type, object obj)
        {
            if (!type.IsClass) { throw new Exception("Registry may only contain class types"); }
            lock (_objectRegistry)
            {
                if (_objectRegistry.ContainsKey(type))
                {
                    if (_objectRegistry[type].Contains(obj))
                    {
                        throw new ArgumentException(ZString.Format("Could not register object '{0}' of type {1} as it was already registered.", obj, type.GetDisplayName()));
                    }

                    _objectRegistry[type].Add(obj);
                }
                else
                {
                    _objectRegistry.Add(type, new List<object>() { obj });
                }
            }
        }

        /// <summary>从注册表移除对象。</summary>
        /// <param name="obj">要从注册表移除的对象。</param>
        /// <typeparam name="T">要从注册表移除的对象类型。</typeparam>
        [Command("deregister-object", "Removes the object to the registry to be used by commands with MonoTargetType = Registry")]
        public static void DeregisterObject<T>(T obj) where T : class { DeregisterObject(typeof(T), obj); }

        /// <summary>从注册表移除对象。</summary>
        /// <param name="type">要从注册表移除的对象类型。</param>
        /// <param name="obj">要从注册表移除的对象。</param>
        public static void DeregisterObject(Type type, object obj)
        {
            if (!type.IsClass) { throw new Exception("Registry may only contain class types"); }
            lock (_objectRegistry)
            {
                if (_objectRegistry.ContainsKey(type) && _objectRegistry[type].Contains(obj))
                {
                    _objectRegistry[type].Remove(obj);
                }
                else
                {
                    throw new ArgumentException(ZString.Format("Could not deregister object '{0}' of type {1} as it was not found in the registry.", obj, type.GetDisplayName()));
                }
            }
        }

        /// <summary>获取指定注册表的大小。</summary>
        /// <returns>注册表大小。</returns>
        /// <typeparam name="T">要查询的注册表。</typeparam>
        public static int GetRegistrySize<T>() where T : class { return GetRegistrySize(typeof(T)); }

        /// <summary>获取指定注册表的大小。</summary>
        /// <returns>注册表大小。</returns>
        /// <param name="type">要查询的注册表。</param>
        public static int GetRegistrySize(Type type)
        {
            return GetRegistryContents(type).Count();
        }

        /// <summary>获取指定注册表里的内容。</summary>
        /// <returns>注册表内容。</returns>
        /// <typeparam name="T">要查询的注册表。</typeparam>
        public static IEnumerable<T> GetRegistryContents<T>() where T : class
        {
            foreach (object obj in GetRegistryContents(typeof(T)))
            {
                yield return (T)obj;
            }
        }

        /// <summary>获取指定注册表里的内容。</summary>
        /// <returns>注册表内容。</returns>
        /// <param name="type">要查询的注册表。</param>
        public static IEnumerable<object> GetRegistryContents(Type type)
        {
            if (!type.IsClass) { throw new Exception("Registry may only contain class types"); }
            lock (_objectRegistry)
            {
                if (_objectRegistry.ContainsKey(type))
                {
                    List<object> registry = _objectRegistry[type];
                    registry.RemoveAll(IsNull);
                    return registry;
                }
                
                return Enumerable.Empty<object>();
            }
        }

        /// <summary>清空指定注册表里的内容。</summary>
        /// <typeparam name="T">要清空的注册表。</typeparam>
        [Command("clear-registry", "Clears the contents of the specified registry")]
        public static void ClearRegistryContents<T>() where T : class
        {
            ClearRegistryContents(typeof(T));
        }

        /// <summary>清空指定注册表里的内容。</summary>
        /// <param name="type">要清空的注册表。</param>
        public static void ClearRegistryContents(Type type)
        {
            if (!type.IsClass) { throw new Exception("Registry may only contain class types"); }
            lock (_objectRegistry)
            {
                if (_objectRegistry.ContainsKey(type))
                {
                    _objectRegistry[type].Clear();
                }
            }
        }

        [Command("display-registry", "Displays the contents of the specified registry")]
        private static IEnumerable<object> DisplayRegistry<T>() where T : class
        {
            if (GetRegistrySize<T>() <= 0) 
            { 
                return ZString.Format("The registry '{0}' is empty", typeof(T).GetDisplayName()).Yield();
            }

            return GetRegistryContents<T>();
        }
    }
}
