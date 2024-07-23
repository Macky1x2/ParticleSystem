Shader "Custom/Emission"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0)
        _Center("Center", Vector) = (0,0,0,0)
    }
        SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        //Cull Front
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

            float3 _Center;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = mul(
                    UNITY_MATRIX_P,
                    mul(
                        UNITY_MATRIX_V,
                        float4(_Center + _Positions[v.instancedId].position, 1)
                    ) + float4(-v.vertex.x, v.vertex.y, 0, 0) * _Positions[v.instancedId].scale
                ); // 常にカメラを向く
                
                /*float3 positionOS = v.vertex.xyz + _Positions[v.instancedId].position;
                o.vertex = UnityObjectToClipPos(positionOS);*/
                
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.instancedId = v.instancedId;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                clip(_Positions[i.instancedId].lifetime - 0.0001);
                clip(_Positions[i.instancedId].scale);
            // sample the texture
            fixed4 col = tex2D(_MainTex, i.uv) * _EmissionColor;
            //fixed4 col = tex2D(_MainTex, i.uv) * _Positions[i.instancedId].color;
            // apply fog
            UNITY_APPLY_FOG(i.fogCoord, col);
            return col;
        }
        ENDCG
    }
    }
}
