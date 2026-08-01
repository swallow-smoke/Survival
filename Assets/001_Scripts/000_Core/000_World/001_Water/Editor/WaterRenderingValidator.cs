#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace _001_Scripts.Core._000_World._001_Water.Editor
{
    internal static class WaterRenderingValidator
    {
        [MenuItem("Tools/Survival/Water/Validate URP Rendering")]
        private static void Validate()
        {
            UniversalRenderPipelineAsset asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (asset == null)
            {
                Debug.LogError("[Water] The active Render Pipeline is not URP.");
                return;
            }

            SerializedObject serialized = new SerializedObject(asset);
            SerializedProperty depth = serialized.FindProperty("m_RequireDepthTexture");
            SerializedProperty opaque = serialized.FindProperty("m_RequireOpaqueTexture");
            bool depthEnabled = depth == null || depth.boolValue;
            bool opaqueEnabled = opaque == null || opaque.boolValue;
            if (!depthEnabled || !opaqueEnabled)
            {
                Debug.LogWarning($"[Water] URP asset '{asset.name}' should enable Depth Texture and Opaque Texture for depth colour, foam and refraction. Settings were not changed automatically.", asset);
                return;
            }

            Debug.Log($"[Water] URP asset '{asset.name}' has the required Depth and Opaque textures enabled.", asset);
        }
    }
}
#endif
