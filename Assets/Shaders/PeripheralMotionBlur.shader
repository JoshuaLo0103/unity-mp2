Shader "Custom/PeripheralMotionBlur"
{
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "PeripheralMotionBlurPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float _MotionBlurStrength;
            float _MotionBlurClearRadius;
            float _MotionBlurEdgeSoftness;

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = positionInputs.positionCS;
                output.screenPos = ComputeScreenPos(positionInputs.positionCS);
                return output;
            }

            float Hash12(float2 p)
            {
                float h = dot(p, float2(127.1, 311.7));
                return frac(sin(h) * 43758.5453123);
            }

            float ComputeEdgeMask(float2 uv)
            {
                float2 centered = uv * 2.0 - 1.0;
                centered.x *= _ScreenParams.x / _ScreenParams.y;

                float safeRadius = max(0.0001, _MotionBlurClearRadius);
                float safeSoftness = max(0.0001, _MotionBlurEdgeSoftness);
                float distanceFromCenter = length(centered);
                return smoothstep(safeRadius, safeRadius + safeSoftness, distanceFromCenter);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.screenPos.xy / max(input.screenPos.w, 0.0001);
                float overlayMask = saturate(ComputeEdgeMask(uv) * _MotionBlurStrength);
                if (overlayMask <= 0.001)
                    return half4(0.0, 0.0, 0.0, 0.0);

                float grain = Hash12(floor(uv * _ScreenParams.xy * 0.14)) * 0.08;
                float3 tint = float3(0.88, 0.92, 0.97) + grain * 0.35;
                float alpha = saturate(overlayMask * (0.22 + grain * 0.5));
                return half4(tint, alpha);
            }
            ENDHLSL
        }
    }
}
