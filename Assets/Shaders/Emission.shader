Shader "Custom/Emission"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0)
    }
        SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma multi_compile_instancing
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                uint instancedId : SV_InstanceID;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                uint instancedId : SV_InstanceID;
            };

            struct Particle {
                float3 velocity;
                float3 position;
                float scale;
                float lifetime;
                float4 color;
            };

            StructuredBuffer<Particle> _Positions;

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _EmissionColor;             //’Ç‰Á

            v2f vert(appdata v)
            {
                v2f o;
                //float3 positionOS = v.vertex.xyz + _Positions[v.instancedId].position;
                o.vertex = mul(
                    UNITY_MATRIX_P,
                    mul(
                        UNITY_MATRIX_V,
                        mul(
                            unity_ObjectToWorld, float4(0, 0, 0, 1)
                        )  + float4(_Positions[v.instancedId].position, 0)
                    ) + float4(v.vertex.xy, 0, 0)
                ); // 常にカメラを向く
                /*mul(
                    UNITY_MATRIX_VP, mul(
                        unity_ObjectToWorld, float4(positionOS, 1.0)
                    )
                );*/
                //o.vertex = UnityObjectToClipPos(positionOS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.instancedId = v.instancedId;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                clip(_Positions[i.instancedId].lifetime - 0.0001);
            // sample the texture
            //fixed4 col = tex2D(_MainTex, i.uv) * _EmissionColor;
            fixed4 col = tex2D(_MainTex, i.uv) * _Positions[i.instancedId].color;
            // apply fog
            UNITY_APPLY_FOG(i.fogCoord, col);
            return col;
        }
        ENDCG
    }
    }
}
