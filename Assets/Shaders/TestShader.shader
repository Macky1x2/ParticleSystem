Shader "Custom/TestShader"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"
        }
        Pass
        {
            HLSLPROGRAM
            #pragma multi_compile_instancing
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                uint instancedId : SV_InstanceID;
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
            };

            struct Particle {
                float3 velocity;
                float3 position;
                float scale;
                float lifetime;
            };

            // C#ë§Ç©ÇÁç¿ïWèÓïÒÇ™ìnÇ≥ÇÍÇÈ
            StructuredBuffer<Particle> _Positions;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 positionOS = IN.positionOS.xyz + _Positions[IN.instancedId].position;
                //float3 positionOS = IN.positionOS.xyz;
                //float3 positionOS = _Positions[IN.instancedId].position;
                //float3 positionOS = float3(0,0,0);
                OUT.vertex = TransformWorldToHClip(positionOS);
                return OUT;
            }

            half4 frag() : SV_Target
            {
                return half4(0, 0, 0.5, 1);
            }
            ENDHLSL
        }
    }
}
