using AlicizaX.Console.Utilities;
using Cysharp.Text;
using System;
using UnityEngine;

namespace AlicizaX.Console.Parsers
{
    public class ComponentParser : PolymorphicAlicizaXConsoleParser<Component>
    {
        public override Component Parse(string value, Type type)
        {
            GameObject obj = ParseRecursive<GameObject>(value);
            Component objComponent = obj.GetComponent(type);

            if (!objComponent)
            {
                throw new ParserInputException(ZString.Format("No component on the object '{0}' of type {1} existed.", value, type.GetDisplayName()));
            }

            return objComponent;
        }
    }
}
