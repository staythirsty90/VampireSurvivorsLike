Shader "Universal Render Pipeline/2D/Sprite-Unlit-Additive"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Bias ("Mip Bias", Range(-4, 4)) = -0.75
        [KeywordEnum(Off, 2x2 RGSS, 8x Halton, 16x16 OGSS)] _SuperSampling ("Super Sampling Technique", Float) = 1
        _AAScale ("AA Pixel Width", Range(0.5, 10.0)) = 1.25

        // Legacy properties. They're here so that materials using this shader can gracefully fallback to the legacy sprite shader.
        [HideInInspector] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // #if defined(DEBUG_DISPLAY)
            // #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/InputData2D.hlsl"
            // #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
            // #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"
            // #endif

            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment

            #pragma shader_feature _ _SUPERSAMPLING_2X2_RGSS _SUPERSAMPLING_8X_HALTON _SUPERSAMPLING_16X16_OGSS

            //#pragma multi_compile _ DEBUG_DISPLAY

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4  positionCS  : SV_POSITION;
                half4   color       : COLOR;
                float2  uv          : TEXCOORD0;
                // #if defined(DEBUG_DISPLAY)
                // float3  positionWS  : TEXCOORD2;
                // #endif
                UNITY_VERTEX_OUTPUT_STEREO
            };

            //TEXTURE2D(_MainTex);
            //SAMPLER(sampler_MainTex);
            sampler2D _MainTex;
            half4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float _Bias;
            float _AAScale;

            Varyings UnlitVertex(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionCS = TransformObjectToHClip(v.positionOS);
                // #if defined(DEBUG_DISPLAY)
                // o.positionWS = TransformObjectToWorld(v.positionOS);
                // #endif
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            half4 tex2DSS(sampler2D tex, float2 uv, float bias, float aascale)
            {
                // get uv derivatives, optionally scaled to reduce aliasing at the cost of clarity
                // used by all 3 super sampling options
                float2 dx = ddx(uv * aascale);
                float2 dy = ddy(uv * aascale);
             
                half4 col = 0;
 
            #if defined(_SUPERSAMPLING_2X2_RGSS)
                // MSAA style "four rooks" rotated grid super sampling
                // samples the texture 4 times
 
                float2 uvOffsets = float2(0.125, 0.375);
 
                col += tex2Dbias(tex, float4(uv + uvOffsets.x * dx + uvOffsets.y * dy, 0, bias));
                col += tex2Dbias(tex, float4(uv - uvOffsets.x * dx - uvOffsets.y * dy, 0, bias));
                col += tex2Dbias(tex, float4(uv + uvOffsets.y * dx - uvOffsets.x * dy, 0, bias));
                col += tex2Dbias(tex, float4(uv - uvOffsets.y * dx + uvOffsets.x * dy, 0, bias));
 
                col *= 0.25;
            #elif defined(_SUPERSAMPLING_8X_HALTON)
                // 8 points from a 2, 3 Halton sequence
                // similar to what TAA uses, though they usually use more points
                // samples the texture 8 times
                // better quality for really fine details
 
                float2 halton[8] = {
                    float2(1,-3) / 16.0,
                    float2(-1,3) / 16.0,
                    float2(5,1) / 16.0,
                    float2(-3,-5) / 16.0,
                    float2(-5,5) / 16.0,
                    float2(-7,-1) / 16.0,
                    float2(3,7) / 16.0,
                    float2(7,-7) / 16.0
                };
 
                for (int i=0; i<8; i++)
                    col += tex2Dbias(tex, float4(uv + halton[i].x * dx + halton[i].y * dy, 0, bias));
 
                col *= 0.125;
            #elif defined(_SUPERSAMPLING_16X16_OGSS)
                // brute force ground truth 16x16 ordered grid super sampling
                // samples the texture 256 times! you should not use this!
                // does not use tex2Dbias, but instead always samples the top mip
 
                float gridDim = 16;
                float halfGridDim = gridDim / 2;
 
                for (float u=0; u<gridDim; u++)
                {
                    float uOffset = (u - halfGridDim + 0.5) / gridDim;
                    for (float v=0; v<gridDim; v++)
                    {
                        float vOffset = (v - halfGridDim + 0.5) / gridDim;
                        col += tex2Dlod(tex, float4(uv + uOffset * dx + vOffset * dy, 0, 0));
                    }
                }
 
                col /= (gridDim * gridDim);
            #else
                // no super sampling, just bias
 
                col = tex2Dbias(tex, float4(uv, 0, bias));
            #endif
                return col;
            }


            float4 texturePointSmooth(sampler2D tex, float2 uvs)
                {
                    float2 size;
                    size.x = _MainTex_TexelSize.z;
                    size.y = _MainTex_TexelSize.w;

                    float2 pixel = float2(1.0,1.0) / size;

                    uvs -= pixel * float2(0.5,0.5);
                    float2 uv_pixels = uvs * size;
                    float2 delta_pixel = frac(uv_pixels) - float2(0.5,0.5);

                    float2 ddxy = fwidth(uv_pixels);
                    float2 mip = log2(ddxy) - 0.5;

                    float2 clampedUV = uvs + (clamp(delta_pixel / ddxy, 0.0, 1.0) - delta_pixel) * pixel;
                    return tex2Dlod(tex, float4(clampedUV,0, min(mip.x, mip.y)));
                }

            half4 UnlitFragment(Varyings i) : SV_Target
            {
                //float4 mainTex = i.color + SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                //float4 mainTex = i.color + texturePointSmooth(_MainTex, i.uv);
                float4 mainTex = i.color + tex2DSS(_MainTex, i.uv, _Bias, _AAScale);
                // #if defined(DEBUG_DISPLAY)
                // SurfaceData2D surfaceData;
                // InputData2D inputData;
                // half4 debugColor = 0;

                // InitializeSurfaceData(mainTex.rgb, mainTex.a, surfaceData);
                // InitializeInputData(i.uv, inputData);
                // SETUP_DEBUG_DATA_2D(inputData, i.positionWS);

                // if(CanDebugOverrideOutputColor(surfaceData, inputData, debugColor))
                // {
                //     return debugColor;
                // }
                // #endif

                return mainTex;
            }
            ENDHLSL
        }
    }
}
