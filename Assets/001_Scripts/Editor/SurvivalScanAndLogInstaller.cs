#if UNITY_EDITOR
using _001_Scripts.Data;
using _001_Scripts.Entities;
using _001_Scripts.Structure;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts.Editor
{
    [InitializeOnLoad]
    internal static class SurvivalScanAndLogInstaller
    {
        private const string ScanMaterialPath = "Assets/003_Resources/Materials/ScanGridOverlay.mat";
        private const string SweepMaterialPath = "Assets/003_Resources/Materials/ScanSweepPlane.mat";
        private const string ArtifactMaterialPath = "Assets/003_Resources/Materials/ScannableArtifact.mat";
        private const string LogMaterialPath = "Assets/003_Resources/Materials/LogDevice.mat";
        private const string PickupPrefabPath = "Assets/002_Prefabs/Test.prefab";

        static SurvivalScanAndLogInstaller() => EditorApplication.delayCall += InstallIfNeeded;

        [MenuItem("Tools/Survival/Install Scene-Authored Scan And Log Samples")]
        private static void InstallMenu() => Install();

        private static void InstallIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.name != "SampleScene") return;
            Install();
        }

        private static void Install()
        {
            Material scanMaterial = GetOrCreateMaterial(ScanMaterialPath, "Survival/Scan Grid Overlay",
                new Color(.16f, .95f, 1f, .72f), true);
            Material sweepMaterial = GetOrCreateMaterial(SweepMaterialPath, "Survival/Scan Grid Overlay",
                new Color(1f, 1f, 1f, .58f), true);
            if (scanMaterial) scanMaterial.SetFloat("_EffectMode", 0f);
            if (sweepMaterial) sweepMaterial.SetFloat("_EffectMode", 1f);
            Material artifactMaterial = GetOrCreateMaterial(ArtifactMaterialPath, "Universal Render Pipeline/Lit",
                new Color(.08f, .16f, .22f, 1f), false);
            Material logMaterial = GetOrCreateMaterial(LogMaterialPath, "Universal Render Pipeline/Lit",
                new Color(.18f, .12f, .28f, 1f), false);
            if (!scanMaterial || !sweepMaterial || !artifactMaterial || !logMaterial) return;

            bool sceneChanged = RemoveLegacyScanGridOverlays();
            sceneChanged |= EnsureCollectibleLog(logMaterial);
            sceneChanged |= EnsureScannableArtifact(artifactMaterial, scanMaterial, sweepMaterial);
            bool prefabChanged = EnsurePickupPrefab(scanMaterial, sweepMaterial);
            if (!sceneChanged && !prefabChanged) return;

            if (sceneChanged)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log("[Survival] Installed editable scan targets, rewards, and world-item codex scanning.");
        }

        private static bool RemoveLegacyScanGridOverlays()
        {
            bool changed = false;
            foreach (Transform item in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (!item || item.name != "UV Scan Grid Overlay") continue;
                UnityEngine.Object.DestroyImmediate(item.gameObject);
                changed = true;
            }
            return changed;
        }

        private static bool EnsureCollectibleLog(Material bodyMaterial)
        {
            CollectibleLog collectible = UnityEngine.Object.FindAnyObjectByType<CollectibleLog>(FindObjectsInactive.Include);
            if (!collectible) return false;

            bool changed = false;
            Transform root = collectible.transform;
            GameObject body = FindDirectChild(root, "Log Device Body");
            if (!body)
            {
                body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Undo.RegisterCreatedObjectUndo(body, "Create Log Device Body");
                body.name = "Log Device Body";
                body.transform.SetParent(root, false);
                body.transform.localPosition = new Vector3(0f, .18f, 0f);
                body.transform.localScale = new Vector3(.72f, .22f, .52f);
                UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
                body.GetComponent<Renderer>().sharedMaterial = bodyMaterial;
                changed = true;
            }

            WorldLogHologram hologram = root.GetComponentInChildren<WorldLogHologram>(true);
            if (!hologram)
            {
                var hologramObject = new GameObject("Log Hologram", typeof(RectTransform), typeof(Canvas),
                    typeof(CanvasGroup), typeof(WorldLogHologram));
                Undo.RegisterCreatedObjectUndo(hologramObject, "Create Log Hologram");
                hologramObject.transform.SetParent(root, false);
                hologramObject.transform.localPosition = new Vector3(0f, 1.05f, 0f);
                hologram = hologramObject.GetComponent<WorldLogHologram>();

                var imageObject = new GameObject("Image Slot (Replace Sprite Here)", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Image), typeof(Outline));
                imageObject.transform.SetParent(hologramObject.transform, false);
                RectTransform imageRect = imageObject.GetComponent<RectTransform>();
                imageRect.anchorMin = Vector2.zero;
                imageRect.anchorMax = Vector2.one;
                imageRect.offsetMin = imageRect.offsetMax = Vector2.zero;
                Image image = imageObject.GetComponent<Image>();
                image.sprite = null;
                image.raycastTarget = false;
                image.color = new Color(.18f, .8f, 1f, .24f);
                Outline outline = imageObject.GetComponent<Outline>();
                outline.effectColor = new Color(.26f, .9f, 1f, .9f);
                outline.effectDistance = new Vector2(3f, -3f);
                hologram.ConfigureView(hologramObject.GetComponent<CanvasGroup>(), image);
                changed = true;
            }
            else if (hologram.ImageSlot &&
                     hologram.ImageSlot.name == "Image Slot (Replace Sprite Here)" &&
                     hologram.ImageSlot.sprite &&
                     AssetDatabase.GetAssetPath(hologram.ImageSlot.sprite).Contains("unity_builtin_extra"))
            {
                hologram.ImageSlot.sprite = null;
                EditorUtility.SetDirty(hologram.ImageSlot);
                changed = true;
            }

            if (changed)
            {
                SetLayerRecursively(root, 12);
                EditorUtility.SetDirty(collectible);
                EditorUtility.SetDirty(hologram);
            }
            return changed;
        }

        private static bool EnsureScannableArtifact(Material bodyMaterial, Material scanMaterial,
            Material sweepMaterial)
        {
            foreach (ScannableTarget existing in UnityEngine.Object.FindObjectsByType<ScannableTarget>(
                         FindObjectsInactive.Include))
            {
                if (existing.name != "Sample Scannable Artifact") continue;
                bool changed = existing.AddReward(ScanReward.BlueprintProgress(15, 1));
                changed |= EnsureScanBoxVisual(existing.transform, scanMaterial, sweepMaterial,
                    out Renderer volume, out Transform horizontal, out Transform vertical,
                    out Vector3 center, out Vector3 size);
                if (changed || !existing.HasScanBoxVisuals)
                {
                    existing.ConfigureScanBox(volume, horizontal, vertical, center, size);
                    EditorUtility.SetDirty(existing);
                    changed = true;
                }
                return changed;
            }

            var root = new GameObject("Sample Scannable Artifact", typeof(BoxCollider), typeof(ScannableTarget));
            Undo.RegisterCreatedObjectUndo(root, "Create Scannable Artifact");
            root.transform.position = new Vector3(9.25f, .72f, 1.25f);
            BoxCollider rootCollider = root.GetComponent<BoxCollider>();
            rootCollider.center = Vector3.zero;
            rootCollider.size = new Vector3(.78f, 1.22f, .78f);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Artifact Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(.72f, 1.15f, .72f);
            UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().sharedMaterial = bodyMaterial;

            EnsureScanBoxVisual(root.transform, scanMaterial, sweepMaterial,
                out Renderer volumeRenderer, out Transform horizontalSweep, out Transform verticalSweep,
                out Vector3 scanCenter, out Vector3 scanSize);

            ScannableTarget target = root.GetComponent<ScannableTarget>();
            target.ConfigureScanBox(volumeRenderer, horizontalSweep, verticalSweep, scanCenter, scanSize);
            target.Configure("미확인 데이터 코어", 4f, "artifact-scan-01", new[] { volumeRenderer });
            target.AddReward(ScanReward.BlueprintProgress(15, 1));
            SetLayerRecursively(root.transform, 12);
            EditorUtility.SetDirty(target);
            return true;
        }

        private static bool EnsurePickupPrefab(Material scanMaterial, Material sweepMaterial)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(PickupPrefabPath);
            if (!contents) return false;
            bool changed = false;
            try
            {
                if (!contents.GetComponent<Entity>())
                {
                    contents.AddComponent<Entity>();
                    changed = true;
                }
                if (!contents.GetComponent<WorldItem>())
                {
                    contents.AddComponent<WorldItem>();
                    changed = true;
                }

                ScannableTarget target = contents.GetComponent<ScannableTarget>();
                if (!target)
                {
                    target = contents.AddComponent<ScannableTarget>();
                    changed = true;
                }

                changed |= EnsureScanBoxVisual(contents.transform, scanMaterial, sweepMaterial,
                    out Renderer volumeRenderer, out Transform horizontalSweep, out Transform verticalSweep,
                    out Vector3 scanCenter, out Vector3 scanSize);

                if (changed || !target.HasScanBoxVisuals)
                {
                    target.ConfigureWorldItem(1.5f, new[] { volumeRenderer });
                    target.ConfigureScanBox(volumeRenderer, horizontalSweep, verticalSweep, scanCenter, scanSize);
                    SetLayerRecursively(contents.transform, 12);
                    PrefabUtility.SaveAsPrefabAsset(contents, PickupPrefabPath);
                    changed = true;
                }
                return changed;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static bool EnsureScanBoxVisual(Transform targetRoot, Material volumeMaterial,
            Material sweepMaterial, out Renderer volumeRenderer, out Transform horizontalSweep,
            out Transform verticalSweep, out Vector3 localCenter, out Vector3 localSize)
        {
            Transform effectRoot = targetRoot.Find("Scan Effect");
            Transform volume = effectRoot ? effectRoot.Find("Scan Volume") : null;
            horizontalSweep = effectRoot ? effectRoot.Find("Horizontal Sweep") : null;
            verticalSweep = effectRoot ? effectRoot.Find("Vertical Sweep") : null;
            volumeRenderer = volume ? volume.GetComponent<Renderer>() : null;

            if (volumeRenderer && horizontalSweep && verticalSweep)
            {
                localCenter = volume.localPosition;
                localSize = new Vector3(
                    Mathf.Max(.05f, volume.localScale.x - .04f),
                    Mathf.Max(.05f, volume.localScale.y - .04f),
                    Mathf.Max(.05f, volume.localScale.z - .04f));
                return false;
            }

            if (effectRoot) UnityEngine.Object.DestroyImmediate(effectRoot.gameObject);
            Transform legacy = targetRoot.Find("UV Scan Grid Overlay");
            if (legacy) UnityEngine.Object.DestroyImmediate(legacy.gameObject);

            CalculateLocalRendererBounds(targetRoot, out localCenter, out localSize);
            var effectObject = new GameObject("Scan Effect");
            effectObject.transform.SetParent(targetRoot, false);
            effectRoot = effectObject.transform;

            Vector3 paddedSize = localSize + Vector3.one * .04f;
            GameObject volumeObject = CreateScanCube("Scan Volume", effectRoot, volumeMaterial);
            volumeObject.transform.localPosition = localCenter;
            volumeObject.transform.localScale = paddedSize;
            volumeRenderer = volumeObject.GetComponent<Renderer>();

            GameObject horizontalObject = CreateScanCube("Horizontal Sweep", effectRoot, sweepMaterial);
            horizontalObject.transform.localPosition = localCenter - Vector3.up * localSize.y * .5f;
            horizontalObject.transform.localScale = new Vector3(paddedSize.x * 1.04f, .025f,
                paddedSize.z * 1.04f);
            horizontalSweep = horizontalObject.transform;

            GameObject verticalObject = CreateScanCube("Vertical Sweep", effectRoot, sweepMaterial);
            verticalObject.transform.localPosition = localCenter - Vector3.right * localSize.x * .5f;
            verticalObject.transform.localScale = new Vector3(.025f, paddedSize.y * 1.04f,
                paddedSize.z * 1.04f);
            verticalSweep = verticalObject.transform;
            return true;
        }

        private static GameObject CreateScanCube(string objectName, Transform parent, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = objectName;
            cube.transform.SetParent(parent, false);
            UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
            Renderer cubeRenderer = cube.GetComponent<Renderer>();
            cubeRenderer.sharedMaterial = material;
            cubeRenderer.enabled = false;
            return cube;
        }

        private static void CalculateLocalRendererBounds(Transform root, out Vector3 center, out Vector3 size)
        {
            bool hasPoint = false;
            Vector3 min = Vector3.zero;
            Vector3 max = Vector3.zero;
            foreach (Renderer source in root.GetComponentsInChildren<Renderer>(true))
            {
                string sourceName = source.gameObject.name;
                if (sourceName == "UV Scan Grid Overlay" || sourceName == "Scan Volume" ||
                    sourceName == "Horizontal Sweep" || sourceName == "Vertical Sweep") continue;

                Bounds bounds = source.bounds;
                for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                    Vector3 local = root.InverseTransformPoint(corner);
                    if (!hasPoint)
                    {
                        min = max = local;
                        hasPoint = true;
                    }
                    else
                    {
                        min = Vector3.Min(min, local);
                        max = Vector3.Max(max, local);
                    }
                }
            }

            if (!hasPoint)
            {
                center = Vector3.zero;
                size = Vector3.one;
                return;
            }
            center = (min + max) * .5f;
            size = new Vector3(
                Mathf.Max(.05f, max.x - min.x),
                Mathf.Max(.05f, max.y - min.y),
                Mathf.Max(.05f, max.z - min.z));
        }

        private static Material GetOrCreateMaterial(string path, string shaderName, Color color, bool emission)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find(shaderName);
            if (!shader)
            {
                Debug.LogWarning($"[Survival] Shader is not ready yet: {shaderName}");
                return null;
            }

            if (!material)
            {
                material = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader) material.shader = shader;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_ScanColor")) material.SetColor("_ScanColor", color);
            if (emission && material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color * 3f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject FindDirectChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            return child ? child.gameObject : null;
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++) SetLayerRecursively(root.GetChild(i), layer);
        }
    }
}
#endif
