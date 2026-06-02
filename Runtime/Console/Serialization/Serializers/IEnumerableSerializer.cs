using AlicizaX.Console.Pooling;
using Cysharp.Text;
using System;
using System.Collections;

namespace AlicizaX.Console.Serializers
{
    public class IEnumerableSerializer : IEnumerableSerializer<IEnumerable>
    {
        public override int Priority => base.Priority - 1000;

        protected override IEnumerable GetObjectStream(IEnumerable value)
        {
            return value;
        }
    }

    public abstract class IEnumerableSerializer<T> : PolymorphicAlicizaXConsoleSerializer<T> where T : class, IEnumerable
    {


        public override string SerializeFormatted(T value, AlicizaXConsoleTheme theme)
        {
            Type type = value.GetType();
            Utf16ValueStringBuilder builder = StringBuilderPool.GetStringBuilder();

            string left = "[";
            string seperator = ",";
            string right = "]";
            if (theme)
            {
                theme.GetCollectionFormatting(type, out left, out seperator, out right);
            }

            builder.Append(left);

            bool firstIteration = true;
            foreach (object item in GetObjectStream(value))
            {
                if (firstIteration)
                {
                    firstIteration = false;
                }
                else
                {
                    builder.Append(seperator);
                }

                builder.Append(SerializeRecursive(item, theme));
            }

            builder.Append(right);

            return StringBuilderPool.ReleaseAndToString(builder);
        }

        protected abstract IEnumerable GetObjectStream(T value);
    }
}
