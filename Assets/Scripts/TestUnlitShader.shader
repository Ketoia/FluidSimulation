Shader "Unlit/TestUnlitShader"
{
    Properties
    {
        [HideInInspector]_MainTex ("Texture", 2D) = "white" {}
        _Color1 ("First Color", Color) = (.25, .5, .5, 1)
        _Color2 ("Second Color", Color) = (.25, .5, .5, 1)
        _Color3 ("Third Color", Color) = (.25, .5, .5, 1)

        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }

        // The SubShader block containing the Shader code. 
    SubShader
    {
        // SubShader Tags define when and under which conditions a SubShader block or
        // a pass is executed.
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" }

        // Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            // The HLSL code block. Unity SRP uses the HLSL language.
            HLSLPROGRAM
            // This line defines the name of the vertex shader. 
            #pragma vertex vert
            // This line defines the name of the fragment shader. 
            #pragma fragment frag

            // The Core.hlsl file contains definitions of frequently used HLSL
            // macros and functions, and also contains #include references to other
            // HLSL files (for example, Common.hlsl, SpaceTransforms.hlsl, etc.).
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"                       

            CBUFFER_START(MyRarelyUpdatedVariables)
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            CBUFFER_END

            // The structure definition defines which variables it contains.
            // This example uses the Attributes structure as an input structure in
            // the vertex shader.
            struct Attributes
            {
                // The positionOS variable contains the vertex positions in object
                // space.
                float4 positionOS   : POSITION;    
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                // The positions in this struct must have the SV_POSITION semantic.
                float4 positionHCS  : SV_POSITION;
                float2 uv : TEXCOORD0;
            };            

            // The vertex shader definition with properties defined in the Varyings 
            // structure. The type of the vert function must match the type (struct)
            // that it returns.
            Varyings vert(Attributes IN)
            {
                // Declaring the output object (OUT) with the Varyings struct.
                Varyings OUT;
                // The TransformObjectToHClip function transforms vertex positions
                // from object space to homogenous space
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                // Returning the output.
                return OUT;
            }

            // The fragment shader definition.            
            half4 frag(Varyings IN) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, TRANSFORM_TEX(IN.uv, _MainTex));
            }
            ENDHLSL
        }
    }

    /*
    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque"
            // "LightMode" = "Universal2D"
        }

        LOD 100

        // Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        // // Cull Off
        // ZTest LEqual
        // ZWrite Off

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert 
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"  
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"  


            struct appdata
            {
                float4 position : POSITION;
                // float2 uv : TEXCOORD0;
                // float4 normal : NORMAL;
                // float4 texcoord1 : TEXCOORD1;
            };
             
            struct v2f
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                // float3 normal : TEXCOORD2;
                // float3 position : TEXCOORD1;
                // float3 viewDir : TEXCOORD3;
                // DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 3);
            };

            // This macro declares _BaseMap as a Texture2D object.
            TEXTURE2D(_MainTex);
            // This macro declares the sampler for the _BaseMap texture.
            SAMPLER(sampler_MainTex);

            float4 _MainTex_ST;
            float4 _Color1;
            float4 _Color2;
            float4 _Color3;
             
            half _Smoothness;
            half _Metallic;

            v2f vert (appdata v)
            {
                v2f o;
                float3 pos = TransformObjectToWorld(v.position.xyz);
                // o.normal = TransformObjectToWorldNormal(v.normal.xyz);
                // o.viewDir = normalize(_WorldSpaceCameraPos - o.positionWS);
                o.position = TransformWorldToHClip(v.position.xyz);
                // o.uv = v.uv ;

                //OUTPUT_LIGHTMAP_UV(v.texcoord1, unity_LightmapST, o.lightmapUV );
                // OUTPUT_SH(o.normalWS.xyz, o.vertexSH );

                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                return float4(1, 1, 1, 1);

                // // sample the texture
                // float4 normal = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, TRANSFORM_TEX(i.uv, _MainTex));
                // normal.xyz = normal.xyz * 2 - 1;
                // i.normal = normal;
                
                // float dotProduct = dot(normal.xyz, float3(0, 1, 0));
                // dotProduct = (-dotProduct + 1) * 0.5; //from 0 to 1
                
                // float4 col;

                // if (dotProduct < 0.25)
                // {
                //     col = _Color1;
                // }
                // else if (dotProduct < 0.5)
                // {
                //     float time = (dotProduct - 0.25) * 4;
                //     col = _Color2 * time + _Color1 * (-time + 1);
                // }
                // else if (dotProduct < 0.75)
                // {
                //     float time = (dotProduct - 0.5) * 4;
                //     col = _Color3 * time + _Color2 * (-time + 1);
                // }
                // else if (dotProduct < 1)
                // {
                //     col = _Color3;
                // }

                // Light LightDir = GetMainLight();

                // // float3 lightValue = clamp(dot(normal.xyz, LightDir), 0, 1);

                // return float4(col.xyz * lightValue , normal.a);
            }
            ENDHLSL
        }
    }
    */
}
