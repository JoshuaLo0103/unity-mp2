Shader "Custom/StylizedToonRim"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _DetailAlbedoMap("Detail Map", 2D) = "white" {}
        _ShadowColor("Shadow Color", Color) = (0.5, 0.2, 0.78, 1)
        _ShadowTintStrength("Shadow Tint Strength", Range(0, 1)) = 1
        _Threshold("Light Threshold", Range(0, 1)) = 0.52
        _BandSmoothness("Band Smoothness", Range(0.001, 0.4)) = 0.08
        _RimColor("Rim Color", Color) = (0.78, 0.95, 1, 1)
        _RimPower("Rim Power", Range(0.5, 8)) = 3
        _RimIntensity("Rim Intensity", Range(0, 2)) = 0.35
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
        _EmissionMap("Emission Map", 2D) = "black" {}
        _EmissionStrength("Emission Strength", Range(0, 4)) = 1
        _DetailBlend("Detail Blend", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_DetailAlbedoMap);
            SAMPLER(sampler_DetailAlbedoMap);
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _ShadowColor;
                half4 _RimColor;
                half4 _EmissionColor;
                half _Threshold;
                half _BandSmoothness;
                half _ShadowTintStrength;
                half _RimPower;
                half _RimIntensity;
                half _EmissionStrength;
                half _DetailBlend;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                float3 viewDirWS : TEXCOORD4;
                half fogFactor : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.shadowCoord = GetShadowCoord(positionInputs);
                output.viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half3 SampleStylizedAlbedo(float2 uv)
            {
                half3 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb;
                half3 detailTex = SAMPLE_TEXTURE2D(_DetailAlbedoMap, sampler_DetailAlbedoMap, uv).rgb;
                half3 textured = baseTex * lerp(half3(1, 1, 1), detailTex, _DetailBlend);
                return textured * _BaseColor.rgb;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                half3 viewDirWS = SafeNormalize(input.viewDirWS);
                half3 albedo = SampleStylizedAlbedo(input.uv);

                Light mainLight = GetMainLight(input.shadowCoord);
                half ndotl = saturate(dot(normalWS, mainLight.direction));
                half lightBand = smoothstep(_Threshold - _BandSmoothness, _Threshold + _BandSmoothness, ndotl);

                half albedoLuma = dot(albedo, half3(0.299h, 0.587h, 0.114h));
                half shadowLuma = max(albedoLuma, 0.45h);
                half3 neutralShade = lerp(albedo * 0.42h, albedo, lightBand);
                half3 multipliedShadow = neutralShade * _ShadowColor.rgb;
                half3 tintedShadow = _ShadowColor.rgb * shadowLuma;
                half3 shadowColor = lerp(multipliedShadow, tintedShadow, _ShadowTintStrength);
                half shadowBlend = saturate(1.0h - mainLight.shadowAttenuation) * smoothstep(0.15h, 0.45h, ndotl);
                half3 toonColor = lerp(neutralShade, shadowColor, shadowBlend);
                half3 ambient = SampleSH(normalWS) * albedo * 0.25h;

                #if defined(_ADDITIONAL_LIGHTS)
                uint lightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0; lightIndex < lightCount; ++lightIndex)
                {
                    Light light = GetAdditionalLight(lightIndex, input.positionWS);
                    half additionalNdotL = saturate(dot(normalWS, light.direction));
                    half additionalBand = smoothstep(_Threshold - _BandSmoothness, _Threshold + _BandSmoothness, additionalNdotL * light.distanceAttenuation * light.shadowAttenuation);
                    toonColor += albedo * light.color * additionalBand * 0.35h;
                }
                #endif

                half rim = pow(1.0h - saturate(dot(normalWS, viewDirWS)), max(_RimPower, 0.001h)) * _RimIntensity;
                half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb * _EmissionColor.rgb * _EmissionStrength;

                half3 finalColor = toonColor * mainLight.color + ambient + rim * _RimColor.rgb + emission;
                finalColor = MixFog(finalColor, input.fogFactor);
                return half4(finalColor, _BaseColor.a);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
        UsePass "Universal Render Pipeline/Lit/Meta"
    }
}
