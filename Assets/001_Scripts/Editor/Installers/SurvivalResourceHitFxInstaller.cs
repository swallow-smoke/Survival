#if UNITY_EDITOR
using System.Collections.Generic;
using AstraNope.Gameplay.Player;
using AstraNope.Gameplay.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AstraNope.Editor
{
    [InitializeOnLoad]
    internal static class SurvivalResourceHitFxInstaller
    {
        private const string RootName = "ResourceHitParticles_v1";
        private const string MaterialPath = "Assets/003_Resources/Materials/ResourceHitParticles.mat";
        private const int PoolSize = 8;

        static SurvivalResourceHitFxInstaller() => EditorApplication.delayCall += InstallIfNeeded;

        [MenuItem("Tools/Survival/Install Scene-Authored Resource Hit FX")]
        private static void InstallMenu() => Install(forceRebuild: true);

        private static void InstallIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.name != "SampleScene") return;
            Install(forceRebuild: false);
        }

        private static void Install(bool forceRebuild)
        {
            InteractionHandler handler = UnityEngine.Object.FindFirstObjectByType<InteractionHandler>(
                FindObjectsInactive.Include);
            if (!handler)
            {
                Debug.LogWarning("[Survival] InteractionHandler was not found; resource hit FX was not installed.");
                return;
            }

            Transform existing = handler.transform.Find(RootName);
            if (existing && !forceRebuild)
            {
                Material material = EnsureMaterial();
                foreach (ParticleSystem system in existing.GetComponentsInChildren<ParticleSystem>(true))
                    system.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
                Assign(handler, existing.GetComponent<ResourceHitParticlePool>());
                EditorSceneManager.MarkSceneDirty(handler.gameObject.scene);
                EditorSceneManager.SaveScene(handler.gameObject.scene);
                return;
            }
            if (existing) Undo.DestroyObjectImmediate(existing.gameObject);

            GameObject root = new GameObject(RootName, typeof(ResourceHitParticlePool));
            Undo.RegisterCreatedObjectUndo(root, "Install Resource Hit FX");
            root.transform.SetParent(handler.transform, false);
            root.transform.localPosition = Vector3.zero;

            Material particleMaterial = EnsureMaterial();
            var particles = new List<ParticleSystem>(PoolSize);
            for (int i = 0; i < PoolSize; i++)
            {
                GameObject child = new GameObject($"HitSpark_{i:00}", typeof(ParticleSystem));
                child.transform.SetParent(root.transform, false);
                ParticleSystem system = child.GetComponent<ParticleSystem>();
                Configure(system, particleMaterial);
                particles.Add(system);
            }

            ResourceHitParticlePool pool = root.GetComponent<ResourceHitParticlePool>();
            SerializedObject poolSerialized = new SerializedObject(pool);
            SerializedProperty list = poolSerialized.FindProperty("particles");
            list.arraySize = particles.Count;
            for (int i = 0; i < particles.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = particles[i];
            poolSerialized.ApplyModifiedPropertiesWithoutUndo();
            Assign(handler, pool);

            EditorUtility.SetDirty(handler);
            EditorUtility.SetDirty(pool);
            EditorSceneManager.MarkSceneDirty(handler.gameObject.scene);
            EditorSceneManager.SaveScene(handler.gameObject.scene);
            Debug.Log("[Survival] Installed editable scene-authored resource hit particle pool.");
        }

        private static void Assign(InteractionHandler handler, ResourceHitParticlePool pool)
        {
            if (!pool) return;
            SerializedObject serialized = new SerializedObject(handler);
            SerializedProperty property = serialized.FindProperty("resourceHitParticles");
            if (property == null || property.objectReferenceValue == pool) return;
            property.objectReferenceValue = pool;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(handler);
            EditorUtility.SetDirty(handler);
            EditorSceneManager.MarkSceneDirty(handler.gameObject.scene);
            EditorSceneManager.SaveScene(handler.gameObject.scene);
        }

        private static void Configure(ParticleSystem system, Material material)
        {
            var main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = .28f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.18f, .38f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 3.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(.035f, .11f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(.44f, .82f, 1f, .95f), new Color(1f, .73f, .34f, .95f));
            main.maxParticles = 24;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.None;

            var emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 9, 14) });

            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = .12f;

            var color = system.colorOverLifetime;
            color.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(.28f, .65f, 1f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;

            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 30;
            renderer.sharedMaterial = material;
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private static Material EnsureMaterial()
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing) return existing;
            const string folder = "Assets/003_Resources/Materials";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/003_Resources", "Materials");
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                            Shader.Find("Particles/Standard Unlit") ??
                            Shader.Find("Sprites/Default");
            Material material = new Material(shader) { name = "ResourceHitParticles" };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }
    }
}
#endif
