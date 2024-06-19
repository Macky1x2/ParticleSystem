Shader "Unlit/TutorialEmissionShader"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _MainColor("Color" ,color) = (1,1,1,1)

        [HDR]
        _EmissionColor("EmissionColor",color) = (0,0,0,0)
    }
        SubShader
        {
            Tags { "RenderType" = "Opaque" }
            LOD 200
            Pass
            {
                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
                #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include  "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

                struct appdata
                {
                    float4 vertex : POSITION;
                    half3 normal: NORMAL;
                    float2 uv : TEXCOORD0;
                };

                struct v2f
                {
                float4 vertex : SV_POSITION;
                float3 normalWS : TEXCOORD1;
                float4 shadowCoord : TEXCOORD3;
                    float2 uv : TEXCOORD0;
                };

                TEXTURE2D(_MainTex);
                SAMPLER(sampler_MainTex);
                float4 _MainColor;
                float _Alpha;
                float _AlphaThreshold;
                float4 _EmissionColor;

                v2f vert(appdata v)
                {
                    v2f o;
                    // o.vertex = TransformObjectToHClip(v.vertex);
                     //面の法線を取得、ライトの当たる向きを計算
                     VertexNormalInputs normal = GetVertexNormalInputs(v.normal);
                     o.uv = v.uv;
                     o.normalWS = normal.normalWS;
                     VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
                     o.vertex = vertexInput.positionCS;
                     o.shadowCoord = GetShadowCoord(vertexInput);
                     return o;
                 }

                 float4 frag(v2f i) : SV_Target
                 {
                     float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _MainColor;
                     //Light.hlslで提供されるUnityのライトを取得する関数
                     Light lt = GetMainLight(i.shadowCoord);

                     //ライトの向きを計算
                     float strength = dot(lt.direction, i.normalWS);
                     float4 lightColor = float4(lt.color, 1) * (lt.distanceAttenuation * lt.shadowAttenuation);
                     col = col * lightColor * strength + _EmissionColor;
                     return col;
                 }
                 ENDHLSL
             }
        }
}