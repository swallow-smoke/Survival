#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace _001_Scripts.Core._000_World._001_Water.Editor
{
    [CustomEditor(typeof(SplineRiverWaterBody))]
    internal sealed class SplineRiverWaterBodyEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            bool changed = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();

            SplineRiverWaterBody river = (SplineRiverWaterBody)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("River Build", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Cached Samples", river.SampleCount.ToString());

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate / Rebuild"))
                {
                    Undo.RecordObject(river, "Rebuild River");
                    river.Rebuild();
                    MarkDirty(river);
                }

                if (GUILayout.Button("Clear Generated Mesh"))
                {
                    Undo.RecordObject(river, "Clear River Mesh");
                    river.ClearGeneratedMesh();
                    MarkDirty(river);
                }
            }

            if (GUILayout.Button("Bake Mesh Asset")) BakeMesh(river);
            if (changed && river.AutoRebuild)
            {
                river.Rebuild();
                MarkDirty(river);
            }

            DrawValidation(river);
        }

        private static void BakeMesh(SplineRiverWaterBody river)
        {
            river.Rebuild();
            Mesh source = river.GeneratedMesh;
            if (source == null)
            {
                EditorUtility.DisplayDialog("River Mesh", "Generate a valid river mesh first.", "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanelInProject("Bake River Mesh", river.name + "_RiverMesh",
                "asset", "Choose where to save the generated river mesh.");
            if (string.IsNullOrEmpty(path)) return;

            Mesh baked = UnityEngine.Object.Instantiate(source);
            baked.name = river.name + "_RiverMesh";
            baked.hideFlags = HideFlags.None;
            AssetDatabase.CreateAsset(baked, path);
            AssetDatabase.SaveAssets();
            Undo.RecordObject(river.GetComponent<MeshFilter>(), "Assign Baked River Mesh");
            river.GetComponent<MeshFilter>().sharedMesh = baked;
            MarkDirty(river);
        }

        private static void DrawValidation(SplineRiverWaterBody river)
        {
            if (river.GetComponent<UnityEngine.Splines.SplineContainer>() == null)
                EditorGUILayout.HelpBox("SplineContainer is required.", MessageType.Error);
            if (river.GetComponent<MeshRenderer>().sharedMaterial == null)
                EditorGUILayout.HelpBox("Assign a river material. The default project material was not found.", MessageType.Warning);
            if (river.SampleCount < 2)
                EditorGUILayout.HelpBox("The spline needs at least two knots before a mesh can be generated.", MessageType.Info);
        }

        private static void MarkDirty(SplineRiverWaterBody river)
        {
            EditorUtility.SetDirty(river);
            if (river.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(river.gameObject.scene);
        }
    }
}
#endif
