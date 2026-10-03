// ColorRPG/Foliage - frunzisul copacilor/tufelor generate procedural (ProcNature):
// carduri cu textura de frunze (alpha cut), normale "sferice" (lumina moale ca la copacii reali),
// lumina care trece prin frunze (translucenta), vant, zapada pe varfuri, umbre, SSAO, ceata.
Shader "ColorRPG/Foliage"
{
    Properties
    {
        [MainTexture] _BaseMap ("Leaves (alpha)", 2D) = "white" {}
        [MainColor] _BaseColor ("Color", Color) = (0.3, 0.55, 0.2, 1)
        _Color2 ("Color variation", Color) = (0.45, 0.62, 0.18, 1)
        _Cutoff ("Alpha cutoff", Range(0, 1)) = 0.42
        _WindStrength ("Wind strength", Float) = 0.12
        _WindSpeed ("Wind speed", Float) = 1.3
        _Translucency ("Translucency", Range(0, 1)) = 0.55
        _SnowAmount ("Snow", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" "RenderPipeline" = "UniversalPipeline" }
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor, _Color2;
            half _Cutoff, _WindStrength, _WindSpeed, _Translucency, _SnowAmount;
        CBUFFER_END
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

        // vant: tot copacul se leagana (dupa inaltime, r) + frunzele tremura (g)
        float3 CR_Wind(float3 ws, half4 vc)
        {
            float t = _Time.y * _WindSpeed;
            float sway = sin(t + ws.x * 0.07 + ws.z * 0.05) * 0.7 + sin(t * 2.1 + ws.x * 0.13) * 0.3;
            float flutter = sin(t * 7.0 + vc.g * 40.0 + ws.y * 3.0) * 0.12;
            float h = vc.r * vc.r;
            ws.x += (sway + flutter) * _WindStrength * h;
            ws.z += (sway * 0.5 - flutter) * _WindStrength * 0.6 * h;
            ws.y += flutter * _WindStrength * 0.3 * vc.r;
            return ws;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
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
                half4 color : COLOR;     // r = inaltimea in copac, g = variatie, b = ocluzie (interior), a = zapada
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half4 color : TEXCOORD3;
                half fog : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                float3 ws = CR_Wind(TransformObjectToWorld(v.positionOS.xyz), v.color);
                o.positionWS = ws;
                o.positionCS = TransformWorldToHClip(ws);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.color = v.color;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                clip(tex.a - _Cutoff);
                float3 N = normalize(i.normalWS);
                float3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);
                half3 albedo = tex.rgb * lerp(_BaseColor.rgb, _Color2.rgb, i.color.g);
                // zapada pe partea de sus a crengilor
                half snow = saturate(_SnowAmount * i.color.a * smoothstep(0.0, 0.5, N.y) * 1.6);
                albedo = lerp(albedo, half3(0.92, 0.95, 1.0), snow);

                Light light = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half shadow = lerp(0.3, 1.0, light.shadowAttenuation);
                half ndl = saturate(dot(N, light.direction) * 0.6 + 0.4);
                half ao = lerp(0.55, 1.0, i.color.b);
            #if defined(_SCREEN_SPACE_OCCLUSION)
                AmbientOcclusionFactor aof = GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(i.positionCS));
                ao *= aof.indirectAmbientOcclusion;
            #endif
                half3 direct = light.color * ndl * shadow;
                half3 ambient = SampleSH(N) * ao;
                // lumina care trece prin frunze cand soarele e in spatele copacului
                half back = pow(saturate(dot(V, -light.direction)), 3.0) * _Translucency * (1.0 - snow);
                half3 col = albedo * (direct * ao + ambient) + albedo * light.color * back * shadow * 0.9;
                // mic luciu pe frunze
                half3 H = normalize(light.direction + V);
                col += light.color * pow(saturate(dot(N, H)), 24.0) * 0.06 * shadow;
                col = MixFog(col, i.fog);
                return half4(col, 1);
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
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            float3 _LightPosition;
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                float3 ws = CR_Wind(TransformObjectToWorld(v.positionOS.xyz), v.color);
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
                o.positionCS = cs;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.positionCS = TransformWorldToHClip(CR_Wind(TransformObjectToWorld(v.positionOS.xyz), v.color));
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }
            half frag(Varyings i) : SV_Target
            {
                clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a - _Cutoff);
                return i.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 normalWS : TEXCOORD1; };
            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.positionCS = TransformWorldToHClip(CR_Wind(TransformObjectToWorld(v.positionOS.xyz), v.color));
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a - _Cutoff);
                float3 n = normalize(i.normalWS);
            #if defined(_GBUFFER_NORMALS_OCT)
                float2 oct = PackNormalOctQuadEncode(n);
                return half4(PackFloat2To888(saturate(oct * 0.5 + 0.5)), 0.0);
            #else
                return half4(n, 0.0);
            #endif
            }
            ENDHLSL
        }
    }
    Fallback Off
}
