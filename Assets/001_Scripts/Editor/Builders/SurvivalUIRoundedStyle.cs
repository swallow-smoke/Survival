#if UNITY_EDITOR
using System.IO;
using AstraNope.UI.Panels;
using AstraNope.UI.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AstraNope.Editor
{
    [InitializeOnLoad]
    internal static class SurvivalUIRoundedStyle
    {
        private const int StyleVersion = 6;
        private const string RoundedSpritePath = "Assets/003_Resources/UI/SurvivalRoundedPanel.png";
        private static readonly Color DarkPurple = new(.055f, .035f, .11f, .66f);
        private static readonly Color MidPurple = new(.13f, .085f, .23f, .68f);
        private static readonly Color WhitePurple = new(.93f, .89f, 1f, 1f);
        private static readonly Color MutedPurple = new(.70f, .63f, .82f, 1f);
        private static readonly Color AccentPurple = new(.66f, .46f, 1f, 1f);

        static SurvivalUIRoundedStyle()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.delayCall += ApplyOnceIfNeeded;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += ApplyOnceIfNeeded;
        }

        [MenuItem("Tools/Survival UI/Apply Dark Purple Rounded Style")]
        private static void ApplyFromMenu() => ApplyAll(true);

        private static void ApplyOnceIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.name != "SampleScene") return;

            var hud = UnityEngine.Object.FindFirstObjectByType<HUDPanel>(FindObjectsInactive.Include);
            var inventory = UnityEngine.Object.FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
            var craft = UnityEngine.Object.FindFirstObjectByType<CraftPanel>(FindObjectsInactive.Include);
            if (GetVersion(hud) >= StyleVersion && GetVersion(inventory) >= StyleVersion &&
                GetVersion(craft) >= StyleVersion) return;
            ApplyAll(true);
        }

        private static void ApplyAll(bool saveScene)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            ApplyPrefab("Assets/002_Prefabs/UI/InventorySlot.prefab");
            ApplyPrefab("Assets/002_Prefabs/UI/BlueprintSlot.prefab");

            var hud = UnityEngine.Object.FindFirstObjectByType<HUDPanel>(FindObjectsInactive.Include);
            var inventory = UnityEngine.Object.FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
            var craft = UnityEngine.Object.FindFirstObjectByType<CraftPanel>(FindObjectsInactive.Include);
            var logs = UnityEngine.Object.FindFirstObjectByType<LogPanel>(FindObjectsInactive.Include);
            var blueprints = UnityEngine.Object.FindFirstObjectByType<BlueprintPanel>(FindObjectsInactive.Include);
            var uiManager = UnityEngine.Object.FindFirstObjectByType<AstraNope.UI.Panels.UIManager>(FindObjectsInactive.Include);
            if (uiManager) ApplyToRoot(uiManager.gameObject);
            ApplyPanel(hud);
            ApplyPanel(inventory);
            ApplyPanel(craft);
            ApplyPanel(logs);
            ApplyPanel(blueprints);

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded) return;
            EditorSceneManager.MarkSceneDirty(scene);
            if (saveScene) EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Survival UI] Applied dark-purple rounded UGUI style and interaction feedback.");
        }

        private static void ApplyPanel(MonoBehaviour panel)
        {
            if (!panel) return;
            Undo.RegisterFullObjectHierarchyUndo(panel.gameObject, "Apply Survival UI Style");
            if (panel is HUDPanel) NormalizeHudCanvas(panel.gameObject);
            ApplyToRoot(panel.gameObject);
            var serialized = new SerializedObject(panel);
            var version = serialized.FindProperty("editorStyleVersion");
            if (version != null) version.intValue = StyleVersion;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(panel);
        }

        private static void NormalizeHudCanvas(GameObject hud)
        {
            var rect = hud.GetComponent<RectTransform>();
            if (rect)
            {
                rect.localScale = Vector3.one;
                EditorUtility.SetDirty(rect);
            }

            var scaler = hud.GetComponent<CanvasScaler>();
            if (!scaler) return;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
            EditorUtility.SetDirty(scaler);
        }

        internal static void ApplyToRoot(GameObject root)
        {
            if (!root) return;
            var rounded = GetOrCreateRoundedSprite();
            EnsureUnifiedDataPanelShell(root, rounded);
            bool isHud = root.GetComponent<HUDPanel>();

            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (rounded && ShouldUseRoundedSprite(image))
                {
                    image.sprite = rounded;
                    image.type = Image.Type.Sliced;
                    image.pixelsPerUnitMultiplier = 1f;
                }
                image.color = NormalizeSurfaceOpacity(image, ResolveImageColor(image, isHud));
                EditorUtility.SetDirty(image);
            }

            foreach (var outline in root.GetComponentsInChildren<Outline>(true))
            {
                outline.effectColor = new Color(.82f, .72f, 1f, .28f);
                outline.effectDistance = new Vector2(1f, -1f);
                outline.useGraphicAlpha = false;
                EditorUtility.SetDirty(outline);
            }

            foreach (var label in root.GetComponentsInChildren<Text>(true))
            {
                label.color = ResolveTextColor(label);
                EditorUtility.SetDirty(label);
            }

            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                button.transition = Selectable.Transition.ColorTint;
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, .88f, 1f, 1f);
                colors.pressedColor = new Color(.72f, .58f, .90f, 1f);
                colors.selectedColor = new Color(.90f, .78f, 1f, 1f);
                colors.disabledColor = new Color(.38f, .32f, .48f, .48f);
                colors.colorMultiplier = 1f;
                colors.fadeDuration = .09f;
                button.colors = colors;
                AddInteraction(button.gameObject);
                EditorUtility.SetDirty(button);
            }

            foreach (var slot in root.GetComponentsInChildren<ItemSlot>(true))
                AddInteraction(slot.gameObject);
        }

        private static void EnsureUnifiedDataPanelShell(GameObject root, Sprite rounded)
        {
            bool isDataPanel = root.GetComponent<InventoryPanel>() || root.GetComponent<LogPanel>() ||
                               root.GetComponent<BlueprintPanel>() || root.GetComponent<CraftPanel>();
            if (!isDataPanel) return;

            Transform card = FindNamed(root.transform, "InventoryCard") ?? FindNamed(root.transform, "LogCard") ??
                             FindNamed(root.transform, "BlueprintCard") ?? FindNamed(root.transform, "CraftCard");
            if (!card) return;
            var cardRect = card as RectTransform;
            if (cardRect) cardRect.sizeDelta = new Vector2(1500f, 760f);

            Transform old = card.Find("UnifiedOrganicGradient");
            if (old) Undo.DestroyObjectImmediate(old.gameObject);
            var gradientGo = new GameObject("UnifiedOrganicGradient", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(OrganicGradientGraphic));
            Undo.RegisterCreatedObjectUndo(gradientGo, "Create unified organic UI shell");
            gradientGo.layer = card.gameObject.layer;
            gradientGo.transform.SetParent(card, false);
            var gradientRect = gradientGo.GetComponent<RectTransform>();
            gradientRect.anchorMin = Vector2.zero;
            gradientRect.anchorMax = Vector2.one;
            gradientRect.offsetMin = new Vector2(2f, 2f);
            gradientRect.offsetMax = new Vector2(-2f, -2f);
            gradientGo.GetComponent<OrganicGradientGraphic>().Configure(
                new Color(.16f, .075f, .29f, .70f), new Color(.015f, .006f, .05f, .78f),
                new Color(.70f, .38f, 1f, .14f), new Color(.24f, .52f, 1f, .09f));
            gradientRect.SetAsFirstSibling();

            CreateOrganicCapsule(card, rounded, "OrganicGlowLeft", new Vector2(0f, 0f), new Vector2(430f, 118f),
                new Vector2(66f, 38f), new Vector2(0f, 0f), 8f);
            CreateOrganicCapsule(card, rounded, "OrganicGlowRight", new Vector2(1f, 1f), new Vector2(510f, 96f),
                new Vector2(-60f, -102f), new Vector2(1f, 1f), -7f);

            Transform header = card.Find("Header");
            if (header)
            {
                var headerRect = header as RectTransform;
                if (headerRect)
                {
                    headerRect.anchorMin = new Vector2(0f, 1f);
                    headerRect.anchorMax = new Vector2(1f, 1f);
                    headerRect.pivot = new Vector2(.5f, 1f);
                    headerRect.sizeDelta = new Vector2(-40f, 64f);
                    headerRect.anchoredPosition = new Vector2(0f, -14f);
                }
                var headerImage = header.GetComponent<Image>();
                if (headerImage)
                {
                    headerImage.sprite = rounded;
                    headerImage.type = Image.Type.Sliced;
                    if (!header.GetComponent<Outline>())
                    {
                        var headerOutline = Undo.AddComponent<Outline>(header.gameObject);
                        headerOutline.effectColor = new Color(.72f, .52f, 1f, .34f);
                        headerOutline.effectDistance = new Vector2(1f, -1f);
                        headerOutline.useGraphicAlpha = false;
                    }
                }
                Transform oldLine = header.Find("HeaderFlowLine");
                if (oldLine) Undo.DestroyObjectImmediate(oldLine.gameObject);
                var line = new GameObject("HeaderFlowLine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(line, "Create unified header line");
                line.layer = header.gameObject.layer;
                line.transform.SetParent(header, false);
                var lineImage = line.GetComponent<Image>();
                lineImage.raycastTarget = false;
                var lineRect = line.GetComponent<RectTransform>();
                lineRect.anchorMin = new Vector2(0f, 0f);
                lineRect.anchorMax = new Vector2(1f, 0f);
                lineRect.pivot = new Vector2(.5f, 0f);
                lineRect.sizeDelta = new Vector2(0f, 2f);
                lineRect.anchoredPosition = Vector2.zero;
            }
        }

        private static void CreateOrganicCapsule(Transform parent, Sprite rounded, string name, Vector2 anchor,
            Vector2 size, Vector2 position, Vector2 pivot, float rotation)
        {
            Transform old = parent.Find(name);
            if (old) Undo.DestroyObjectImmediate(old.gameObject);
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "Create organic UI decoration");
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            var image = go.GetComponent<Image>();
            image.sprite = rounded;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            rect.SetSiblingIndex(1);
        }

        private static Transform FindNamed(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }

        private static bool ShouldUseRoundedSprite(Image image)
        {
            if (!image) return false;

            // Content images must keep their own sprite. Only the slot/card behind
            // them receives the rounded UI sprite.
            var itemSlot = image.GetComponentInParent<ItemSlot>();
            if (itemSlot && image.gameObject != itemSlot.gameObject) return false;

            string name = image.name.ToLowerInvariant();
            if (name.Contains("icon") || name.Contains("itemimage") || name.Contains("thumbnail") ||
                name.Contains("portrait")) return false;
            return true;
        }

        private static Sprite GetOrCreateRoundedSprite()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
            if (existing) return existing;

            EnsureFolder("Assets/003_Resources", "UI");
            const int size = 64;
            const float radius = 20f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, false)
            {
                name = "SurvivalRoundedPanel",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[size * size];
            float half = size * .5f;
            float inner = half - radius;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float px = Mathf.Abs(x + .5f - half) - inner;
                float py = Mathf.Abs(y + .5f - half) - inner;
                float outside = Mathf.Sqrt(Mathf.Max(px, 0f) * Mathf.Max(px, 0f) +
                                           Mathf.Max(py, 0f) * Mathf.Max(py, 0f));
                float distance = outside + Mathf.Min(Mathf.Max(px, py), 0f) - radius;
                byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(.5f - distance) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(RoundedSpritePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(RoundedSpritePath, ImportAssetOptions.ForceSynchronousImport);

            var importer = AssetImporter.GetAtPath(RoundedSpritePath) as TextureImporter;
            if (importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spritePixelsPerUnit = 100f;
                importer.spriteBorder = new Vector4(22f, 22f, 22f, 22f);
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }

        private static Color ResolveImageColor(Image image, bool isHud)
        {
            string name = image.name;
            string path = GetPath(image.transform);
            if (isHud && name == "UGUI_HUDRoot") return new Color(.055f, .035f, .11f, .015f);
            if (name == "UGUI_InventoryRoot") return new Color(.012f, .006f, .035f, .22f);
            if (name == "InventoryCard" || name == "LogCard" || name == "BlueprintCard" || name == "CraftCard")
                return new Color(.04f, .02f, .09f, .76f);
            if (name == "SlotBay" || name == "ItemDetails") return new Color(.075f, .045f, .15f, .60f);
            if (name.EndsWith("Row")) return new Color(.12f, .075f, .20f, isHud ? .12f : .68f);
            if (name == "Accent") return new Color(.66f, .46f, 1f, .78f);
            if (name == "OrganicGlowLeft") return new Color(.62f, .32f, 1f, .055f);
            if (name == "OrganicGlowRight") return new Color(.20f, .48f, 1f, .045f);
            if (name == "HeaderFlowLine") return new Color(.69f, .48f, 1f, .46f);
            if (name == "RecipeTooltip") return new Color(.18f, .18f, .20f, .70f);
            if (name.Contains("Divider")) return new Color(.75f, .62f, 1f, .30f);
            if (name == "Track") return new Color(.15f, .11f, .23f, .72f);
            if (name == "Fill")
            {
                if (path.Contains("Oxygen") || path.Contains("산소")) return new Color(.77f, .65f, 1f, .95f);
                if (path.Contains("Health") || path.Contains("체력")) return new Color(.94f, .91f, 1f, .96f);
                if (path.Contains("Hunger") || path.Contains("배고픔")) return new Color(.84f, .55f, 1f, .95f);
                if (path.Contains("Hydration") || path.Contains("수분")) return new Color(.61f, .48f, 1f, .95f);
                return new Color(.70f, .52f, 1f, .94f);
            }
            if (name.EndsWith("TabButton"))
            {
                bool inventoryActive = path.Contains("InventoryPanel") && name.Contains("Inventory");
                bool craftActive = path.Contains("CraftPanel") && name.Contains("Craft");
                return inventoryActive || craftActive
                    ? new Color(.52f, .34f, .84f, .96f)
                    : new Color(.19f, .12f, .31f, .82f);
            }
            if (image.GetComponent<Button>())
            {
                if (name.Contains("Craft") || name.Contains("Use") || name.Contains("Register"))
                    return new Color(.45f, .28f, .74f, .90f);
                return new Color(.19f, .12f, .31f, .82f);
            }
            if (name.Contains("Dim")) return new Color(.025f, .015f, .05f, .45f);
            if (name.Contains("Header")) return new Color(.16f, .10f, .28f, .66f);
            if (name.Contains("Card") || name.Contains("Panel") || name.Contains("Area") ||
                name.Contains("Details") || name.Contains("List"))
                return DarkPurple;
            if (name.Contains("Slot") || name.Contains("Template") || name.Contains("Preview"))
                return MidPurple;
            return new Color(image.color.r * .55f + .08f, image.color.g * .38f + .05f,
                image.color.b * .70f + .14f, Mathf.Min(image.color.a, .84f));
        }

        private static Color NormalizeSurfaceOpacity(Image image, Color value)
        {
            string name = image.name.ToLowerInvariant();
            string path = GetPath(image.transform);
            bool modalRoot = name.EndsWith("root") &&
                             (path.Contains("InventoryPanel") || path.Contains("LogPanel") ||
                              path.Contains("BlueprintPanel") || path.Contains("CraftPanel"));
            if (modalRoot)
            {
                value.a = .22f;
                return value;
            }

            bool contentImage = image.sprite && (name.Contains("icon") || name.Contains("itemimage") ||
                                                  name.Contains("thumbnail") || name.Contains("portrait"));
            if (contentImage) return value;
            if (value.a >= .45f) value.a = Mathf.Clamp(value.a, .70f, .80f);
            return value;
        }

        private static Color ResolveTextColor(Text label)
        {
            string name = label.name;
            if (name.Contains("Meta") || name.Contains("Description") || name.Contains("Hint") ||
                name.Contains("Capacity")) return MutedPurple;
            if (name.Contains("State") || name.Contains("Icon") || name.Contains("Value"))
                return new Color(.78f, .68f, 1f, 1f);
            return WhitePurple;
        }

        private static void AddInteraction(GameObject go)
        {
            if (!go.GetComponent<SurvivalUIInteractableFX>())
                Undo.AddComponent<SurvivalUIInteractableFX>(go);
        }

        private static string GetPath(Transform target)
        {
            string path = target.name;
            while (target.parent)
            {
                target = target.parent;
                path = target.name + "/" + path;
            }
            return path;
        }

        private static int GetVersion(MonoBehaviour panel)
        {
            if (!panel) return StyleVersion;
            var property = new SerializedObject(panel).FindProperty("editorStyleVersion");
            return property?.intValue ?? 0;
        }

        private static void ApplyPrefab(string path)
        {
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(path)) return;
            var root = PrefabUtility.LoadPrefabContents(path);
            ApplyToRoot(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
#endif
