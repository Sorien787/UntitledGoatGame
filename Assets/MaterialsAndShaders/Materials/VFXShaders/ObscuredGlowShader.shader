Shader "Custom/ObscuredGlowShader"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Glowable" = "True"
        }
        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            fixed4 _GlowColour;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float depth : DEPTH;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.depth = -mul(UNITY_MATRIX_MV, v.vertex).z * _ProjectionParams.w;
                return o;
            }

            fixed4 frag(v2f i) : SV_TARGET
            {
                //float invert = 1 - i.depth;
                return _GlowColour;
            }
            ENDCG
        }
    }
}
