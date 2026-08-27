using System;
using System.Collections.Generic;
using BluePrint = AstraNope.Data.Blueprints;
using UnityEngine;

namespace AstraNope.Data.Databases
{
    [CreateAssetMenu(fileName = "BluePrints", menuName = "Data/Create BluePrints", order = 0)]
    public class BluePrintDataBase : ScriptableObject
    {
        [Serializable]
        private sealed class BluePrintCollection
        {
            public List<BluePrint.BluePrint> blueprints = new();
        }

        [SerializeField, Tooltip("JSON source of truth. Defaults to Resources/Data/Blueprints.json.")]
        private TextAsset jsonSource;
        [SerializeField, HideInInspector]
        private List<BluePrint.BluePrint> bluePrints = new();

        public TextAsset JsonSource => jsonSource;

        private void OnEnable() => Reload();

        public void Reload()
        {
            if (!jsonSource) jsonSource = Resources.Load<TextAsset>("Data/Blueprints");
            if (!jsonSource)
            {
                bluePrints = new List<BluePrint.BluePrint>();
                Debug.LogError("[Blueprints] JSON source was not found at Resources/Data/Blueprints.json.", this);
                return;
            }

            LoadJson(jsonSource.text);
        }

        public void LoadJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Blueprint JSON cannot be empty.", nameof(json));

            var collection = JsonUtility.FromJson<BluePrintCollection>(json);
            if (collection?.blueprints == null)
                throw new FormatException("Blueprint JSON must contain a 'blueprints' array.");

            var ids = new HashSet<int>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var blueprint in collection.blueprints)
            {
                if (blueprint == null) throw new FormatException("Blueprint entries cannot be null.");
                if (!ids.Add(blueprint.bluePrintId))
                    throw new FormatException($"Duplicate blueprint id: {blueprint.bluePrintId}.");
                if (string.IsNullOrWhiteSpace(blueprint.bluePrintName))
                    throw new FormatException($"Blueprint {blueprint.bluePrintId} has no name.");
                if (!names.Add(blueprint.bluePrintName))
                    throw new FormatException($"Duplicate blueprint name: {blueprint.bluePrintName}.");
                blueprint.categoryPath = NormalizeCategoryPath(blueprint.categoryPath);
                blueprint.unlockRequired = Math.Max(1, blueprint.unlockRequired);
                blueprint.unlockProgress = Math.Max(0, blueprint.unlockProgress);
                if (blueprint.isUnlocked)
                    blueprint.unlockProgress = Math.Max(blueprint.unlockProgress, blueprint.unlockRequired);
                else if (blueprint.unlockProgress >= blueprint.unlockRequired)
                    blueprint.isUnlocked = true;
                blueprint.recipe ??= new List<BluePrint.RecipeEntry>();
                foreach (var entry in blueprint.recipe)
                    if (entry == null || entry.count <= 0)
                        throw new FormatException($"Blueprint {blueprint.bluePrintId} has an invalid recipe entry.");
            }

            bluePrints = collection.blueprints;
        }

        private static string NormalizeCategoryPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "Misc";
            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++) parts[i] = parts[i].Trim();
            return parts.Length == 0 ? "Misc" : string.Join("/", parts);
        }

        public BluePrint.BluePrint GetBluePrint(int id) => bluePrints.Find(item => item.bluePrintId == id);
        public BluePrint.BluePrint GetBluePrint(string name) => bluePrints.Find(item =>
            string.Equals(item.bluePrintName, name, StringComparison.OrdinalIgnoreCase));
        public BluePrint.BluePrint GetBluePrint(BluePrint.BluePrint bluePrint) =>
            bluePrints.Find(item => ReferenceEquals(item, bluePrint));
        public IReadOnlyList<BluePrint.BluePrint> GetAllBluePrints() => bluePrints;
        public bool Exist(int id) => bluePrints.Exists(item => item.bluePrintId == id);
    }
}
