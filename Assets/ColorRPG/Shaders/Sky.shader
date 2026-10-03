// ColorRPG/Sky - cer procedural: gradient (zenit / orizont / jos), soare cu halou
// si nori animati (fbm), pentru URP. Culorile se schimba pe fiecare harta din cod.
Shader "ColorRPG/Sky"
{
    Properties
    {
        _TopColor ("Top (zenith)", Color) = (0.25, 0.5, 0.95, 1)
        _HorizonColor ("Horizon", Color) = (0.72, 0.86, 1, 1)
        _BottomColor ("Bottom", Color) = (0.45, 0.55, 0.5, 1)
        _SunColor ("Sun", Color) = (1, 0.95, 0.8, 1)
        _SunSize ("Sun size", Range(0.001, 0.1)) = 0.025
        _CloudColor ("Clouds", Color) = (1, 1, 1, 1)
        _CloudShade ("Cloud shade", Color) = (0.72, 0.78, 0.88, 1)
        _CloudCover ("Cloud cover", Range(0, 1)) = 0.45
        _CloudScale ("Cloud scale", Float) = 1.3
        _CloudSpeed ("Cloud speed", Float) = 0.01
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor, _HorizonColor, _BottomColor, _SunColor, _CloudColor, _CloudShade;
                float _SunSize, _CloudCover, _CloudScale, _CloudSpeed;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.dir = v.positionOS.xyz;
                return o;
            }

            float hash21(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            float vnoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = hash21(i), b = hash21(i + float2(1, 0)), c = hash21(i + float2(0, 1)), d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }
            float fbm(float2 p)
            {
                float v = 0.0, a = 0.5;
                for (int k = 0; k < 5; k++) { v += a * vnoise(p); p = p * 2.03 + 17.1; a *= 0.5; }
                return v;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float h = d.y;
                half3 col = h > 0 ? lerp(_HorizonColor.rgb, _TopColor.rgb, pow(saturate(h), 0.45))
                                  : lerp(_HorizonColor.rgb, _BottomColor.rgb, pow(saturate(-h * 3.0), 0.6));

                // soare (directia luminii principale)
                float3 sunDir = normalize(_MainLightPosition.xyz);
                float sd = dot(d, sunDir);
                float disc = smoothstep(1.0 - _SunSize, 1.0 - _SunSize * 0.55, sd);
                float glow = pow(saturate(sd), 48.0) * 0.55 + pow(saturate(sd), 6.0) * 0.18;
                col += _SunColor.rgb * (disc * 2.5 + glow);

                // nori pe un "plan" deasupra lumii
                if (h > 0.0)
                {
                    float2 uv = d.xz / (h + 0.12) * _CloudScale + _Time.y * _CloudSpeed * float2(1.0, 0.35);
                    float n = fbm(uv);
                    float cover = smoothstep(1.0 - _CloudCover, 1.0 - _CloudCover + 0.28, n);
                    float thick = fbm(uv * 1.7 + 3.1);
                    half3 cc = lerp(_CloudShade.rgb, _CloudColor.rgb, saturate(thick * 1.2 + glow * 2.0));
                    float fade = smoothstep(0.0, 0.18, h);
                    col = lerp(col, cc, cover * fade * 0.92);
                }
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
