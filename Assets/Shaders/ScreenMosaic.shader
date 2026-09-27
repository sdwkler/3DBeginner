Shader "Hidden/ScreenMosaic"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Pixel ("Pixel", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Transparent" }
        GrabPass { "_MosaicTex" }
        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always
            Blend One Zero

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MosaicTex;
            float _Pixel;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 grab : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.grab = ComputeGrabScreenPos(o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.grab.xy / max(i.grab.w, 0.0001);
                float pixel = max(_Pixel, 1);
                float2 steps = pixel / _ScreenParams.xy;
                uv = (floor(uv / steps) + 0.5) * steps;
                return tex2D(_MosaicTex, uv);
            }
            ENDCG
        }
    }
}
