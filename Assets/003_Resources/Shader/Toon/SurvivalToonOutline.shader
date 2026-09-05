Shader "Survival/Toon/DOTS Outline"
{
    Properties
    {
        _OutlineColor(
            "Outline Color",
            Color
        ) = (0.035, 0.025, 0.025, 1)

        _OutlineWidth(
            "Outline Width (Pixels)",
            Range(0, 12)
        ) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"

            // Body보다 먼저 그리기
            "Queue" = "Geometry-1"
        }

        Pass
        {
            Name "Outline"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            // Inverted Hull 핵심
            Cull Front

            ZWrite On
            ZTest LEqual

            HLSLPROGRAM

            #pragma target 4.5

            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment

            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)

                float4 _OutlineColor;
                float _OutlineWidth;

            CBUFFER_END

            // =========================================================
            // DOTS per-instance values
            // =========================================================

            #ifdef UNITY_DOTS_INSTANCING_ENABLED

                UNITY_DOTS_INSTANCING_START(UserPropertyMetadata)

                    UNITY_DOTS_INSTANCED_PROP(
                        float4,
                        _OutlineColor
                    )

                    UNITY_DOTS_INSTANCED_PROP(
                        float,
                        _OutlineWidth
                    )

                UNITY_DOTS_INSTANCING_END(
                    UserPropertyMetadata
                )

            #endif

            float4 GetOutlineColor()
            {
                #ifdef UNITY_DOTS_INSTANCING_ENABLED

                    return
                        UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(
                            float4,
                            _OutlineColor
                        );

                #else

                    return _OutlineColor;

                #endif
            }

            float GetOutlineWidth()
            {
                #ifdef UNITY_DOTS_INSTANCING_ENABLED

                    return
                        UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(
                            float,
                            _OutlineWidth
                        );

                #else

                    return _OutlineWidth;

                #endif
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings OutlineVertex(
                Attributes input
            )
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(
                    input,
                    output
                );

                // -----------------------------------------------------
                // Original position
                // -----------------------------------------------------

                float3 positionWS =
                    TransformObjectToWorld(
                        input.positionOS.xyz
                    );

                float3 normalWS =
                    normalize(
                        TransformObjectToWorldNormal(
                            input.normalOS
                        )
                    );

                float4 positionCS =
                    TransformWorldToHClip(
                        positionWS
                    );

                // -----------------------------------------------------
                // Project normal into screen space
                // -----------------------------------------------------

                // 현재 위치보다 Normal 방향으로
                // 1 world-unit 이동한 지점의 clip 위치를 구함.
                float4 normalPositionCS =
                    TransformWorldToHClip(
                        positionWS +
                        normalWS
                    );

                float2 positionNDC =
                    positionCS.xy /
                    max(
                        positionCS.w,
                        0.00001
                    );

                float2 normalPositionNDC =
                    normalPositionCS.xy /
                    max(
                        normalPositionCS.w,
                        0.00001
                    );

                float2 screenDirectionNDC =
                    normalPositionNDC -
                    positionNDC;

                // NDC -> pixel space
                float2 screenDirectionPixels =
                    screenDirectionNDC *
                    (_ScreenParams.xy * 0.5);

                float directionLength =
                    length(
                        screenDirectionPixels
                    );

                if (directionLength > 0.00001)
                {
                    screenDirectionPixels /=
                        directionLength;
                }
                else
                {
                    screenDirectionPixels =
                        float2(0, 0);
                }

                float outlineWidth =
                    max(
                        GetOutlineWidth(),
                        0.0
                    );

                // Pixel width -> NDC
                float2 outlineOffsetNDC =
                    screenDirectionPixels *
                    outlineWidth *
                    (2.0 / _ScreenParams.xy);

                // NDC offset -> Clip offset
                positionCS.xy +=
                    outlineOffsetNDC *
                    positionCS.w;

                output.positionCS =
                    positionCS;

                return output;
            }

            half4 OutlineFragment(
                Varyings input
            ) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                return GetOutlineColor();
            }

            ENDHLSL
        }
    }

    FallBack Off
}