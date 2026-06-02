using AlicizaX.Console.Utilities;
using Cysharp.Text;
using UnityEngine;
using GameObjectExtensions = AlicizaX.Console.Utilities.GameObjectExtensions;

namespace AlicizaX.Console.Parsers
{
    public class GameObjectParser : BasicAlicizaXConsoleParser<GameObject>
    {
        public override GameObject Parse(string value)
        {
            string name = ParseRecursive<string>(value);
            GameObject obj = GameObjectExtensions.Find(name, true);

            if (!obj)
            {
                throw new ParserInputException(ZString.Format("Could not find GameObject of name {0}.", value));
            }

            return obj;
        }
    }
}
