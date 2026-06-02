using Cysharp.Text;

namespace AlicizaX.Console.Utilities
{
    public static class ZStringBuilderExtensions
    {
        public static string ToStringAndDispose(this ref Utf16ValueStringBuilder builder)
        {
            try
            {
                return builder.ToString();
            }
            finally
            {
                builder.Dispose();
            }
        }
    }
}
