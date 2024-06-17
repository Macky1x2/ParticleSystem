Shader "Lit/NewTest"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }
    SubShader
    {
        CGINCLUDE
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
        ENDCG

        Pass
        {
            Tags { "RenderType" = "Opaque" "LightMode" = "ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fwdbase

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                uint instancedId : SV_InstanceID;
            };

            struct v2f {
                float4 pos      : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv       : TEXCOORD0;
                SHADOW_COORDS(1)
            };

            struct Particle {
                float3 velocity;
                float3 position;
                float scale;
                float lifetime;
            };

            float map(float v, float min1, float max1, float min2, float max2) {
                return min2 + (v - min1) * (max2 - min2) / (max1 - min1);
            }

            StructuredBuffer<Particle> _Positions;

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert(appdata v) {
                /*v2f o;
                float3 positionOS = v.vertex.xyz + _Positions[v.instancedId].position;
                o.pos = UnityObjectToClipPos(positionOS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;*/

                v2f o;
                float3 positionOS = v.vertex.xyz + _Positions[v.instancedId].position;
                o.pos = UnityObjectToClipPos(positionOS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                TRANSFER_SHADOW(o)

                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed4 shadow = map(SHADOW_ATTENUATION(i), 0, 1, 0.5, 1);
                col *= shadow;
                return col;
            }
            ENDCG
        }

        Pass
        {
            Tags { "LightMode" = "ShadowCaster" }

            CGPROGRAM
            #pragma vertex vert_shadow
            #pragma fragment frag_shadow
            #pragma multi_compile_instancing
            #pragma multi_compile_shadowcaster

            struct v2f {
                V2F_SHADOW_CASTER;
            };

            v2f vert_shadow(appdata_base v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v)
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 frag_shadow(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
