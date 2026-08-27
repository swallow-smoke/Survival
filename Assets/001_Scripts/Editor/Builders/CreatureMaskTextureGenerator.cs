using System.IO;
using UnityEditor;
using UnityEngine;

namespace AstraNope.Editor
{
    public static class CreatureMaskTextureGenerator
    {
        private const string OutputFolder = "Assets/003_Resources/Textures/Creature";
        private const int Resolution = 512;

        [MenuItem("Tools/Survival/Creatures/Generate Placeholder Masks")]
        public static void Generate()
        {
            Directory.CreateDirectory(OutputFolder);
            Write("CreatureRegionMask", BuildRegionMask());
            Write("CreaturePatternMask", BuildPatternMask());
            Write("CreatureSpecialMask", BuildSpecialMask());
            AssetDatabase.Refresh();
            Debug.Log($"Creature placeholder masks written to {OutputFolder}.");
        }

        private static Color[] BuildRegionMask()
        {
            Color[] pixels = new Color[Resolution * Resolution];
            for (int y = 0; y < Resolution; y++)
            {
                float v = y / (float)(Resolution - 1);
                for (int x = 0; x < Resolution; x++)
                {
                    float u = x / (float)(Resolution - 1);
                    float accent = Mathf.Clamp01(1f - Mathf.Abs(u - 0.5f) * 6f) *
                                   Mathf.Clamp01(1f - Mathf.Abs(v - 0.78f) * 10f);
                    float secondary = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((0.45f - v) * 3f));
                    float primary = Mathf.Clamp01(1f - secondary - accent);
                    pixels[y * Resolution + x] = new Color(primary, secondary, accent, 1f);
                }
            }
            return pixels;
        }

        private static Color[] BuildPatternMask()
        {
            Color[] pixels = new Color[Resolution * Resolution];
            for (int y = 0; y < Resolution; y++)
            {
                float v = y / (float)(Resolution - 1);
                for (int x = 0; x < Resolution; x++)
                {
                    float u = x / (float)(Resolution - 1);

                    float stripes = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 9f)) > 0.62f ? 1f : 0f;
                    float spots = SpotField(u, v);
                    float twoTone = v > 0.55f ? 1f : 0f;
                    float gradient = Mathf.SmoothStep(0f, 1f, v);

                    pixels[y * Resolution + x] = new Color(stripes, spots, twoTone, gradient);
                }
            }
            return pixels;
        }

        private static Color[] BuildSpecialMask()
        {
            Color[] pixels = new Color[Resolution * Resolution];
            for (int y = 0; y < Resolution; y++)
            {
                float v = y / (float)(Resolution - 1);
                for (int x = 0; x < Resolution; x++)
                {
                    float u = x / (float)(Resolution - 1);
                    float rings = Mathf.Abs(Mathf.Sin(
                        Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f)) * Mathf.PI * 14f));
                    float value = rings > 0.78f ? 1f : 0f;
                    pixels[y * Resolution + x] = new Color(value, value, value, 1f);
                }
            }
            return pixels;
        }

        private static float SpotField(float u, float v)
        {
            const int cells = 8;
            float cu = u * cells;
            float cv = v * cells;
            int gx = Mathf.FloorToInt(cu);
            int gy = Mathf.FloorToInt(cv);
            Random.InitState(gx * 73856093 ^ gy * 19349663);
            Vector2 center = new Vector2(gx + Random.value, gy + Random.value);
            float radius = 0.18f + Random.value * 0.14f;
            return Vector2.Distance(new Vector2(cu, cv), center) < radius ? 1f : 0f;
        }

        private static void Write(string name, Color[] pixels)
        {
            Texture2D texture = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false, true);
            texture.SetPixels(pixels);
            texture.Apply();
            string path = Path.Combine(OutputFolder, name + ".png");
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.sRGBTexture = false;
                importer.alphaIsTransparency = false;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
        }
    }
}
