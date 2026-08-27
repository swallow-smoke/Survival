#if UNITY_EDITOR
using AstraNope.Services;
using AstraNope.WorldObjects.Entities;
using AstraNope.Gameplay.Player;
using AstraNope.WorldObjects.Items;
using AstraNope.WorldObjects.Structures;
using AstraNope.WorldObjects.Vehicles;
using AstraNope.UI.Panels;
using AstraNope.WorldObjects.Vehicles.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace AstraNope.Editor
{
    public static class SurvivalSubmarineFabricatorInstaller
    {
        private const string StationName = "SubmarineFabricatorStation";
        private const string PanelName = "SubmarineFabricatorPanel";
        private const string PrototypeName = "PrototypeSmallSubmarine";
        private const string PrototypePrefabPath = "Assets/002_Prefabs/testsub.prefab";
        private const string MaterialFolder = "Assets/003_Resources/Materials/SubmarineFabricator";

        [InitializeOnLoadMethod]
        private static void InstallIntoSampleSceneOnce()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                var scene = EditorSceneManager.GetActiveScene();
                if (!scene.IsValid() || scene.path != "Assets/000_Scenes/SampleScene.unity") return;
                var panel = UnityEngine.Object.FindAnyObjectByType<SubmarineFabricatorPanel>(FindObjectsInactive.Include);
                var station = GameObject.Find(StationName);
                var prototype = GameObject.Find(PrototypeName);
                if (panel && station && panel.VisualVersion == SubmarineFabricatorPanel.CurrentVisualVersion &&
                    prototype && prototype.GetComponent<SmallSubVehicle>() is
                        { ConfigurationVersion: SmallSubVehicle.CurrentConfigurationVersion } &&
                    panel.GetComponent<CanvasGroup>() is { alpha: <= .001f, interactable: false, blocksRaycasts: false })
                    return;
                InstallExperience();
            };
        }

        [MenuItem("Tools/Survival/Install Submarine Fabricator Prototype")]
        public static void InstallExperience()
        {
            var uiManager = UnityEngine.Object.FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
            if (!uiManager)
            {
                Debug.LogError("[SubmarineFabricator] UIManager is required in the open scene.");
                return;
            }

            var submarinePrefab = EnsurePrototypePrefab();
            if (!submarinePrefab) return;
            var station = EnsureStation(submarinePrefab);
            EnsureScenePrototype(submarinePrefab, station);
            var panel = EnsurePanel(uiManager, station);
            uiManager.uiPanels["SubmarineFabricator"] = panel;
            EditorUtility.SetDirty(uiManager);
            EditorUtility.SetDirty(panel);
            EditorUtility.SetDirty(station);
            EditorSceneManager.MarkSceneDirty(uiManager.gameObject.scene);
            EditorSceneManager.SaveScene(uiManager.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[SubmarineFabricator] Installed the one-seat submarine prefab, scene test vehicle, and fabricator.");
        }

        private static SubmarineFabricatorPanel EnsurePanel(UIManager uiManager,
            SubmarineFabricator station)
        {
            var panel = UnityEngine.Object.FindAnyObjectByType<SubmarineFabricatorPanel>(FindObjectsInactive.Include);
            if (!panel)
            {
                var go = new GameObject(PanelName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                    typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(SubmarineFabricatorPanel));
                Undo.RegisterCreatedObjectUndo(go, "Create Submarine Fabricator UI");
                go.transform.SetParent(uiManager.transform, false);
                panel = go.GetComponent<SubmarineFabricatorPanel>();
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
            canvas.sortingOrder = 185;
            var scaler = panel.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
            var radial = panel.GetComponent<AstraNope.UI.Components.SimpleRadialMenuView>();
            if (!radial) radial = Undo.AddComponent<AstraNope.UI.Components.SimpleRadialMenuView>(panel.gameObject);
            radial.SetRoundedPanelSprite(AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/003_Resources/UI/SurvivalRoundedPanel.png"));
            EditorUtility.SetDirty(radial);
            panel.Configure(station);
            panel.RebuildVisualTreeForEditor();
            return panel;
        }

        private static SubmarineFabricator EnsureStation(GameObject submarinePrefab)
        {
            var station = GameObject.Find(StationName);
            if (!station)
            {
                station = new GameObject(StationName);
                Undo.RegisterCreatedObjectUndo(station, "Create Submarine Fabricator Station");
                station.transform.position = GetStationPosition();
                station.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }
            station.layer = 12;

            var collider = station.GetComponent<BoxCollider>();
            if (!collider) collider = Undo.AddComponent<BoxCollider>(station);
            collider.center = new Vector3(0, 1.25f, 0);
            collider.size = new Vector3(4.3f, 2.5f, 3.2f);

            if (!station.GetComponent<Entity>()) Undo.AddComponent<Entity>(station);
            if (!station.GetComponent<AstraNope.WorldObjects.Entities.Structure>())
                Undo.AddComponent<AstraNope.WorldObjects.Entities.Structure>(station);
            var interactable = station.GetComponent<SubmarineFabricator>();
            if (!interactable) interactable = Undo.AddComponent<SubmarineFabricator>(station);

            Transform spawnPoint = station.transform.Find("SubmarineSpawnPoint");
            if (!spawnPoint)
            {
                var spawn = new GameObject("SubmarineSpawnPoint");
                spawn.transform.SetParent(station.transform, false);
                spawn.transform.localPosition = new Vector3(0, .15f, -5.8f);
                spawnPoint = spawn.transform;
            }
            interactable.Configure(spawnPoint, submarinePrefab);

            if (!station.transform.Find("FabricatorVisuals")) BuildStationVisuals(station.transform);
            return interactable;
        }

        private static GameObject EnsurePrototypePrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrototypePrefabPath);
            if (!prefab)
            {
                Debug.LogError($"[Submarine] Source prefab was not found: {PrototypePrefabPath}");
                return null;
            }

            var root = PrefabUtility.LoadPrefabContents(PrototypePrefabPath);
            try
            {
                var entity = root.GetComponent<Entity>() ?? root.AddComponent<Entity>();
                entity.Configure("prototype_small_submarine", "1인용 소형 잠수함", EntityKind.Submarine);
                if (!root.GetComponent<Health>()) root.AddComponent<Health>();
                if (!root.GetComponent<AstraNope.WorldObjects.Entities.Vehicle>())
                    root.AddComponent<AstraNope.WorldObjects.Entities.Vehicle>();
                if (!root.GetComponent<Submarine>()) root.AddComponent<Submarine>();
                if (!root.GetComponent<PrototypeSubmarine>()) root.AddComponent<PrototypeSubmarine>();

                var body = root.GetComponent<Rigidbody>() ?? root.AddComponent<Rigidbody>();
                body.mass = 400f;
                body.useGravity = false;
                body.linearDamping = 1.5f;
                body.angularDamping = 2f;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                var cameraAnchor = root.transform.Find("CameraAnchor");
                if (!cameraAnchor)
                {
                    var anchor = new GameObject("CameraAnchor");
                    anchor.transform.SetParent(root.transform, false);
                    anchor.transform.localPosition = new Vector3(0f, .55f, .65f);
                    cameraAnchor = anchor.transform;
                }

                var exitPoint = root.transform.Find("ExitPoint");
                if (!exitPoint)
                {
                    var exit = new GameObject("ExitPoint");
                    exit.transform.SetParent(root.transform, false);
                    exit.transform.localPosition = new Vector3(-1.7f, .2f, 0f);
                    exitPoint = exit.transform;
                }

                var controller = root.GetComponent<SmallSubVehicle>() ?? root.AddComponent<SmallSubVehicle>();
                controller.Configure(body, cameraAnchor, 10f, 8f, .5f);
                var seat = root.GetComponentInChildren<SeatComponent>(true) ?? root.AddComponent<SeatComponent>();
                seat.Configure(cameraAnchor, exitPoint, controller);

                var hatch = root.GetComponentInChildren<HatchComponent>(true);
                if (!hatch)
                {
                    var boardingPoint = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    boardingPoint.name = "BoardingPoint";
                    boardingPoint.transform.SetParent(root.transform, false);
                    boardingPoint.transform.localPosition = new Vector3(-.75f, .35f, 0f);
                    boardingPoint.transform.localScale = new Vector3(.35f, .7f, .8f);
                    var renderer = boardingPoint.GetComponent<Renderer>();
                    if (renderer) renderer.enabled = false;
                    hatch = boardingPoint.AddComponent<HatchComponent>();
                }
                hatch.gameObject.layer = 12;
                hatch.ConfigureSmallSeat(seat);

                PrefabUtility.SaveAsPrefabAsset(root, PrototypePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrototypePrefabPath);
        }

        private static void EnsureScenePrototype(GameObject prefab, SubmarineFabricator station)
        {
            var instance = GameObject.Find(PrototypeName);
            if (!instance)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Undo.RegisterCreatedObjectUndo(instance, "Place Prototype Small Submarine");
                instance.name = PrototypeName;
            }

            var spawnPoint = station.transform.Find("SubmarineSpawnPoint");
            Vector3 basePosition = spawnPoint ? spawnPoint.position : station.transform.position;
            instance.transform.SetPositionAndRotation(basePosition + station.transform.right * 3.2f,
                station.transform.rotation);
            EditorUtility.SetDirty(instance);
        }

        private static Vector3 GetStationPosition()
        {
            var workbench = GameObject.Find("WorkbenchStation");
            if (workbench) return workbench.transform.position + new Vector3(4.8f, 0f, 0f);
            return new Vector3(11.2f, 0f, 3.25f);
        }

        private static void BuildStationVisuals(Transform station)
        {
            var root = new GameObject("FabricatorVisuals");
            root.transform.SetParent(station, false);

            var metal = LoadOrCreateMaterial("FabricatorMetal", new Color(.045f, .085f, .11f), .82f, .58f, false);
            var orange = LoadOrCreateMaterial("FabricatorOrange", new Color(.92f, .32f, .035f), .58f, .5f, false);
            var holo = LoadOrCreateMaterial("FabricatorHologram", new Color(.02f, .72f, .9f), .1f, .78f, true);

            Primitive("CircularBase", PrimitiveType.Cylinder, root.transform,
                new Vector3(0, .18f, 0), new Vector3(2.1f, .18f, 2.1f), metal);
            Primitive("InnerPad", PrimitiveType.Cylinder, root.transform,
                new Vector3(0, .38f, 0), new Vector3(1.55f, .06f, 1.55f), orange);
            Primitive("Projector", PrimitiveType.Cylinder, root.transform,
                new Vector3(0, .62f, 0), new Vector3(.54f, .22f, .54f), metal);
            Primitive("HologramCore", PrimitiveType.Sphere, root.transform,
                new Vector3(0, 1.18f, 0), new Vector3(.56f, .56f, .56f), holo);

            for (int i = 0; i < 3; i++)
            {
                float angle = i * 120f;
                Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                Primitive($"RadialArm_{i}", PrimitiveType.Cube, root.transform,
                    direction * 1.25f + Vector3.up * .45f, new Vector3(.28f, .18f, 1.65f), metal,
                    Quaternion.Euler(0, angle, 0));
                Primitive($"Emitter_{i}", PrimitiveType.Cylinder, root.transform,
                    direction * 1.92f + Vector3.up * .82f, new Vector3(.23f, .55f, .23f), holo);
            }

            Primitive("GantryLeft", PrimitiveType.Cube, root.transform,
                new Vector3(-1.7f, 1.55f, .75f), new Vector3(.24f, 2.45f, .24f), metal);
            Primitive("GantryRight", PrimitiveType.Cube, root.transform,
                new Vector3(1.7f, 1.55f, .75f), new Vector3(.24f, 2.45f, .24f), metal);
            Primitive("GantryTop", PrimitiveType.Cube, root.transform,
                new Vector3(0, 2.72f, .75f), new Vector3(3.65f, .24f, .24f), orange);

            var labelGo = new GameObject("StationLabel", typeof(TextMesh));
            labelGo.transform.SetParent(root.transform, false);
            labelGo.transform.localPosition = new Vector3(0, 2.48f, .58f);
            labelGo.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var label = labelGo.GetComponent<TextMesh>();
            label.text = "SUBMARINE  FABRICATOR";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 38;
            label.characterSize = .022f;
            label.color = new Color(.2f, .95f, 1f);
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
