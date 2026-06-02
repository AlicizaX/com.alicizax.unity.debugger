using AlicizaX.Console.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AlicizaX.Console
{
    /// <summary>
    /// 处理控制台返回值的格式化序列化。
    /// </summary>
    public class AlicizaXConsoleSerializer
    {
        private readonly IAlicizaXConsoleSerializer[] _serializers;
        private readonly Dictionary<Type, IAlicizaXConsoleSerializer> _serializerLookup = new Dictionary<Type, IAlicizaXConsoleSerializer>();
        private readonly HashSet<Type> _unserializableLookup = new HashSet<Type>();

        private readonly Func<object, AlicizaXConsoleTheme, string> _recursiveSerializer;

        /// <summary>
        /// 用自定义序列化器集合创建 AlicizaXConsole Serializer。
        /// </summary>
        /// <param name="serializers">此 AlicizaXConsole Serializer 要使用的 IAlicizaXConsoleSerializer。</param>
        public AlicizaXConsoleSerializer(IEnumerable<IAlicizaXConsoleSerializer> serializers)
        {
            _recursiveSerializer = SerializeFormatted;
            _serializers = serializers.OrderByDescending(x => x.Priority)
                                      .ToArray();
        }

        /// <summary>
        /// 用默认注入的序列化器创建 AlicizaXConsole Serializer。
        /// </summary>
        public AlicizaXConsoleSerializer() : this(new InjectionLoader<IAlicizaXConsoleSerializer>().GetInjectedInstances())
        {

        }

        /// <summary>
        /// 把对象格式化序列化，方便在控制台显示。
        /// </summary>
        /// <param name="value">要格式化并序列化的值。</param>
        /// <param name="theme">（可选）用于格式化结果的 AlicizaXConsoleTheme。</param>
        /// <returns>格式化后的序列化结果。</returns>
        public string SerializeFormatted(object value, AlicizaXConsoleTheme theme = null)
        {
            if (value is null)
            {
                return string.Empty;
            }

            Type type = value.GetType();
            string result = string.Empty;

            string SerializeInternal(IAlicizaXConsoleSerializer serializer)
            {
                try
                {
                    return serializer.SerializeFormatted(value, theme, _recursiveSerializer);
                }
                catch (Exception e)
                {
                    throw new Exception($"Serialization of {type.GetDisplayName()} via {serializer} failed:\n{e.Message}", e);
                }
            }

            if (_serializerLookup.ContainsKey(type))
            {
                result = SerializeInternal(_serializerLookup[type]);
            }
            else if (_unserializableLookup.Contains(type))
            {
                result = value.ToString();
            }
            else
            {
                bool converted = false;

                foreach (IAlicizaXConsoleSerializer serializer in _serializers)
                {
                    if (serializer.CanSerialize(type))
                    {
                        result = SerializeInternal(serializer);

                        _serializerLookup[type] = serializer;
                        converted = true;
                        break;
                    }
                }

                if (!converted)
                {
                    result = value.ToString();
                    _unserializableLookup.Add(type);
                }
            }

            if (theme && !string.IsNullOrWhiteSpace(result))
            {
                result = theme.ColorizeReturn(result, type);
            }

            return result;
        }
    }
}
