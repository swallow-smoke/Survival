#if UNITY_EDITOR
using _001_Scripts.Controller;
using _001_Scripts.Entities;
using _001_Scripts.Data.SOJ;
using _001_Scripts.Managers;
using _001_Scripts.Structure;
using _001_Scripts.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace _001_Scripts.Editor
{
    public static class SurvivalWorkbenchInstaller
    {
        private const string StationName = "WorkbenchStation";
        private const string PanelName = "WorkbenchPanel";
        private const string MaterialFolder = "Assets/003_Resources/Materials/Workbench";

        [InitializeOnLoadMethod]
        private static void InstallIntoSampleSceneOnce()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                var scene = EditorSceneManager.GetActiveScene();
                if (!scene.IsValid() || scene.path != "Assets/000_Scenes/SampleScene.unity") return;
                var existing = UnityEngine.Object.FindAnyObjectByType<WorkbenchPanel>(FindObjectsInactive.Include);
                if (existing && existing.VisualVersion == WorkbenchPanel.CurrentVisualVersion &&
                    existing.GetComponent<CanvasGroup>() is { alpha: <= .001f, interactable: false, blocksRaycasts: false })
                    return;
                InstallWorkbenchExperience();
            };
        }

        [MenuItem("Tools/Survival/Install Workbench Experience")]
        public static void InstallWorkbenchExperience()
        {
            var uiManager = UnityEngine.Object.FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
            if (!uiManager)
            {
                Debug.LogError("[Workbench] UIManager is required in the open scene.");
                return;
            }

            var panel = EnsurePanel(uiManager);
            EnsureStation();
            uiManager.uiPanels["Workbench"] = panel;
            EditorUtility.SetDirty(uiManager);
            EditorUtility.SetDirty(panel);
            EditorSceneManager.MarkSceneDirty(uiManager.gameObject.scene);
            EditorSceneManager.SaveScene(uiManager.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Workbench] Installed dedicated station and compact radial recipe UI.");
        }

        public static void RebuildSampleScene()
        {
            const string scenePath = "Assets/000_Scenes/SampleScene.unity";
            if (EditorSceneManager.GetActiveScene().path != scenePath)
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            InstallWorkbenchExperience();
            SurvivalSubmarineFabricatorInstaller.InstallExperience();
        }

        private static WorkbenchPanel EnsurePanel(UIManager uiManager)
        {
            var panel = UnityEngine.Object.FindAnyObjectByType<WorkbenchPanel>(FindObjectsInactive.Include);
            if (!panel)
            {
                var go = new GameObject(PanelName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                    typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(WorkbenchPanel));
                Undo.RegisterCreatedObjectUndo(go, "Create Workbench UI");
                go.transform.SetParent(uiManager.transform, false);
                panel = go.GetComponent<WorkbenchPanel>();
            }

            panel.gameObject.layer = 5;
            panel.transform.localScale = Vector3.one;
            var rect = panel.transform as RectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var canvas = panel.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 180;
            var scaler = panel.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;

            var serialized = new SerializedObject(panel);
            serialized.FindProperty("blueprintDatabase").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<BluePrintDataBase>("Assets/003_Resources/Data/BluePrints.asset");
            serialized.FindProperty("itemDatabase").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<ItemDataBase>("Assets/003_Resources/Data/ItemDataBase.asset");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            panel.RebuildVisualTreeForEditor();
            return panel;
        }

        private static void EnsureStation()
        {
            var existing = GameObject.Find(StationName);
            if (existing)
            {
                EnsureStationComponents(existing);
                return;
            }

            var station = new GameObject(StationName);
            Undo.RegisterCreatedObjectUndo(station, "Create Workbench Station");
            station.layer = 12;
            PositionNearPlayer(station.transform);
            EnsureStationComponents(station);

            var visualRoot = new GameObject("WorkbenchVisuals");
            visualRoot.transform.SetParent(station.transform, false);
            var wood = LoadOrCreateMaterial("WorkbenchWood", new Color(.20f, .105f, .055f), .12f, .35f, false);
            var metal = LoadOrCreateMaterial("WorkbenchMetal", new Color(.075f, .105f, .12f), .72f, .46f, false);
            var holo = LoadOrCreateMaterial("WorkbenchHologram", new Color(.02f, .58f, .72f), .18f, .7f, true);

            Primitive("WorkSurface", PrimitiveType.Cube, visualRoot.transform,
                new Vector3(0, 1.02f, 0), new Vector3(2.55f, .16f, 1.05f), wood);
            Primitive("FrontBeam", PrimitiveType.Cube, visualRoot.transform,
                new Vector3(0, .77f, -.42f), new Vector3(2.35f, .18f, .16f), metal);
            Primitive("BackBeam", PrimitiveType.Cube, visualRoot.transform,
                new Vector3(0, .77f, .42f), new Vector3(2.35f, .18f, .16f), metal);
            Primitive("LegLeft", PrimitiveType.Cube, visualRoot.transform,
                new Vector3(-1.02f, .48f, 0), new Vector3(.18f, .95f, .72f), metal);
            Primitive("LegRight", PrimitiveType.Cube, visualRoot.transform,
                new Vector3(1.02f, .48f, 0), new Vector3(.18f, .95f, .72f), metal);
            Primitive("BackPanel", PrimitiveType.Cube, visualRoot.transform,
                new Vector3(0, 1.48f, .43f), new Vector3(1.55f, .72f, .12f), metal);
            Primitive("Display", PrimitiveType.Cube, visualRoot.transform,
                new Vector3(0, 1.52f, .355f), new Vector3(1.22f, .45f, .025f), holo);
            Primitive("ProjectorBase", PrimitiveType.Cylinder, visualRoot.transform,
                new Vector3(0, 1.18f, -.04f), new Vector3(.34f, .055f, .34f), metal);
            Primitive("HologramDisc", PrimitiveType.Cylinder, visualRoot.transform,
                new Vector3(0, 1.245f, -.04f), new Vector3(.24f, .012f, .24f), holo);
            Primitive("ToolRail", PrimitiveType.Cube, visualRoot.transform,
                new Vector3(-.83f, 1.20f, .34f), new Vector3(.34f, .14f, .12f), holo);
            Primitive("PowerCell", PrimitiveType.Cylinder, visualRoot.transform,
                new Vector3(.91f, 1.24f, .28f), new Vector3(.10f, .22f, .10f), holo,
                Quaternion.Euler(90, 0, 0));

            var labelGo = new GameObject("StationLabel", typeof(TextMesh));
            labelGo.transform.SetParent(visualRoot.transform, false);
            labelGo.transform.localPosition = new Vector3(0, 1.58f, .285f);
            labelGo.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var label = labelGo.GetComponent<TextMesh>();
            label.text = "WORKBENCH  ONLINE";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 42;
            label.characterSize = .018f;
            label.color = new Color(.2f, .95f, 1f);
        }

        private static void EnsureStationComponents(GameObject station)
        {
            station.layer = 12;
            var collider = station.GetComponent<BoxCollider>();
            if (!collider) collider = Undo.AddComponent<BoxCollider>(station);
            collider.center = new Vector3(0, .9f, 0);
            collider.size = new Vector3(2.7f, 1.8f, 1.2f);
            if (!station.GetComponent<Entity>()) Undo.AddComponent<Entity>(station);
            if (!station.GetComponent<_001_Scripts.Entities.Structure>())
                Undo.AddComponent<_001_Scripts.Entities.Structure>(station);
            var fabricator = station.GetComponent<Fabricator>();
            if (!fabricator) fabricator = Undo.AddComponent<Fabricator>(station);
            fabricator.Configure("Workbench", "제작대 사용");
        }

        private static void PositionNearPlayer(Transform station)
        {
            var player = UnityEngine.Object.FindAnyObjectByType<MovementController>();
            if (!player)
            {
                station.position = new Vector3(0, 0, 3);
                station.rotation = Quaternion.Euler(0, 180, 0);
                return;
            }

            Vector3 forward = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < .1f) forward = Vector3.forward;
            Vector3 position = player.transform.position + forward * 3.25f;
            if (Physics.Raycast(position + Vector3.up * 3f, Vector3.down, out var hit, 8f, ~0,
                    QueryTriggerInteraction.Ignore))
                position.y = hit.point.y;
            else
                position.y = player.transform.position.y;
            station.position = position;
            station.rotation = Quaternion.LookRotation(-forward, Vector3.up);
        }

        private static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 position,
            Vector3 scale, Material material, Quaternion? rotation = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation ?? Quaternion.identity;
            go.transform.localScale = scale;
            var collider = go.GetComponent<Collider>();
            if (collider) UnityEngine.Object.DestroyImmediate(collider);
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static Material LoadOrCreateMaterial(string name, Color color, float metallic, float smoothness,
            bool emission)
        {
            EnsureFolder(MaterialFolder);
            string path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else material.color = color;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (emission)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 2.2f);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
