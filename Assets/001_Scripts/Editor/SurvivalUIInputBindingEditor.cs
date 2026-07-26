#if UNITY_EDITOR
using _001_Scripts.Controller.Handler;
using _001_Scripts.UI;
using _001_Scripts.UI.Component;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _001_Scripts.Editor
{
    [InitializeOnLoad]
    internal static class SurvivalUIInputBindingEditor
    {
        private const int BindingVersion = 4;

        static SurvivalUIInputBindingEditor()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.delayCall += ApplyIfNeeded;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += ApplyIfNeeded;
        }

        [MenuItem("Tools/Survival UI/Repair Inventory and Craft Input Bindings")]
        private static void ApplyFromMenu() => Apply(true);

        private static void ApplyIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.name != "SampleScene") return;
            var input = UnityEngine.Object.FindFirstObjectByType<InputHandler>(FindObjectsInactive.Include);
            if (!input) return;
            var version = new SerializedObject(input).FindProperty("uiBindingVersion");
            var inventory = UnityEngine.Object.FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
            var craft = UnityEngine.Object.FindFirstObjectByType<CraftPanel>(FindObjectsInactive.Include);
            var inventoryButton = inventory ? new SerializedObject(inventory).FindProperty("inventoryTabButton") : null;
            var craftButton = inventory ? new SerializedObject(inventory).FindProperty("craftTabButton") : null;
            var craftInventoryButton = craft ? new SerializedObject(craft).FindProperty("inventoryTabButton") : null;
            if (version != null && version.intValue >= BindingVersion &&
                inventoryButton != null && inventoryButton.objectReferenceValue != null &&
                craftButton != null && craftButton.objectReferenceValue != null &&
                craftInventoryButton != null && craftInventoryButton.objectReferenceValue != null &&
                inventory && inventory.transform.localScale != Vector3.zero &&
                craft && craft.transform.localScale != Vector3.zero) return;
            Apply(true);
        }

        private static void Apply(bool saveScene)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var input = UnityEngine.Object.FindFirstObjectByType<InputHandler>(FindObjectsInactive.Include);
            var inventory = UnityEngine.Object.FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
            var craft = UnityEngine.Object.FindFirstObjectByType<CraftPanel>(FindObjectsInactive.Include);
            if (!input || !inventory || !craft) return;

            Undo.RegisterFullObjectHierarchyUndo(inventory.gameObject, "Connect Inventory Craft Tab");
            inventory.transform.localScale = Vector3.one;
            craft.transform.localScale = Vector3.one;
            EnsureNavigationForPanels(inventory, craft);

            var inputSerialized = new SerializedObject(input);
            var version = inputSerialized.FindProperty("uiBindingVersion");
            if (version != null) version.intValue = BindingVersion;
            inputSerialized.ApplyModifiedPropertiesWithoutUndo();
            RemoveLegacyInventoryUnityEvent(input.GetComponent<PlayerInput>());

            SurvivalUIRoundedStyle.ApplyToRoot(inventory.gameObject);
            SurvivalUIRoundedStyle.ApplyToRoot(craft.gameObject);
            EditorUtility.SetDirty(input);
            EditorUtility.SetDirty(inventory);
            EditorUtility.SetDirty(craft);
            var scene = inventory.gameObject.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            if (saveScene) EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Survival UI] Bound Tab/V and added working Inventory/Craft top navigation.");
        }

        internal static void EnsureNavigationForPanels(InventoryPanel inventory, CraftPanel craft)
        {
            if (!inventory || !craft) return;
            var inventoryRoot = inventory.transform.Find("UGUI_InventoryRoot");
            var craftRoot = craft.transform.Find("UGUI_CraftRoot");
            if (!inventoryRoot || !craftRoot) return;

            Undo.RegisterFullObjectHierarchyUndo(inventory.gameObject, "Create Inventory Top Navigation");
            Undo.RegisterFullObjectHierarchyUndo(craft.gameObject, "Create Craft Top Navigation");
            var inventoryTabs = EnsureTopNavigation(inventoryRoot, true);
            var craftTabs = EnsureTopNavigation(craftRoot, false);
            AssignTabs(inventory, inventoryTabs[0], inventoryTabs[1]);
            AssignTabs(craft, craftTabs[0], craftTabs[1]);

            var legacy = inventory.transform.Find("UGUI_InventoryRoot/InventoryCard/Header/CraftTabButton");
            if (legacy && legacy != inventoryTabs[1].transform)
                Undo.DestroyObjectImmediate(legacy.gameObject);
        }

        private static Button[] EnsureTopNavigation(Transform root, bool inventoryActive)
        {
            var existing = root.Find("TopNavigation");
            if (existing)
            {
                var inventoryButton = existing.Find("InventoryTabButton")?.GetComponent<Button>();
                var craftButton = existing.Find("CraftTabButton")?.GetComponent<Button>();
                if (inventoryButton && craftButton) return new[] { inventoryButton, craftButton };
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            var nav = new GameObject("TopNavigation", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Outline));
            nav.layer = 5;
            nav.transform.SetParent(root, false);
            var navRect = nav.GetComponent<RectTransform>();
            navRect.anchorMin = new Vector2(.5f, 1f);
            navRect.anchorMax = new Vector2(.5f, 1f);
            navRect.pivot = new Vector2(.5f, 1f);
            navRect.anchoredPosition = new Vector2(0f, -20f);
            navRect.sizeDelta = new Vector2(372f, 54f);
            navRect.localScale = Vector3.one;
            nav.GetComponent<Image>().color = new Color(.055f, .035f, .11f, .86f);
            var outline = nav.GetComponent<Outline>();
            outline.effectColor = new Color(.82f, .72f, 1f, .24f);
            outline.effectDistance = new Vector2(1f, -1f);

            var inventory = CreateTabButton("InventoryTabButton", nav.transform, "인벤토리   [TAB]",
                new Vector2(8f, 7f), new Vector2(182f, -7f), inventoryActive);
            var craft = CreateTabButton("CraftTabButton", nav.transform, "제작   [V]",
                new Vector2(190f, 7f), new Vector2(364f, -7f), !inventoryActive);
            return new[] { inventory, craft };
        }

        private static Button CreateTabButton(string name, Transform parent, string value,
            Vector2 offsetMin, Vector2 offsetMax, bool active)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button), typeof(SurvivalUIInteractableFX));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
            var image = go.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = active
                ? new Color(.52f, .34f, .84f, .96f)
                : new Color(.19f, .12f, .31f, .82f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.layer = 5;
            labelObject.transform.SetParent(go.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(6, 4);
            labelRect.offsetMax = new Vector2(-6, -4);
            var label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = value;
            label.fontSize = 13;
            label.fontStyle = FontStyle.Bold;
            label.color = new Color(.93f, .89f, 1f, 1f);
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            return button;
        }

        private static void AssignTabs(InventoryPanel panel, Button inventory, Button craft)
        {
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("inventoryTabButton").objectReferenceValue = inventory;
            serialized.FindProperty("craftTabButton").objectReferenceValue = craft;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(panel);
        }

        private static void AssignTabs(CraftPanel panel, Button inventory, Button craft)
        {
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("inventoryTabButton").objectReferenceValue = inventory;
            serialized.FindProperty("craftTabButton").objectReferenceValue = craft;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(panel);
        }

        private static void RemoveLegacyInventoryUnityEvent(PlayerInput playerInput)
        {
            if (!playerInput) return;
            var serialized = new SerializedObject(playerInput);
            var events = serialized.FindProperty("m_ActionEvents");
            if (events == null) return;
            for (int i = 0; i < events.arraySize; i++)
            {
                var entry = events.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("m_ActionId")?.stringValue !=
                    "b38420cd-c2ad-49d4-8c5e-a8235118750f") continue;
                entry.FindPropertyRelative("m_ActionName").stringValue = "Player/Inventory[/Keyboard/tab]";
                var persistentCalls = entry.FindPropertyRelative("m_PersistentCalls");
                var calls = persistentCalls?.FindPropertyRelative("m_Calls");
                calls?.ClearArray();
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
