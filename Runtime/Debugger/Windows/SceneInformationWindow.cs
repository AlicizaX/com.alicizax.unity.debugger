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
            private int _lastActiveSceneHandle;
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
                    && activeScene.handle == _lastActiveSceneHandle
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
                _lastActiveSceneHandle = activeScene.handle;
                _lastIsDirty = activeScene.isDirty;

                VisualElement section = CreateSection("Scene Information", out VisualElement card);
                card.Add(CreateRow("Scene Count", _lastSceneCount.ToString()));
                card.Add(CreateRow("Scene Count In Build Settings", SceneManager.sceneCountInBuildSettings.ToString()));

                card.Add(CreateRow("Active Scene Handle", activeScene.handle.ToString()));
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
        }
    }
}
