#if !AlicizaXConsole_DISABLED && !AlicizaXConsole_DISABLE_BUILTIN_ALL && !AlicizaXConsole_DISABLE_BUILTIN_EXTRA
using System.Collections.Generic;
using UnityEngine;

namespace AlicizaX.Console.Extras
{
    public static class ScreenCommands
    {
        [Command("fullscreen", "开关全屏")]
        private static bool Fullscreen
        {
            get => Screen.fullScreen;
            set => Screen.fullScreen = value;
        }

        [Command("screen-dpi", "查看屏幕 DPI")]
        private static float DPI => Screen.dpi;

        [Command("screen-orientation", "设置屏幕方向")]
        [CommandPlatform(Platform.MobilePlatforms)]
        private static ScreenOrientation Orientation
        {
            get => Screen.orientation;
            set => Screen.orientation = value;
        }

        [Command("current-resolution", "查看当前分辨率")]
        private static Resolution GetCurrentResolution()
        {
            Resolution resolution = new Resolution
            {
                width = Screen.width,
                height = Screen.height,
#if UNITY_6000_0_OR_NEWER
                refreshRateRatio = Screen.currentResolution.refreshRateRatio
#else
                refreshRate = Screen.currentResolution.refreshRate
#endif
            };

            return resolution;
        }

        [Command("supported-resolutions", "列出支持的分辨率")]
        [CommandPlatform(Platform.AllPlatforms ^ Platform.WebGLPlayer)]
        private static IEnumerable<Resolution> GetSupportedResolutions()
        {
            foreach (Resolution resolution in Screen.resolutions)
            {
                yield return resolution;
            }
        }

        [Command("set-resolution")]
        private static void SetResolution(int x, int y)
        {
            SetResolution(x, y, Screen.fullScreen);
        }

        [Command("set-resolution", "设置分辨率")]
        private static void SetResolution(int x, int y, bool fullscreen)
        {
            Screen.SetResolution(x, y, fullscreen);
        }

        [Command("capture-screenshot")]
        [CommandDescription("保存 PNG 截图")]
        private static void CaptureScreenshot(
            [CommandParameterDescription("截图文件名")] string filename,
            [CommandParameterDescription("截图放大倍数")] int superSize = 1
        )
        {
            ScreenCapture.CaptureScreenshot(filename, superSize);
        }
    }
}
#endif
