Shader "Hidden/ObjectOverlay"
{
	HLSLINCLUDE

#include "UnityCG.cginc"
#define MAX_GROUP_COUNT 16

	// This provides access to the vertices of the mesh being rendered
	struct appdata
	{
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
	};

	struct v2f
	{
		float4 vertex : SV_POSITION;
		float eyeDepth : TEXCOORD0;
	};

	float4 _FillColor;
	float _ZBias;

	UNITY_DECLARE_TEX2D(_CameraDepthTexture);

	// Vertex shader
	v2f Vert(appdata v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.eyeDepth = UnityObjectToViewPos(v.vertex).z;
		COMPUTE_EYEDEPTH(o.eyeDepth);
		return o;
	}


	float4 FragDrawAlways(UNITY_VPOS_TYPE screenPos : VPOS, float eyeDepth : TEXCOORD0) : SV_Target
	{
		return _FillColor;
	}

	float4 FragDrawUsingDepth(UNITY_VPOS_TYPE screenPos : VPOS, float eyeDepth : TEXCOORD0) : SV_Target
	{
		// The depth of the current fragment
		float vDepth = eyeDepth / _ProjectionParams.z;
		// The depth of the fragment in the depth buffer
		float gDepth = Linear01Depth(_CameraDepthTexture.Load(int3(screenPos.xy, 0)).r);
		// Depth test
		if (vDepth + _ZBias < gDepth)
			return float4(0.0f, 0.0f, 0.0f, 0.0f);

		return _FillColor;
	}


		ENDHLSL

	SubShader
	{
		Pass // 0
		{

			Name "Pass_OnlyBehindStencils"
			ZTest Always
			ZWrite Off
			Cull Back
			Stencil {
				Ref 3
				Comp equal
			}

			HLSLPROGRAM
				#pragma target 3.0
				#pragma vertex Vert
				#pragma fragment FragDrawUsingDepth
			ENDHLSL
		}

		Pass // 1
		{
	
			Name "Pass_Always"
			ZTest Always
			ZWrite Off
			Cull Back

			HLSLPROGRAM
				#pragma target 3.0
				#pragma vertex Vert
				#pragma fragment FragDrawAlways
			ENDHLSL
		}
	}
}
