using UnityEngine;

namespace AlicizaX.Console.Parsers
{
    public class Vector2Parser : BasicCachedAlicizaXConsoleParser<Vector2>
    {
        public override Vector2 Parse(string value)
        {
            return ParseRecursive<Vector4>(value);
        }
    }

    public class Vector3Parser : BasicCachedAlicizaXConsoleParser<Vector3>
    {
        public override Vector3 Parse(string value)
        {
            return ParseRecursive<Vector4>(value);
        }
    }

    public class Vector4Parser : BasicCachedAlicizaXConsoleParser<Vector4>
    {
        public override Vector4 Parse(string value)
        {
            string[] vectorParts = value.SplitScoped(',');
            Vector4 parsedVector = new Vector4();

            if (vectorParts.Length < 2 || vectorParts.Length > 4)
            {
                throw new ParserInputException($"Cannot parse '{value}' as a vector, the format must be either x,y x,y,z or x,y,z,w.");
            }

            for (int i = 0; i < vectorParts.Length; i++)
            {
                parsedVector[i] = ParseRecursive<float>(vectorParts[i]);
            }

            return parsedVector;
        }
    }

    public class Vector2IntParser : BasicCachedAlicizaXConsoleParser<Vector2Int>
    {
        public override Vector2Int Parse(string value)
        {
            return (Vector2Int)ParseRecursive<Vector3Int>(value);
        }
    }

    public class Vector3IntParser : BasicCachedAlicizaXConsoleParser<Vector3Int>
    {
        public override Vector3Int Parse(string value)
        {
            string[] vectorParts = value.Split(',');
            Vector3Int parsedVector = new Vector3Int();

            if (vectorParts.Length < 2 || vectorParts.Length > 3)
            {
                throw new ParserInputException($"Cannot parse '{value}' as an int vector, the format must be either x,y or x,y,z");
            }

            int i = 0;
            try
            {
                for (; i < vectorParts.Length; i++)
                {
                    parsedVector[i] = int.Parse(vectorParts[i]);
                }

                return parsedVector;
            }
            catch
            {
                throw new ParserInputException($"Cannot parse '{vectorParts[i]}' as it must be integral.");
            }
        }
    }

    public class QuaternionParser : BasicCachedAlicizaXConsoleParser<Quaternion>
    {
        public override Quaternion Parse(string value)
        {
            Vector4 vector = ParseRecursive<Vector4>(value);
            return new Quaternion(vector.x, vector.y, vector.z, vector.w);
        }
    }
}
