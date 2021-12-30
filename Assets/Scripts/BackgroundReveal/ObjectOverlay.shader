Shader "Hidden/ObjectOverlay"
{
	HLSLINCLUDE

#include "UnityCG.cginc"
#define MAX_GROUP_COUNT 16

#if Pattern_Diamond
		// - - x - -
		// - x - x -
		// x - c - x
		// - x - x -
		// - - x - -
		static const int samplePattern_Count = 8;
	static const float2 samplePattern[8] =
	{
		float2(2, 0),
		float2(-2, 0),
		float2(1, 1),
		float2(-1,-1),
		float2(0, 2),
		float2(0,-2),
		float2(-1, 1),
		float2(1,-1)
	};

#elif Pattern_Rect
		// - - - - -
		// - x x x -
		// - x c x -
		// - x x x -
		// - - - - -
		static const int samplePattern_Count = 8;
	static const float2 samplePattern[8] =
	{
		float2(1, 0),
		float2(-1, 0),
		float2(1, 1),
		float2(-1,-1),
		float2(0, 1),
		float2(0,-1),
		float2(-1, 1),
		float2(1,-1)
	};
#endif

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
	float4 _OutlineColor;

	float4 _FillColors[MAX_GROUP_COUNT];
	float4 _OutlineColors[MAX_GROUP_COUNT];
	int _GroupID;
	float _ZBias;

	UNITY_DECLARE_TEX2D(_OverlayIDTexture);
	UNITY_DECLARE_TEX2D(_CameraDepthTexture);

	bool IsEdgePixel(int2 screenPos, const int sampleCount, const float2 offsets[8])
	{
		// Get overlay id of the pixel currently being rendered
		float center = _OverlayIDTexture.Load(int3(screenPos.xy, 0)).r;
		// Compare it to all neighbors using the sample offsets
		for (int i = 0; i < sampleCount; i++)
		{
			float neighbor = _OverlayIDTexture.Load(int3(screenPos.xy + offsets[i], 0)).r;
			if (neighbor != center)
			{
				// This is an edge pixel! use outline color
				return true;
			}
		}

		return false;
	}

	bool IsEdgePixel(float groupID, int2 screenPos, const int sampleCount, const float2 offsets[8])
	{
		// Compare it to all neighbors using the sample offsets
		for (int i = 0; i < sampleCount; i++)
		{
			float neighbor = _OverlayIDTexture.Load(int3(screenPos.xy + offsets[i], 0)).r;
			if (neighbor != groupID)
			{
				// This is an edge pixel! use outline color
				return true;
			}
		}

		return false;
	}

	// Vertex shader
	v2f Vert(appdata v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.eyeDepth = UnityObjectToViewPos(v.vertex).z;
		COMPUTE_EYEDEPTH(o.eyeDepth);

		return o;
	}

	// Vertex shader passthrough
	v2f VertPassthrough(appdata v)
	{
		v2f o;
		o.vertex = v.vertex;
		o.eyeDepth = UnityObjectToViewPos(v.vertex).z;
		COMPUTE_EYEDEPTH(o.eyeDepth);
		return o;
	}

	// Fragment shader (first pass)
	float4 FragWriteOverlayID(UNITY_VPOS_TYPE screenPos : VPOS, float eyeDepth : TEXCOORD0) : SV_Target
	{
#if EnableDepthTest
		// The depth of the current fragment
		float vDepth = eyeDepth / _ProjectionParams.z;

		// The depth of the fragment in the depth buffer
		float gDepth = Linear01Depth(_CameraDepthTexture.Load(int3(screenPos.xy, 0)).r);

		// Depth test
		if (vDepth - _ZBias < gDepth)
			return float4(0.0, 0.0, 0.0, 0.0);
#endif
		//// Map range [0, 255] to [0.0f, 1.0f]
		return _FillColor;
	}

	float4 FragOverlay(UNITY_VPOS_TYPE screenPos : VPOS, float eyeDepth : TEXCOORD0) : SV_Target
	{
		return float4(1, 0, 0, 1);

		float center = _OverlayIDTexture.Load(int3(screenPos.xy, 0)).r;
		int centerID = round(center * 255.0);
		if (IsEdgePixel(center, screenPos, samplePattern_Count, samplePattern))
		{
			return _OutlineColors[centerID];
		}
		else
		{
			return _FillColors[centerID];
		}
		
	}

	float4 FragOverlayDirect(UNITY_VPOS_TYPE screenPos : VPOS, float eyeDepth : TEXCOORD0) : SV_Target
	{
		return float4(1, 0, 0, 1);

		float center = _OverlayIDTexture.Load(int3(screenPos.xy, 0)).r;
		int centerID = round(center * 255.0);

		if (IsEdgePixel(center, screenPos, samplePattern_Count, samplePattern))
		{
			return _OutlineColor;
		}
		else
		{
			return _FillColor;
		}
		
	}

		ENDHLSL

	SubShader
	{
		Pass // 0
		{
			//Stencil {
			//	Ref 1
			//	Comp always
			//	Pass replace
			//}

			Name "Pass_WriteID"
			ZTest Always
			ZWrite On
			Cull Back
			Blend SrcAlpha OneMinusSrcAlpha
			HLSLPROGRAM
				#pragma target 3.0
				#pragma multi_compile Pattern_Diamond Pattern_Rect
				#pragma multi_compile_local __ EnableDepthTest
				#pragma vertex Vert
				#pragma fragment FragWriteOverlayID
			ENDHLSL
		}

		Pass // 1
		{
			//Stencil {
			//	Ref 1
			//	Comp equal
			//}

			Name "Pass_RenderOverlay"
			ZTest LEqual
			ZWrite Off
			Cull Off
			//Blend Off
			Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
			Blend SrcAlpha OneMinusSrcAlpha

			HLSLPROGRAM
				#pragma target 3.0
				#pragma multi_compile Pattern_Diamond Pattern_Rect 
				#pragma vertex Vert
				#pragma fragment FragOverlayDirect
			ENDHLSL
		}
	}
}
