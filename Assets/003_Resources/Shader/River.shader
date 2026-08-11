Shader "Custom/TransparentToonWater"
{
    Properties
    {
        [Header(Depth Color)]
        _ShallowColor       ("Shallow Color", Color) = (0.08, 0.68, 0.76, 1)
        _MidColor           ("Mid Color",     Color) = (0.02, 0.36, 0.58, 1)
        _DeepColor          ("Deep Color",    Color) = (0.00, 0.09, 0.24, 1)
        _DepthScale         ("Depth Scale", Float) = 14.0
        _ColorSteps         ("Depth Color Steps", Range(2, 12)) = 5
        _FacetColorStrength ("Facet Color Strength", Range(0, 1)) = 0.55

        [Header(Transparency)]
        _ShallowOpacity     ("Shallow Opacity", Range(0, 1)) = 0.07
        _Transparency       ("Maximum Opacity", Range(0, 1)) = 0.34
        _ShallowTintStrength("Shallow Tint Strength", Range(0, 1)) = 0.04
        _DeepTintStrength   ("Deep Tint Strength", Range(0, 1)) = 0.36

        [Header(Surface Detail)]
        _NormalMapA         ("Normal Map A", 2D) = "bump" {}
        _NormalMapB         ("Normal Map B", 2D) = "bump" {}
        _TilingA            ("Tiling A", Float) = 1.15
        _TilingB            ("Tiling B", Float) = 2.10
        _FlowSpeed          ("Flow Speed", Float) = 0.045
        _NormalStrength     ("Refraction Strength", Range(0, 0.08)) = 0.008
        _MicroNormalStrength("Micro Normal Strength", Range(0, 1)) = 0.16
        _FacetStrength      ("Low Poly Facet Strength", Range(0, 1)) = 0.84

        [Header(Toon Lighting)]
        _LightSteps         ("Toon Light Steps", Range(2, 8)) = 4
        _ShadowStrength     ("Shadow Strength", Range(0, 1)) = 0.26
        _ShadowTint         ("Shadow Tint", Color) = (0.70, 0.86, 1.00, 1)
        _SpecularThreshold  ("Specular Threshold", Range(0, 1)) = 0.72
        _SpecularSoftness   ("Specular Softness", Range(0.001, 0.2)) = 0.035

        [Header(Reflection And Highlight)]
        _Smoothness         ("Smoothness", Range(0, 1)) = 0.68
        _ReflectionStrength ("Reflection Strength", Range(0, 1)) = 0.08
        _FresnelPower       ("Fresnel Power", Range(1, 10)) = 4.5
        _SpecularStrength   ("Specular Strength", Range(0, 1)) = 0.28

        [Header(Rim)]
        _RimColor           ("Rim Color", Color) = (0.52, 0.88, 1.00, 1)
        _RimPower           ("Rim Power", Range(0.5, 8)) = 3.2
        _RimStrength        ("Rim Strength", Range(0, 1)) = 0.22

        [Header(Foam)]
        _FoamThreshold      ("Foam Depth Threshold", Range(0, 1)) = 0.055
        _FoamWidth          ("Foam Edge Width", Range(0.001, 0.25)) = 0.035
        _FoamAmount         ("Foam Amount", Range(0, 1)) = 0.34
        _FoamColor          ("Foam Color", Color) = (0.82, 0.96, 1.0, 1)
        _FoamNoiseSpeed     ("Foam Noise Speed", Float) = 0.08

        [Header(Vertex Waves)]
        [Toggle] _UseWaves  ("Use CPU-Synced Waves", Float) = 1
        _Wave0              ("Wave 0 (DirX DirZ Amplitude Wavelength)", Vector) = (1, 0, 0.08, 12)
        _Wave1              ("Wave 1", Vector) = (0, 1, 0.05, 8)
        _Wave2              ("Wave 2", Vector) = (1, 1, 0.035, 5.5)
        _Wave3              ("Wave 3", Vector) = (-1, 1, 0.025, 3.5)
        _WaveSpeed          ("Wave Speeds", Vector) = (0.35, 0.28, -0.22, 0.18)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex WaterVert
            #pragma fragment WaterFrag

            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _MidColor;
                float4 _DeepColor;
                float _DepthScale;
                float _ColorSteps;
                float _FacetColorStrength;

                float _ShallowOpacity;
                float _Transparency;
                float _ShallowTintStrength;
                float _DeepTintStrength;

                float4 _NormalMapA_ST;
                float4 _NormalMapB_ST;
                float _TilingA;
                float _TilingB;
                float _FlowSpeed;
                float _NormalStrength;
                float _MicroNormalStrength;
                float _FacetStrength;

                float _LightSteps;
                float _ShadowStrength;
                float4 _ShadowTint;
                float _SpecularThreshold;
                float _SpecularSoftness;

                float _Smoothness;
                float _ReflectionStrength;
                float _FresnelPower;
                float _SpecularStrength;

                float4 _RimColor;
                float _RimPower;
                float _RimStrength;

                float _FoamThreshold;
                float _FoamWidth;
                float _FoamAmount;
                float4 _FoamColor;
                float _FoamNoiseSpeed;

                float _UseWaves;
                float4 _Wave0;
                float4 _Wave1;
                float4 _Wave2;
                float4 _Wave3;
                float4 _WaveSpeed;
            CBUFFER_END

            TEXTURE2D_X(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            TEXTURE2D(_NormalMapA);
            SAMPLER(sampler_NormalMapA);
            TEXTURE2D(_NormalMapB);
            SAMPLER(sampler_NormalMapB);

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float4 tangentWS  : TEXCOORD2;
                float2 uv         : TEXCOORD3;
                float4 screenPos  : TEXCOORD4;
                float eyeDepth    : TEXCOORD5;
                float fogFactor   : TEXCOORD6;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float SimpleNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);

                float r0, r1, r2, r3;
                Hash_Tchou_2_1_float(i,                r0);
                Hash_Tchou_2_1_float(i + float2(1, 0), r1);
                Hash_Tchou_2_1_float(i + float2(0, 1), r2);
                Hash_Tchou_2_1_float(i + float2(1, 1), r3);

                return lerp(lerp(r0, r1, f.x), lerp(r2, r3, f.x), f.y);
            }

            float3 NormalBlendRNM(float3 a, float3 b)
            {
                float3 t = a + float3(0, 0, 1);
                float3 u = b * float3(-1, -1, 1);
                return normalize((t / max(t.z, 0.0001)) * dot(t, u) - u);
            }

            float EvaluateWave(float4 wave, float speed, float3 positionWS)
            {
                float2 direction = normalize(wave.xy + float2(0.000001, 0));
                float frequency = TWO_PI / max(0.01, wave.w);
                float phase = frequency * dot(direction, positionWS.xz) + speed * _Time.y;
                return wave.z * sin(phase);
            }

            float3 ApplyWaves(float3 positionWS)
            {
                if (_UseWaves > 0.5)
                {
                    positionWS.y += EvaluateWave(_Wave0, _WaveSpeed.x, positionWS);
                    positionWS.y += EvaluateWave(_Wave1, _WaveSpeed.y, positionWS);
                    positionWS.y += EvaluateWave(_Wave2, _WaveSpeed.z, positionWS);
                    positionWS.y += EvaluateWave(_Wave3, _WaveSpeed.w, positionWS);
                }

                return positionWS;
            }

            float3 GetFacetNormal(float3 positionWS, float3 fallbackNormal)
            {
                float3 dpdx = ddx(positionWS);
                float3 dpdy = ddy(positionWS);
                float3 faceNormal = normalize(cross(dpdy, dpdx));

                if (dot(faceNormal, fallbackNormal) < 0.0)
                    faceNormal *= -1.0;

                return faceNormal;
            }

            float3 GetWaterColor(float depthT)
            {
                float steps = max(_ColorSteps, 2.0);
                float steppedDepth = floor(depthT * (steps - 1.0) + 0.5) / (steps - 1.0);
                float stylizedDepth = lerp(depthT, steppedDepth, _FacetColorStrength * 0.45);

                float shallowToMid = saturate(stylizedDepth * 2.0);
                float midToDeep = saturate((stylizedDepth - 0.5) * 2.0);

                float3 shallowMid = lerp(_ShallowColor.rgb, _MidColor.rgb, shallowToMid);
                return lerp(shallowMid, _DeepColor.rgb, midToDeep);
            }

            Varyings WaterVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                float3 positionWS = TransformObjectToWorld(input.positionOS);
                positionWS = ApplyWaves(positionWS);

                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = float4(normalInputs.tangentWS, input.tangentOS.w);
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.eyeDepth = -TransformWorldToView(positionWS).z;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 WaterFrag(Varyings input) : SV_Target
            {
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float time = _Time.y;

                float2 uvA = input.uv * _TilingA + float2(time * _FlowSpeed * 0.22, time * _FlowSpeed);
                float2 uvB = input.uv * _TilingB + float2(-time * _FlowSpeed * 0.72, time * _FlowSpeed * 0.34);

                float3 normalA = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMapA, sampler_NormalMapA, uvA));
                float3 normalB = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMapB, sampler_NormalMapB, uvB));
                float3 microNormalTS = NormalBlendRNM(normalA, normalB);

                microNormalTS.xy *= _MicroNormalStrength;
                microNormalTS.z = sqrt(saturate(1.0 - dot(microNormalTS.xy, microNormalTS.xy)));

                float3 smoothNormal = normalize(input.normalWS);
                float3 faceNormal = GetFacetNormal(input.positionWS, smoothNormal);
                float3 macroNormal = normalize(lerp(smoothNormal, faceNormal, _FacetStrength));

                float3 tangentWS = input.tangentWS.xyz;
                tangentWS = normalize(tangentWS - macroNormal * dot(tangentWS, macroNormal));
                float3 bitangentWS = normalize(cross(macroNormal, tangentWS)) * input.tangentWS.w;
                float3x3 tbn = float3x3(tangentWS, bitangentWS, macroNormal);
                float3 worldNormal = normalize(mul(microNormalTS, tbn));

                float rawSceneDepth = SAMPLE_TEXTURE2D_X(
                    _CameraDepthTexture,
                    sampler_CameraDepthTexture,
                    screenUV).r;

                float sceneEyeDepth = LinearEyeDepth(rawSceneDepth, _ZBufferParams);
                float depthDifference = max(sceneEyeDepth - input.eyeDepth, 0.0);
                float depthT = saturate(depthDifference / max(_DepthScale, 0.001));

                float farFade = saturate(input.eyeDepth / 60.0);
                depthT = max(depthT, farFade * 0.72);

                float2 refractionOffset = microNormalTS.xy * _NormalStrength * lerp(1.0, 0.35, depthT);
                float2 refractedUV = clamp(screenUV + refractionOffset, 0.001, 0.999);

                float3 sceneColor = SAMPLE_TEXTURE2D_X(
                    _CameraOpaqueTexture,
                    sampler_CameraOpaqueTexture,
                    refractedUV).rgb;

                float3 waterColor = GetWaterColor(depthT);
                float waterTint = lerp(_ShallowTintStrength, _DeepTintStrength, depthT);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                float lightSteps = max(_LightSteps, 2.0);
                float NdotL = saturate(dot(macroNormal, mainLight.direction));
                float steppedLight = floor(NdotL * (lightSteps - 1.0) + 0.5) / (lightSteps - 1.0);
                float toonLight = lerp(NdotL, steppedLight, _FacetColorStrength);
                toonLight *= mainLight.shadowAttenuation * mainLight.distanceAttenuation;

                float shadowFactor = lerp(1.0 - _ShadowStrength, 1.0, toonLight);
                float3 shadowTint = lerp(_ShadowTint.rgb, float3(1.0, 1.0, 1.0), toonLight);
                float3 toonWaterColor = waterColor * shadowFactor * shadowTint;
                toonWaterColor *= lerp(float3(1.0, 1.0, 1.0), mainLight.color, 0.22);

                float3 viewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float NdotV = saturate(dot(worldNormal, viewDirection));
                float fresnel = pow(1.0 - NdotV, _FresnelPower);

                float3 reflectionDirection = reflect(-viewDirection, worldNormal);
                float3 reflectionColor = GlossyEnvironmentReflection(
                    reflectionDirection,
                    input.positionWS,
                    _Smoothness,
                    1.0);

                float reflectionMask = saturate(fresnel * _ReflectionStrength);
                toonWaterColor = lerp(toonWaterColor, reflectionColor, reflectionMask);

                float3 halfDirection = normalize(viewDirection + mainLight.direction);
                float NdotH = saturate(dot(worldNormal, halfDirection));
                float specular = smoothstep(
                    _SpecularThreshold - _SpecularSoftness,
                    _SpecularThreshold + _SpecularSoftness,
                    NdotH);

                specular *= _SpecularStrength;
                specular *= mainLight.shadowAttenuation * mainLight.distanceAttenuation;

                float rim = pow(1.0 - NdotV, _RimPower);
                rim = smoothstep(0.18, 0.72, rim) * _RimStrength;

                toonWaterColor += specular * mainLight.color;
                toonWaterColor += rim * _RimColor.rgb;

                float foamEdge = 1.0 - smoothstep(
                    _FoamThreshold,
                    _FoamThreshold + max(_FoamWidth, 0.001),
                    depthT);

                float2 foamUV = input.positionWS.xz * 0.85
                    + float2(time * _FoamNoiseSpeed, -time * _FoamNoiseSpeed * 0.63);

                float foamNoise = SimpleNoise(foamUV);
                float foamPattern = step(0.54, foamNoise);
                float foam = foamEdge * foamPattern * _FoamAmount;

                toonWaterColor = lerp(toonWaterColor, _FoamColor.rgb, foam);

                float opacity = lerp(_ShallowOpacity, _Transparency, depthT);
                opacity += fresnel * 0.025;
                opacity += specular * 0.035;
                opacity += rim * 0.035;
                opacity += foam * 0.30;
                opacity = saturate(opacity);

                // First tint the refracted scene very lightly, then add the toon surface layer.
                float3 tintedScene = lerp(sceneColor, waterColor, waterTint);
                float3 finalColor = lerp(tintedScene, toonWaterColor, opacity);
                finalColor = MixFog(finalColor, input.fogFactor);

                return half4(finalColor, 1.0);
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
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _MidColor;
                float4 _DeepColor;
                float _DepthScale;
                float _ColorSteps;
                float _FacetColorStrength;

                float _ShallowOpacity;
                float _Transparency;
                float _ShallowTintStrength;
                float _DeepTintStrength;

                float4 _NormalMapA_ST;
                float4 _NormalMapB_ST;
                float _TilingA;
                float _TilingB;
                float _FlowSpeed;
                float _NormalStrength;
                float _MicroNormalStrength;
                float _FacetStrength;

                float _LightSteps;
                float _ShadowStrength;
                float4 _ShadowTint;
                float _SpecularThreshold;
                float _SpecularSoftness;

                float _Smoothness;
                float _ReflectionStrength;
                float _FresnelPower;
                float _SpecularStrength;

                float4 _RimColor;
                float _RimPower;
                float _RimStrength;

                float _FoamThreshold;
                float _FoamWidth;
                float _FoamAmount;
                float4 _FoamColor;
                float _FoamNoiseSpeed;

                float _UseWaves;
                float4 _Wave0;
                float4 _Wave1;
                float4 _Wave2;
                float4 _Wave3;
                float4 _WaveSpeed;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float EvaluateWave(float4 wave, float speed, float3 positionWS)
            {
                float2 direction = normalize(wave.xy + float2(0.000001, 0));
                float frequency = TWO_PI / max(0.01, wave.w);
                float phase = frequency * dot(direction, positionWS.xz) + speed * _Time.y;
                return wave.z * sin(phase);
            }

            float3 ApplyWaves(float3 positionWS)
            {
                if (_UseWaves > 0.5)
                {
                    positionWS.y += EvaluateWave(_Wave0, _WaveSpeed.x, positionWS);
                    positionWS.y += EvaluateWave(_Wave1, _WaveSpeed.y, positionWS);
                    positionWS.y += EvaluateWave(_Wave2, _WaveSpeed.z, positionWS);
                    positionWS.y += EvaluateWave(_Wave3, _WaveSpeed.w, positionWS);
                }

                return positionWS;
            }

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                float3 positionWS = TransformObjectToWorld(input.positionOS);
                positionWS = ApplyWaves(positionWS);

                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 lightDirection = normalize(_MainLightPosition.xyz);
                float bias = max(0.005 * (1.0 - saturate(dot(normalWS, lightDirection))), 0.0005);
                positionWS += normalWS * bias;

                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _MidColor;
                float4 _DeepColor;
                float _DepthScale;
                float _ColorSteps;
                float _FacetColorStrength;

                float _ShallowOpacity;
                float _Transparency;
                float _ShallowTintStrength;
                float _DeepTintStrength;

                float4 _NormalMapA_ST;
                float4 _NormalMapB_ST;
                float _TilingA;
                float _TilingB;
                float _FlowSpeed;
                float _NormalStrength;
                float _MicroNormalStrength;
                float _FacetStrength;

                float _LightSteps;
                float _ShadowStrength;
                float4 _ShadowTint;
                float _SpecularThreshold;
                float _SpecularSoftness;

                float _Smoothness;
                float _ReflectionStrength;
                float _FresnelPower;
                float _SpecularStrength;

                float4 _RimColor;
                float _RimPower;
                float _RimStrength;

                float _FoamThreshold;
                float _FoamWidth;
                float _FoamAmount;
                float4 _FoamColor;
                float _FoamNoiseSpeed;

                float _UseWaves;
                float4 _Wave0;
                float4 _Wave1;
                float4 _Wave2;
                float4 _Wave3;
                float4 _WaveSpeed;
            CBUFFER_END

            TEXTURE2D(_NormalMapA);
            SAMPLER(sampler_NormalMapA);
            TEXTURE2D(_NormalMapB);
            SAMPLER(sampler_NormalMapB);

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float4 tangentWS  : TEXCOORD2;
                float2 uv         : TEXCOORD3;
            };

            float3 NormalBlendRNM(float3 a, float3 b)
            {
                float3 t = a + float3(0, 0, 1);
                float3 u = b * float3(-1, -1, 1);
                return normalize((t / max(t.z, 0.0001)) * dot(t, u) - u);
            }

            float EvaluateWave(float4 wave, float speed, float3 positionWS)
            {
                float2 direction = normalize(wave.xy + float2(0.000001, 0));
                float frequency = TWO_PI / max(0.01, wave.w);
                float phase = frequency * dot(direction, positionWS.xz) + speed * _Time.y;
                return wave.z * sin(phase);
            }

            float3 ApplyWaves(float3 positionWS)
            {
                if (_UseWaves > 0.5)
                {
                    positionWS.y += EvaluateWave(_Wave0, _WaveSpeed.x, positionWS);
                    positionWS.y += EvaluateWave(_Wave1, _WaveSpeed.y, positionWS);
                    positionWS.y += EvaluateWave(_Wave2, _WaveSpeed.z, positionWS);
                    positionWS.y += EvaluateWave(_Wave3, _WaveSpeed.w, positionWS);
                }

                return positionWS;
            }

            float3 GetFacetNormal(float3 positionWS, float3 fallbackNormal)
            {
                float3 dpdx = ddx(positionWS);
                float3 dpdy = ddy(positionWS);
                float3 faceNormal = normalize(cross(dpdy, dpdx));

                if (dot(faceNormal, fallbackNormal) < 0.0)
                    faceNormal *= -1.0;

                return faceNormal;
            }

            Varyings DepthNormalsVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                float3 positionWS = TransformObjectToWorld(input.positionOS);
                positionWS = ApplyWaves(positionWS);

                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = float4(normalInputs.tangentWS, input.tangentOS.w);
                output.uv = input.uv;

                return output;
            }

            float4 DepthNormalsFrag(Varyings input) : SV_Target
            {
                float time = _Time.y;
                float2 uvA = input.uv * _TilingA + float2(time * _FlowSpeed * 0.22, time * _FlowSpeed);
                float2 uvB = input.uv * _TilingB + float2(-time * _FlowSpeed * 0.72, time * _FlowSpeed * 0.34);

                float3 normalA = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMapA, sampler_NormalMapA, uvA));
                float3 normalB = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMapB, sampler_NormalMapB, uvB));
                float3 microNormalTS = NormalBlendRNM(normalA, normalB);

                microNormalTS.xy *= _MicroNormalStrength;
                microNormalTS.z = sqrt(saturate(1.0 - dot(microNormalTS.xy, microNormalTS.xy)));

                float3 smoothNormal = normalize(input.normalWS);
                float3 faceNormal = GetFacetNormal(input.positionWS, smoothNormal);
                float3 macroNormal = normalize(lerp(smoothNormal, faceNormal, _FacetStrength));

                float3 tangentWS = input.tangentWS.xyz;
                tangentWS = normalize(tangentWS - macroNormal * dot(tangentWS, macroNormal));
                float3 bitangentWS = normalize(cross(macroNormal, tangentWS)) * input.tangentWS.w;
                float3x3 tbn = float3x3(tangentWS, bitangentWS, macroNormal);
                float3 worldNormal = normalize(mul(microNormalTS, tbn));

                return float4(worldNormal * 0.5 + 0.5, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
