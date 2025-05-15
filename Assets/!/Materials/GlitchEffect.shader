Shader "Custom/GlitchNoiseEffect"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Intensity ("Glitch Intensity", Range(0,1)) = 0.1
        _Speed ("Noise Speed", Range(0,10)) = 1.0
        _Scale ("Noise Scale", Range(1,100)) = 10
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Intensity;
            float _Speed;
            float _Scale;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // Simple pseudo-random generator
            float rand(float2 co)
            {
                return frac(sin(dot(co, float2(12.9898,78.233))) * 43758.5453);
            }

            // Value noise (interpolated random)
            float noise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);

                float a = rand(i);
                float b = rand(i + float2(1.0, 0.0));
                float c = rand(i + float2(0.0, 1.0));
                float d = rand(i + float2(1.0, 1.0));

                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float t = _Time.y; // Unity built-in time
                float n = noise(uv * _Scale + t * _Speed);

                // Apply vertical jitter
                uv.y += (n - 0.5) * _Intensity;

                // Chromatic aberration offsets
                float2 rUV = uv + float2((rand(uv + t) - 0.5) * _Intensity, 0);
                float2 gUV = uv;
                float2 bUV = uv + float2((rand(uv - t) - 0.5) * _Intensity, 0);

                float r = tex2D(_MainTex, rUV).r;
                float g = tex2D(_MainTex, gUV).g;
                float b = tex2D(_MainTex, bUV).b;

                return float4(r, g, b, 1.0);
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}