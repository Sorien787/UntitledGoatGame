Shader "Custom/GrassSwayShader"
{
	Properties
	{
		[Header(Shading)]
		_TopColor("Top Color", Color) = (1,1,1,1)
		_BottomColor("Bottom Color", Color) = (1,1,1,1)
		_TranslucentGain("Translucent Gain", Range(0,1)) = 0.5
		[Space]
		[Header(Blades)]
		_BendRotationRandom("Randomize Grass Bend", Range(0, 1)) = 0.2
		_BladeWidth("Blade Width", Float) = 0.5
		_BladeWidthRandom("Blade Width Randomize", Float) = 0.2
		_BladeHeightMultiplier("Blade Height Multiplier", Float) = 1
		_BladeHeight("Blade Height", Float) = 5
		_BladeHeightRandom("Blade Height Randomize", Float) = 3
		_BladeHeightMinimum("Blade Height Minimum", Float) = 0.2
		_WindDistortionMap("Wind Distortion Map", 2D) = "white" {}
		_BladeForward("Blade Forward", Float) = 0.38
		_BladeCurve("Blade Curvature Amount", Range(1, 4)) = 2
		[Header(Wind)]
		_WindSpeed("Wind Speed", Vector) = (0.05, 0.05, 0, 0)
		_WindStrength("Wind Strength", Float) = 1
		_WindGustSize("Wind Gust Size", Float) = 1
	}

	CGINCLUDE
	#define BLADE_SEGMENTS 3
	#include "UnityCG.cginc"
	#include "Autolight.cginc"
	struct appdata
	{
		float4 vertex : POSITION;
		float3 normal : NORMAL;
		float4 tangent : TANGENT;
		float4 color : COLOR;
		//UNITY_VERTEX_INPUT_INSTANCE_ID
	};

	struct v2g
	{
		float4 vertex : SV_POSITION;
		float3 normal : NORMAL;
		float4 tangent : TANGENT;
		float4 color : COLOR;
		//UNITY_VERTEX_INPUT_INSTANCE_ID
	};

	struct g2f
	{
		float4 pos : SV_POSITION;
#if UNITY_PASS_FORWARDBASE
		float2 uv : TEXCOORD0;
		float3 normal : NORMAL;
		unityShadowCoord4 _ShadowCoord : TEXCOORD1;
#endif
		//UNITY_VERTEX_INPUT_INSTANCE_ID
	};

	v2g vert(appdata v)
	{
		v2g o;

		// set all values in the v2g o to 0.0
		//UNITY_INITIALIZE_OUTPUT(v2g, o);

		// setup the instanced id to be accessed
		//UNITY_SETUP_INSTANCE_ID(v);

		// copy instance id in the appdata v to the v2g o
		//UNITY_TRANSFER_INSTANCE_ID(v, o);

		o.vertex = v.vertex;
		o.normal = v.normal;
		o.tangent = v.tangent;
		o.color = v.color;
		return o;
	}

	// Simple noise function, sourced from http://answers.unity.com/answers/624136/view.html
	// Extended discussion on this function can be found at the following link:
	// https://forum.unity.com/threads/am-i-over-complicating-this-random-function.454887/#post-2949326
	// Returns a number in the 0...1 range.
	float rand(float3 co)
	{
		return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 53.539))) * 43758.5453);
	}


	// Construct a rotation matrix that rotates around the provided axis, sourced from:
	// https://gist.github.com/keijiro/ee439d5e7388f3aafc5296005c8c3f33
	float3x3 AngleAxis3x3(float angle, float3 axis)
	{
		float c, s;
		sincos(angle, s, c);

		float t = 1 - c;
		float x = axis.x;
		float y = axis.y;
		float z = axis.z;

		return float3x3(
			t * x * x + c, t * x * y - s * z, t * x * z + s * y,
			t * x * y + s * z, t * y * y + c, t * y * z - s * x,
			t * x * z - s * y, t * y * z + s * x, t * z * z + c
			);
	}

	float _BendRotationRandom;
	float _BladeWidth;
	float _BladeWidthRandom;
	float _BladeHeightMultiplier;
	float _BladeHeight;
	float _BladeHeightRandom;
	float _BladeHeightMinimum;
	float _BladeForward;
	float _BladeCurve;
	sampler2D _WindDistortionMap;
	float4 _WindDistortionMap_ST;
	float2 _WindSpeed;
	float _WindGustSize;
	float _WindStrength;


	void GenerateGrassVertex(g2f o, inout TriangleStream<g2f> triStream, float3 vertexPosition, float width, float height, float forwardBend, float2 uv, float3x3 transformMatrix)
	{
		float3 tangentPoint = float3(width, forwardBend, height);

		float3 tangentNormal = normalize(float3(0, -1, forwardBend));
		float3 localNormal = mul(transformMatrix, tangentNormal);

		float3 localPosition = vertexPosition + mul(transformMatrix, tangentPoint);

		o.pos = UnityObjectToClipPos(localPosition);

#if UNITY_PASS_FORWARDBASE
		o.uv = uv;
		o.normal = UnityObjectToWorldNormal(localNormal);
		o._ShadowCoord = ComputeScreenPos(o.pos);
#elif UNITY_PASS_SHADOWCASTER
		o.pos = UnityApplyLinearShadowBias(o.pos);
#endif
		triStream.Append(o);
	}


	[maxvertexcount(2 * BLADE_SEGMENTS + 1)]
	void geo(point v2g IN[1] : SV_POSITION, inout TriangleStream<g2f> triStream)
	{
		g2f o;

		// set all values in the g2f o to 0.0
		//UNITY_INITIALIZE_OUTPUT(g2f, o);

		// setup the instanced id to be accessed
		//UNITY_SETUP_INSTANCE_ID(IN[0]);

		// copy instance id in the v2f IN[0] to the g2f o
		//UNITY_TRANSFER_INSTANCE_ID(IN[0], o);

		float3 pos = IN[0].vertex.xyz;

		// seed rand using position to maitain consistency
		float3x3 randomFacingRotationMatrix = AngleAxis3x3(rand(pos) * UNITY_TWO_PI, float3(0, 0, 1));

		float3x3 bendRotationMatrix = AngleAxis3x3(rand(pos.zzx) * _BendRotationRandom * UNITY_PI * 0.5, float3(-1, 0, 0));

		float2 uv = (pos.xz * _WindDistortionMap_ST.xy + _WindDistortionMap_ST.zw + _WindSpeed * _Time.y) / _WindGustSize;
		float2 windSample = (tex2Dlod(_WindDistortionMap, float4(uv, 0, 0)).xy * 2 - 1) * _WindStrength;
		float3 wind = normalize(float3(windSample.x, windSample.y, 0));
		float3x3 windRotation = AngleAxis3x3(UNITY_PI * windSample, wind);

		float3 vNormal = IN[0].normal;
		float4 vTangent = IN[0].tangent;
		float3 vBinormal = cross(vNormal, vTangent) * vTangent.w;

		float3x3 tangentToLocal = float3x3(
			vTangent.x, vBinormal.x, vNormal.x,
			vTangent.y, vBinormal.y, vNormal.y,
			vTangent.z, vBinormal.z, vNormal.z
			);

		float pixelHeightMult = IN[0].color.g;


		float3x3 transformationMatrix = mul(mul(mul(tangentToLocal, windRotation), randomFacingRotationMatrix), bendRotationMatrix);

		float3x3 transformationMatrixFacing = mul(tangentToLocal, randomFacingRotationMatrix);
		//* UNITY_ACCESS_INSTANCED_PROP(Props, _BladeHeightMultiplier) 
		float height = max(((rand(pos.zyx) * 2 - 1) * _BladeHeightRandom + _BladeHeight) * _BladeHeightMultiplier * pixelHeightMult, _BladeHeightMinimum);
		float width = ((rand(pos.xzy) * 2 - 1) * _BladeWidthRandom + _BladeWidth) * sqrt(pixelHeightMult);
		float forward = rand(pos.yyz) * _BladeForward;

		for (int i = 0; i < BLADE_SEGMENTS; i++)
		{
			float t = (float)i / (float)BLADE_SEGMENTS;
			float segmentHeight = height * t;
			float segmentWidth = width * (1 - t) * (1 + t);
			float segmentForward = pow(t, _BladeCurve) * forward;

			float3x3 transformMatrix = i == 0 ? transformationMatrixFacing : transformationMatrix;

			GenerateGrassVertex(o, triStream, pos, segmentWidth, segmentHeight, segmentForward, float2(0, t), transformMatrix);
			GenerateGrassVertex(o, triStream, pos, -segmentWidth, segmentHeight, segmentForward, float2(1, t), transformMatrix);
		}
		GenerateGrassVertex(o, triStream, pos, 0, height, forward, float2(0.5, 1), transformationMatrix);
	}


	ENDCG

	SubShader
	{
		Cull Off

		Pass
		{
			Tags
			{
				"RenderType" = "Opaque"
				"LightMode" = "ForwardBase"
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma geometry geo
			#pragma target 4.6
			#pragma multi_compile_fwdbase
			#pragma multi_compile_instancing

			#include "Lighting.cginc"

			float4 _TopColor;
			float4 _BottomColor;
			float _TranslucentGain;

			//UNITY_INSTANCING_BUFFER_START(Props)
				//UNITY_DEFINE_INSTANCED_PROP(float, _BladeHeightMultiplier)
			//UNITY_INSTANCING_BUFFER_END(Props)


			float4 frag(g2f i, fixed facing : VFACE) : SV_Target
			{
				//UNITY_SETUP_INSTANCE_ID(i);

				float3 normal = facing > 0 ? i.normal : -i.normal;
				float shadow = SHADOW_ATTENUATION(i);
				float NdotL = saturate(saturate(dot(normal, _WorldSpaceLightPos0)) + _TranslucentGain) * shadow;

				float3 ambient = ShadeSH9(float4(normal, 1));
				float4 lightIntensity = NdotL * _LightColor0 +float4(ambient, 1);
				float4 col = lerp(_BottomColor * lightIntensity, _TopColor * lightIntensity, i.uv.y);

				return col;
			}
			ENDCG
		}
		Pass
		{
			Tags
			{
				"LightMode" = "ShadowCaster"
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma geometry geo

			#pragma target 4.6
			#pragma multi_compile_shadowcaster

			float4 frag(g2f i) : SV_Target
			{
				SHADOW_CASTER_FRAGMENT(i)
			}

			ENDCG
		}

	}
}
