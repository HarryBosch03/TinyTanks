Shader "Screen/Scope Overlay"
{
    Properties
    {
        _ShroudSize ("Shroud Size", float) = 1
        _DistortionRadius("Distortion Radius", float) = 1
        _Balance("Balance", Range(-1, 1)) = 0
        _Abberation("Abberation", float) = 0
        _Paralax("Paralax", float) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        Pass
        {
            Cull Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 positionCS : POSITION_CS;
            };

            static const float2 basis[] =
            {
                float2(0, 0),
                float2(1, 0),
                float2(0, 1),
            };

            Varyings vert(uint i : SV_VertexID)
            {
                Varyings o;
                o.vertex = float4(basis[i] * 4 - 1, 0, 1);
                o.positionCS = o.vertex;

                o.uv = basis[i] * 2;
                o.uv.y = 1 - o.uv.y;

                return o;
            }

            TEXTURE2D(_BlitTexture);

            float2 _ScopeOffset;
            float _ShroudSize;
            float _DistortionRadius;
            float _Balance;
            float _Abberation;
            float _Paralax;

            half SphereFalloff(float x, float radius = 1)
            {
                if (x > radius || x < -radius) return radius;
                float n = radius - sqrt(radius * radius - x * x);
                float d = 1;
                return n / d;
            }

            half4 CalcShroud(Varyings input, float abberation)
            {
                float2 aspectScaling = float2(1, _ScreenParams.y / _ScreenParams.x);

                float2 scopeOffset = _ScopeOffset / _ScreenParams.xy;

                float2 uv = input.uv;
                uv = 2 * uv - 1;
                uv -= scopeOffset;
                uv *= aspectScaling;

                half len = length(uv);

                half4 shroud = 0;
                shroud.z = _ShroudSize - SphereFalloff(len, _ShroudSize);

                uv += scopeOffset * _Paralax;
                len = length(uv);

                shroud.xy = normalize(uv) * (len - SphereFalloff(len, _DistortionRadius + abberation));
                shroud.xy /= aspectScaling;
                shroud.xy += scopeOffset;
                shroud.xy = shroud.xy * 0.5 + 0.5;

                return shroud;
            }

            half4 GetColor(Varyings input, float abberation)
            {
                half4 shroud = CalcShroud(input, abberation);
                half4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, shroud.xy);

                col = lerp(0, col, saturate(shroud.z)) * pow(2, _Balance);
                return col;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 color;

                half4 samples[] =
                {
                    GetColor(input, 0),
                    GetColor(input, 0 + _Abberation),
                    GetColor(input, 0 + _Abberation * 2),
                };

                color.r = samples[0].r;
                color.ga = samples[1].ga;
                color.b = samples[2].b;
                return color;
            }
            ENDHLSL
        }
    }
}