#if !AlicizaXConsole_DISABLED && !AlicizaXConsole_DISABLE_BUILTIN_ALL && !AlicizaXConsole_DISABLE_BUILTIN_EXTRA
using AlicizaX.Console.Pooling;
using AlicizaX.Console.Utilities;
using Cysharp.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AlicizaX.Console.Extras
{
    public static class UtilCommands
    {

        [Command("get-object-info", "查看对象信息")]
        private static string ExtractObjectInfo(GameObject target)
        {
            Utf16ValueStringBuilder builder = StringBuilderPool.GetStringBuilder();

            builder.AppendFormat("Extracted info for object '{0}'", target.name);
            builder.AppendLine();
            builder.AppendLine("Transform data:");
            builder.AppendFormat("   - position: {0}", target.transform.position);
            builder.AppendLine();
            builder.AppendFormat("   - rotation: {0}", target.transform.localRotation);
            builder.AppendLine();
            builder.AppendFormat("   - scale: {0}", target.transform.localScale);
            builder.AppendLine();
            if (target.transform.childCount > 0)
            {
                builder.AppendFormat("   - child count: {0}", target.transform.childCount);
                builder.AppendLine();
            }
            if (target.transform.parent)
            {
                builder.AppendFormat("   - parent: {0}", target.transform.parent.name);
                builder.AppendLine();
            }

            Component[] components = target.GetComponents<Component>().OrderBy(x => x.GetType().Name).ToArray();

            if (components.Length > 0)
            {
                builder.AppendLine("Component data:");
                for (int i = 0; i < components.Length; i++)
                {
                    int componentCount = 1;
                    Type componentType = components[i].GetType();
                    builder.Append("   - ");
                    builder.AppendLine(componentType.Name);
                    while (i + 1 < components.Length && components[i + 1].GetType() == componentType)
                    {
                        componentCount++;
                        i++;
                    }

                    if (componentCount > 1) { builder.AppendFormat(" ({0})", componentCount); }
                }
            }

            if (target.transform.childCount > 0)
            {
                builder.AppendLine("Children:");

                int childCount = target.transform.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    builder.Append("   - ");
                    builder.AppendLine(target.transform.GetChild(i).name);
                }
            }

            return StringBuilderPool.ReleaseAndToString(builder);
        }

        [Command("get-scene-hierarchy", "查看场景层级")]
        private static string GetSceneHierarchy()
        {
            List<GameObject> objects = new List<GameObject>();
            Utf16ValueStringBuilder buffer = StringBuilderPool.GetStringBuilder();

            foreach (UnityEngine.SceneManagement.Scene scene in SceneUtilities.GetLoadedScenes())
            {
                objects.Clear();
                scene.GetRootGameObjects(objects);

                buffer.AppendLine(scene.name);
                GetSceneHierarchy(objects.Select(x => x.transform).ToArray(), 0, buffer, new List<bool>());
            }

            return StringBuilderPool.ReleaseAndToString(buffer);
        }

        private static IEnumerable<Transform> GetChildren(this Transform transform)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                yield return transform.GetChild(i);
            }
        }

        private static void GetSceneHierarchy(IList<Transform> roots, int depth, Utf16ValueStringBuilder buffer, IList<bool> drawVertical)
        {
            const char terminalSymbol = '|';
            const char verticalSplitSymbol = '|';
            const char verticalSymbol = '|';
            const char horizontalSymbol = '-';
            const int indentation = 3;

            for (int i = 0; i < roots.Count; i++)
            {
                Transform root = roots[i];

                for (int j = 0; j < depth; j++)
                {
                    buffer.Append(drawVertical[j] ? verticalSymbol : ' ');
                    buffer.Append(' ', indentation - 1);
                }

                bool terminal = i == roots.Count - 1;
                drawVertical.Add(!terminal);

                buffer.Append(terminal ? terminalSymbol : verticalSplitSymbol);
                buffer.Append(horizontalSymbol, indentation - 1);
                buffer.AppendLine(root.name);

                GetSceneHierarchy(root.GetChildren().ToList(), depth + 1, buffer, drawVertical);
                drawVertical.RemoveAt(drawVertical.Count - 1);
            }
        }

        [Command("add-component", "给对象添加组件")]
        private static void AddComponent<T>(GameObject target) where T : Component { target.AddComponent<T>(); }

        [Command("destroy-component", "销毁组件")]
        private static void DestroyComponent<T>(T target) where T : Component { GameObject.Destroy(target); }

        [Command("destroy", "销毁对象")]
        private static void DestroyGO(GameObject target) { GameObject.Destroy(target); }

        [Command("instantiate", "实例化对象")]
        private static void InstantiateGO(
            [CommandParameterDescription("原始对象")] GameObject original,
            [CommandParameterDescription("生成位置")] Vector3 position,
            [CommandParameterDescription("生成旋转")] Quaternion rotation)
        {
            GameObject.Instantiate(original, position, rotation);
        }

        [Command("instantiate", "实例化对象")]
        private static void InstantiateGO(GameObject original, Vector3 position) { GameObject.Instantiate(original).transform.position = position; }

        [Command("instantiate", "实例化对象")]
        private static void InstantiateGO(GameObject original) { GameObject.Instantiate(original); }

        [Command("teleport", "移动对象到位置")]
        private static void TeleportGO(GameObject target, Vector3 position) { target.transform.position = position; }

        [Command("teleport-relative", "按偏移移动对象")]
        private static void TeleportRelativeGO(GameObject target, Vector3 offset) { target.transform.Translate(offset); }

        [Command("rotate", "旋转对象")]
        private static void RotateGO(GameObject target, Quaternion rotation) { target.transform.Rotate(rotation.eulerAngles); }

        [Command("set-active", "设置对象启用状态")]
        private static void SetGOActive(GameObject target, bool active) { target.SetActive(active); }

        [Command("set-parent", "设置父节点")]
        private static void SetGOParent(Transform target, Transform parentTarget) { target.SetParent(parentTarget); }

        [Command("send-message", "向对象发送消息")]
        private static void SendGOMessage(GameObject target, string methodName) { target.SendMessage(methodName); }
    }
}
#endif
