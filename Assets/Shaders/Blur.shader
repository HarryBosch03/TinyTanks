Shader "Hidden/Blur"
{
    Properties {}
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"
        }
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            half4 frag(Varyings i) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord);
                return col;
            }
            ENDHLSL
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            half4 sampleForBlur(float2 uv, int x, int y)
            {
                uv *= _BlitTexture_TexelSize.zw;
                uv = floor(uv + float2(x, y));
                uv *= _BlitTexture_TexelSize.xy;

                return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 colors[] =
                {
                    sampleForBlur(i.texcoord, -1, -1),
                    sampleForBlur(i.texcoord, 0, -1),
                    sampleForBlur(i.texcoord, 1, -1),
                    sampleForBlur(i.texcoord, -1, 0),
                    sampleForBlur(i.texcoord, 0, 0),
                    sampleForBlur(i.texcoord, 1, 0),
                    sampleForBlur(i.texcoord, -1, 1),
                    sampleForBlur(i.texcoord, 0, 1),
                    sampleForBlur(i.texcoord, 1, 1),
                };

                half4 final = 0;
                final += colors[0];
                final += colors[1];
                final += colors[2];
                final += colors[3];
                final += colors[4];
                final += colors[5];
                final += colors[6];
                final += colors[7];
                final += colors[8];

                return final / 9;
            }
            ENDHLSL
        }
    }
}