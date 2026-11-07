using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace AlicizaX.Debugger
{
    public sealed partial class DebuggerComponent
    {
                [DebuggerWindow("Information/Environment")]
        private sealed class EnvironmentInformationWindow : ScrollableDebuggerWindowBase
        {
            private bool _lastIsFocused;
            private NetworkReachability _lastReachability;

            public override void OnEnter()
            {
                Rebuild();
            }

            public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
            {
                // 只有焦点/网络可达性会变，其余是进程级常量。
                if (Application.isFocused == _lastIsFocused
                    && Application.internetReachability == _lastReachability)
                {
                    return;
                }

                Rebuild();
            }

            protected override void BuildWindow(VisualElement root)
            {
                _lastIsFocused = Application.isFocused;
                _lastReachability = Application.internetReachability;

                VisualElement section = CreateSection("Environment Information", out VisualElement card);
                card.Add(CreateRow("Product Name", Application.productName));
                card.Add(CreateRow("Company Name", Application.companyName));
                card.Add(CreateRow("Game Identifier", Application.identifier));
                card.Add(CreateRow("Game Version", Utility.Text.Format("{0}", Application.version)));
                card.Add(CreateRow("Application Version", Application.version));
                card.Add(CreateRow("Unity Version", Application.unityVersion));
                card.Add(CreateRow("Platform", Application.platform.ToString()));
                card.Add(CreateRow("System Language", Application.systemLanguage.ToString()));
                card.Add(CreateRow("Cloud Project Id", Application.cloudProjectId));
                card.Add(CreateRow("Build Guid", Application.buildGUID));
                card.Add(CreateRow("Target Frame Rate", Application.targetFrameRate.ToString()));
                card.Add(CreateRow("Internet Reachability", Application.internetReachability.ToString()));
                card.Add(CreateRow("Background Loading Priority", Application.backgroundLoadingPriority.ToString()));
                card.Add(CreateRow("Is Playing", Application.isPlaying.ToString()));
                card.Add(CreateRow("Splash Screen Is Finished", SplashScreen.isFinished.ToString()));
                card.Add(CreateRow("Run In Background", Application.runInBackground.ToString()));
                card.Add(CreateRow("Install Name", Application.installerName));
                card.Add(CreateRow("Install Mode", Application.installMode.ToString()));
                card.Add(CreateRow("Sandbox Type", Application.sandboxType.ToString()));
                card.Add(CreateRow("Is Mobile Platform", Application.isMobilePlatform.ToString()));
                card.Add(CreateRow("Is Console Platform", Application.isConsolePlatform.ToString()));
                card.Add(CreateRow("Is Editor", Application.isEditor.ToString()));
                card.Add(CreateRow("Is Debug Build", Debug.isDebugBuild.ToString()));
                card.Add(CreateRow("Is Focused", Application.isFocused.ToString()));
                card.Add(CreateRow("Is Batch Mode", Application.isBatchMode.ToString()));
                root.Add(section);

                VisualElement pathSection = CreateSection("Path Information", out VisualElement pathCard);
                pathCard.Add(CreateRow("Current Directory", Utility.Path.GetRegularPath(Environment.CurrentDirectory)));
                pathCard.Add(CreateRow("Data Path", Utility.Path.GetRegularPath(Application.dataPath)));
                pathCard.Add(CreateRow("Persistent Data Path", Utility.Path.GetRegularPath(Application.persistentDataPath)));
                pathCard.Add(CreateRow("Streaming Assets Path", Utility.Path.GetRegularPath(Application.streamingAssetsPath)));
                pathCard.Add(CreateRow("Temporary Cache Path", Utility.Path.GetRegularPath(Application.temporaryCachePath)));
                pathCard.Add(CreateRow("Console Log Path", Utility.Path.GetRegularPath(Application.consoleLogPath)));
                root.Add(pathSection);

                VisualElement webSection = CreateSection("Web Player Information", out VisualElement webCard);
                webCard.Add(CreateRow("Absolute URL", Application.absoluteURL));
                root.Add(webSection);
            }
        }
    }
}
