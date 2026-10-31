// Stylised-realistic water for URP: animated procedural ripples, depth-based absorption, refraction of the opaque
// scene, shoreline foam, sky reflection with fresnel, sun glint, and optional electric arcs (Wing B).
// Needs "Depth Texture" and "Opaque Texture" enabled on the URP asset (PC_RPAsset has both).
Shader "SignalLost/Water"
{
    Properties
    {
        _ShallowColor("Shallow Color", Color) = (0.2, 0.55, 0.55, 1)
        _DeepColor("Deep Color", Color) = (0.02, 0.1, 0.16, 1)
        _Absorption("Absorption (per meter, RGB)", Vector) = (0.45, 0.16, 0.1, 0)
        _DepthMax("Depth For Full Deep Color (m)", Float) = 8
        _DepthBias("Extra Color Depth (m)", Float) = 0
        _SunScatter("Sunlight In Water Body", Range(0, 1)) = 0.3
        _Smoothness("Smoothness", Range(0, 1)) = 0.96
        _ReflectionStrength("Reflection Strength", Range(0, 2)) = 1
        _WaveScale("Wave Scale (1/m)", Float) = 0.12
        _WaveSpeed("Wave Speed", Float) = 0.35
        _NormalStrength("Ripple Strength", Float) = 2.5
        _Refraction("Refraction", Float) = 0.04
        _FoamColor("Foam Color", Color) = (0.9, 0.95, 1, 1)
        _FoamWidth("Shore Foam Width (m)", Float) = 0.6
        _FoamAmount("Shore Foam Amount", Range(0, 1)) = 1
        _CrestFoam("Wave Crest Foam", Range(0, 1)) = 0.25
        _FarFade("Ripple Fade Distance (m)", Float) = 140
        [HDR] _SparkColor("Electric Arc Color", Color) = (0, 0, 0, 0)
        _SparkScale("Electric Arc Scale (1/m)", Float) = 0.9
        _SparkSpeed("Electric Arc Speed", Float) = 1.2
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                float4 _Absorption;
                float _DepthMax;
                float _DepthBias;
                half _SunScatter;
                half _Smoothness;
                half _ReflectionStrength;
                float _WaveScale;
                float _WaveSpeed;
                float _NormalStrength;
                float _Refraction;
                half4 _FoamColor;
                float _FoamWidth;
                half _FoamAmount;
                half _CrestFoam;
                float _FarFade;
                half4 _SparkColor;
                float _SparkScale;
                float _SparkSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half fogFactor : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float VNoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i), b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1)), d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Three drifting octaves, each rotated and moving a different way so the pattern never reads as a scroll.
            // The fine octaves fade with distance so the far sea stays calm instead of shimmering.
            float Height(float2 p, float t, float fade2, float fade3)
            {
                const float2x2 rot = float2x2(0.8, -0.6, 0.6, 0.8);
                float2 q = p * _WaveScale;
                float h = VNoise(q + float2(t * 0.6, t * 0.35)) * 0.55;
                q = mul(rot, q) * 2.1;
                h += VNoise(q + float2(-t * 0.9, t * 0.5)) * 0.3 * fade2;
                q = mul(rot, q) * 2.3;
                h += VNoise(q + float2(t * 1.2, -t * 1.0)) * 0.15 * fade3;
                return h;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 posWS = input.positionWS;
                float3 viewDir = GetWorldSpaceNormalizeViewDir(posWS);
                float dist = distance(_WorldSpaceCameraPos, posWS);
                float t = _Time.y * _WaveSpeed;

                // Ripple normal from the height field's slope
                float fade2 = saturate(1.0 - dist / (_FarFade * 1.5));
                float fade3 = saturate(1.0 - dist / _FarFade);
                float eps = 0.25 / (_WaveScale * 4.8);
                float h0 = Height(posWS.xz, t, fade2, fade3);
                float hx = Height(posWS.xz + float2(eps, 0), t, fade2, fade3);
                float hz = Height(posWS.xz + float2(0, eps), t, fade2, fade3);
                float3 normalWS = normalize(float3(-(hx - h0) / eps * _NormalStrength, 1.0, -(hz - h0) / eps * _NormalStrength));

                // Depth of water under this pixel, with the refracted lookup guarded against objects in front of the water
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float surfaceEye = input.positionCS.w;
                float2 refractUV = screenUV + normalWS.xz * _Refraction;
                float sceneEye = LinearEyeDepth(SampleSceneDepth(refractUV), _ZBufferParams);
                if (sceneEye < surfaceEye) refractUV = screenUV;
                sceneEye = LinearEyeDepth(SampleSceneDepth(refractUV), _ZBufferParams);
                float depth = max(sceneEye - surfaceEye, 0.0);

                // Beer-Lambert: the scene shows through shallow water and fades to the deep color as it gets deeper
                half3 sceneColor = SampleSceneColor(refractUV);
                float colorDepth = depth + _DepthBias;
                half3 transmit = exp(-colorDepth * _Absorption.rgb);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(posWS));
                half3 bodyLight = SampleSH(half3(0, 1, 0)) + mainLight.color * mainLight.shadowAttenuation * _SunScatter;
                half3 body = lerp(_ShallowColor.rgb, _DeepColor.rgb, saturate(colorDepth / _DepthMax)) * bodyLight;
                half3 color = sceneColor * transmit + body * (1.0 - transmit);

                // Reflection, sun glint and lamp highlights: all specular, no diffuse
                InputData inputData = (InputData)0;
                inputData.positionWS = posWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDir;
                inputData.shadowCoord = TransformWorldToShadowCoord(posWS);
                inputData.fogCoord = input.fogFactor;
                inputData.bakedGI = half3(0, 0, 0);
                inputData.normalizedScreenSpaceUV = screenUV;
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = half3(0, 0, 0);
                surface.metallic = 0;
                surface.specular = half3(0.04, 0.04, 0.04);
                surface.smoothness = _Smoothness;
                surface.occlusion = 1;
                surface.normalTS = half3(0, 0, 1);
                surface.alpha = 1;
                half3 spec = UniversalFragmentPBR(inputData, surface).rgb;
                color += spec * _ReflectionStrength;

                // Foam: a band along every shoreline or object that pokes through, broken up by moving noise, plus faint crests
                float foamNoise = VNoise(posWS.xz * 1.7 + float2(t * 2.0, -t * 1.3)) * 0.6 + VNoise(posWS.xz * 4.3 - t * 1.5) * 0.4;
                float shore = saturate(1.0 - depth / max(_FoamWidth, 0.01));
                float foam = smoothstep(0.35, 0.8, foamNoise + shore * 0.85 - 0.35) * shore * _FoamAmount;
                foam = max(foam, smoothstep(0.78, 0.95, h0 * 1.3 + foamNoise * 0.25) * _CrestFoam * fade3);
                color = lerp(color, _FoamColor.rgb * (SampleSH(half3(0, 1, 0)) * 0.6 + mainLight.color * 0.5), saturate(foam));

                // Electric arcs crawling over the surface: thin bright lines where two drifting noise fields cross 0.5
                if (_SparkColor.a > 0)
                {
                    float2 sp = posWS.xz * _SparkScale;
                    float st = _Time.y * _SparkSpeed;
                    float n1 = VNoise(sp + float2(st, st * 0.6));
                    float n2 = VNoise(sp * 1.9 + float2(-st * 1.3, st));
                    // Arcs only show in drifting patches, so most of the surface stays dark and murky
                    float mask = smoothstep(0.5, 0.75, VNoise(sp * 0.3 + float2(st * 0.35, -st * 0.2)));
                    float arcs = saturate(1.0 - abs(n1 - 0.5) * 38.0) + saturate(1.0 - abs(n2 - 0.5) * 46.0) * 0.6;
                    float pulse = 0.35 + 0.65 * step(0.5, VNoise(float2(_Time.y * 11.0, 3.7)));
                    float glow = saturate(1.0 - abs(n1 - 0.5) * 6.0) * 0.05;
                    arcs = (arcs * pulse + glow) * mask;
                    color += _SparkColor.rgb * arcs;
                }

                color = MixFog(color, input.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
