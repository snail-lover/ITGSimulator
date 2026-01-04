
Shader "Hidden/URP/BokehVisitedBlur"
{
    Properties
    {
        _BlurRadius      ("Blur Radius (px)", Range(0,20)) = 6
        _EdgeFeather     ("Edge Feather", Range(0,0.25)) = 0.06
        _DarkenStrength  ("Darken Strength", Range(0,1)) = 0.3

        /* Overlay controls retained (unchanged) -------------------------- */
        _VisitedOverlay   ("Visited Overlay", Range(0,1)) = 0.25
        _UnvisitedOverlay ("Un-visited Overlay", Range(0,1)) = 0.60
        _OverlayTint      ("Overlay Tint", Color) = (0.90,0.80,1.00,1)
        /* ---------------------------------------------------------------- */
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always

        HLSLINCLUDE
        #pragma prefer_hlslcc gles
        #pragma exclude_renderers d3d11_9x

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv         : TEXCOORD0;
        };

        // Fullscreen triangle without vertex buffer
        Varyings Vert (uint id : SV_VertexID)
        {
            Varyings o;
            float2 uv = float2((id << 1) & 2, id & 2);
            o.positionCS = float4(uv * 2.0 - 1.0, 0.0, 1.0);
            o.uv = uv;
            return o;
        }

        CBUFFER_START(UnityPerMaterial)
            float _BlurRadius;        // pixels (0..20)
            float _EdgeFeather;       // 0..0.25
            float _DarkenStrength;    // 0..1
            float _VisitedOverlay;    // 0..1
            float _UnvisitedOverlay;  // 0..1
            float4 _OverlayTint;      // rgb, a ignored
        CBUFFER_END

        // Optional globals that may be set by your script — kept for compatibility
        CBUFFER_START(UnityPerDraw)
            float  _BlurAmount;          // 0..1 global fade (if unused, leave at 1)
            float3 _PlayerWorldPos;      // (unused here, kept for API compatibility)
        CBUFFER_END

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        float4 _MainTex_TexelSize; // x = 1/width, y = 1/height

        // --------- GAUSSIAN 1D (dynamic radius, no sample count property) ---------
        float3 Gaussian1D(TEXTURE2D_PARAM(tex, samp), float2 uv, int radius, float2 texelDir)
        {
            radius = clamp(radius, 0, 20);         // sanity; matches inspector range
            if (radius == 0)
            {
                return SAMPLE_TEXTURE2D(tex, samp, uv).rgb;
            }

            // Derive sigma from the chosen radius. Using radius ~= 2*sigma is a good default.
            float sigma = max(0.01, (float)radius * 0.5);
            float twoSigma2 = 2.0 * sigma * sigma;

            float3 accum = 0.0;
            float  wsum  = 0.0;

            [loop]
            for (int i = -radius; i <= radius; i++)
            {
                float w = exp(- (i * i) / twoSigma2);
                float2 uvOff = uv + texelDir * i;
                accum += w * SAMPLE_TEXTURE2D(tex, samp, uvOff).rgb;
                wsum  += w;
            }
            return accum / max(wsum, 1e-5);
        }
        // --------------------------------------------------------------------------
        ENDHLSL

        // Pass 0 — Horizontal blur
        Pass
        {
            Name "GAUSS_HORIZONTAL"
            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            half4 Frag (Varyings i) : SV_Target
            {
                int radius = (int)round(_BlurRadius);
                float2 dir = float2(_MainTex_TexelSize.x, 0.0);
                float3 col = Gaussian1D(TEXTURE2D_ARGS(_MainTex, sampler_MainTex), i.uv, radius, dir);
                return half4(col, 1);
            }
            ENDHLSL
        }

        // Pass 1 — Vertical blur + optional darken/overlay (kept minimal)
        Pass
        {
            Name "GAUSS_VERTICAL"
            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            half4 Frag (Varyings i) : SV_Target
            {
                int radius = (int)round(_BlurRadius);
                float2 dir = float2(0.0, _MainTex_TexelSize.y);
                float3 col = Gaussian1D(TEXTURE2D_ARGS(_MainTex, sampler_MainTex), i.uv, radius, dir);

                // Optional simple post-tone to preserve prior behavior (non-destructive if values are 0)
                float blurMask = saturate(_BlurAmount);       // if not driven, leave _BlurAmount at 1
                col *= (1.0 - _DarkenStrength * blurMask);

                // overlay tint as a multiply, strength from the two sliders (choose whichever is higher)
                float overlayFac = saturate(max(_VisitedOverlay, _UnvisitedOverlay) * blurMask);
                col = lerp(col, col * _OverlayTint.rgb, overlayFac);

                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
