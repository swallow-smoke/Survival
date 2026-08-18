Shader "Survival/Creature"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        [NoScaleOffset] _RegionMask ("Region Mask (R=Primary G=Secondary B=Accent)", 2D) = "red" {}
        [NoScaleOffset] _PatternMask ("Pattern Mask (R=Stripes G=Spots B=TwoTone A=Gradient)", 2D) = "black" {}
        [NoScaleOffset] _SpecialMask ("Special Pattern Mask (R)", 2D) = "black" {}
        _PrimaryColor ("Primary Color", Color) = (1,1,1,1)
        _SecondaryColor ("Secondary Color", Color) = (0.8,0.8,0.8,1)
        _AccentColor ("Accent Color", Color) = (1,1,1,1)
        _PatternColor ("Pattern Color", Color) = (0,0,0,1)
        _PatternParams ("Pattern Params (x=kind y=strength)", Vector) = (0,0,0,0)
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex LitPassVertex
            #pragma fragment CreatureFragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"

            TEXTURE2D(_RegionMask);      SAMPLER(sampler_RegionMask);
            TEXTURE2D(_PatternMask);     SAMPLER(sampler_PatternMask);
            TEXTURE2D(_SpecialMask);     SAMPLER(sampler_SpecialMask);

            CBUFFER_START(UnityPerMaterialCreature)
                float4 _PrimaryColor;
                float4 _SecondaryColor;
                float4 _AccentColor;
                float4 _PatternColor;
                float4 _PatternParams;
            CBUFFER_END

            #ifdef UNITY_DOTS_INSTANCING_ENABLED
            UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                UNITY_DOTS_INSTANCED_PROP(float4, _PrimaryColor)
                UNITY_DOTS_INSTANCED_PROP(float4, _SecondaryColor)
                UNITY_DOTS_INSTANCED_PROP(float4, _AccentColor)
                UNITY_DOTS_INSTANCED_PROP(float4, _PatternColor)
                UNITY_DOTS_INSTANCED_PROP(float4, _PatternParams)
            UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

            #define _PrimaryColor    UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _PrimaryColor)
            #define _SecondaryColor  UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _SecondaryColor)
            #define _AccentColor     UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _AccentColor)
            #define _PatternColor    UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _PatternColor)
            #define _PatternParams   UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _PatternParams)
            #endif

            // Pattern kinds mirror WorldBuilder.Entities.Creatures.CreaturePatternKind.
            // 0 = None, 1 = Stripes, 2 = Spots, 3 = TwoTone, 4 = Gradient, 5 = Special
            float SamplePatternCoverage(float2 uv, float kind)
            {
                float4 packed = SAMPLE_TEXTURE2D(_PatternMask, sampler_PatternMask, uv);
                float coverage = 0.0;
                coverage += packed.r * step(0.5, kind) * step(kind, 1.5);
                coverage += packed.g * step(1.5, kind) * step(kind, 2.5);
                coverage += packed.b * step(2.5, kind) * step(kind, 3.5);
                coverage += packed.a * step(3.5, kind) * step(kind, 4.5);
                coverage += SAMPLE_TEXTURE2D(_SpecialMask, sampler_SpecialMask, uv).r * step(4.5, kind);
                return saturate(coverage);
            }

            half3 ResolveCreatureAlbedo(float2 uv, half3 baseAlbedo)
            {
                float3 regions = SAMPLE_TEXTURE2D(_RegionMask, sampler_RegionMask, uv).rgb;
                float total = max(regions.r + regions.g + regions.b, 1e-4);
                regions /= total;

                half3 tint = _PrimaryColor.rgb * regions.r
                           + _SecondaryColor.rgb * regions.g
                           + _AccentColor.rgb * regions.b;

                float kind = _PatternParams.x;
                float strength = saturate(_PatternParams.y);
                float coverage = SamplePatternCoverage(uv, kind) * strength * step(0.5, kind);
                tint = lerp(tint, _PatternColor.rgb, coverage);

                return baseAlbedo * tint;
            }

            half4 CreatureFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                SurfaceData surfaceData;
                InitializeStandardLitSurfaceData(input.uv, surfaceData);
                surfaceData.albedo = ResolveCreatureAlbedo(input.uv, surfaceData.albedo);

                InputData inputData;
                InitializeInputData(input, surfaceData.normalTS, inputData);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
