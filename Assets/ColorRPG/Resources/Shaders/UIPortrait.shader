// ColorRPG/UIPortrait - portretul eroului (randat fara post-processing) primeste in UI
// acelasi "look" ca lumea: putin mai mult contrast si culori mai vii (altfel parea sters).
// (pornit din UIIconArt)
// Culoarea Image-ului NU mai coloreaza iconita (ar strica culorile reale); se pastreaza doar:
//  * transparenta (alpha)
//  * intunecarea pentru culori inchise (ex. item blocat / gri) - ca sa se vada ca e "inactiv".
Shader "ColorRPG/UIPortrait"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata_t { float4 vertex : POSITION; float4 color : COLOR; float2 texcoord : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 texcoord : TEXCOORD0; };
            sampler2D _MainTex;
            fixed4 _Color;
            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.texcoord);
                half3 rgb = c.rgb;
                half l = dot(rgb, half3(0.299, 0.587, 0.114));
                rgb = lerp(l.xxx, rgb, 1.18);                 // saturatie
                rgb = (rgb - 0.5) * 1.12 + 0.5;               // contrast
                c.rgb = saturate(rgb) * i.color.rgb;
                c.a *= i.color.a;
                return c;
            }
            ENDCG
        }
    }
}
