#if !AlicizaXConsole_DISABLED && !AlicizaXConsole_DISABLE_BUILTIN_ALL && !AlicizaXConsole_DISABLE_BUILTIN_EXTRA
using UnityEngine;

namespace AlicizaX.Console.Extras
{
    public static class GraphicsCommands
    {
        [Command("max-fps", "设置最大帧率，-1 不限制")]
        private static int MaxFPS
        {
            get => Application.targetFrameRate;
            set => Application.targetFrameRate = value;
        }

        [Command("vsync", "开关垂直同步")]
        private static bool VSync
        {
            get => QualitySettings.vSyncCount > 0;
            set => QualitySettings.vSyncCount = value ? 1 : 0;
        }

        [Command("msaa", "设置 MSAA 采样数")]
        private static int MSAA
        {
            get => QualitySettings.antiAliasing;
            set => QualitySettings.antiAliasing = value;
        }
    }
}
#endif
