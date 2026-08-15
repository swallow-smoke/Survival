#if UNITY_EDITOR
using System.Collections.Generic;
using _001_Scripts.Data.Item;
using _001_Scripts.Data.Item.Attributes;
using _001_Scripts.Data.Item.Modifier;
using _001_Scripts.Data.SOJ;
using _001_Scripts.Object.Item;
using _001_Scripts.Type.Item;
using UnityEditor;
using UnityEngine;
using ItemDefinition = _001_Scripts.Data.Item.Item;
using ItemAttribute = _001_Scripts.Data.Item.Attributes.Attributes;
using ItemModifier = _001_Scripts.Data.Item.Modifier.Modifier;

namespace _001_Scripts.Editor
{
    [InitializeOnLoad]
    internal static class SurvivalPrototypeItemPrefabBuilder
    {
        private const string PrefabFolder = "Assets/003_Resources/Prefabs/FirstPersonItems";
        private const string MaterialFolder = "Assets/003_Resources/Materials/PrototypeItems";
        private const string DatabasePath = "Assets/003_Resources/Data/ItemDataBase.asset";
        internal const int BuildToolItemId = 19;

        static SurvivalPrototypeItemPrefabBuilder() => EditorApplication.delayCall += BuildIfNeeded;

        [MenuItem("Tools/Survival/Rebuild Prototype First Person Items")]
        private static void RebuildMenu() => Build(rebuild: true);

        private static void BuildIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Build(rebuild: false);
        }

        private static void Build(bool rebuild)
        {
            EnsureFolder(PrefabFolder);
            EnsureFolder(MaterialFolder);

            Material dark = Material("Prototype_Dark", new Color(.045f, .055f, .075f), .65f, .55f);
            Material metal = Material("Prototype_Metal", new Color(.22f, .25f, .31f), .85f, .68f);
            Material cyan = Material("Prototype_Cyan", new Color(.04f, .52f, .72f), .3f, .7f, true);
            Material orange = Material("Prototype_Orange", new Color(.85f, .26f, .07f), .42f, .55f);
            Material yellow = Material("Prototype_Yellow", new Color(.95f, .72f, .08f), .25f, .52f, true);
            Material white = Material("Prototype_White", new Color(.72f, .8f, .88f), .5f, .7f);

            GameObject scanner = BuildOrLoad("FP_Scanner", rebuild,
                () => BuildScanner(dark, metal, cyan));
            GameObject harvest = BuildOrLoad("FP_HarvestTool", rebuild,
                () => BuildHarvestTool(dark, metal, orange, cyan));
            GameObject flashlight = BuildOrLoad("FP_Flashlight", rebuild,
                () => BuildFlashlight(dark, metal, yellow));
            GameObject oxygen = BuildOrLoad("FP_OxygenTank", rebuild,
                () => BuildOxygenTank(dark, metal, white, cyan, orange));
            GameObject buildTool = BuildOrLoad("FP_BuildTool", rebuild,
                () => BuildBuildTool(dark, metal, cyan, yellow));

            AssignItems(scanner, harvest, flashlight, oxygen, buildTool);
            AssetDatabase.SaveAssets();
            if (rebuild) AssetDatabase.Refresh();
        }

        private static GameObject BuildScanner(Material dark, Material metal, Material cyan)
        {
            GameObject root = Root("FP_Scanner");
            Part(root.transform, PrimitiveType.Cube, "Body", Vector3.zero,
                new Vector3(.22f, .14f, .3f), Vector3.zero, dark);
            Renderer screen = Part(root.transform, PrimitiveType.Cube, "ScanScreen", new Vector3(0f, .078f, -.015f),
                new Vector3(.17f, .018f, .17f), Vector3.zero, cyan);
            Part(root.transform, PrimitiveType.Cube, "LowerGrip", new Vector3(0f, -.13f, -.04f),
                new Vector3(.105f, .16f, .105f), new Vector3(12f, 0f, 0f), metal);
            Part(root.transform, PrimitiveType.Cylinder, "ScannerNose", new Vector3(0f, .015f, .17f),
                new Vector3(.075f, .025f, .075f), new Vector3(90f, 0f, 0f), cyan);
            root.AddComponent<PrototypeHeldItemAction>().Configure(PrototypeHeldItemKind.Scanner, null, screen);
            return root;
        }

        private static GameObject BuildHarvestTool(Material dark, Material metal, Material orange, Material cyan)
        {
            GameObject root = Root("FP_HarvestTool");
            Part(root.transform, PrimitiveType.Cylinder, "Grip", new Vector3(0f, -.02f, 0f),
                new Vector3(.035f, .23f, .035f), new Vector3(8f, 0f, -12f), dark);
            Part(root.transform, PrimitiveType.Cube, "Head", new Vector3(-.045f, .205f, 0f),
                new Vector3(.29f, .045f, .07f), new Vector3(0f, 0f, -8f), metal);
            Part(root.transform, PrimitiveType.Cube, "CuttingEdge", new Vector3(-.19f, .235f, 0f),
                new Vector3(.105f, .02f, .085f), new Vector3(0f, 0f, 18f), orange);
            Renderer cell = Part(root.transform, PrimitiveType.Cube, "PowerCell", new Vector3(.035f, -.055f, .025f),
                new Vector3(.055f, .12f, .055f), Vector3.zero, cyan);
            root.AddComponent<PrototypeHeldItemAction>().Configure(PrototypeHeldItemKind.HarvestTool, null, cell);
            return root;
        }

        private static GameObject BuildFlashlight(Material dark, Material metal, Material yellow)
        {
            GameObject root = Root("FP_Flashlight");
            Part(root.transform, PrimitiveType.Cylinder, "Body", Vector3.zero,
                new Vector3(.065f, .17f, .065f), new Vector3(90f, 0f, 0f), dark);
            Part(root.transform, PrimitiveType.Cylinder, "Bezel", new Vector3(0f, 0f, .18f),
                new Vector3(.095f, .035f, .095f), new Vector3(90f, 0f, 0f), metal);
            Renderer lens = Part(root.transform, PrimitiveType.Cylinder, "Lens", new Vector3(0f, 0f, .218f),
                new Vector3(.076f, .008f, .076f), new Vector3(90f, 0f, 0f), yellow);
            Part(root.transform, PrimitiveType.Cube, "Grip", new Vector3(0f, -.105f, -.035f),
                new Vector3(.075f, .14f, .085f), new Vector3(-8f, 0f, 0f), dark);

            GameObject lightObject = new GameObject("SpotLight", typeof(Light));
            lightObject.transform.SetParent(root.transform, false);
            lightObject.transform.localPosition = new Vector3(0f, 0f, .23f);
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Spot;
            light.range = 22f;
            light.spotAngle = 44f;
            light.innerSpotAngle = 28f;
            light.intensity = 8f;
            light.color = new Color(.74f, .9f, 1f);
            light.shadows = LightShadows.Soft;
            light.enabled = false;
            root.AddComponent<PrototypeHeldItemAction>().Configure(PrototypeHeldItemKind.Flashlight, light, lens);
            return root;
        }

        private static GameObject BuildOxygenTank(Material dark, Material metal, Material white,
            Material cyan, Material orange)
        {
            GameObject root = Root("FP_OxygenTank");
            Part(root.transform, PrimitiveType.Cylinder, "Tank", Vector3.zero,
                new Vector3(.105f, .23f, .105f), Vector3.zero, white);
            Part(root.transform, PrimitiveType.Cylinder, "TopBand", new Vector3(0f, .16f, 0f),
                new Vector3(.11f, .035f, .11f), Vector3.zero, orange);
            Part(root.transform, PrimitiveType.Cylinder, "BottomBand", new Vector3(0f, -.16f, 0f),
                new Vector3(.11f, .035f, .11f), Vector3.zero, dark);
            Part(root.transform, PrimitiveType.Cube, "Valve", new Vector3(0f, .255f, 0f),
                new Vector3(.075f, .065f, .075f), Vector3.zero, metal);
            Renderer gauge = Part(root.transform, PrimitiveType.Cylinder, "Gauge", new Vector3(.075f, .25f, -.01f),
                new Vector3(.045f, .014f, .045f), new Vector3(90f, 0f, 0f), cyan);
            Part(root.transform, PrimitiveType.Cube, "CarryHandle", new Vector3(-.11f, .03f, 0f),
                new Vector3(.04f, .19f, .055f), Vector3.zero, dark);
            root.AddComponent<PrototypeHeldItemAction>().Configure(PrototypeHeldItemKind.OxygenTank, null, gauge);
            return root;
        }

        private static GameObject BuildBuildTool(Material dark, Material metal, Material cyan, Material yellow)
        {
            GameObject root = Root("FP_BuildTool");
            Part(root.transform, PrimitiveType.Cylinder, "Grip", new Vector3(0f, -.11f, -.03f),
                new Vector3(.045f, .11f, .045f), new Vector3(14f, 0f, 0f), dark);
            Part(root.transform, PrimitiveType.Cube, "Body", Vector3.zero,
                new Vector3(.13f, .12f, .26f), Vector3.zero, metal);
            Part(root.transform, PrimitiveType.Cube, "EmitterArmLeft", new Vector3(-.055f, .02f, .19f),
                new Vector3(.028f, .028f, .14f), new Vector3(0f, -9f, 0f), dark);
            Part(root.transform, PrimitiveType.Cube, "EmitterArmRight", new Vector3(.055f, .02f, .19f),
                new Vector3(.028f, .028f, .14f), new Vector3(0f, 9f, 0f), dark);
            Renderer emitter = Part(root.transform, PrimitiveType.Cylinder, "Emitter", new Vector3(0f, .02f, .27f),
                new Vector3(.06f, .018f, .06f), new Vector3(90f, 0f, 0f), cyan);
            Part(root.transform, PrimitiveType.Cube, "BlueprintPlate", new Vector3(0f, .072f, -.02f),
                new Vector3(.1f, .015f, .13f), new Vector3(-16f, 0f, 0f), yellow);
            root.AddComponent<PrototypeHeldItemAction>().Configure(PrototypeHeldItemKind.BuildTool, null, emitter);
            return root;
        }

        private static GameObject Root(string name)
        {
            GameObject root = new GameObject(name);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            return root;
        }

        private static Renderer Part(Transform parent, PrimitiveType type, string name, Vector3 position,
            Vector3 scale, Vector3 euler, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localRotation = Quaternion.Euler(euler);
            part.transform.localScale = scale;
            Collider collider = part.GetComponent<Collider>();
            if (collider) UnityEngine.Object.DestroyImmediate(collider);
            Renderer renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        private static GameObject BuildOrLoad(string name, bool rebuild, System.Func<GameObject> factory)
        {
            string path = $"{PrefabFolder}/{name}.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing && !rebuild) return existing;
            GameObject temporary = factory();
            try { return PrefabUtility.SaveAsPrefabAsset(temporary, path); }
            finally { UnityEngine.Object.DestroyImmediate(temporary); }
        }

        private static void AssignItems(GameObject scanner, GameObject harvest, GameObject flashlight,
            GameObject oxygen, GameObject buildTool)
        {
            ItemDataBase database = AssetDatabase.LoadAssetAtPath<ItemDataBase>(DatabasePath);
            if (!database) return;

            Assign(database, 14, scanner, addQuickSlot: true);
            Assign(database, 6, harvest, addQuickSlot: true);
            if (database.itemList.TryGetValue(17, out ItemDefinition oxygenItem))
            {
                oxygenItem.equipmentSlot = EquipmentSlotType.Body;
                oxygenItem.firstPersonPrefab = null;
                RemoveAttribute(oxygenItem, AttributesType.QuickSlottable);
            }

            if (!database.itemList.TryGetValue(18, out ItemDefinition flashlightItem))
            {
                flashlightItem = new ItemDefinition
                {
                    itemId = 18,
                    itemName = "Field Flashlight",
                    itemDesc = "Compact waterproof flashlight for dark underwater spaces.",
                    itemGrade = ItemGrade.common,
                    itemType = ItemType.weapon,
                    weight = .7f,
                    ItemAttributes = new List<ItemAttribute>
                    {
                        Attribute(AttributesType.Equippable, ModifierType.DurabilityMax, 120f),
                        Attribute(AttributesType.QuickSlottable)
                    }
                };
                database.itemList.Add(18, flashlightItem);
            }
            flashlightItem.firstPersonPrefab = flashlight;
            EnsureAttribute(flashlightItem, AttributesType.QuickSlottable);

            if (!database.itemList.TryGetValue(BuildToolItemId, out ItemDefinition buildToolItem))
            {
                buildToolItem = new ItemDefinition
                {
                    itemId = BuildToolItemId,
                    itemName = "건축 도구",
                    itemDesc = "청사진을 홀로그램으로 투사해 구조물을 배치하는 휴대용 건축 도구입니다.",
                    itemGrade = ItemGrade.common,
                    itemType = ItemType.weapon,
                    weight = 1.4f,
                    ItemAttributes = new List<ItemAttribute>
                    {
                        Attribute(AttributesType.Equippable, ModifierType.DurabilityMax, 150f),
                        Attribute(AttributesType.QuickSlottable),
                        Attribute(AttributesType.Buildable)
                    }
                };
                database.itemList.Add(BuildToolItemId, buildToolItem);
            }
            buildToolItem.firstPersonPrefab = buildTool;
            EnsureAttribute(buildToolItem, AttributesType.QuickSlottable);
            EnsureAttribute(buildToolItem, AttributesType.Buildable);
            EditorUtility.SetDirty(database);
        }

        private static void Assign(ItemDataBase database, int id, GameObject prefab, bool addQuickSlot)
        {
            if (!database.itemList.TryGetValue(id, out ItemDefinition item)) return;
            item.firstPersonPrefab = prefab;
            if (addQuickSlot) EnsureAttribute(item, AttributesType.QuickSlottable);
        }

        private static void EnsureAttribute(ItemDefinition item, AttributesType type)
        {
            item.ItemAttributes ??= new List<ItemAttribute>();
            if (item.ItemAttributes.Exists(attribute => attribute != null && attribute.attrType == type)) return;
            item.ItemAttributes.Add(Attribute(type));
        }

        private static void RemoveAttribute(ItemDefinition item, AttributesType type)
        {
            item.ItemAttributes?.RemoveAll(attribute => attribute != null && attribute.attrType == type);
        }

        private static ItemAttribute Attribute(AttributesType type) => new()
        {
            attrType = type,
            modifiers = new List<ItemModifier>()
        };

        private static ItemAttribute Attribute(AttributesType type, ModifierType modifier, float value) => new()
        {
            attrType = type,
            modifiers = new List<ItemModifier> { new() { modifierType = modifier, value = value } }
        };

        private static Material Material(string name, Color color, float metallic, float smoothness,
            bool emission = false)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (emission)
            {
                material.EnableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color * 2.5f);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
