Shader "Universal Render Pipeline/2D/Sprite-Unlit-Instanced"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _Scale("Scale", Float) = 1
        
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

            //allow instancing
            #pragma multi_compile_instancing

            #pragma vertex UnlitVertex
            #pragma geometry UnlitGeometry
            #pragma fragment UnlitFragment

            struct appdata {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2g
            {
                float4 positionWS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                float4 digits       : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct g2f
            {
                float4  position    : SV_POSITION;
                half4   color       : COLOR;
                float2  uv          : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID 
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            half4 _MainTex_ST;
            half4 _Color;
            half _Scale;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _Alphas)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Digits)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2g UnlitVertex(appdata v)
            {
                v2g o = (v2g)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                float4 digit = UNITY_ACCESS_INSTANCED_PROP(Props, _Digits);
                o.positionWS.xyz = ComputeWorldSpacePosition(v.vertex, UNITY_MATRIX_I_V);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.digits = digit;
                o.color = v.color;
                return o;
            }

            [maxvertexcount(16)]
            void UnlitGeometry(point v2g v[1], inout TriangleStream<g2f> triStream) {
                g2f o = (g2f)0;
                
                UNITY_SETUP_INSTANCE_ID(v[0]);
                UNITY_TRANSFER_INSTANCE_ID(v[0], o);

                const float4 pos = v[0].positionWS;
                const float4 dig = v[0].digits;
                const float2 uv  = v[0].uv;
                const float digs[] = { dig.w, dig.z, dig.y, dig.x };
                const float offset = 0.1;
                const float size = 1;
                
                bool skipCheck = true;
                int skipCount = 0;

                for (int i = 0; i < 4; i++) {
                    
                    const float dig = digs[i];

                    if (skipCheck) {
                        if (dig == 0) {
                            skipCount++;
                            continue;
                        }
                        skipCheck = false;
                    }
                    
                    int skip = i - skipCount;
                    float4 p = float4(skip + 0, 0, 0, 1);
                    o.position = TransformObjectToHClip(p * _Scale);
                    o.uv = float2(dig, 0);
                    triStream.Append(o);

                    p = float4(skip + 0, size, 0, 1);
                    o.position =TransformObjectToHClip(p * _Scale);
                    o.uv = float2(dig, 1);
                    triStream.Append(o);

                    p = float4(skip + size, 0, 0, 1);
                    o.position = TransformObjectToHClip(p * _Scale);
                    o.uv = float2(dig + offset, 0);
                    triStream.Append(o);

                    p = float4(skip + size, size, 0, 1);
                    o.position = TransformObjectToHClip(p * _Scale);
                    o.uv = float2(dig + offset, 1);
                    triStream.Append(o);
                }
            }

            half4 UnlitFragment(g2f i) : SV_Target
            {
                //setup instance id
                UNITY_SETUP_INSTANCE_ID(i);

                float4 mainTex = _Color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                mainTex.a *= UNITY_ACCESS_INSTANCED_PROP(Props, _Alphas);
                return mainTex;
            }
            ENDHLSL
        }
    }
}