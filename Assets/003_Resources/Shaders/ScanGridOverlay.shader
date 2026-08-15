Shader "Survival/Scan Grid Overlay"
{
    Properties
    {
        [HDR] _ScanColor ("Scan Color", Color) = (0.16, 0.95, 1, 0.72)
        _SurfaceAlpha ("Surface Alpha", Range(0, 1)) = 0.18
        _EdgeGlow ("Edge Glow", Range(0, 4)) = 1.35
        _EffectMode ("Effect Mode", Float) = 0
        _ScanProgress ("Scan Progress", Range(0, 1)) = 0
        _ScanActive ("Scan Active", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent+30" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "ScanBox"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ScanColor;
                float _SurfaceAlpha;
                float _EdgeGlow;
                float _EffectMode;
                float _ScanProgress;
                float _ScanActive;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float active = saturate(_ScanActive);
                float pulse = .88 + .12 * sin(_Time.y * 5.5);

                if (_EffectMode > .5)
                {
                    float2 edgeDistance = min(input.uv, 1.0 - input.uv);
                    float softEdge = smoothstep(0.0, .12, min(edgeDistance.x, edgeDistance.y));
                    float planeAlpha = lerp(.18, .62, softEdge) * _ScanColor.a * active;
                    float3 planeGlow = _ScanColor.rgb * (2.2 + pulse * 1.1);
                    return half4(planeGlow, planeAlpha);
                }

                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                float fresnel = pow(1.0 - saturate(abs(dot(normalWS, viewDirWS))), 2.2);
                float alpha = (_SurfaceAlpha + fresnel * .38) * _ScanColor.a * active;
                float3 glow = _ScanColor.rgb * (.55 + fresnel * _EdgeGlow) * pulse;
                return half4(glow, alpha);
            }
            ENDHLSL
        }
    }
}
