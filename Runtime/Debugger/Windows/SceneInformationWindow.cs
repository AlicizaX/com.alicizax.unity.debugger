using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace AlicizaX.Debugger
{
    public sealed partial class DebuggerComponent
    {
                [DebuggerWindow("Information/Other/Scene")]
        private sealed class SceneInformationWindow : ScrollableDebuggerWindowBase
        {
            private int _lastSceneCount;
            private ulong _lastActiveSceneHandle;
            private bool _lastIsDirty;

            public override void OnEnter()
            {
                Rebuild();
            }

            public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
            {
                // 场景加载/卸载或激活场景切换才重建。
                UnityEngine.SceneManagement.Scene activeScene = SceneManager.GetActiveScene();
                if (SceneManager.sceneCount == _lastSceneCount
                    && GetSceneHandle(activeScene) == _lastActiveSceneHandle
                    && activeScene.isDirty == _lastIsDirty)
                {
                    return;
                }

                Rebuild();
            }

            protected override void BuildWindow(VisualElement root)
            {
                UnityEngine.SceneManagement.Scene activeScene = SceneManager.GetActiveScene();
                _lastSceneCount = SceneManager.sceneCount;
                _lastActiveSceneHandle = GetSceneHandle(activeScene);
                _lastIsDirty = activeScene.isDirty;

                VisualElement section = CreateSection("Scene Information", out VisualElement card);
                card.Add(CreateRow("Scene Count", _lastSceneCount.ToString()));
                card.Add(CreateRow("Scene Count In Build Settings", SceneManager.sceneCountInBuildSettings.ToString()));

                card.Add(CreateRow("Active Scene Handle", _lastActiveSceneHandle.ToString()));
                card.Add(CreateRow("Active Scene Name", activeScene.name));
                card.Add(CreateRow("Active Scene Path", activeScene.path));
                card.Add(CreateRow("Active Scene Build Index", activeScene.buildIndex.ToString()));
                card.Add(CreateRow("Active Scene Is Dirty", activeScene.isDirty.ToString()));
                card.Add(CreateRow("Active Scene Is Loaded", activeScene.isLoaded.ToString()));
                card.Add(CreateRow("Active Scene Is Valid", activeScene.IsValid().ToString()));
                card.Add(CreateRow("Active Scene Root Count", activeScene.rootCount.ToString()));
                card.Add(CreateRow("Active Scene Is Sub Scene", activeScene.isSubScene.ToString()));
                root.Add(section);
            }

            /// <summary>
            /// Unity 6000.5 起 <see cref="UnityEngine.SceneManagement.Scene.handle"/> 的类型是 SceneHandle，
            /// 它到 int 的隐式转换已废弃（编译错误），需改用 <c>GetRawData()</c>；旧版本仍是 int 句柄。
            /// 统一折算成 ulong，比较与展示只用一份代码。
            /// </summary>
            private static ulong GetSceneHandle(UnityEngine.SceneManagement.Scene scene)
            {
#if UNITY_6000_5_OR_NEWER
                return scene.handle.GetRawData();
#else
                return unchecked((uint)scene.handle);
#endif
            }
        }
    }
}
