#if UNITY_EDITOR
using System.Collections.Generic;
using AstraNope.Gameplay.Player;
using AstraNope.Data.Buildings;
using AstraNope.WorldObjects.Entities;
using AstraNope.Services;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace AstraNope.Editor
{
    internal static class SurvivalBuildingPlacementInstaller
    {
        private const string StructureFolder = "Assets/002_Prefabs/Structures";
        private const string MaterialFolder = "Assets/003_Resources/Materials/Building";
        private const string StructurePath = StructureFolder + "/HabitatFoundation.prefab";
        private const string PreviewPath = StructureFolder + "/HabitatFoundationPreview.prefab";
        private const int BlueprintId = 15;

        [InitializeOnLoadMethod]
        private static void InstallWhenReady()
        {
            EditorApplication.delayCall += TryInstallIntoOpenSampleScene;
            EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeChanged;
            EditorSceneManager.activeSceneChangedInEditMode -= HandleActiveSceneChanged;
            EditorSceneManager.activeSceneChangedInEditMode += HandleActiveSceneChanged;
        }

        private static void HandlePlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += TryInstallIntoOpenSampleScene;
        }

        private static void HandleActiveSceneChanged(UnityEngine.SceneManagement.Scene previous,
            UnityEngine.SceneManagement.Scene next) => EditorApplication.delayCall += TryInstallIntoOpenSampleScene;

        private static void TryInstallIntoOpenSampleScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != "Assets/000_Scenes/SampleScene.unity") return;
            if (UnityEngine.Object.FindAnyObjectByType<BuildingPlacementController>(FindObjectsInactive.Include) &&
                UnityEngine.Object.FindAnyObjectByType<BuildToolController>(FindObjectsInactive.Include)) return;
            Install();
        }

        [MenuItem("Tools/Survival/Install Building Placement Prototype")]
        private static void Install()
        {
            GameObject structure = EnsureStructurePrefab(preview: false);
            GameObject preview = EnsureStructurePrefab(preview: true);
            InteractionHandler interaction = UnityEngine.Object.FindAnyObjectByType<InteractionHandler>(FindObjectsInactive.Include);
            if (!interaction)
            {
                Debug.LogError("[Building] InteractionHandler was not found in the open scene.");
                return;
            }

            var controller = interaction.GetComponent<BuildingPlacementController>();
            if (!controller) controller = Undo.AddComponent<BuildingPlacementController>(interaction.gameObject);
            if (!interaction.GetComponent<BuildToolController>())
                Undo.AddComponent<BuildToolController>(interaction.gameObject);
            Transform view = Camera.main ? Camera.main.transform : interaction.transform;
            controller.Configure(view, new List<BuildingDefinition>
            {
                new()
                {
                    blueprintId = BlueprintId,
                    displayName = "Habitat Foundation",
                    structurePrefab = structure,
                    previewPrefab = preview,
                    maxDistance = 8f,
                    gridSize = .25f,
                    rotationStep = 15f,
                    minimumSurfaceUp = .55f,
                    surfaceOffset = .025f,
                    boundsCenter = new Vector3(0f, .15f, 0f),
                    boundsSize = new Vector3(3.8f, .3f, 3.8f)
                }
            });

            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(interaction.gameObject.scene);
            EditorSceneManager.SaveScene(interaction.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Building] Installed the scene-authored placement controller and Habitat Foundation prefabs.");
        }

        [MenuItem("Tools/Survival/Place Build Tool Pickup")]
        private static void PlaceBuildToolPickup()
        {
            InteractionHandler interaction = UnityEngine.Object.FindAnyObjectByType<InteractionHandler>(FindObjectsInactive.Include);
            if (!interaction)
            {
                Debug.LogError("[Building] InteractionHandler was not found in the open scene.");
                return;
            }

            ItemSpawner spawner = UnityEngine.Object.FindAnyObjectByType<ItemSpawner>(FindObjectsInactive.Include);
            var pickupPrefab = spawner
                ? new SerializedObject(spawner).FindProperty("pickupPrefab").objectReferenceValue as GameObject
                : null;
            if (!pickupPrefab)
            {
                Debug.LogError("[Building] ItemSpawner with a pickup prefab was not found in the open scene.");
                return;
            }

            Transform player = interaction.transform;
            var pickup = (GameObject)PrefabUtility.InstantiatePrefab(pickupPrefab, player.gameObject.scene);
            pickup.transform.SetPositionAndRotation(
                player.position + player.forward * 1.5f + Vector3.up * .3f, Quaternion.identity);
            WorldItem worldItem = pickup.GetComponentInChildren<WorldItem>(true);
            if (!worldItem)
            {
                UnityEngine.Object.DestroyImmediate(pickup);
                Debug.LogError("[Building] The pickup prefab does not contain a WorldItem component.");
                return;
            }

            worldItem.Setup(SurvivalPrototypeItemPrefabBuilder.BuildToolItemId, 1);
            pickup.name = "BuildToolPickup";
            EditorUtility.SetDirty(worldItem);
            Undo.RegisterCreatedObjectUndo(pickup, "Place Build Tool Pickup");
            Selection.activeGameObject = pickup;
            EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
            Debug.Log("[Building] Placed a build tool pickup in front of the player.", pickup);
        }

        private static GameObject EnsureStructurePrefab(bool preview)
        {
            EnsureFolder(StructureFolder);
            EnsureFolder(MaterialFolder);
            string path = preview ? PreviewPath : StructurePath;
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing) return existing;

            Material material = EnsureMaterial(preview);
            GameObject root = new(preview ? "HabitatFoundationPreview" : "HabitatFoundation");
            try
            {
                Part(root.transform, "Deck", new Vector3(0f, .12f, 0f), new Vector3(4f, .24f, 4f), material,
                    keepCollider: !preview);
                Part(root.transform, "InnerPlate", new Vector3(0f, .255f, 0f), new Vector3(3.35f, .05f, 3.35f),
                    material, keepCollider: false);
                Part(root.transform, "NorthRail", new Vector3(0f, .38f, 1.82f), new Vector3(3.65f, .28f, .12f),
                    material, keepCollider: !preview);
                Part(root.transform, "SouthRail", new Vector3(0f, .38f, -1.82f), new Vector3(3.65f, .28f, .12f),
                    material, keepCollider: !preview);
                Part(root.transform, "EastRail", new Vector3(1.82f, .38f, 0f), new Vector3(.12f, .28f, 3.65f),
                    material, keepCollider: !preview);
                Part(root.transform, "WestRail", new Vector3(-1.82f, .38f, 0f), new Vector3(.12f, .28f, 3.65f),
                    material, keepCollider: !preview);

                if (!preview)
                {
                    Entity entity = root.AddComponent<Entity>();
                    entity.Configure("habitat_foundation", "Habitat Foundation", EntityKind.Structure);
                    root.AddComponent<AstraNope.WorldObjects.Entities.Structure>();
                }
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void Part(Transform parent, string name, Vector3 position, Vector3 scale,
            Material material, bool keepCollider)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider) UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
        }

        private static Material EnsureMaterial(bool preview)
        {
            string path = MaterialFolder + (preview ? "/BuildingPreview.mat" : "/BuildingStructure.mat");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = preview ? "BuildingPreview" : "BuildingStructure" };
                AssetDatabase.CreateAsset(material, path);
            }

            Color color = preview ? new Color(.12f, 1f, .86f, .42f) : new Color(.16f, .22f, .28f, 1f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", preview ? .1f : .75f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .72f);
            if (preview)
            {
                material.EnableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color * 2.4f);
                if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
                if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/');
            string parent = path[..split];
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path[(split + 1)..]);
        }
    }
}
#endif
