using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AlicizaX.Console.Utilities
{
    public static class GameObjectExtensions
    {
        private const int MaxCachedGameObjects = 64;

        private static readonly Dictionary<string, GameObject> GameObjectCache = new Dictionary<string, GameObject>();
        private static readonly List<GameObject> RootGameObjectBuffer = new List<GameObject>();

        /// <summary>
        /// 缓存查找结果。键来自命令参数（任意文本），必须设上限，
        /// 否则一次会话里每个不同的名字都会永久占一个条目。
        /// </summary>
        private static GameObject Cache(string name, GameObject obj)
        {
            if (GameObjectCache.Count >= MaxCachedGameObjects)
            {
                GameObjectCache.Clear();
            }

            GameObjectCache[name] = obj;
            return obj;
        }

        public static GameObject Find(string name, bool includeInactive = false)
        {
            if (GameObjectCache.TryGetValue(name, out GameObject obj)
                && obj
                && obj.activeInHierarchy | includeInactive
                && obj.name == name)
            {
                return obj;
            }

            obj = GameObject.Find(name);
            if (obj)
            {
                return Cache(name, obj);
            }

            if (includeInactive)
            {
                int sceneCount = SceneManager.sceneCountInBuildSettings;
                for (int i = 0; i < sceneCount; i++)
                {
                    UnityEngine.SceneManagement.Scene scene = SceneManager.GetSceneByBuildIndex(i);
                    if (scene.isLoaded)
                    {
                        RootGameObjectBuffer.Clear();
                        scene.GetRootGameObjects(RootGameObjectBuffer);

                        foreach (GameObject root in RootGameObjectBuffer)
                        {
                            obj = Find(name, root);
                            if (obj)
                            {
                                return Cache(name, obj);
                            }
                        }
                    }
                }

                obj = Resources
                    .FindObjectsOfTypeAll<GameObject>()
                    .Where(x => !x.hideFlags.HasFlag(HideFlags.HideInHierarchy))
                    .FirstOrDefault(x => x.name == name);

                if (obj)
                {
                    return Cache(name, obj);
                }
            }

            return null;
        }

        public static GameObject Find(string name, GameObject root)
        {
            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.transform.childCount; i++)
            {
                GameObject obj = Find(name, root.transform.GetChild(i).gameObject);
                if (obj)
                {
                    return obj;
                }
            }

            return null;
        }
    }
}
