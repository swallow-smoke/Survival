#if UNITY_EDITOR
using System;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WorldBuilder.Entities.Resources;

namespace AstraNope.Editor
{
    [InitializeOnLoad]
    public static class ResourceWorldSmokeRunner
    {
        private const string SessionKey = "WorldBuilder.ResourceSmoke.Active";
        private static double startedAt;

        static ResourceWorldSmokeRunner()
        {
            if (SessionState.GetBool(SessionKey, false)) AttachPoller();
        }

        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/000_Scenes/SampleScene.unity", OpenSceneMode.Single);
            SessionState.SetBool(SessionKey, true);
            AttachPoller();
            EditorApplication.isPlaying = true;
        }

        private static void AttachPoller()
        {
            startedAt = EditorApplication.timeSinceStartup;
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            if (!EditorApplication.isPlaying)
            {
                if (EditorApplication.timeSinceStartup - startedAt > 60d)
                    Finish(2, "Timed out entering Play Mode.");
                return;
            }

            World world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                EntityManager manager = world.EntityManager;
                int runtimes = Count(manager, typeof(WorldBuilder.Entities.WorldEntityRuntimeConfig));
                int nodes = CountWithoutPrefabs(manager, typeof(ResourceNode));
                int drops = CountWithoutPrefabs(manager, typeof(DroppedItem));
                if (runtimes == 1 && nodes > 0 && drops > 0)
                {
                    Finish(0, $"Resource smoke passed. runtime={runtimes}, nodes={nodes}, drops={drops}");
                    return;
                }
            }

            if (EditorApplication.timeSinceStartup - startedAt > 30d)
                Finish(3, "Resource world did not produce runtime nodes and dropped items within 30 seconds.");
        }

        private static int Count(EntityManager manager, System.Type component)
        {
            using EntityQuery query = manager.CreateEntityQuery(ComponentType.ReadOnly(component));
            return query.CalculateEntityCount();
        }

        private static int CountWithoutPrefabs(EntityManager manager, System.Type component)
        {
            using EntityQuery query = manager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly(component) },
                None = new[] { ComponentType.ReadOnly<Prefab>() }
            });
            return query.CalculateEntityCount();
        }

        private static void Finish(int exitCode, string message)
        {
            EditorApplication.update -= Poll;
            SessionState.SetBool(SessionKey, false);
            if (exitCode == 0) Debug.Log($"[WorldBuilder] {message}");
            else Debug.LogError($"[WorldBuilder] {message}");
            EditorApplication.Exit(exitCode);
        }
    }
}
#endif
