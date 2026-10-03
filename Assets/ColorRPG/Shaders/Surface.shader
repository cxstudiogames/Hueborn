// ColorRPG/Surface - shaderul "realist" pentru toate obiectele si solul.
// PBR (lumina URP: soare, lumini aditionale, umbre, SSAO, ceata) + detalii procedurale in spatiul lumii:
//  - variatie de culoare pe zone mari (pete) si fine (granulatie),
//  - relief (bump) calculat din zgomot, fara texturi externe,
//  - tip de suprafata ghicit dupa culoare: frunzis, lemn, piatra/metal, zapada (sclipiri),
//  - umbrire de contact langa sol + lumina de margine (rim).
// Proprietatile au aceleasi nume ca la URP/Lit (_BaseMap, _BaseColor, _Smoothness, _Metallic, _EmissionColor).
Shader "ColorRPG/Surface"
{
    Properties
    {
        [MainTexture] _BaseMap ("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.25
        _Metallic ("Metallic", Range(0, 1)) = 0
        [HDR] _EmissionColor ("Emission", Color) = (0, 0, 0, 1)
        _DetailStrength ("Detail strength", Range(0, 2)) = 1
        _DetailScale ("Detail scale", Float) = 1
        _BumpStrength ("Bump strength", Range(0, 3)) = 1
        _MacroStrength ("Macro variation", Range(0, 1)) = 1
        _RimStrength ("Rim light", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _EmissionColor;
            half _Smoothness, _Metallic;
            half _DetailStrength, _DetailScale, _BumpStrength, _MacroStrength, _RimStrength;
        CBUFFER_END

        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        ENDHLSL

        // -----------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half fog : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // ---- zgomot procedural (value noise 3D)
            float CR_Hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }
            float CR_Noise(float3 x)
            {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                float a = CR_Hash(i);
                float b = CR_Hash(i + float3(1, 0, 0));
                float c = CR_Hash(i + float3(0, 1, 0));
                float d = CR_Hash(i + float3(1, 1, 0));
                float e = CR_Hash(i + float3(0, 0, 1));
                float g = CR_Hash(i + float3(1, 0, 1));
                float h = CR_Hash(i + float3(0, 1, 1));
                float k = CR_Hash(i + float3(1, 1, 1));
                return lerp(lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y), lerp(lerp(e, g, f.x), lerp(h, k, f.x), f.y), f.z);
            }
            float CR_Fbm(float3 p)
            {
                return CR_Noise(p) * 0.6 + CR_Noise(p * 2.07 + 13.1) * 0.28 + CR_Noise(p * 4.3 + 5.7) * 0.12;
            }
            float3 CR_Hsv(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = c.g < c.b ? float4(c.bg, K.wz) : float4(c.gb, K.xy);
                float4 q = c.r < p.x ? float4(p.xyw, c.r) : float4(c.r, p.yzx);
                float d = q.x - min(q.w, q.y);
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + 1e-5)), d / (q.x + 1e-5), q.x);
            }

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                VertexPositionInputs pos = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fog = ComputeFogFactor(pos.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 P = i.positionWS;
                float3 N = normalize(i.normalWS);
                float3 V = GetWorldSpaceNormalizeViewDir(P);
                float dist = length(GetCameraPositionWS() - P);

                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                half3 albedo = tex.rgb * _BaseColor.rgb;

                // ---- ce fel de suprafata e (dupa culoare)
                float3 hsv = CR_Hsv(saturate(albedo));
                float satW = smoothstep(0.18, 0.34, hsv.y);
                float foliage = satW * saturate(1.0 - abs(hsv.x - 0.3) / 0.15);
                float wood = satW * saturate(1.0 - abs(hsv.x - 0.075) / 0.06) * (1.0 - smoothstep(0.5, 0.85, hsv.z));
                float snow = (1.0 - smoothstep(0.08, 0.22, hsv.y)) * smoothstep(0.72, 0.9, hsv.z);
                float stone = (1.0 - smoothstep(0.1, 0.3, hsv.y)) * (1.0 - snow);
                float metal = saturate(_Metallic * 1.5);
                float ground = smoothstep(0.85, 0.97, N.y) * (1.0 - smoothstep(0.6, 2.5, P.y));

                float fade = saturate(1.0 - dist / 70.0);          // detaliile fine dispar in departare (fara "purici")
                float ds = _DetailStrength;
                float3 p = P * _DetailScale;

                // ---- variatie mare (pete) - si pe departe
                float macro = CR_Noise(P * 0.06 + 3.1) * 0.65 + CR_Noise(P * 0.19 + 11.7) * 0.35;
                albedo *= lerp(1.0, 0.8 + 0.4 * macro, _MacroStrength * 0.7 * (1.0 - metal));
                albedo = lerp(albedo, albedo * half3(1.1, 1.06, 0.78), foliage * saturate(macro * 1.6 - 0.5) * 0.6 * _MacroStrength);

                // ---- granulatie fina + relief
                float fine = CR_Fbm(p * lerp(2.6, 4.2, foliage));
                float coarse = CR_Fbm(p * 0.8 + 21.0);
                float grain = CR_Noise(P * float3(9.0, 0.6, 9.0) * _DetailScale + 4.0);
                float amp = (0.16 + 0.16 * foliage + 0.2 * stone - 0.08 * snow) * (1.0 - 0.7 * metal) * ds * fade;
                albedo *= 1.0 + (fine - 0.5) * amp * 2.0;
                albedo *= 1.0 + (grain - 0.5) * 0.5 * wood * ds * fade;
                // cavitati mai intunecate pe piatra
                albedo *= lerp(1.0, 0.62 + 0.38 * smoothstep(0.25, 0.7, coarse), stone * 0.75 * ds * fade * (1.0 - metal));
                albedo = saturate(albedo);

                // relief: inaltime procedurala -> normala perturbata (bump cu derivate de ecran)
                float h = (fine * (0.7 + 0.6 * stone) + coarse * (0.5 + 0.8 * stone) + grain * 0.6 * wood) * 0.02;
                h *= _BumpStrength * (1.0 - 0.75 * metal) * (1.0 - 0.4 * snow) * fade;
                float3 dpdx = ddx(P), dpdy = ddy(P);
                float dhdx = ddx(h), dhdy = ddy(h);
                float3 r1 = cross(dpdy, N), r2 = cross(N, dpdx);
                float det = dot(dpdx, r1);
                float3 grad = sign(det) * (dhdx * r1 + dhdy * r2);
                float3 Nb = normalize(abs(det) * N - grad);
                N = (abs(det) > 1e-10) ? Nb : N;

                // ---- ocluzie: cavitati + contact cu solul (doar pe peretii obiectelor)
                half ao = lerp(1.0, 0.55 + 0.45 * smoothstep(0.2, 0.75, coarse), (0.35 + 0.4 * stone) * ds * fade * (1.0 - metal));
                float wall = 1.0 - saturate(abs(N.y));
                ao *= lerp(1.0, lerp(0.62, 1.0, saturate(P.y / 0.9)), wall * 0.85 * (1.0 - ground));

                // ---- luciu dupa tipul suprafetei
                half sm = _Smoothness;
                half targetSm = 0.12 * foliage + 0.18 * wood + (0.18 + 0.25 * fine) * stone + 0.42 * snow;
                half wSum = saturate(foliage + wood + stone + snow);
                sm = lerp(sm, targetSm / max(wSum, 0.001), wSum * 0.65 * (1.0 - metal));
                sm = saturate(sm * lerp(1.0, 0.75 + 0.5 * fine, fade));

                // ---- lumina (PBR URP)
                InputData inputData = (InputData)0;
                inputData.positionWS = P;
                inputData.normalWS = N;
                inputData.viewDirectionWS = V;
                inputData.shadowCoord = TransformWorldToShadowCoord(P);
                inputData.fogCoord = i.fog;
                inputData.vertexLighting = half3(0, 0, 0);
                inputData.bakedGI = SampleSH(N);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData s = (SurfaceData)0;
                s.albedo = albedo;
                s.metallic = _Metallic;
                s.specular = half3(0, 0, 0);
                s.smoothness = sm;
                s.occlusion = ao;
                s.emission = _EmissionColor.rgb;
                // lava: suprafata care curge si are crusta (doar pe baltile/craterele foarte stralucitoare)
                float em = max(_EmissionColor.r, max(_EmissionColor.g, _EmissionColor.b));
                float lavaW = smoothstep(1.52, 1.6, em) * smoothstep(0.8, 0.95, N.y);
                if (lavaW > 0.001)
                {
                    float fl = CR_Fbm(float3(P.x * 0.45 + _Time.y * 0.12, _Time.y * 0.07, P.z * 0.45 - _Time.y * 0.05));
                    float fl2 = CR_Noise(float3(P.x * 1.7 - _Time.y * 0.3, _Time.y * 0.2, P.z * 1.7));
                    float hot = smoothstep(0.32, 0.62, fl * 0.8 + fl2 * 0.3);
                    s.emission *= lerp(1.0, 0.08 + 1.35 * hot, lavaW);
                    s.albedo = lerp(s.albedo, lerp(half3(0.08, 0.05, 0.04), s.albedo, hot), lavaW);
                    s.smoothness = lerp(s.smoothness, 0.15 + 0.5 * hot, lavaW);
                }
                s.alpha = 1.0;
                s.normalTS = half3(0, 0, 1);

                half4 col = UniversalFragmentPBR(inputData, s);

                // lumina de margine (contur luminos discret, obiectele "ies" din fundal)
                half rim = pow(1.0 - saturate(dot(N, V)), 4.0) * _RimStrength * (1.0 - ground);
                col.rgb += rim * (inputData.bakedGI * 0.6 + GetMainLight().color * 0.15) * (0.4 + albedo);

                // sclipiri pe zapada
                float spark = step(0.993, CR_Hash(floor(P * 38.0) + floor(V * 6.0))) * snow * fade;
                col.rgb += spark * GetMainLight().color * 0.9;

                col.rgb = MixFog(col.rgb, inputData.fogCoord);
                col.a = 1.0;
                return col;
            }
            ENDHLSL
        }

        // -----------------------------------------------------------------
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
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 vert(Attributes v) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(v);
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                float3 nws = TransformObjectToWorldNormal(v.normalOS);
            #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                float3 ld = normalize(_LightPosition - ws);
            #else
                float3 ld = _LightDirection;
            #endif
                float4 cs = TransformWorldToHClip(ApplyShadowBias(ws, nws, ld));
            #if UNITY_REVERSED_Z
                cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE);
            #else
                cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                return cs;
            }

            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }

        // -----------------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }
            half frag(Varyings i) : SV_Target { return i.positionCS.z; }
            ENDHLSL
        }

        // -----------------------------------------------------------------
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
            #if defined(_GBUFFER_NORMALS_OCT)
                float2 oct = PackNormalOctQuadEncode(n);
                float2 rem = saturate(oct * 0.5 + 0.5);
                return half4(PackFloat2To888(rem), 0.0);
            #else
                return half4(n, 0.0);
            #endif
            }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Lit"
}
