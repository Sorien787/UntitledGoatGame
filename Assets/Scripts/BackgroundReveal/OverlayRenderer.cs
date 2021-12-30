using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// TODO PART 2
//
// Pass1 write stencil
// Pass2 fullscreen with depth and stencil test

//[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class OverlayRenderer : MonoBehaviour
{
    private Shader _shader;
    private Material _material;
    private Camera _camera;
    private CommandBuffer _commandBuffer;

    [Header("Settings")]
	[SerializeField] private CameraEvent _cameraEvent = CameraEvent.BeforeImageEffects;
	[SerializeField] private DepthTests _depthTest = DepthTests.LEqual;
	[SerializeField] private OutlinePatterns _outlinePattern = OutlinePatterns.Diamond;

    [Range(0.000001f, 0.001f)]
	[SerializeField] private float _Bias = 0.001f;

    [Header("Render Textures")]
	[SerializeField] private RenderTextureFormat _groupIDTextureFormat = RenderTextureFormat.R8;

	[SerializeField] private GroupColors _GroupColors = new GroupColors();

	[SerializeField] private Color m_OutlineColor = Color.white;
	[SerializeField] private Color m_FillColor = Color.grey;


    public void OnEnable()
    {
        if (!_camera)
            _camera = Camera.main;
		if (!_camera)
		{
			Debug.LogError("No camera found in OverlayRenderer, dumbass", gameObject);
			enabled = false;
			return;
		}
		_shader = Shader.Find("Hidden/ObjectOverlay");
        _material = new Material(_shader);
        _commandBuffer = new CommandBuffer();
        _camera.AddCommandBuffer(_cameraEvent, _commandBuffer);
    }

    public void OnDisable()
    {
        if (_commandBuffer != null && _camera != null)
        {
            _camera.RemoveCommandBuffer(_cameraEvent, _commandBuffer);
            _commandBuffer.Release();
            _commandBuffer = null;
        }
    }

	private void LateUpdate()
    {
        Debug.Assert(_commandBuffer != null);
        _commandBuffer.Clear();

        PrepareShaderParameters();

        RenderOverlayObjects(OverlaySelectable.Instances);
    }

	public void SetDepthTestMode(DepthTests depthTest)
	{
		_depthTest = depthTest;
		SetDepthTestModeInternal();
	}

	private void SetDepthTestModeInternal()
	{
		// Depth testing
		if (_depthTest == DepthTests.LEqual)
			_material.EnableKeyword("EnableDepthTest");
		else
			_material.DisableKeyword("EnableDepthTest");
	} 

	private void SetOutlinePatternInternal()
	{
		// Pattern Selection
		if (_outlinePattern == OutlinePatterns.Rect)
		{
			_material.DisableKeyword("Pattern_Diamond");
			_material.EnableKeyword("Pattern_Rect");
		}
		else
		{
			_material.DisableKeyword("Pattern_Rect");
			_material.EnableKeyword("Pattern_Diamond");
		}
	}

	private void SetZBiasInternal()
	{
		_commandBuffer.SetGlobalFloat(ShaderIDs._ZBias, _Bias);
	}

	private void PrepareShaderParameters()
    {
		SetOutlinePatternInternal();

		SetDepthTestModeInternal();

		SetZBiasInternal();

	}
	[SerializeField] private Material m_TestMat;
    void RenderOverlayObjects(IEnumerable<OverlaySelectable> instances)
    {
        // Request needed textures
        RenderTexture overlayIDTexture = RenderTexture.GetTemporary(_camera.pixelWidth, _camera.pixelHeight, 24, _groupIDTextureFormat);

		// Step 1: Write Group IDs to overlayIDTexture
		//_commandBuffer.SetRenderTarget(overlayIDTexture);

		_commandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, overlayIDTexture);
		//_commandBuffer.ClearRenderTarget(true, true, Color.clear, 1.0f);
		foreach (var instance in instances)
        {
			if (!instance.HighlightAlways)
				return;

			_commandBuffer.SetGlobalInt(ShaderIDs._GroupID, instance.GroupId);
			//// draws the meshes to the texture using the command buffer extension
			_commandBuffer.SetGlobalVector(ShaderIDs._FillColor, instance.GetHighlightColor);

			_commandBuffer.DrawAllMeshes(instance.gameObject, _material, PassIDs.Pass_WriteGroupID);

			//_commandBuffer.DrawAllMeshes(instance.gameObject, m_TestMat, 0);
            
        }
		RenderTexture.ReleaseTemporary(overlayIDTexture);

        // Step 2: Apply overlay effect
   //     _commandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, overlayIDTexture);
   //     _commandBuffer.SetGlobalTexture(ShaderIDs._OverlayIDTexture, overlayIDTexture);
   //     foreach (var instance in instances)
   //     {
			//if (!instance.HighlightAlways)
			//	return;
			//_commandBuffer.SetGlobalVector(ShaderIDs._FillColor, instance.GetHighlightColor);// _GroupColors[instance._overlayGroupID]._fillColor);
			//_commandBuffer.SetGlobalVector(ShaderIDs._OutlineColor, instance.GetHighlightColor);// _GroupColors[instance._overlayGroupID]._outlineColor);
   //         _commandBuffer.DrawAllMeshes(instance.gameObject, _material, PassIDs.Pass_RenderOverlay);
            
   //     }

        // Don't forget to release the temporary render texture
        //RenderTexture.ReleaseTemporary(overlayIDTexture);
    }

    public static class ShaderIDs
    {
        public static readonly int _GroupID = Shader.PropertyToID("_GroupID");
        public static readonly int _ZBias = Shader.PropertyToID("_ZBias");
		public static readonly int _DepthTest = Shader.PropertyToID("_DepthTest");
        public static readonly int _OverlayIDTexture = Shader.PropertyToID("_OverlayIDTexture");
        public static readonly int _FillColor = Shader.PropertyToID("_FillColor");
        public static readonly int _OutlineColor = Shader.PropertyToID("_OutlineColor");
    }

    public static class PassIDs
    {
        public static readonly int Pass_WriteGroupID = 0;
        public static readonly int Pass_RenderOverlay = 1;
    }
}