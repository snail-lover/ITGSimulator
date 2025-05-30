// --- START OF PartialFadeWallUnlitPass.shader ---
Shader "MyCustomShaders/PartialFadeWallUnlit"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)

        // Properties for our partial fade effect
        _OcclusionPoint("Occlusion Point (World)", Vector) = (0,0,0,0) // Store as float4, use .xyz
        _OcclusionRadius("Occlusion Radius", Float) = 1.5
        _OcclusionSoftness("Occlusion Softness (0-1)", Range(0.01, 1.0)) = 0.5
        _OcclusionActive("Occlusion Active", Float) = 0.0 // 0 for off, 1 for on

        // URP Standard Properties (needed for Transparency)
        _Surface("__surface", Float) = 0.0 // 0 = Opaque, 1 = Transparent
        _Blend("__blend", Float) = 0.0 // 0 = Alpha, 1 = Premultiply, 2 = Additive, 3 = Multiply
        _SrcBlend("__src", Float) = 1.0
        _DstBlend("__dst", Float) = 0.0
        _ZWrite("__zw", Float) = 1.0
        // _Cull ("__cull", Float) = 2.0 // Cull Back typically
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent" // Important for URP to classify it
            "Queue" = "Transparent"      // Render after opaque objects
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardUnlit" // Or just "Unlit"
            Tags { "LightMode" = "UniversalForward" } // Essential URP tag

            // Blend state for transparency
            Blend [_SrcBlend] [_DstBlend] // Takes values from properties
            ZWrite [_ZWrite]              // Takes value from property
            // Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex UnlitPassVertex
            #pragma fragment UnlitPassFragment

            // URP Core Includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl" // For space transforms
            // If doing a Lit shader, you'd include SurfaceInput.hlsl, UniversalFragmentPBR.hlsl etc.

            // CBUFFER contents are automatically populated by URP if properties match
            // For material properties that change per-instance, they are often in UnityPerMaterial CBUFFER
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; // For texture tiling/offset
                half4 _BaseColor;
                float4 _OcclusionPoint; // Using float4, access .xyz
                float _OcclusionRadius;
                float _OcclusionSoftness;
                float _OcclusionActive;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID // For instancing
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1; // World space position
                UNITY_VERTEX_OUTPUT_STEREO // For stereo rendering
            };

            Varyings UnlitPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 UnlitPassFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 baseMapColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 finalColor = baseMapColor * _BaseColor; // Base texture * tint (includes alpha)

                // Partial Occlusion Logic
                half finalAlpha = finalColor.a; // Start with material's base alpha

                if (_OcclusionActive > 0.5h) // Check if effect is on
                {
                    float dist = distance(input.positionWS.xyz, _OcclusionPoint.xyz);
                    float innerRadius = _OcclusionRadius * (1.0h - _OcclusionSoftness);
                    
                    // Smoothstep: 0 if dist <= innerRadius, 1 if dist >= _OcclusionRadius, smooth in between
                    // This mask is 0 inside the hole, 1 outside.
                    float holeVisibilityMask = smoothstep(innerRadius, _OcclusionRadius, dist);

                    finalAlpha = finalColor.a * holeVisibilityMask;
                }

                finalColor.a = saturate(finalAlpha); // Ensure alpha is 0-1
                return finalColor;
            }
            ENDHLSL
        }
    }
    // Fallback "Hidden/Universal Render Pipeline/FallbackError" // Good practice
}
// --- END OF PartialFadeWallUnlitPass.shader ---