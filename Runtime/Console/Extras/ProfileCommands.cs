#if !AlicizaXConsole_DISABLED && !AlicizaXConsole_DISABLE_BUILTIN_ALL && !AlicizaXConsole_DISABLE_BUILTIN_EXTRA
using AlicizaX.Debugger;

namespace AlicizaX.Console.Extras
{
    public static class ProfileCommands
    {
        [Command("profile", "控制运行时 profile 功能。用法: profile stats true/false")]
        [CommandDescription("profile stats <bool> — 开关右上角半透明 Stats 悬浮窗（FPS / 三角面 / DrawCall / 内存）")]
        private static string Profile(string feature, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(feature))
            {
                return "用法: profile stats <true|false>";
            }

            if (!string.Equals(feature, "stats", System.StringComparison.OrdinalIgnoreCase))
            {
                return "未知 profile 功能 '" + feature + "'。支持: stats";
            }

            DebuggerComponent debugger = DebuggerComponent.Instance;
            if (debugger == null)
            {
                return "DebuggerComponent 不可用。";
            }

            debugger.SetStatsOverlayVisible(enabled);
            return enabled ? "Stats 悬浮窗: 开启" : "Stats 悬浮窗: 关闭";
        }
    }
}
#endif
