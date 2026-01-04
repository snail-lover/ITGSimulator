// UI_Blur_Shader.shader
Shader "UI/Better Gaussian Blur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _BlurAmount ("Blur Amount", Range(0, 10)) = 0.0

        // --- Stencil properties for UI Masking ---
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "CanUseSpriteAtlas"="true"
            "RenderType"="Transparent"
        }

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

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            float _BlurAmount;

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
                float2 texelSize = _MainTex_TexelSize.xy * _BlurAmount;
                fixed4 col = fixed4(0,0,0,0);

                // 9-tap Gaussian kernel
                col += tex2D(_MainTex, i.texcoord + texelSize * float2(-1.0, -1.0)) * 0.0625;
                col += tex2D(_MainTex, i.texcoord + texelSize * float2( 0.0, -1.0)) * 0.125;
                col += tex2D(_MainTex, i.texcoord + texelSize * float2( 1.0, -1.0)) * 0.0625;

                col += tex2D(_MainTex, i.texcoord + texelSize * float2(-1.0,  0.0)) * 0.125;
                col += tex2D(_MainTex, i.texcoord + texelSize * float2( 0.0,  0.0)) * 0.25;
                col += tex2D(_MainTex, i.texcoord + texelSize * float2( 1.0,  0.0)) * 0.125;

                col += tex2D(_MainTex, i.texcoord + texelSize * float2(-1.0,  1.0)) * 0.0625;
                col += tex2D(_MainTex, i.texcoord + texelSize * float2( 0.0,  1.0)) * 0.125;
                col += tex2D(_MainTex, i.texcoord + texelSize * float2( 1.0,  1.0)) * 0.0625;
                
                // Preserve original alpha and apply tint
                col.a = tex2D(_MainTex, i.texcoord).a;
                return col * i.color;
            }
            ENDCG
        }
    }
}