#if !AlicizaXConsole_DISABLED && !AlicizaXConsole_DISABLE_BUILTIN_ALL && !AlicizaXConsole_DISABLE_BUILTIN_EXTRA
using UnityEngine;

namespace AlicizaX.Console.Extras
{
    public static class ApplicationCommands
    {
        [Command("quit", "退出应用")]
        [CommandPlatform(Platform.AllPlatforms ^ (Platform.EditorPlatforms | Platform.WebGLPlayer))]
        private static void Quit()
        {
            Application.Quit();
        }
    }
}
#endif
