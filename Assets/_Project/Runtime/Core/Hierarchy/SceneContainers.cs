using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlanetIO
{
    public static class SceneContainers
    {
        public const string Players = "Players";
        public const string Enemies = "Enemies";
        public const string Food = "Food";
        public const string Comets = "Comets";
        public const string Effects = "Effects";

        private const string RootName = "[Runtime]";
        private static readonly Dictionary<string, Transform> Cache = new();
        private static Scene _cachedScene;

        public static void Attach(Transform target, string category)
        {
            if (target == null)
            {
                return;
            }

            Transform container = Get(category);
            if (target.parent != container)
            {
                target.SetParent(container, true);
            }
        }

        public static Transform Get(string category)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene != _cachedScene)
            {
                Cache.Clear();
                _cachedScene = scene;
            }

            if (Cache.TryGetValue(category, out Transform cached) && cached != null)
            {
                return cached;
            }

            Transform root = FindOrCreateRoot(scene);
            Transform container = root.Find(category);
            if (container == null)
            {
                container = new GameObject(category).transform;
                container.SetParent(root, false);
            }

            Cache[category] = container;
            return container;
        }

        private static Transform FindOrCreateRoot(Scene scene)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.name == RootName)
                {
                    return rootObject.transform;
                }
            }

            GameObject created = new(RootName);
            SceneManager.MoveGameObjectToScene(created, scene);
            return created.transform;
        }
    }
}
