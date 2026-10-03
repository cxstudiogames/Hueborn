// ColorRPG/Grass - fire de iarba desenate cu GPU instancing (GrassField):
// culoare de la radacina la varf, leganare in vant, lumina + umbre de la soare, ceata.
Shader "ColorRPG/Grass"
{
    Properties
    {
        _BaseColor ("Root color", Color) = (0.18, 0.42, 0.12, 1)
        _TipColor ("Tip color", Color) = (0.62, 0.86, 0.35, 1)
        _WindStrength ("Wind strength", Float) = 0.22
        _WindSpeed ("Wind speed", Float) = 1.6
        _WindScale ("Wind scale", Float) = 0.12
        _Translucency ("Translucency", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor, _TipColor;
                float _WindStrength, _WindSpeed, _WindScale, _Translucency;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;          // r = inaltimea pe fir (0 radacina, 1 varf), g = variatie
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half2 hv : TEXCOORD2;
                half fog : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                float hgt = v.color.r;
                float t = _Time.y * _WindSpeed;
                float w = sin(t + ws.x * _WindScale + ws.z * _WindScale * 0.7) * 0.7 + sin(t * 2.3 + ws.x * 0.9 + ws.z * 0.6) * 0.3;
                ws.x += w * _WindStrength * hgt * hgt;
                ws.z += w * _WindStrength * 0.4 * hgt * hgt;
                o.positionWS = ws;
                o.positionCS = TransformWorldToHClip(ws);
                float3 n = TransformObjectToWorldNormal(v.normalOS);
                o.normalWS = normalize(lerp(n, float3(0, 1, 0), 0.55));
                o.hv = half2(hgt, v.color.g);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i, bool front : SV_IsFrontFace) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half3 albedo = lerp(_BaseColor.rgb, _TipColor.rgb, i.hv.x) * (0.82 + 0.36 * i.hv.y);
                float3 n = normalize(i.normalWS);
                Light light = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half ndl = saturate(dot(n, light.direction)) * 0.65 + 0.35;
                half shadow = lerp(0.35, 1.0, light.shadowAttenuation);
                half3 direct = light.color * ndl * shadow;
                half3 ambient = SampleSH(n) * 0.9;
                half3 col = albedo * (direct + ambient) + albedo * light.color * _Translucency * i.hv.x * shadow * 0.35;
                col *= lerp(0.55, 1.0, i.hv.x);   // radacina mai intunecata (ocluzie)
                col = MixFog(col, i.fog);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
