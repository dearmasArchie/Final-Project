// World-space triplanar PBR for the greybox walls, floors and props. Textures tile every _TileSize meters
// no matter how a cube is scaled, so stretched ProBuilder/primitive geometry never smears its texture.
// Expects Poly Haven maps: albedo, OpenGL normal, and ARM (R = AO, G = roughness, B = metal).
Shader "SignalLost/Triplanar Lit"
{
    Properties
    {
        [MainColor] _BaseColor("Tint", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [Normal][NoScaleOffset] _BumpMap("Normal (OpenGL)", 2D) = "bump" {}
        [NoScaleOffset] _ARMMap("AO / Roughness / Metal", 2D) = "white" {}
        _TileSize("Tile Size (m)", Float) = 3
        _NormalStrength("Normal Strength", Range(0, 2)) = 1
        _MetallicScale("Metallic", Range(0, 1)) = 0
        _SmoothnessScale("Smoothness", Range(0, 1)) = 1
        _AOStrength("AO Strength", Range(0, 1)) = 1
        _BlendSharpness("Blend Sharpness", Range(1, 16)) = 6
        // Used by the shadow/depth passes borrowed from URP Lit
        [HideInInspector] _Cutoff("Alpha Cutoff", Float) = 0.5
        // The material's original greybox color, kept by BunkerArtBuilder so re-runs tint from the same color
        [HideInInspector] _GreyboxColor("Greybox Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            TEXTURE2D(_ARMMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _TileSize;
                half _NormalStrength;
                half _MetallicScale;
                half _SmoothnessScale;
                half _AOStrength;
                half _BlendSharpness;
                half _Cutoff;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 n = normalize(input.normalWS);
                half3 w = pow(abs(n), _BlendSharpness);
                w /= (w.x + w.y + w.z);

                float3 p = input.positionWS / max(_TileSize, 0.01);
                float2 uvX = p.zy, uvY = p.xz, uvZ = p.xy;

                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvX).rgb * w.x
                             + SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvY).rgb * w.y
                             + SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvZ).rgb * w.z;
                half3 arm = SAMPLE_TEXTURE2D(_ARMMap, sampler_BaseMap, uvX).rgb * w.x
                          + SAMPLE_TEXTURE2D(_ARMMap, sampler_BaseMap, uvY).rgb * w.y
                          + SAMPLE_TEXTURE2D(_ARMMap, sampler_BaseMap, uvZ).rgb * w.z;

                // Whiteout-blended triplanar normals (tangent normals swizzled onto each projection axis)
                half3 tnX = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BaseMap, uvX), _NormalStrength);
                half3 tnY = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BaseMap, uvY), _NormalStrength);
                half3 tnZ = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BaseMap, uvZ), _NormalStrength);
                tnX = half3(tnX.xy + n.zy, abs(tnX.z) * n.x);
                tnY = half3(tnY.xy + n.xz, abs(tnY.z) * n.y);
                tnZ = half3(tnZ.xy + n.xy, abs(tnZ.z) * n.z);
                float3 normalWS = normalize(tnX.zyx * w.x + tnY.xzy * w.y + tnZ.xyz * w.z);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = albedo * _BaseColor.rgb;
                surface.metallic = saturate(arm.b * _MetallicScale);
                surface.smoothness = saturate((1 - arm.g) * _SmoothnessScale);
                surface.occlusion = lerp(1, arm.r, _AOStrength);
                surface.normalTS = half3(0, 0, 1);
                surface.alpha = 1;

                half4 color = UniversalFragmentPBR(inputData, surface);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return color;
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/SHADOWCASTER"
        UsePass "Universal Render Pipeline/Lit/DEPTHONLY"
        UsePass "Universal Render Pipeline/Lit/DEPTHNORMALS"
    }

    FallBack "Universal Render Pipeline/Lit"
}
