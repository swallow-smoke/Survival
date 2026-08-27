#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AstraNope.Core.World.Entities.Creatures.AI;
using AstraNope.Data.Creatures;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WorldBuilder.Entities;
using WorldBuilder.Entities.Authoring;
using WorldBuilder.Entities.Creatures;
using WorldBuilder.Entities.Creatures.Authoring;
using WorldBuilder.Runtime.Grid;

namespace AstraNope.Editor
{
    /// <summary>
    /// Converts every FBX in the ray model folder into a DOTS creature prefab, registers it in the
    /// existing WorldEntity catalog and creates idempotent spawn zones in WorldEntities.unity.
    /// Re-run after adding a model; existing non-ray catalog entries are preserved.
    /// </summary>
    public static class RayCreatureEcsInstaller
    {
        private const string ModelFolder = "Assets/003_Resources/Models";
        private const string CatalogFolder = "Assets/003_Resources/Data/Creatures";
        private const string CatalogPath = CatalogFolder + "/RaySpeciesCatalog.asset";
        private const string PrefabFolder = "Assets/WorldBuilder/Creatures/Rays";
        private const string EntityScenePath = "Assets/000_Scenes/WorldEntities.unity";
        private const string SpawnRootName = "RayCreatureSpawnZones";
        private const int FirstPrefabId = 2100;
        private const string GridPath = "Assets/WorldBuilder/WorldGridSettings.asset";
        private const string AutoInstallSessionKey = "Survival.RayCreatureEcsInstaller.AutoInstall.v2";

        [InitializeOnLoadMethod]
        private static void ScheduleInitialInstall()
        {
            if (SessionState.GetBool(AutoInstallSessionKey, false)) return;
            SessionState.SetBool(AutoInstallSessionKey, true);
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    Debug.LogWarning("[Survival] Ray ECS installation was deferred because the Editor is entering Play Mode. " +
                                     "Run Tools/Survival/Creatures/Install Ray ECS Creatures after leaving Play Mode.");
                    return;
                }

                try
                {
                    Install();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            };
        }

        [MenuItem("Tools/Survival/Creatures/Install Ray ECS Creatures")]
        public static void Install()
        {
            EnsureFolder(CatalogFolder);
            EnsureFolder(PrefabFolder);

            RaySpeciesCatalog catalog = LoadOrCreateCatalog();
            RefreshCatalogModels(catalog);
            AssetDatabase.SaveAssets();
            List<RayPrefabRecord> rayPrefabs = BuildPrefabs(catalog);
            if (rayPrefabs.Count == 0)
                throw new InvalidOperationException($"No ray FBX models were found under '{ModelFolder}'.");

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene entityScene = EditorSceneManager.OpenScene(EntityScenePath, OpenSceneMode.Single);
                WorldEntityRuntimeAuthoring runtime = GetOrRepairRuntime();

                EnsureCreatureRuntime(runtime.gameObject);
                RegisterPrefabs(runtime, rayPrefabs);
                EnsureRaySpawnZones(runtime.gameObject, catalog);
                EditorSceneManager.MarkSceneDirty(entityScene);
                EditorSceneManager.SaveScene(entityScene);
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Survival] Installed {rayPrefabs.Count} ray species into the WorldBuilder ECS creature pipeline.");
        }

        private static RaySpeciesCatalog LoadOrCreateCatalog()
        {
            RaySpeciesCatalog catalog = AssetDatabase.LoadAssetAtPath<RaySpeciesCatalog>(CatalogPath);
            if (catalog != null) return catalog;
            catalog = ScriptableObject.CreateInstance<RaySpeciesCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            return catalog;
        }

        private static void RefreshCatalogModels(RaySpeciesCatalog catalog)
        {
            string[] modelPaths = AssetDatabase.FindAssets("t:Model", new[] { ModelFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => string.Equals(Path.GetExtension(path), ".fbx", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            HashSet<GameObject> registered = new HashSet<GameObject>(
                catalog.MutableSpecies.Where(value => value != null && value.Model != null).Select(value => value.Model));
            HashSet<int> usedIds = new HashSet<int>(
                catalog.MutableSpecies.Where(value => value != null).Select(value => value.PrefabId));
            int nextId = FirstPrefabId;

            foreach (string modelPath in modelPaths)
            {
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (model == null || registered.Contains(model)) continue;
                while (usedIds.Contains(nextId)) nextId++;

                string modelName = Path.GetFileNameWithoutExtension(modelPath);
                RaySpeciesDefinition definition = new RaySpeciesDefinition();
                definition.Configure(Humanize(modelName), nextId, model,
                    DefaultSpawnCenter(catalog.MutableSpecies.Count), DefaultSpeed(modelName), DefaultScale(modelName));
                catalog.MutableSpecies.Add(definition);
                registered.Add(model);
                usedIds.Add(nextId);
                nextId++;
            }

            EditorUtility.SetDirty(catalog);
        }

        private static List<RayPrefabRecord> BuildPrefabs(RaySpeciesCatalog catalog)
        {
            List<RayPrefabRecord> result = new List<RayPrefabRecord>();
            HashSet<int> ids = new HashSet<int>();
            foreach (RaySpeciesDefinition definition in catalog.Species)
            {
                if (definition == null || definition.Model == null) continue;
                if (!ids.Add(definition.PrefabId))
                    throw new InvalidOperationException($"Duplicate ray prefab id {definition.PrefabId} in {CatalogPath}.");

                GameObject prefab = BuildPrefab(definition);
                result.Add(new RayPrefabRecord(definition, prefab));
            }
            return result;
        }

        private static GameObject BuildPrefab(RaySpeciesDefinition definition)
        {
            string safeName = SanitizeFileName(definition.DisplayName.Replace(" ", string.Empty));
            string path = $"{PrefabFolder}/{definition.PrefabId}_{safeName}.prefab";
            GameObject root = new GameObject($"Ray_{safeName}");
            try
            {
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(definition.Model);
                visual.name = "Visual";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one * definition.ModelScale;

                WorldEntityAuthoring worldEntity = root.AddComponent<WorldEntityAuthoring>();
                Set(worldEntity, "prefabId", definition.PrefabId);
                Set(worldEntity, "kind", (int)WorldEntityKind.Creature);
                Set(worldEntity, "flags", (int)WorldEntityFlags.RegionStreamed);
                Set(worldEntity, "trackChunk", true);

                CreatureAuthoring creature = root.AddComponent<CreatureAuthoring>();
                Set(creature, "displayName", definition.DisplayName);
                Set(creature, "sizeClass", (int)CreatureSizeClass.Medium);
                Set(creature, "personality", (int)CreaturePersonality.Wary);
                Set(creature, "interactions", (int)CreatureInteractionMask.Scan);
                Set(creature, "randomSeed", (long)(uint)definition.PrefabId);
                Set(creature, "cruiseSpeed", definition.CruiseSpeed);
                Set(creature, "turnSpeedDegrees", definition.TurnSpeedDegrees);
                Set(creature, "wanderRadius", definition.WanderRadius);
                Set(creature, "verticalRadius", definition.VerticalRadius);
                Set(creature, "arriveRadius", 1.25f);
                Set(creature, "repathIntervalSeconds", 5f);
                Set(creature, "fleeDistanceOverride", definition.FleeDistance);
                Set(creature, "leashToHomeRegion", true);
                Set(creature, "regionMargin", 3f);
                Set(creature, "despawnGraceSeconds", 12f);

                RayAIAuthoring ray = root.AddComponent<RayAIAuthoring>();
                Set(ray, "maximumBankDegrees", definition.MaximumBankDegrees);
                Set(ray, "bankResponsiveness", definition.BankResponsiveness);
                AddInteractionCollider(root);

                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void AddInteractionCollider(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = root.transform.InverseTransformPoint(bounds.center);
            Vector3 size = root.transform.InverseTransformVector(bounds.size);
            collider.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
        }

        private static void EnsureCreatureRuntime(GameObject runtimeObject)
        {
            if (runtimeObject.GetComponent<CreatureRuntimeAuthoring>() == null)
                runtimeObject.AddComponent<CreatureRuntimeAuthoring>();
        }

        private static void EnsureRaySpawnZones(GameObject runtimeObject, RaySpeciesCatalog catalog)
        {
            RaySpawnZonesAuthoring authoring = runtimeObject.GetComponent<RaySpawnZonesAuthoring>() ??
                                                runtimeObject.AddComponent<RaySpawnZonesAuthoring>();
            SetObjectReference(authoring, "catalog", catalog);
        }

        private static WorldEntityRuntimeAuthoring GetOrRepairRuntime()
        {
            WorldEntityRuntimeAuthoring runtime = UnityEngine.Object.FindFirstObjectByType<WorldEntityRuntimeAuthoring>();
            if (runtime != null) return runtime;

            GameObject runtimeObject = GameObject.Find("WorldEntityRuntime") ?? new GameObject("WorldEntityRuntime");
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(runtimeObject) > 0)
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(runtimeObject);
            runtime = runtimeObject.AddComponent<WorldEntityRuntimeAuthoring>();
            WorldGridSettings grid = AssetDatabase.LoadAssetAtPath<WorldGridSettings>(GridPath);
            if (grid == null) throw new InvalidOperationException($"Missing WorldGridSettings at '{GridPath}'.");
            SetObjectReference(runtime, "gridSettings", grid);
            return runtime;
        }

        private static void RegisterPrefabs(WorldEntityRuntimeAuthoring runtime, List<RayPrefabRecord> rays)
        {
            SerializedObject serialized = new SerializedObject(runtime);
            SerializedProperty entries = serialized.FindProperty("prefabs");
            HashSet<int> rayIds = new HashSet<int>(rays.Select(value => value.Definition.PrefabId));
            Dictionary<int, GameObject> preserved = CollectWorldEntityPrefabs(rayIds);
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                int id = entry.FindPropertyRelative("PrefabId").intValue;
                GameObject prefab = (GameObject)entry.FindPropertyRelative("Prefab").objectReferenceValue;
                if (!rayIds.Contains(id) && prefab != null) preserved[id] = prefab;
            }

            entries.arraySize = preserved.Count + rays.Count;
            int index = 0;
            foreach (KeyValuePair<int, GameObject> value in preserved.OrderBy(value => value.Key))
                WriteEntry(entries.GetArrayElementAtIndex(index++), value.Key, value.Value);
            foreach (RayPrefabRecord value in rays)
                WriteEntry(entries.GetArrayElementAtIndex(index++), value.Definition.PrefabId, value.Prefab);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(runtime);
        }

        private static Dictionary<int, GameObject> CollectWorldEntityPrefabs(HashSet<int> excludedIds)
        {
            Dictionary<int, GameObject> result = new Dictionary<int, GameObject>();
            string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/WorldBuilder" })
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            foreach (string path in prefabPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                WorldEntityAuthoring authoring = prefab != null ? prefab.GetComponent<WorldEntityAuthoring>() : null;
                if (authoring == null || excludedIds.Contains(authoring.PrefabId)) continue;
                if (result.TryGetValue(authoring.PrefabId, out GameObject duplicate) && duplicate != prefab)
                    throw new InvalidOperationException($"Duplicate WorldEntity prefab id {authoring.PrefabId}: " +
                                                        $"'{AssetDatabase.GetAssetPath(duplicate)}' and '{path}'.");
                result[authoring.PrefabId] = prefab;
            }
            return result;
        }

        private static void RebuildSpawnZones(List<RayPrefabRecord> rays)
        {
            GameObject existing = GameObject.Find(SpawnRootName);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
            GameObject root = new GameObject(SpawnRootName);
            foreach (RayPrefabRecord record in rays)
            {
                RaySpeciesDefinition definition = record.Definition;
                GameObject zoneObject = new GameObject($"{definition.PrefabId}_{definition.DisplayName}");
                zoneObject.transform.SetParent(root.transform, false);
                zoneObject.transform.position = definition.SpawnCenter;
                CreatureSpawnZoneAuthoring zone = zoneObject.AddComponent<CreatureSpawnZoneAuthoring>();
                Set(zone, "prefabId", definition.PrefabId);
                Set(zone, "size", definition.SpawnVolume);
                Set(zone, "allowedGrades", (int)CreatureGradeMask.All);
                Set(zone, "spawnInterval", definition.SpawnInterval);
                Set(zone, "maximumAlive", definition.MaximumAlive);
                Set(zone, "spawnPerTick", definition.SpawnPerTick);
                Set(zone, "randomSeed", (long)(uint)definition.PrefabId);
            }
        }

        private static void WriteEntry(SerializedProperty entry, int id, GameObject prefab)
        {
            entry.FindPropertyRelative("PrefabId").intValue = id;
            entry.FindPropertyRelative("Prefab").objectReferenceValue = prefab;
        }

        private static void Set(UnityEngine.Object target, string propertyName, object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.Update();
            SerializedProperty property = serialized.FindProperty(propertyName) ??
                                          throw new InvalidOperationException($"Missing property '{propertyName}' on {target.GetType().Name}.");
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

        private static void SetObjectReference(UnityEngine.Object target, string propertyName,
            UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.Update();
            SerializedProperty property = serialized.FindProperty(propertyName) ??
                                          throw new InvalidOperationException($"Missing property '{propertyName}' on {target.GetType().Name}.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static Vector3 DefaultSpawnCenter(int index)
        {
            float angle = index * Mathf.PI * 2f / 5f;
            return new Vector3(Mathf.Cos(angle) * 18f, -8f, Mathf.Sin(angle) * 18f);
        }

        private static float DefaultSpeed(string modelName)
            => modelName.IndexOf("king", StringComparison.OrdinalIgnoreCase) >= 0 ? 1.4f :
               modelName.IndexOf("electric", StringComparison.OrdinalIgnoreCase) >= 0 ? 2.8f : 2f;

        private static float DefaultScale(string modelName)
            => modelName.IndexOf("king", StringComparison.OrdinalIgnoreCase) >= 0 ? 1.25f : 1f;

        private static string Humanize(string value)
            => string.Join(" ", value.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(word => char.ToUpperInvariant(word[0]) + word.Substring(1)));

        private static string SanitizeFileName(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
            return value;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int separator = path.LastIndexOf('/');
            string parent = path.Substring(0, separator);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(separator + 1));
        }

        private readonly struct RayPrefabRecord
        {
            public readonly RaySpeciesDefinition Definition;
            public readonly GameObject Prefab;

            public RayPrefabRecord(RaySpeciesDefinition definition, GameObject prefab)
            {
                Definition = definition;
                Prefab = prefab;
            }
        }
    }
}
#endif
