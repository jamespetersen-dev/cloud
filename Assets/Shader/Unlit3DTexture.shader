Shader "Custom/Unlit3DTexture"
{
    Properties
    {
        _MainTex ("3D Texture", 3D) = "" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler3D _MainTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 texcoord : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.vertex.xyz; // Use world position for sampling
                o.texcoord += 1;
                o.texcoord /= 2;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex3D(_MainTex, i.texcoord);
            }
            ENDCG
        }
    }
}