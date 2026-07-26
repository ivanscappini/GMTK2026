Shader "Custom/CRTGlitchScreen"
{
    Properties
    {
        _MainTex ("Render Texture", 2D) = "white" {}

        _FisheyeStrength ("Fisheye Strength", Range(0, 1)) = 0.3
        _VignetteStrength ("Vignette Strength", Range(0, 1)) = 0.4
        _VignetteSoftness ("Vignette Softness", Range(0.01, 1)) = 0.5

        _Contrast ("Contrast", Range(0, 1)) = 0.8

        _ScanlineCount ("Scanline Count", Float) = 240
        _ScanlineStrength ("Scanline Strength", Range(0,1)) = 0.15

        _RollSpeed ("Roll Speed", Float) = 0.15
        _RollWidth ("Roll Band Width", Range(0.01, 0.2)) = 0.04
        _TearStrength ("Roll Tear Strength", Range(0, 0.2)) = 0.03
        _TearJitterRate ("Roll Tear Jitter Rate", Float) = 8

        _SnowCycleLength ("Snow Line Cycle Length (sec)", Float) = 3.0
        _SnowWidthMin ("Snow Line Width Min", Range(0.01, 0.3)) = 0.03
        _SnowWidthMax ("Snow Line Width Max", Range(0.01, 0.3)) = 0.1
        _SnowChance ("Snow Line Chance", Range(0,1)) = 0.3
        _SnowFollowChance ("Snow Line Quick-Follow Chance", Range(0,1)) = 0.25

        _WhiteFlashChance ("White Flash Chance", Range(0, 1)) = 0.01

        _BarHeight ("Bar Height", Range(0, 0.3)) = 0.08
        _BarNoiseAmount ("Bar Noise Amount", Range(0, 1)) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            float _FisheyeStrength;
            float _VignetteStrength, _VignetteSoftness;
            float _Contrast;
            float _ScanlineCount, _ScanlineStrength;
            float _RollSpeed, _RollWidth, _TearStrength, _TearJitterRate;
            float _SnowCycleLength, _SnowWidthMin, _SnowWidthMax, _SnowChance, _SnowFollowChance;
            float _WhiteFlashChance;
            float _BarHeight, _BarNoiseAmount;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float rand(float2 co)
            {
                return frac(sin(dot(co, float2(12.9898,78.233))) * 43758.5453);
            }

            // barrel distortion: pushes pixels outward from center, more so near edges
            float2 fisheyeUV(float2 uv, float strength)
            {
                float2 centered = uv * 2.0 - 1.0;
                float dist = length(centered);
                float distortion = 1.0 + strength * dist * dist;
                float2 distorted = centered * distortion;
                return distorted * 0.5 + 0.5;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // --- FISHEYE (must run first, everything else samples this uv) ---
                float2 uv = fisheyeUV(i.uv, _FisheyeStrength);

                // outside valid range after distortion -> render as dark bezel
                if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
                    return fixed4(0, 0, 0, 1);

                // --- ROLLING TEAR BAND (mimics vertical hold drift) ---
                float rollPos = frac(_Time.y * _RollSpeed);
                float distToRoll = abs(uv.y - rollPos);
                if (distToRoll < _RollWidth)
                {
                    float bandFalloff = 1 - (distToRoll / _RollWidth);
                    float jitter = (rand(float2(floor(_Time.y * _TearJitterRate), uv.y)) - 0.5);
                    uv.x += jitter * _TearStrength * bandFalloff;
                }

                fixed4 col = tex2D(_MainTex, uv);

                // --- CONTRAST REDUCTION ---
                col.rgb = lerp(fixed3(0.5, 0.5, 0.5), col.rgb, _Contrast);

                // --- SCANLINES ---
                float scanline = sin(uv.y * _ScanlineCount * 3.14159 * 2) * 0.5 + 0.5;
                col.rgb *= lerp(1.0, scanline, _ScanlineStrength);

                // --- RISING SNOW LINE (smooth continuous glide, varied width, optional quick follow-up) ---
                {
                    float cycleIndex = floor(_Time.y / _SnowCycleLength);
                    float progress = frac(_Time.y / _SnowCycleLength); // 0..1, continuous within a cycle

                    // primary line: glides from just below the screen to just above it over one full cycle
                    float seedA = cycleIndex;
                    float appearsA = step(rand(float2(seedA, 0.37)), _SnowChance);
                    float widthA = lerp(_SnowWidthMin, _SnowWidthMax, rand(float2(seedA, 0.81)));
                    float posA = lerp(1 + widthA, -widthA, progress);
                    float distA = abs(uv.y - posA);
                    float maskA = saturate(1 - distA / widthA) * appearsA;

                    // secondary line: smaller, sometimes trails shortly behind the primary in the same cycle
                    float followRoll = rand(float2(seedA, 5.2));
                    float followsB = appearsA * step(followRoll, _SnowFollowChance);
                    float delay = lerp(0.1, 0.25, rand(float2(seedA, 9.4))); // fraction of cycle before it starts
                    float progressB = saturate((progress - delay) / (1 - delay));
                    float activeB = step(delay, progress) * followsB;
                    float widthB = widthA * lerp(0.35, 0.6, rand(float2(seedA, 3.14)));
                    float posB = lerp(1 + widthB, -widthB, progressB);
                    float distB = abs(uv.y - posB);
                    float maskB = saturate(1 - distB / widthB) * activeB;

                    float noiseA = rand(uv * (_Time.y + 1.0));
                    float noiseB = rand(uv * (_Time.y + 2.0) + 5.0);
                    float3 snowColor = (maskA >= maskB) ? fixed3(noiseA, noiseA, noiseA) : fixed3(noiseB, noiseB, noiseB);
                    float snowMask = max(maskA, maskB);

                    col.rgb = lerp(col.rgb, snowColor, snowMask);
                }

                // --- OCCASIONAL WHITE FLASH ---
                float flickerSeed = floor(_Time.y * 12);
                float flickerRoll = rand(float2(flickerSeed, 0.5));
                if (flickerRoll < _WhiteFlashChance)
                {
                    col.rgb = fixed3(1, 1, 1);
                }

                // --- VIGNETTE (darkened, curved-glass edges) ---
                float2 vigCentered = i.uv * 2.0 - 1.0; // use original uv, not fisheye-distorted
                float vigDist = length(vigCentered);
                float vignette = 1.0 - smoothstep(_VignetteSoftness, 1.0, vigDist) * _VignetteStrength;
                col.rgb *= vignette;

                // --- STATIC NOISE BARS (top & bottom, drawn last so nothing above overrides them) ---
                if (i.uv.y < _BarHeight || i.uv.y > 1 - _BarHeight)
                {
                    float barNoise = rand(i.uv * frac(_Time.y * 4.0));
                    col.rgb = fixed3(barNoise, barNoise, barNoise) * _BarNoiseAmount;
                }

                return col;
            }
            ENDCG
        }
    }
}
