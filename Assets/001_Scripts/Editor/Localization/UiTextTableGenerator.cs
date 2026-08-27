using UnityEditor;
using UnityEngine;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace AstraNope.Editor.Localization
{
    /// <summary>
    /// Creates/updates the Localization "ui" String Table Collection from the generated
    /// UiText catalog (Assets/001_Scripts/Data/Localization/UiTextCatalog.g.cs).
    /// Run once after opening the project (and whenever the catalog regenerates):
    /// Tools → Astra Noep → Localization → Update 'ui' Tables from Catalog.
    /// </summary>
    public static class UiTextTableGenerator
    {
        private const string ExportFolder = "Assets/001_Scripts/Localization/Tables";
        private const string TableName = "ui";

        [MenuItem("Tools/Astra Noep/Localization/Update 'ui' Tables from Catalog")]
        public static void UpdateTables()
        {
            EnsureFolder(ExportFolder);

            var collection =
                LocalizationEditorSettings.GetStringTableCollection(TableName) as StringTableCollection;
            if (collection == null)
                collection = LocalizationEditorSettings.CreateStringTableCollection(TableName, ExportFolder);

            var korean = GetOrCreateTable(collection, "ko-KR");
            var english = GetOrCreateTable(collection, "en");

            int addedKo = 0;
            int addedEn = 0;
            var catalog = global::AstraNope.Localization.UiText.Defaults;
            foreach (var pair in catalog)
            {
                if (string.IsNullOrEmpty(pair.Key) || string.IsNullOrEmpty(pair.Value))
                    continue;

                if (korean.SharedData.GetEntry(pair.Key) == null)
                {
                    korean.AddEntry(pair.Key, pair.Value);
                    addedKo++;
                }

                // Seed English with Korean text until real translations are provided.
                if (english.SharedData.GetEntry(pair.Key) == null)
                {
                    english.AddEntry(pair.Key, pair.Value);
                    addedEn++;
                }
            }

            EditorUtility.SetDirty(korean.SharedData);
            EditorUtility.SetDirty(korean);
            EditorUtility.SetDirty(english.SharedData);
            EditorUtility.SetDirty(english);
            AssetDatabase.SaveAssets();

            Debug.Log("[L10n] 'ui' tables updated — " +
                      $"ko-KR new entries: {addedKo}, en seeded: {addedEn}, " +
                      $"catalog total: {catalog.Count}. Tables live under {ExportFolder}.");
        }

        private static StringTable GetOrCreateTable(StringTableCollection collection, string code)
        {
            foreach (var table in collection.StringTables)
            {
                if (table != null && table.LocaleIdentifier.Code == code)
                    return table;
            }

            return collection.AddNewTable((LocaleIdentifier)code) as StringTable;
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
                return;

            var parts = assetPath.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
