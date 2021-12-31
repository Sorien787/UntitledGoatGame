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
	[SerializeField] [Range(0.000001f, 0.001f)] private float _zBias = 0.000001f;

    [Header("Render Textures")]
	[SerializeField] private RenderTextureFormat _groupIDTextureFormat = RenderTextureFormat.R8;

	int _passId = 0;

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

	public void SetZDepth(float zBias)
	{
		_zBias = zBias;
		SetZBiasInternal();
	}

	private void SetZBiasInternal()
	{
		_commandBuffer.SetGlobalFloat(ShaderIDs._ZBias, _zBias);
	}

	private void SetDepthTestModeInternal()
	{
		// Depth testing
		if (_depthTest == DepthTests.LEqual)
		{
			_passId = PassIDs.Pass_OnlyBehindStencils;
		}
		else
		{
			_passId = PassIDs.Pass_Always;
		}
	} 

	private void PrepareShaderParameters()
    {
		SetDepthTestModeInternal();
	}

	[SerializeField] private Material m_TestMat;
    void RenderOverlayObjects(IEnumerable<OverlaySelectable> instances)
    {
        // Request needed textures
        RenderTexture overlayIDTexture = RenderTexture.GetTemporary(_camera.pixelWidth, _camera.pixelHeight, 24, _groupIDTextureFormat);

		_commandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, overlayIDTexture);

		foreach (var instance in instances)
        {
			if (!instance.HighlightAlways)
				continue;

			_commandBuffer.SetGlobalVector(ShaderIDs._FillColor, instance.GetHighlightColor);

			_commandBuffer.DrawAllMeshes(instance.gameObject, _material, _passId);
            
        }
		RenderTexture.ReleaseTemporary(overlayIDTexture);
    }

    public static class ShaderIDs
    {
        public static readonly int _FillColor = Shader.PropertyToID("_FillColor");
		public static readonly int _ZBias = Shader.PropertyToID("_ZBias");
    }

    public static class PassIDs
    {
        public static readonly int Pass_OnlyBehindStencils = 0;
        public static readonly int Pass_Always = 1;
    }
}