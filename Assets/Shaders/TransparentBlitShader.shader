Shader "Custom/TransparentBlitShader"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {} // Camera image
        _OverlayTex ("Overlay Texture", 2D) = "white" {} // Compute shader output
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            sampler2D _OverlayTex;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the camera image and compute shader output
                fixed4 cameraColor = tex2D(_MainTex, i.uv);
                fixed4 overlayColor = tex2D(_OverlayTex, i.uv);

                // Blend overlay texture over the camera image
                return lerp(cameraColor, overlayColor, overlayColor.a);
            }
            ENDCG
        }
    }
}