#if !AlicizaXConsole_DISABLED && !AlicizaXConsole_DISABLE_BUILTIN_ALL && !AlicizaXConsole_DISABLE_BUILTIN_EXTRA
using UnityEngine;

namespace AlicizaX.Console.Extras
{
    public static class TimeCommands
    {
        [Command("time-scale", "设置时间缩放")]
        private static float TimeScale
        {
            get => Time.timeScale;
            set => Time.timeScale = value;
        }
    }
}
#endif
