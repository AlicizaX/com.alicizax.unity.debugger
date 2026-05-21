using UnityEngine;

namespace AlicizaX.Console.Serializers
{
    public class Vector2Serializer : BasicAlicizaXConsoleSerializer<Vector2>
    {
        public override string SerializeFormatted(Vector2 value, AlicizaXConsoleTheme theme)
        {
            return $"({value.x}, {value.y})";
        }
    }

    public class Vector3Serializer : BasicAlicizaXConsoleSerializer<Vector3>
    {
        public override string SerializeFormatted(Vector3 value, AlicizaXConsoleTheme theme)
        {
            return $"({value.x}, {value.y}, {value.z})";
        }
    }

    public class Vector4Serializer : BasicAlicizaXConsoleSerializer<Vector4>
    {
        public override string SerializeFormatted(Vector4 value, AlicizaXConsoleTheme theme)
        {
            return $"({value.x}, {value.y}, {value.z}, {value.w})";
        }
    }

    public class Vector2IntSerializer : BasicAlicizaXConsoleSerializer<Vector2Int>
    {
        public override string SerializeFormatted(Vector2Int value, AlicizaXConsoleTheme theme)
        {
            return $"({value.x}, {value.y})";
        }
    }

    public class Vector3IntSerializer : BasicAlicizaXConsoleSerializer<Vector3Int>
    {
        public override string SerializeFormatted(Vector3Int value, AlicizaXConsoleTheme theme)
        {
            return $"({value.x}, {value.y}, {value.z})";
        }
    }

    public class UnityObjectSerializer : PolymorphicAlicizaXConsoleSerializer<Object>
    {
        public override string SerializeFormatted(Object value, AlicizaXConsoleTheme theme)
        {
            return value.name;
        }
    }
}
