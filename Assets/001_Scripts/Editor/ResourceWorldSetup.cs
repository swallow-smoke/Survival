#if UNITY_EDITOR
using System;
using System.Reflection;
using _001_Scripts.Controller;
using _001_Scripts.Core;
using _001_Scripts.Data.Item;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Scenes;
using WorldBuilder.Entities;
using WorldBuilder.Entities.Authoring;
using WorldBuilder.Entities.Resources;
using WorldBuilder.Entities.Resources.Authoring;
using WorldBuilder.Runtime.Grid;

namespace _001_Scripts.Editor
{
    public static class ResourceWorldSetup
    {
        private const string GridPath = "Assets/WorldBuilder/WorldGridSettings.asset";
        private const string PrefabFolder = "Assets/WorldBuilder/Resources/Prefabs";
        private const string EntityScenePath = "Assets/000_Scenes/WorldEntities.unity";
        private const string MainScenePath = "Assets/000_Scenes/SampleScene.unity";
        private const string ToolCatalogPath = "Assets/003_Resources/Data/HarvestToolCatalog.asset";

        [MenuItem("Tools/WorldBuilder/Resources/Create Survival Resource World")]
        public static void CreateSurvivalResourceWorld()
        {
            EnsureFolder("Assets/WorldBuilder");
            EnsureFolder("Assets/WorldBuilder/Resources");
            EnsureFolder(PrefabFolder);

            WorldGridSettings grid = LoadOrCreateGrid();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            grid = AssetDatabase.LoadAssetAtPath<WorldGridSettings>(GridPath);
            HarvestToolCatalog toolCatalog = AssetDatabase.LoadAssetAtPath<HarvestToolCatalog>(ToolCatalogPath);
            if (toolCatalog == null)
                throw new InvalidOperationException($"Missing harvest tool catalog at {ToolCatalogPath}.");
            GameObject droppedItem = CreateDroppedItemPrefab();
            GameObject tree = CreateResourcePrefab("TreeResource", 1100, PrimitiveType.Capsule,
                new Vector3(1.2f, 3f, 1.2f), new Color(0.25f, 0.55f, 0.2f), "Tree",
                60f, HarvestMethod.Hand | HarvestMethod.Axe, -1, 0, 1f, 3, 2, 4, 60f);
            GameObject ore = CreateResourcePrefab("CopperOreResource", 1200, PrimitiveType.Cube,
                new Vector3(1.8f, 1.3f, 1.6f), new Color(0.45f, 0.65f, 0.7f), "Copper Ore",
                90f, HarvestMethod.Pickaxe | HarvestMethod.Drill, -1, 1, 1.25f, 4, 1, 3, 90f);
            GameObject reinforced = CreateResourcePrefab("ReinforcedOreResource", 1300, PrimitiveType.Cube,
                new Vector3(2.2f, 1.7f, 2f), new Color(0.2f, 0.75f, 0.9f), "Reinforced Deposit",
                180f, HarvestMethod.Drill, 7, 3, 3f, 4, 3, 6, 180f);

            CreateEntityScene(grid, droppedItem, tree, ore, reinforced);
            AttachSubSceneAndRegionFocus(grid, toolCatalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[WorldBuilder] Survival resource world created and linked to SampleScene.");
        }

        private static WorldGridSettings LoadOrCreateGrid()
        {
            WorldGridSettings grid = AssetDatabase.LoadAssetAtPath<WorldGridSettings>(GridPath);
            if (grid != null) return grid;
            grid = ScriptableObject.CreateInstance<WorldGridSettings>();
            grid.SetWorldId("SurvivalWorld");
            grid.Configure(128f, 4, 32f, Vector3.zero);
            AssetDatabase.CreateAsset(grid, GridPath);
            return grid;
        }

        private static GameObject CreateDroppedItemPrefab()
        {
            string path = $"{PrefabFolder}/DroppedItem.prefab";
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = "DroppedItem";
            root.transform.localScale = Vector3.one * 0.35f;
            SetColor(root, new Color(0.95f, 0.75f, 0.2f));
            WorldEntityAuthoring entity = root.AddComponent<WorldEntityAuthoring>();
            ConfigureEntity(entity, 1000, WorldEntityKind.DroppedItem);
            DroppedItemAuthoring item = root.AddComponent<DroppedItemAuthoring>();
            Set(item, "itemId", 0);
            Set(item, "count", 1);
            Set(item, "displayName", "Dropped Item");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateResourcePrefab(string name, int prefabId, PrimitiveType primitive,
            Vector3 scale, Color color, string displayName, float health, HarvestMethod methods,
            int requiredToolId, int minimumTier, float minimumPower, int itemId,
            int minimumDrop, int maximumDrop, float respawnSeconds)
        {
            string path = $"{PrefabFolder}/{name}.prefab";
            GameObject root = GameObject.CreatePrimitive(primitive);
            root.name = name;
            root.transform.localScale = scale;
            SetColor(root, color);
            WorldEntityAuthoring entity = root.AddComponent<WorldEntityAuthoring>();
            ConfigureEntity(entity, prefabId, WorldEntityKind.Resource);
            ResourceNodeAuthoring resource = root.AddComponent<ResourceNodeAuthoring>();
            Set(resource, "displayName", displayName);
            Set(resource, "health", health);
            Set(resource, "hitCooldownSeconds", 0.25f);
            Set(resource, "respawnSeconds", respawnSeconds);
            Set(resource, "allowedMethods", (int)methods);
            Set(resource, "requiredToolItemId", requiredToolId);
            Set(resource, "minimumToolTier", minimumTier);
            Set(resource, "minimumToolPower", minimumPower);
            Set(resource, "droppedItemPrefabId", 1000);
            Set(resource, "randomSeed", (long)(uint)prefabId);

            SerializedObject serialized = new SerializedObject(resource);
            SerializedProperty drops = serialized.FindProperty("drops");
            drops.arraySize = 1;
            SerializedProperty drop = drops.GetArrayElementAtIndex(0);
            drop.FindPropertyRelative("ItemId").intValue = itemId;
            drop.FindPropertyRelative("MinimumCount").intValue = minimumDrop;
            drop.FindPropertyRelative("MaximumCount").intValue = maximumDrop;
            drop.FindPropertyRelative("Probability").floatValue = 1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CreateEntityScene(WorldGridSettings grid, params GameObject[] prefabs)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject runtimeObject = new GameObject("WorldEntityRuntime");
            WorldEntityRuntimeAuthoring runtime = runtimeObject.AddComponent<WorldEntityRuntimeAuthoring>();
            Set(runtime, "gridSettings", grid);
            SerializedObject serialized = new SerializedObject(runtime);
            SerializedProperty entries = serialized.FindProperty("prefabs");
            entries.arraySize = prefabs.Length;
            int[] ids = { 1000, 1100, 1200, 1300 };
            for (int i = 0; i < prefabs.Length; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("PrefabId").intValue = ids[i];
                entry.FindPropertyRelative("Prefab").objectReferenceValue = prefabs[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "ResourceSpawnSurface";
            ground.transform.SetPositionAndRotation(new Vector3(0f, -0.5f, 0f), Quaternion.identity);
            ground.transform.localScale = new Vector3(100f, 1f, 100f);
            UnityEngine.Object.DestroyImmediate(ground.GetComponent<MeshRenderer>());
            UnityEngine.Object.DestroyImmediate(ground.GetComponent<MeshFilter>());

            CreateZone("LooseItems", ResourceFieldSpawnKind.DroppedItem, 1000, 0,
                new Vector3(0f, 1f, 0f), new Vector3(35f, 8f, 35f), 20, 2, 1f);
            CreateZone("Trees", ResourceFieldSpawnKind.ResourceNode, 1100, 0,
                new Vector3(0f, 1f, 0f), new Vector3(65f, 8f, 65f), 18, 2, 2f);
            CreateZone("CopperOre", ResourceFieldSpawnKind.ResourceNode, 1200, 0,
                new Vector3(15f, 1f, 10f), new Vector3(45f, 8f, 45f), 10, 1, 3f);
            CreateZone("ReinforcedOre", ResourceFieldSpawnKind.ResourceNode, 1300, 0,
                new Vector3(-20f, 1f, -10f), new Vector3(30f, 8f, 30f), 4, 1, 8f);
            EditorSceneManager.SaveScene(scene, EntityScenePath);
        }

        private static void CreateZone(string name, ResourceFieldSpawnKind kind, int prefabId, int itemId,
            Vector3 position, Vector3 size, int maximumAlive, int spawnPerTick, float interval)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.position = position;
            ResourceFieldSpawnZoneAuthoring zone = gameObject.AddComponent<ResourceFieldSpawnZoneAuthoring>();
            Set(zone, "kind", (int)kind);
            Set(zone, "prefabId", prefabId);
            Set(zone, "itemId", itemId);
            Set(zone, "minimumItemCount", 1);
            Set(zone, "maximumItemCount", 3);
            Set(zone, "size", size);
            Set(zone, "raycastHeight", 10f);
            Set(zone, "spawnInterval", interval);
            Set(zone, "maximumAlive", maximumAlive);
            Set(zone, "spawnPerTick", spawnPerTick);
            Set(zone, "randomSeed", (long)(uint)prefabId);
        }

        private static void AttachSubSceneAndRegionFocus(WorldGridSettings grid, HarvestToolCatalog toolCatalog)
        {
            Scene main = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            GameObject subSceneObject = GameObject.Find("WorldEntities_SubScene") ?? new GameObject("WorldEntities_SubScene");
            SubScene subScene = subSceneObject.GetComponent<SubScene>() ?? subSceneObject.AddComponent<SubScene>();
            subScene.SceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(EntityScenePath);
            subScene.AutoLoadScene = true;

            GLifeTimeScope lifetimeScope = UnityEngine.Object.FindFirstObjectByType<GLifeTimeScope>();
            if (lifetimeScope == null)
                throw new InvalidOperationException("SampleScene requires GLifeTimeScope.");
            Set(lifetimeScope, "harvestToolCatalog", toolCatalog);

            PlayerController player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                WorldEntityRegionFocus focus = player.GetComponent<WorldEntityRegionFocus>() ??
                                               player.gameObject.AddComponent<WorldEntityRegionFocus>();
                Set(focus, "gridSettings", grid);
                Set(focus, "focus", player.transform);
                Set(focus, "regionRadius", 1);
            }
            else
            {
                Debug.LogWarning("[WorldBuilder] PlayerController was not found; add WorldEntityRegionFocus manually.");
            }
            EditorSceneManager.MarkSceneDirty(main);
            EditorSceneManager.SaveScene(main);
        }

        private static void ConfigureEntity(WorldEntityAuthoring authoring, int prefabId, WorldEntityKind kind)
        {
            Set(authoring, "prefabId", prefabId);
            Set(authoring, "kind", (int)kind);
            Set(authoring, "flags", (int)WorldEntityFlags.RegionStreamed);
            Set(authoring, "trackChunk", true);
        }

        private static void SetColor(GameObject gameObject, Color color)
        {
            Renderer renderer = gameObject.GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader) { color = color };
            string path = $"Assets/WorldBuilder/Resources/{gameObject.name}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing == null) AssetDatabase.CreateAsset(material, path);
            else
            {
                existing.shader = material.shader;
                existing.color = color;
                UnityEngine.Object.DestroyImmediate(material);
                material = existing;
            }
            renderer.sharedMaterial = material;
        }

        private static void Set(UnityEngine.Object target, string propertyName, object value)
        {
            if (value is UnityEngine.Object reference)
            {
                FieldInfo field = target.GetType().GetField(propertyName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                                  throw new InvalidOperationException($"Missing field {propertyName} on {target.GetType().Name}.");
                field.SetValue(target, reference);
                EditorUtility.SetDirty(target);
                return;
            }

            SerializedObject serialized = new SerializedObject(target);
            serialized.Update();
            SerializedProperty property = serialized.FindProperty(propertyName) ??
                                          throw new InvalidOperationException($"Missing property {propertyName} on {target.GetType().Name}.");
            switch (value)
            {
                case int integer: property.intValue = integer; break;
                case long longValue: property.longValue = longValue; break;
                case float number: property.floatValue = number; break;
                case bool boolean: property.boolValue = boolean; break;
                case string text: property.stringValue = text; break;
                case Vector3 vector: property.vector3Value = vector; break;
                default: throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int separator = path.LastIndexOf('/');
            string parent = path.Substring(0, separator);
            string name = path.Substring(separator + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
