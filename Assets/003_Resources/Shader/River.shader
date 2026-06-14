Shader "Custom/Water"
{
    Properties
    {
        _ShallowColor    ("Shallow Color",     Color)       = (0.1, 0.7, 0.6, 1)
        _DeepColor       ("Deep Color",        Color)       = (0.0, 0.1, 0.3, 1)
        _DepthScale      ("Depth Scale",       Float)       = 5.0

        _NormalMapA      ("Normal Map A",      2D)          = "bump" {}
        _NormalMapB      ("Normal Map B",      2D)          = "bump" {}
        _TilingA         ("Tiling A",          Float)       = 1.0
        _TilingB         ("Tiling B",          Float)       = 1.5
        _FlowSpeed       ("Flow Speed",        Float)       = 0.05
        _NormalStrength  ("Normal Strength",   Float)       = 0.05

        _Smoothness      ("Smoothness",        Range(0,1))  = 0.9
        _FresnelPower    ("Fresnel Power",     Float)       = 4.0
        _Transparency    ("Transparency",      Range(0,1))  = 0.7

        _FoamThreshold   ("Foam Threshold",    Range(0,1))  = 0.1
        _FoamColor       ("Foam Color",        Color)       = (1,1,1,1)
        _FoamNoiseSpeed  ("Foam Noise Speed",  Float)       = 0.1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
        }

        // ═══════════════════════════════════════════════
        // Pass 1 – Universal Forward
        // ═══════════════════════════════════════════════
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            Cull   Back
            ZWrite Off
            ZTest  LEqual
            Blend  SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex   WaterVert
            #pragma fragment WaterFrag

            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"

            // ── SRP Batcher 호환 CBUFFER ────────────────
            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float  _DepthScale;

                float4 _NormalMapA_ST;
                float4 _NormalMapB_ST;
                float  _TilingA;
                float  _TilingB;
                float  _FlowSpeed;
                float  _NormalStrength;

                float  _Smoothness;
                float  _FresnelPower;
                float  _Transparency;

                float  _FoamThreshold;
                float4 _FoamColor;
                float  _FoamNoiseSpeed;
            CBUFFER_END

            // Depth / Opaque texture — URP 정식 선언
            TEXTURE2D_X(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            TEXTURE2D(_NormalMapA); SAMPLER(sampler_NormalMapA);
            TEXTURE2D(_NormalMapB); SAMPLER(sampler_NormalMapB);

            // ── Structs ─────────────────────────────────
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
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float4 tangentWS   : TEXCOORD2;
                float2 uv          : TEXCOORD3;
                float4 screenPos   : TEXCOORD4;
                float  eyeDepth    : TEXCOORD5;   // 버텍스 eye-space 깊이
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ── Value Noise (거품용) ─────────────────────
            float SimpleNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);

                float r0, r1, r2, r3;
                Hash_Tchou_2_1_float(i,                  r0);
                Hash_Tchou_2_1_float(i + float2(1, 0),   r1);
                Hash_Tchou_2_1_float(i + float2(0, 1),   r2);
                Hash_Tchou_2_1_float(i + float2(1, 1),   r3);

                return lerp(lerp(r0, r1, f.x), lerp(r2, r3, f.x), f.y);
            }

            // ── Reoriented Normal Blend ──────────────────
            float3 NormalBlendRNM(float3 A, float3 B)
            {
                float3 t = A + float3(0, 0, 1);
                float3 u = B * float3(-1, -1, 1);
                return normalize((t / t.z) * dot(t, u) - u);
            }

            // ── Vertex ──────────────────────────────────
            Varyings WaterVert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs vpi = GetVertexPositionInputs(IN.positionOS);
                VertexNormalInputs   vni = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionCS = vpi.positionCS;
                OUT.positionWS = vpi.positionWS;
                OUT.normalWS   = vni.normalWS;
                OUT.tangentWS  = float4(vni.tangentWS, IN.tangentOS.w);
                OUT.uv         = IN.uv;
                OUT.screenPos  = ComputeScreenPos(vpi.positionCS);
                OUT.eyeDepth   = -vpi.positionVS.z;  // view space Z는 음수, 부호 반전해서 양수로

                return OUT;
            }

            // ── Fragment ────────────────────────────────
            half4 WaterFrag(Varyings IN) : SV_Target
            {
                // ── 1. NDC UV ────────────────────────────
                float2 ndcUV = IN.screenPos.xy / IN.screenPos.w;

                // ── 2. 노멀 샘플 ─────────────────────────
                float  t    = _Time.x;
                float2 uvA  = IN.uv * _TilingA + float2(0, t * _FlowSpeed);
                float2 uvB  = IN.uv * _TilingB + float2(0, t * _FlowSpeed * 0.7);

                half3 nA = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMapA, sampler_NormalMapA, uvA));
                half3 nB = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMapB, sampler_NormalMapB, uvB));
                half3 blendedNormalTS = NormalBlendRNM(nA, nB);

                // ── 3. 깊이 계산 ─────────────────────────
                float rawSceneDepth = SAMPLE_TEXTURE2D_X(
                    _CameraDepthTexture, sampler_CameraDepthTexture, ndcUV).r;
                float sceneEyeDepth   = LinearEyeDepth(rawSceneDepth, _ZBufferParams);
                float surfaceEyeDepth = IN.eyeDepth;  // 버텍스에서 뽑은 정밀한 eye depth

                float depthDiff = max(sceneEyeDepth - surfaceEyeDepth, 0.0);

                // 0 = 얕음, 1 = 깊음
                float depthT = saturate(depthDiff / _DepthScale);

                // 멀리 있는 픽셀 보정: 씬 깊이와 수면 깊이가 같으면(=하늘/먼 배경)
                // depthDiff가 0이 아니어도 불투명 처리
                float farFade = saturate(surfaceEyeDepth / 50.0);  // 50 유닛 이상이면 강제 불투명
                depthT = max(depthT, farFade * 0.8);

                // ── 4. 굴절 UV (노멀 오프셋) ─────────────
                float2 refractUV = ndcUV + blendedNormalTS.xy * _NormalStrength;

                // 굴절이 수면 밖으로 나가는 것 방지
                refractUV = clamp(refractUV, 0.001, 0.999);

                half3 sceneColor = SAMPLE_TEXTURE2D_X(
                    _CameraOpaqueTexture, sampler_CameraOpaqueTexture, refractUV).rgb;

                // ── 5. 깊이 기반 수색 혼합 ───────────────
                // 얕음 = ShallowColor, 깊음 = DeepColor
                half3 waterColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthT);

                // 굴절된 씬과 수심 색 혼합 (깊을수록 수색 강해짐)
                half3 baseColor = lerp(sceneColor, waterColor, saturate(depthT + 0.3));

                // ── 6. Fresnel 반사 ──────────────────────
                // TBN → world normal
                float3 bitangentWS = cross(IN.normalWS, IN.tangentWS.xyz) * IN.tangentWS.w;
                float3x3 TBN       = float3x3(IN.tangentWS.xyz, bitangentWS, IN.normalWS);
                float3 worldNormal = normalize(mul(blendedNormalTS, TBN));

                float3 viewDir = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                float  NdotV   = saturate(dot(worldNormal, viewDir));
                float  fresnel = pow(1.0 - NdotV, _FresnelPower);

                // 반사 프로브
                half3 reflColor = GlossyEnvironmentReflection(
                    reflect(-viewDir, worldNormal),
                    IN.positionWS,
                    _Smoothness,
                    1.0);

                baseColor = lerp(baseColor, reflColor, fresnel * 0.6);

                // ── 7. 거품 (얕은 곳에서만) ─────────────
                // depthT < _FoamThreshold 인 영역 = 얕음 = 거품
                float foamNoise = SimpleNoise(IN.uv * 150.0 + t * _FoamNoiseSpeed);
                float foamMask  = step(depthT, _FoamThreshold); // 얕은 곳 = 1
                float foam      = foamMask * foamNoise;

                baseColor = lerp(baseColor, _FoamColor.rgb, foam);

                // ── 8. 스페큘러 ──────────────────────────
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light  mainLight   = GetMainLight(shadowCoord);

                float3 halfDir = normalize(viewDir + mainLight.direction);
                float  spec    = pow(saturate(dot(worldNormal, halfDir)),
                                     exp2(_Smoothness * 10.0 + 1.0));
                baseColor += spec * mainLight.color * mainLight.shadowAttenuation * fresnel;

                // ── 9. 알파 (깊을수록 불투명) ────────────
                float alpha = lerp(_Transparency * 0.4, _Transparency, depthT);
                alpha = saturate(alpha + foam * 0.9);

                return half4(baseColor, alpha);
            }
            ENDHLSL
        }

        // ═══════════════════════════════════════════════
        // Pass 2 – ShadowCaster
        // ═══════════════════════════════════════════════
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite    On
            ZTest     LEqual
            ColorMask 0
            Cull      Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex   ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float  _DepthScale;
                float4 _NormalMapA_ST;
                float4 _NormalMapB_ST;
                float  _TilingA;
                float  _TilingB;
                float  _FlowSpeed;
                float  _NormalStrength;
                float  _Smoothness;
                float  _FresnelPower;
                float  _Transparency;
                float  _FoamThreshold;
                float4 _FoamColor;
                float  _FoamNoiseSpeed;
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

            Varyings ShadowVert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);

                float3 posWS  = TransformObjectToWorld(IN.positionOS);
                float3 normWS = TransformObjectToWorldNormal(IN.normalOS);

                // _MainLightPosition.xyz = light direction (URP 모든 버전 공통)
                float3 lightDir = normalize(_MainLightPosition.xyz);
                float  bias     = max(0.005 * (1.0 - saturate(dot(normWS, lightDir))), 0.0005);
                posWS += normWS * bias;

                OUT.positionCS = TransformWorldToHClip(posWS);
                return OUT;
            }

            half4 ShadowFrag(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }

        // ═══════════════════════════════════════════════
        // Pass 3 – DepthNormals  (SSAO용)
        // ═══════════════════════════════════════════════
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            ZTest  LEqual
            Cull   Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex   DNVert
            #pragma fragment DNFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float  _DepthScale;
                float4 _NormalMapA_ST;
                float4 _NormalMapB_ST;
                float  _TilingA;
                float  _TilingB;
                float  _FlowSpeed;
                float  _NormalStrength;
                float  _Smoothness;
                float  _FresnelPower;
                float  _Transparency;
                float  _FoamThreshold;
                float4 _FoamColor;
                float  _FoamNoiseSpeed;
            CBUFFER_END

            TEXTURE2D(_NormalMapA); SAMPLER(sampler_NormalMapA);
            TEXTURE2D(_NormalMapB); SAMPLER(sampler_NormalMapB);

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
                float3 normalWS   : TEXCOORD0;
                float4 tangentWS  : TEXCOORD1;
                float2 uv         : TEXCOORD2;
            };

            Varyings DNVert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);

                VertexNormalInputs vni = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.normalWS   = vni.normalWS;
                OUT.tangentWS  = float4(vni.tangentWS, IN.tangentOS.w);
                OUT.uv         = IN.uv;
                return OUT;
            }

            float4 DNFrag(Varyings IN) : SV_Target
            {
                float  t   = _Time.x;
                float2 uvA = IN.uv * _TilingA + float2(0, t * _FlowSpeed);
                float2 uvB = IN.uv * _TilingB + float2(0, t * _FlowSpeed * 0.7);

                half3 nA = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMapA, sampler_NormalMapA, uvA));
                half3 nB = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMapB, sampler_NormalMapB, uvB));
                half3 blended = normalize(nA + nB);

                float3 bitangent   = cross(IN.normalWS, IN.tangentWS.xyz) * IN.tangentWS.w;
                float3x3 TBN       = float3x3(IN.tangentWS.xyz, bitangent, IN.normalWS);
                float3 worldNormal = normalize(mul(blended, TBN));

                return float4(worldNormal * 0.5 + 0.5, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
