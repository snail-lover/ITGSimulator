Shader "Custom/PixelArtAnimatedGradientV3"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,0.5,0,1) // Orange-ish fire color
        _BackgroundColor ("Background Color (Transparent)", Color) = (0,0,0,0) // Color for empty space

        _PixelColumns("Pixel Columns (Horizontal Density)", Range(16, 128)) = 48
        _InitialPixelHeight("Initial Pixel Height (Vertical Density)", Range(0.01, 0.2)) = 0.05 // Normalized height

        _AnimationSpeed ("Animation Speed", Range(0.01, 2)) = 0.3
        _ParticleLifetime ("Particle Max Travel (Normalized Screen Height)", Range(0.5, 2.0)) = 1.0

        _ThinningStartHeight("Vertical Thinning Start Height (0-1)", Range(0.0, 1.0)) = 0.2
        _MaxThinningFactor("Max Vertical Thinning (0-1)", Range(0.0, 0.9)) = 0.7 // 0=no_thin, 0.9=pixels become very short

        _FadeOutStartHeight ("Fade Out Start Height (0-1)", Range(0.0, 1.0)) = 0.5
        _FadeOutEndHeight ("Fade Out End Height (0-1)", Range(0.1, 1.0)) = 0.95

        _SpawnNoiseScale ("Spawn Noise Scale", Range(1, 100)) = 15
        _SpawnNoiseThreshold("Spawn Noise Threshold", Range(0.1, 0.9)) = 0.6 // Higher = more sparse

        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
        }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex   : POSITION;
                float2 uv       : TEXCOORD0;
                float4 color    : COLOR;
            };

            struct v2f
            {
                float2 uv       : TEXCOORD0;
                float4 vertex   : SV_POSITION;
                float4 color    : COLOR;
                float4 worldPosition : TEXCOORD1;
            };

            fixed4 _BaseColor;
            fixed4 _BackgroundColor;
            float _PixelColumns;
            float _InitialPixelHeight;
            float _AnimationSpeed;
            float _ParticleLifetime;
            float _ThinningStartHeight;
            float _MaxThinningFactor;
            float _FadeOutStartHeight;
            float _FadeOutEndHeight;
            float _SpawnNoiseScale;
            float _SpawnNoiseThreshold;

            // Simple 2D pseudo-random noise function
            float2 random2(float2 p)
            {
                return frac(sin(float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)))) * 43758.5453);
            }
            float random(float2 p) { return random2(p).x; }


            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                o.worldPosition = v.vertex;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // --- Base Horizontal Pixelation ---
                float col = floor(i.uv.x * _PixelColumns); // Current column index
                float colUV_x = col / _PixelColumns;       // UV x-coordinate for this column's start

                // --- Particle Stream Animation & Looping ---
                // Each column has its own set of particle "streams"
                // `streamTimeOffset` gives each column a different start time for its animation cycle.
                float streamTimeOffset = random(float2(col, col * 0.731)) * 10.0; // Randomize start for each column
                
                // `particleBaseY` is the "ideal" starting Y position of a particle in the current animation cycle for this column.
                // It moves from 0 up to `_ParticleLifetime`. When it exceeds `_ParticleLifetime`, it wraps around due to frac().
                float particleBaseY = frac((_Time.y + streamTimeOffset) * _AnimationSpeed) * _ParticleLifetime;

                // --- Determine if current screen pixel (i.uv.y) is part of an active particle ---
                // Calculate the "age" or "progress" of the particle that *would* cover i.uv.y
                // if it started at particleBaseY and had _InitialPixelHeight.
                // This is tricky. We are essentially checking if i.uv.y falls within the vertical span
                // of any active particle in this column.
                
                // Let's determine the "bottom" of the particle this fragment might belong to.
                // This means finding the `particleBaseY` such that `i.uv.y` is within its height.
                // This needs to be "snapped" to the initial pixel height grid.
                float y_relative_to_flow = frac(i.uv.y / _ParticleLifetime - (_Time.y + streamTimeOffset) * _AnimationSpeed);
                float particle_instance_base_y = (y_relative_to_flow * _ParticleLifetime);
                
                // Effective current Y position on screen for this particle instance
                float currentScreenY = particle_instance_base_y;


                // --- Particle Spawning (Noise at the bottom of a stream's cycle) ---
                // Use noise to decide if a new particle stream should be "active" for this column
                // when its `particleBaseY` is near the bottom (i.e., it's "spawning").
                // The noise should be fairly consistent for the duration of one particle's lifetime.
                // We sample noise based on the column and a "generation ID" that changes less frequently.
                float generationId = floor(((_Time.y + streamTimeOffset) * _AnimationSpeed) / _ParticleLifetime);
                float spawnNoise = random(float2(col, generationId) * _SpawnNoiseScale);

                if (spawnNoise < _SpawnNoiseThreshold)
                {
                    return _BackgroundColor * i.color; // Particle stream not active for this cycle in this column
                }

                // If currentScreenY is effectively beyond the screen or lifetime, discard.
                if (currentScreenY > _ParticleLifetime || currentScreenY > 1.0) {
                     return _BackgroundColor * i.color;
                }


                // --- Vertical Thinning (Pixel Height Reduction) ---
                float currentPixelHeight = _InitialPixelHeight;
                if (currentScreenY > _ThinningStartHeight)
                {
                    float thinningProgress = saturate((currentScreenY - _ThinningStartHeight) / (1.0 - _ThinningStartHeight));
                    currentPixelHeight = lerp(_InitialPixelHeight, _InitialPixelHeight * (1.0 - _MaxThinningFactor), thinningProgress);
                    currentPixelHeight = max(currentPixelHeight, 0.001); // Prevent zero height
                }

                // Check if i.uv.y is within the *current* (possibly thinned) height of the particle
                // The "bottom" of the conceptual pixel block is currentScreenY (its conceptual start)
                // The "top" is currentScreenY + currentPixelHeight
                // However, our i.uv.y is fixed. We need to check if the *current point* i.uv.y
                // falls into a "lit" part of a conceptual pixel block.
                // This is done by discretizing i.uv.y based on the current particle's base and its *current* height.
                float y_in_particle_normalized = (i.uv.y - currentScreenY) / currentPixelHeight;

                if (y_in_particle_normalized < 0.0 || y_in_particle_normalized > 1.0)
                {
                     // This fragment is not part of the "lit" portion of the thinned particle
                     return _BackgroundColor * i.color;
                }
                // Snap to "pixel bands" based on the original animated flow, NOT the thinned height directly for color sampling.
                // This ensures the "pixel block" look remains.
                // The particle's base Y for visual pixelation.
                float pixelSnapY = floor(currentScreenY / _InitialPixelHeight) * _InitialPixelHeight;
                if (i.uv.y < pixelSnapY || i.uv.y > pixelSnapY + currentPixelHeight) {
                     return _BackgroundColor * i.color; // Not within the visible part of this pixel
                }


                // --- Vertical Fade-Out (Alpha) ---
                float alpha = 1.0;
                if (currentScreenY > _FadeOutStartHeight)
                {
                    float fadeRange = _FadeOutEndHeight - _FadeOutStartHeight;
                    if (fadeRange <= 0.001) fadeRange = 0.001; // Avoid div by zero
                    
                    float fadeProgress = saturate((currentScreenY - _FadeOutStartHeight) / fadeRange);
                    alpha = 1.0 - fadeProgress;
                }
                alpha = saturate(alpha);


                // --- Final Color ---
                fixed4 outputColor = _BaseColor;
                outputColor.a *= alpha;

                // Apply UI Image component's color (for overall tint/alpha)
                outputColor *= i.color;

                return outputColor;
            }
            ENDCG
        }
    }
    FallBack "UI/Default"
}