using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SolidRenderReplacementEffect : MonoBehaviour
{
	[SerializeField] private Shader replacementShader;
	[SerializeField] private Camera cam;
	private static RenderTexture Prepass;
	private void OnEnable()
	{
		//Prepass = new RenderTexture(Screen.width, Screen.height, 24);
		//Prepass.antiAliasing = QualitySettings.antiAliasing;
		//if (replacementShader != null && cam != null)
		//	cam.SetReplacementShader(replacementShader, "Glowable");
	}


	[HideInInspector]
	public Material material;
	[ImageEffectOpaque]
	private void OnRenderImage(RenderTexture src, RenderTexture dest)
	{
		//Graphics.Blit(src, dest);

		//Graphics.SetRenderTarget(Prepass);
		//GL.Clear(false, true, Color.clear);

		//Graphics.Blit()

		//cam.depthTextureMode = DepthTextureMode.Depth;

		//if (material == null || material.shader != replacementShader)
		//{
		//	material = new Material(replacementShader);
		//}

	}
	private void OnDisable()
	{
		cam.ResetReplacementShader();
	}
}
