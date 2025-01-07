Shader "Custom/Unlit3DTexture"
{
    Properties
    {
        _MainTex ("3D Texture", 3D) = "" {}
        _Offset ("Offset", Vector) = (0, 0, 0, 0)
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
            float4 _Offset;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 worldSpace : TEXCOORD2;
                float4 vertex : SV_POSITION;
                float3 texcoord : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldSpace = mul(unity_ObjectToWorld, v.vertex);
                o.texcoord = v.vertex.xyz;
                o.texcoord += 0.5;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                i.texcoord += _Offset.xyz;

                float c1, c2, c3;
                if (i.texcoord.x == 1) { c1 = i.texcoord.x; } else { c1 = frac(i.texcoord.x);}
                if (i.texcoord.y == 1) { c2 = i.texcoord.y; } else { c2 = frac(i.texcoord.y);}
                if (i.texcoord.z == 1) { c3 = i.texcoord.z; } else { c3 = frac(i.texcoord.z);}

                float3 sample = float3(c1, c2, c3);
                return tex3D(_MainTex, sample);
                /*if (_Offset)

                fixed4 c1 = fixed4(i.worldSpace, 1); c1 *= _Offset.x;
                fixed4 c2 = fixed4(frac(i.texcoord), 1); c2 *= (1 - _Offset.x);
                return c1 + c2;*/
                //return fixed4(i.worldSpace, 1);
                //return fixed4((i.texcoord.x), (i.texcoord.y), (i.texcoord.z), 1);
                //return tex3D(_MainTex, i.texcoord);
            }
            ENDCG
        }
    }
}