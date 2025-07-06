Shader "Custom/GhibliCloudsShader_Puffy"
{
    Properties
    {
        [Header(Sky and Colors)]
        _SkyColorTop ("Sky Color Top", Color) = (0.2, 0.4, 0.8, 1.0)
        _SkyColorBottom ("Sky Color Bottom", Color) = (0.5, 0.7, 1.0, 1.0)
        _HighlightColor ("Cloud Highlight Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _ShadowColor ("Cloud Shadow Color", Color) = (0.65, 0.7, 0.8, 1.0)
        _MainTex ("Dummy Texture (Not Used)", 2D) = "white" {} 

        [Header(Cloud Shape and Structure)]
        _CloudScale ("Cloud Scale", Float) = 0.8
        _CloudCoverage ("Cloud Coverage", Range(0, 1)) = 0.4
        _CloudSharpness ("Edge Sharpness", Range(0.01, 1)) = 0.05
        _HighlightIntensity ("Highlight Intensity", Range(0, 0.5)) = 0.15
        _HorizonBias("Horizon Bias", Range(0, 1)) = 0.7

        [Header(Movement)]
        _ScrollSpeed1 ("Scroll Speed Layer 1", Float) = 0.01
        _ScrollSpeed2 ("Scroll Speed Layer 2", Float) = 0.015
        
        [Header(Advanced Noise)]
        _Octaves("Noise Detail (Octaves)", Range(4, 8)) = 6
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            // --- Properties ---
            float4 _SkyColorTop, _SkyColorBottom, _HighlightColor, _ShadowColor;
            float _CloudScale, _CloudCoverage, _CloudSharpness, _HighlightIntensity, _HorizonBias;
            float _ScrollSpeed1, _ScrollSpeed2;
            int _Octaves;

            // --- Structs ---
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; UNITY_FOG_COORDS(1) float4 vertex : SV_POSITION; };

            // --- Noise Functions ---
            float2 random(float2 st) {
                st = float2(dot(st, float2(127.1, 311.7)), dot(st, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(st) * 43758.5453123);
            }
            float noise(float2 st) {
                float2 i = floor(st); float2 f = frac(st);
                float a = dot(random(i), f); float b = dot(random(i + float2(1.0, 0.0)), f - float2(1.0, 0.0));
                float c = dot(random(i + float2(0.0, 1.0)), f - float2(0.0, 1.0)); float d = dot(random(i + float2(1.0, 1.0)), f - float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y) + 0.5;
            }
            
            // --- UPDATED FBM: Now uses _Octaves property for more control ---
            float fbm(float2 st) {
                float value = 0.0; float amplitude = 0.5;
                for (int i = 0; i < _Octaves; i++) {
                    value += amplitude * noise(st);
                    st *= 2.0; amplitude *= 0.5;
                }
                return value;
            }

            // --- Vertex Shader (Unchanged) ---
            v2f vert (appdata v) {
                v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o;
            }

            // --- Fragment Shader (Completely Reworked Logic) ---
            fixed4 frag (v2f i) : SV_Target
            {
                // 1. Prepare UVs with scrolling and scaling
                float2 uv_main = (i.uv - 0.5) * _CloudScale + float2(_Time.y * _ScrollSpeed1, 0.3);
                float2 uv_distort = i.uv * _CloudScale * 2.0 + float2(_Time.y * _ScrollSpeed2, 0.1);

                // 2. Generate layered noise for complex shapes
                // The distortion fbm "warps" the coordinates of the main fbm, creating more billowy shapes.
                float distortion = fbm(uv_distort) * 0.4;
                float noise_value = fbm(uv_main + distortion);

                // 3. NEW: Apply a vertical bias to create a horizon
                // This makes clouds appear denser at the bottom of the screen.
                noise_value -= i.uv.y * _HorizonBias;

                // 4. Multi-Step Coloring for a "Painted" Look
                // Step 4a: Define the sky gradient
                float3 sky_color = lerp(_SkyColorBottom.rgb, _SkyColorTop.rgb, i.uv.y);

                // Step 4b: Create the main cloud silhouette mask.
                // This defines where the cloud exists at all.
                float cloud_mask = smoothstep(_CloudCoverage - _CloudSharpness, _CloudCoverage + _CloudSharpness, noise_value);

                // Step 4c: Fill the silhouette with the shadow color. This is the base of the cloud.
                float3 color = lerp(sky_color, _ShadowColor.rgb, cloud_mask);
                
                // Step 4d: Create a second, tighter mask for the bright highlights.
                // This mask only activates where the noise value is high, simulating where light hits the cloud directly.
                float highlight_threshold = _CloudCoverage + _HighlightIntensity;
                float highlight_mask = smoothstep(highlight_threshold, highlight_threshold + _CloudSharpness, noise_value);

                // Step 4e: "Paint" the highlights on top of the shadow color.
                // We multiply by cloud_mask to ensure highlights only appear on the cloud.
                color = lerp(color, _HighlightColor.rgb, highlight_mask * cloud_mask);

                // Return the final, layered color
                return float4(color, 1.0);
            }
            ENDHLSL
        }
    }
}