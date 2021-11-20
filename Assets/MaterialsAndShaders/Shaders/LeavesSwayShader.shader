Shader "Custom/LeavesSwayShader" {

    Properties{
        _Tint("Tint", Color) = (1,1,1,1)

        _WindDistortionMap("Wind Distortion Map", 2D) = "white" {}
        _WindSpeed("Wind Speed", Vector) = (0.05, 0.05, 0, 0)
        _WindStrength("Wind Strength", Float) = 1
        _WindGustSize("Wind Gust Size", Float) = 1

    }

        SubShader{

            CGPROGRAM
            #pragma target 3.0
            #pragma surface surf Standard vertex:vert addshadow
            #pragma multi_compile_instancing

            sampler2D _WindDistortionMap;
            float4 _WindDistortionMap_ST;
            float2 _WindSpeed;
            float _WindGustSize;
            float _WindStrength;

            fixed4 _Tint;

            //Structs
            struct Input {
                float2 uv_MainTex;
            };

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _Tint)
            UNITY_INSTANCING_BUFFER_END(Props)

            // Vertex Manipulation Function
            void vert(inout appdata_full i) {

                //Gets the vertex's World Position 
               float3 worldPos = mul(unity_ObjectToWorld, i.vertex).xyz;
               float2 uv = (worldPos.xz * _WindDistortionMap_ST.xy + _WindDistortionMap_ST.zw + _WindSpeed * _Time.y) / _WindGustSize;
               float3 windSample = (tex2Dlod(_WindDistortionMap, float4(uv, 0, 0)).rgb* 2 - 1);
               float3 wind = normalize(float3(windSample.x, windSample.y, windSample.z)) * _WindStrength;

               wind.xyz *= i.color.r;
               wind.z *= i.color.b;

               i.vertex.xyz += wind.xyz;

           }

            // Surface Shader
            void surf(Input IN, inout SurfaceOutputStandard o) {
                o.Albedo = UNITY_ACCESS_INSTANCED_PROP(Props, _Tint).rgb;
                o.Alpha = c.a;
            }

    ENDCG
        }

            Fallback "Diffuse"
}