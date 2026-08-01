#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace _001_Scripts.Core._000_World._001_Water.Editor
{
    [CustomEditor(typeof(RiverTerrainCarver))]
    internal sealed class RiverTerrainCarverEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            RiverTerrainCarver carver = (RiverTerrainCarver)target;
            EditorGUILayout.HelpBox("Terrain is changed only when Apply Carving is pressed. Unity Undo restores the previous TerrainData.", MessageType.Info);
            using (new EditorGUI.DisabledScope(carver.River == null || carver.Terrain == null))
            {
                if (GUILayout.Button("Apply Carving")) Apply(carver);
            }
        }

        private static void Apply(RiverTerrainCarver carver)
        {
            Terrain terrain = carver.Terrain;
            TerrainData data = terrain.terrainData;
            if (data == null) return;

            SplineRiverWaterBody river = carver.River;
            river.Rebuild();
            Bounds bounds = river.WorldBounds;
            Vector3 terrainPosition = terrain.transform.position;
            Vector3 terrainSize = data.size;
            int resolution = data.heightmapResolution;
            int maxIndex = resolution - 1;

            int minX = Mathf.Clamp(Mathf.FloorToInt((bounds.min.x - terrainPosition.x) / terrainSize.x * maxIndex), 0, maxIndex);
            int maxX = Mathf.Clamp(Mathf.CeilToInt((bounds.max.x - terrainPosition.x) / terrainSize.x * maxIndex), 0, maxIndex);
            int minZ = Mathf.Clamp(Mathf.FloorToInt((bounds.min.z - terrainPosition.z) / terrainSize.z * maxIndex), 0, maxIndex);
            int maxZ = Mathf.Clamp(Mathf.CeilToInt((bounds.max.z - terrainPosition.z) / terrainSize.z * maxIndex), 0, maxIndex);
            int width = maxX - minX + 1;
            int height = maxZ - minZ + 1;
            if (width <= 0 || height <= 0) return;

            float[,] heights = data.GetHeights(minX, minZ, width, height);
            Undo.RegisterCompleteObjectUndo(data, "Carve River Terrain");

            for (int z = 0; z < height; z++)
            {
                float worldZ = terrainPosition.z + (minZ + z) / (float)maxIndex * terrainSize.z;
                for (int x = 0; x < width; x++)
                {
                    float worldX = terrainPosition.x + (minX + x) / (float)maxIndex * terrainSize.x;
                    if (!river.TryGetNearestCenterline(new Vector3(worldX, 0f, worldZ),
                            out Vector3 center, out float riverWidth, out float riverDepth, out float distance)) continue;

                    float halfWidth = riverWidth * 0.5f;
                    float outer = halfWidth + carver.BankFalloff;
                    if (distance > outer) continue;
                    float blend = distance <= halfWidth || carver.BankFalloff <= 0.0001f
                        ? 1f
                        : 1f - Mathf.SmoothStep(0f, 1f, (distance - halfWidth) / carver.BankFalloff);
                    float targetHeight = Mathf.Clamp01((center.y - riverDepth - terrainPosition.y) / terrainSize.y);
                    float carved = Mathf.Lerp(heights[z, x], targetHeight, blend);
                    heights[z, x] = Mathf.Min(heights[z, x], carved);
                }
            }

            data.SetHeights(minX, minZ, heights);
            EditorUtility.SetDirty(data);
            EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);
        }
    }
}
#endif
