// Pixel-art flame aura around a sprite's silhouette.
// Draw it on a copy of the character's sprite (see SpriteFlameAura.cs), behind the character: it takes the sprite's
// alpha, smears it upward through scrolling noise and keeps only what lies OUTSIDE the body, so small flame tongues
// lick off the outline. Additive, unlit. Vertex colour alpha scales the whole effect (driven by the component).
// Requires sprites imported with Mesh Type = Full Rect so there is room around the silhouette to draw into.
Shader "DiceDrive/Sprite Flame Aura"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        [HDR] _ColorCore ("Core Color", Color) = (1.6, 1.1, 0.45, 1)
        [HDR] _ColorEdge ("Edge Color", Color) = (1.2, 0.25, 0.05, 1)
        _Height ("Flame Height (texels)", Range(1, 24)) = 9
        _Spread ("Sideways Spread (texels)", Range(0, 6)) = 2
        _NoiseScale ("Noise Scale", Range(0.02, 0.5)) = 0.18
        _Speed ("Rise Speed", Range(0, 10)) = 3.5
        _Threshold ("Breakup", Range(0, 1)) = 0.45
        _Intensity ("Intensity", Range(0, 3)) = 0.8
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha One

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_TexelSize;
                half4 _ColorCore;
                half4 _ColorEdge;
                float _Height;
                float _Spread;
                float _NoiseScale;
                float _Speed;
                float _Threshold;
                float _Intensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            half Alpha(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 texel = _MainTex_TexelSize.xy;
                float2 texSize = _MainTex_TexelSize.zw;

                // Snap to the sprite's pixel grid so the flames are pixel art too.
                float2 px = floor(input.uv * texSize);
                float2 uv = (px + 0.5) * texel;
                float t = _Time.y * _Speed;

                // Noise in texel space, scrolling upward.
                float2 np = px * _NoiseScale;
                float n = ValueNoise(np + float2(0, -t)) * 0.65 + ValueNoise(np * 2.1 + float2(3.1, -t * 1.7)) * 0.35;

                // Sideways wobble that varies per row: tongues sway.
                float sway = (ValueNoise(float2(px.y * _NoiseScale * 0.7, t * 0.6)) - 0.5) * 2.0 * _Spread;

                // Look below this pixel: anything solid underneath "burns" upward into here, fading with distance.
                half flame = 0;
                [unroll]
                for (int i = 1; i <= 6; i++)
                {
                    float k = i / 6.0;
                    float2 offset = float2(sway * k, -_Height * k) * texel;
                    flame = max(flame, Alpha(uv + offset) * (1.0 - k * 0.85));
                }
                // Thin rim on the sides as well, so the outline glows even where nothing is below.
                half side = max(Alpha(uv + float2(texel.x, 0)), Alpha(uv - float2(texel.x, 0)));
                flame = max(flame, side * 0.55);

                // Only outside the body; the character itself stays clean.
                flame *= 1.0 - Alpha(uv);

                // Break into tongues with the noise, hard-edged for a pixel look.
                half f = saturate((flame * (0.55 + n) - _Threshold) * 3.0);
                f = floor(f * 4.0) / 4.0; // 4 brightness steps

                half3 color = lerp(_ColorEdge.rgb, _ColorCore.rgb, f);
                half a = f * _Intensity * input.color.a;
                return half4(color * input.color.rgb, a);
            }
            ENDHLSL
        }
    }
}
