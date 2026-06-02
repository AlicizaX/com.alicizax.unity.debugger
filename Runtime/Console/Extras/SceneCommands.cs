#if !AlicizaXConsole_DISABLED && !AlicizaXConsole_DISABLE_BUILTIN_ALL && !AlicizaXConsole_DISABLE_BUILTIN_EXTRA
using AlicizaX.Console.Suggestors.Tags;
using AlicizaX.Console.Utilities;
using Cysharp.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AlicizaX.Console.Extras
{
    public static class SceneCommands
    {
        private static async Task PollUntilAsync(int pollInterval, Func<bool> predicate)
        {
            while (!predicate())
            {
                await Task.Delay(pollInterval);
            }
        }

        [Command("load-scene", "按名称加载场景")]
        private static async Task LoadScene(
            [SceneName]
            string sceneName,

            [CommandParameterDescription("单一模式替换当前场景，叠加模式保留原场景")]
            LoadSceneMode loadMode = LoadSceneMode.Single)
        {
            AsyncOperation asyncOperation = SceneUtilities.LoadSceneAsync(sceneName, loadMode);
            await PollUntilAsync(16, () => asyncOperation.isDone);
        }

        [Command("load-scene-index", "按索引加载场景")]
        private static async Task LoadScene(int sceneIndex,
        [CommandParameterDescription("单一模式替换当前场景，叠加模式保留原场景")]LoadSceneMode loadMode = LoadSceneMode.Single)
        {
            AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneIndex, loadMode);
            await PollUntilAsync(16, () => asyncOperation.isDone);
        }

        [Command("unload-scene", "按名称卸载场景")]
        private static async Task UnloadScene([SceneName(LoadedOnly = true)] string sceneName)
        {
            AsyncOperation asyncOperation = SceneManager.UnloadSceneAsync(sceneName);
            await PollUntilAsync(16, () => asyncOperation.isDone);
        }

        [Command("unload-scene-index", "按索引卸载场景")]
        private static async Task UnloadScene(int sceneIndex)
        {
            AsyncOperation asyncOperation = SceneManager.UnloadSceneAsync(sceneIndex);
            await PollUntilAsync(16, () => asyncOperation.isDone);
        }

        [Command("all-scenes", "列出构建中的场景")]
        private static IEnumerable<KeyValuePair<int, string>> GetAllScenes()
        {
            int sceneIndex = 0;
            foreach (string sceneName in SceneUtilities.GetAllSceneNames())
            {
                yield return new KeyValuePair<int, string>(sceneIndex++, sceneName);
            }
        }

        [Command("loaded-scenes", "列出已加载场景")]
        private static IEnumerable<KeyValuePair<int, string>> GetLoadedScenes()
        {
            return SceneUtilities.GetLoadedScenes()
                .OrderBy(x => x.buildIndex)
                .Select(x => new KeyValuePair<int, string>(x.buildIndex, x.name));
        }

        [Command("active-scene", "查看当前活动场景")]
        private static string GetCurrentScene()
        {
            UnityEngine.SceneManagement.Scene scene = SceneManager.GetActiveScene();
            return scene.name;
        }

        [Command("set-active-scene", "设置活动场景")]
        private static void SetActiveScene([SceneName(LoadedOnly = true)] string sceneName)
        {
            UnityEngine.SceneManagement.Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.isLoaded)
            {
                throw new ArgumentException(ZString.Format("Scene {0} must be loaded before it can be set active", sceneName));
            }

            SceneManager.SetActiveScene(scene);
        }
    }
}
#endif
