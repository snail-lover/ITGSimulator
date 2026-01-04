Shader "Hidden/URP/BokehVisitedBlur"
{
    Properties
    {
        _BlurRadius      ("Blur Radius (px)", Range(0,20)) = 6
        _SampleCount     ("Sample Count", Range(1,64)) = 24
        _EdgeFeather     ("Edge Feather", Range(0,0.25)) = 0.06
        _BokehShapeFactor("Bokeh Shape (0=circle,1=hex)", Range(0,1)) = 0.0
        _DarkenStrength  ("Darken Strength", Range(0,1)) = 0.3

        /* NEW ------------------------------------------------------------ */
        _VisitedOverlay   ("Visited Overlay", Range(0,1)) = 0.25
        _UnvisitedOverlay ("Un-visited Overlay", Range(0,1)) = 0.60
        _OverlayTint      ("Overlay Tint", Color) = (0.90,0.80,1.00,1)
        /* ---------------------------------------------------------------- */
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        ZTest Always  ZWrite Off  Cull Off
        Blend One Zero

        Pass
        {
            Name "BokehVisitedBlur"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _BlurRadius;
                int   _SampleCount;
                float _BokehShapeFactor;
                float _EdgeFeather;
                float _DarkenStrength;

                float _VisitedOverlay;      // 0‒1
                float _UnvisitedOverlay;    // 0‒1
                float4 _OverlayTint;        // rgb, a ignored
            CBUFFER_END

            // ---------- GLOBALS FROM SCRIPT ----------
            float  _BlurAmount;          // 0‒1 global fade
            float3 _PlayerWorldPos;      // player position
            float  _ActivationBelt;      // belt thickness (0‒0.25)

            #define MAX_BOXES 32
            float4x4 _WorldToBoxArr[MAX_BOXES];
            float    _OverlayArr   [MAX_BOXES];   // NEW : per-box overlay (visited / un-visited)
            int      _BoxCount;

            #define PI 3.14159265358979

            half4 Frag (Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float3 src = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord).rgb;

                /* ----------------------------------------------------------------
                 *  MASK-BUILD ‒ for each box we compute:
                 *    edgePix  : 1 inside, 0 outside  (with feather)
                 *    wPlayer  : 1 when player inside, 0 outside belt
                 *    overlay  : visited ? small : big
                 *  We keep the **max inside-mask** and **max overlay strength**.
                 * ----------------------------------------------------------------*/
                float insideMask = 0.0;   // 0 outside (blurred) → 1 inside player zone (sharp)
                float overlayFac = 0.0;   // 0 none → 1 full overlay

float3 baseCol = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord).rgb;

float rawDepth = SampleSceneDepth(i.texcoord);
#if UNITY_REVERSED_Z
    bool validDepth = rawDepth > 0.0 + 1e-6;
#else
    bool validDepth = rawDepth < 1.0 - 1e-6;
#endif

// Skip skybox / far-plane pixels
if (!validDepth)
    return half4(baseCol, 1);

                if (validDepth && _BoxCount > 0 && _BlurAmount > 0.0001)
                {
                    float deviceDepth = rawDepth;
                #if !UNITY_REVERSED_Z
                    deviceDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, deviceDepth);
                #endif
                    float3 worldP = ComputeWorldSpacePosition(i.texcoord, deviceDepth, UNITY_MATRIX_I_VP);

                    [loop]
                    for (int b = 0; b < MAX_BOXES; b++)
                    {
                        if (b >= _BoxCount) break;

                        float4x4 m = _WorldToBoxArr[b];
                        float3 pPix = mul(m, float4(worldP,1)).xyz;         // pixel in box-space
                        float3 pPl  = mul(m, float4(_PlayerWorldPos,1)).xyz; // player in box-space

                        // signed distances to the box inner walls (0.5 units from centre)
                        float sPix = min(min(0.5 - abs(pPix.x), 0.5 - abs(pPix.y)), 0.5 - abs(pPix.z));
                        float sPl  = min(min(0.5 - abs(pPl .x), 0.5 - abs(pPl .y)), 0.5 - abs(pPl .z));

                        float edgePix = smoothstep(-_EdgeFeather, _EdgeFeather, sPix);         // 0→1
                        float wPlayer = smoothstep(-_ActivationBelt, _ActivationBelt, sPl);    // 0→1

                        insideMask = max(insideMask, edgePix * wPlayer);                      // 1 only for *current* zone

                        // overlay only if NOT current zone  → (1-wPlayer)
                        float overlayThis = (1.0 - wPlayer) * edgePix * _OverlayArr[b];
                        overlayFac = max(overlayFac, overlayThis);
                    }
                }

                // ---------- BLUR ----------
                if (_BlurAmount <= 0.0001 || insideMask >= 0.999)       // fully inside current zone
                    return half4(src,1);

                const int MAX_SAMPLES = 64;
                int taps = clamp(_SampleCount,1,MAX_SAMPLES);
                float2 radiusUV = (_BlurRadius * _BlurAmount) / _ScreenParams.xy;

                float3 acc = src;
                float  wSum = 1.0;

                const float GOLD = 2.39996323;
                [loop]
                for (int k=1;k<MAX_SAMPLES && k<taps;k++)
                {
                    float t = (float)k;
                    float a = t * GOLD;
                    float r = t / (taps - 1 + 1e-5);

                    float2 dir = float2(cos(a), sin(a)); 
                    float2 uv  = i.texcoord + dir * r * radiusUV;

                    float3 c = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, 0).rgb;
                    acc += c; wSum += 1.0;
                }

                float3 blurred = acc / max(1.0,wSum);

                // ---------- COMPOSE ----------
                float3 col = lerp(blurred, src, insideMask);                 // keep src in current zone
                float blurMask = (1.0 - insideMask) * _BlurAmount;

                // darken
                col *= 1.0 - _DarkenStrength * blurMask;

                // colour overlay (tint multiply)
                col = lerp(col, col * _OverlayTint.rgb, saturate(overlayFac));

                return half4(col,1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
