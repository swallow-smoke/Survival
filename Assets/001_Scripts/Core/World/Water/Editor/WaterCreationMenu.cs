#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Splines;

namespace AstraNope.Core.World.Water.Editor
{
    internal static class WaterCreationMenu
    {
        private const string DefaultProfilePath = "Assets/005_Settings/Water/DefaultRiverProfile.asset";
        private const string DefaultRiverMaterialPath = "Assets/003_Resources/Materials/SG_River.mat";

        [MenuItem("GameObject/Survival/Water/Ocean", false, 10)]
        private static void CreateOcean(MenuCommand command)
        {
            GameObject gameObject = CreateRoot("Ocean", command);
            OceanBody ocean = gameObject.AddComponent<OceanBody>();
            MeshRenderer renderer = CreateSurface(gameObject.transform, new Vector3(10f, 1f, 10f));
            SerializedObject serialized = new SerializedObject(ocean);
            serialized.FindProperty("surfaceRenderer").objectReferenceValue = renderer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Selection.activeGameObject = gameObject;
        }

        [MenuItem("GameObject/Survival/Water/Lake", false, 11)]
        private static void CreateLake(MenuCommand command)
        {
            GameObject gameObject = CreateRoot("Lake", command);
            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(20f, 5f, 20f);
            LakeWaterBody lake = gameObject.AddComponent<LakeWaterBody>();
            MeshRenderer renderer = CreateSurface(gameObject.transform, new Vector3(2f, 1f, 2f));
            SerializedObject serialized = new SerializedObject(lake);
            serialized.FindProperty("surface").objectReferenceValue = renderer.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Selection.activeGameObject = gameObject;
        }

        [MenuItem("GameObject/Survival/Water/Spline River", false, 12)]
        private static void CreateSplineRiver(MenuCommand command)
        {
            GameObject gameObject = CreateRoot("Spline River", command);
            gameObject.AddComponent<SplineContainer>();
            gameObject.AddComponent<MeshFilter>();
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            Material material = AssetDatabase.LoadAssetAtPath<Material>(DefaultRiverMaterialPath);
            if (material != null) renderer.sharedMaterial = material;
            SplineRiverWaterBody river = gameObject.AddComponent<SplineRiverWaterBody>();
            gameObject.AddComponent<RiverTerrainCarver>();
            RiverProfile profile = EnsureDefaultProfile(material);
            SerializedObject riverObject = new SerializedObject(river);
            riverObject.FindProperty("profile").objectReferenceValue = profile;
            riverObject.ApplyModifiedPropertiesWithoutUndo();
            river.Rebuild();
            Selection.activeGameObject = gameObject;
        }

        [MenuItem("Tools/Survival/Water/Create Validation Setup")]
        private static void CreateValidationSetup()
        {
            GameObject root = new GameObject("Water Validation Setup");
            Undo.RegisterCreatedObjectUndo(root, "Create Water Validation Setup");
            WaterQueryService service = new GameObject("Water Query Service").AddComponent<WaterQueryService>();
            Undo.RegisterCreatedObjectUndo(service.gameObject, "Create Water Query Service");
            service.transform.SetParent(root.transform);

            CreateOcean(new MenuCommand(root));
            GameObject ocean = Selection.activeGameObject;
            ocean.transform.position = new Vector3(0f, 0f, 0f);
            CreateLake(new MenuCommand(root));
            GameObject lake = Selection.activeGameObject;
            lake.transform.position = new Vector3(25f, 1f, 0f);
            CreateSplineRiver(new MenuCommand(root));
            GameObject river = Selection.activeGameObject;
            river.transform.position = new Vector3(-25f, 1f, 0f);

            GameObject buoyant = GameObject.CreatePrimitive(PrimitiveType.Cube);
            buoyant.name = "Buoyant Test Cube";
            buoyant.transform.SetParent(root.transform);
            buoyant.transform.position = new Vector3(0f, 3f, 0f);
            buoyant.AddComponent<Rigidbody>().mass = 500f;
            buoyant.AddComponent<AstraNope.WorldObjects.Vehicles.BuoyancyController>();
            buoyant.AddComponent<WaterDebugProbe>();
            Undo.RegisterCreatedObjectUndo(buoyant, "Create Buoyant Test Cube");

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(root.scene);
        }

        private static GameObject CreateRoot(string name, MenuCommand command)
        {
            GameObject gameObject = new GameObject(name);
            GameObjectUtility.SetParentAndAlign(gameObject, command.context as GameObject);
            Undo.RegisterCreatedObjectUndo(gameObject, $"Create {name}");
            StageUtility.PlaceGameObjectInCurrentStage(gameObject);
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            return gameObject;
        }

        private static MeshRenderer CreateSurface(Transform parent, Vector3 scale)
        {
            GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Plane);
            surface.name = "Surface";
            surface.layer = LayerMask.NameToLayer("Water");
            surface.transform.SetParent(parent, false);
            surface.transform.localPosition = Vector3.zero;
            surface.transform.localScale = scale;
            Collider collider = surface.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            MeshRenderer renderer = surface.GetComponent<MeshRenderer>();
            Material material = AssetDatabase.LoadAssetAtPath<Material>(DefaultRiverMaterialPath);
            if (material != null) renderer.sharedMaterial = material;
            return renderer;
        }

        private static RiverProfile EnsureDefaultProfile(Material material)
        {
            RiverProfile profile = AssetDatabase.LoadAssetAtPath<RiverProfile>(DefaultProfilePath);
            if (profile != null) return profile;

            string directory = Path.GetDirectoryName(DefaultProfilePath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
                CreateFolders(directory);
            profile = ScriptableObject.CreateInstance<RiverProfile>();
            AssetDatabase.CreateAsset(profile, DefaultProfilePath);
            if (material != null)
            {
                SerializedObject profileObject = new SerializedObject(profile);
                profileObject.FindProperty("material").objectReferenceValue = material;
                profileObject.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static void CreateFolders(string path)
        {
            string[] segments = path.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, segments[i]);
                current = next;
            }
        }
    }
}
#endif
