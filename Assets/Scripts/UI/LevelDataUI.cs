using UnityEngine;
using UnityEngine.Video;
using TMPro;
using UnityEngine.EventSystems;
using System;

public class LevelDataUI : MonoBehaviour, IPointerExitHandler, IPointerEnterHandler, IPointerDownHandler
{
	[Header("Object References")]
	[SerializeField] private VideoPlayer m_VideoPlayer;
	[SerializeField] private GameObject m_BlurPlaneGo;
	[SerializeField] private RectTransform m_RectTransformForVideo;
	[SerializeField] private UnityEngine.UI.RawImage m_RawImage;
	[SerializeField] private UnityEngine.UI.AspectRatioFitter m_AspectRatioFitter;
	[SerializeField] private CanvasGroup m_OutGlowCanvasGroup;
	[SerializeField] private CanvasGroup m_LevelSplashCanvasGroup;

	[Header("Animation References and Params")]
	[SerializeField] [Range(0.05f, 1.0f)] private float m_AnimInOutFadeTime;
	[SerializeField] private StarUI m_StarUI;

	private int m_OutGlowFadeId;
	private int m_LevelSplashId;

	private bool m_bIsUnlocked = false;
	private bool m_bIsSelected = false;
	private int m_LevelId;

	public event Action<int> OnSelectLevel;

	#region UnityInterfaces
	private void OnVideoPlayerPrepared(VideoPlayer source)
	{
		m_VideoPlayer.frame = 0;
		if (!m_bIsSelected)
			m_VideoPlayer.Pause();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (m_bIsUnlocked && !m_bIsSelected)
		{
			HideStarSplash();
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		m_bIsPointerIn = false;
		if (m_bIsUnlocked && !m_bIsSelected)
		{
			ShowStarSplash();
		}
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		m_bIsPointerIn = true;
		if (eventData.button == PointerEventData.InputButton.Left)
		{
			if (m_bIsUnlocked && !m_bIsSelected)
			{
				OnSelectLevel.Invoke(m_LevelId);
			}
		}
	}

	bool m_bIsPointerIn = false;

	private void ShowStarSplash()
	{
		LeanTween.cancel(m_LevelSplashId);
		m_LevelSplashId = LeanTween.alphaCanvas(m_LevelSplashCanvasGroup, 1.0f, m_AnimInOutFadeTime).setEaseInOutCubic().uniqueId;
		m_VideoPlayer.Pause();
		m_VideoPlayer.frame = 0;
	}

	private void HideStarSplash()
	{
		LeanTween.cancel(m_LevelSplashId);
		m_LevelSplashId = LeanTween.alphaCanvas(m_LevelSplashCanvasGroup, 0.0f, m_AnimInOutFadeTime).setEaseInOutCubic().uniqueId;
		Debug.Log("Attempting to play video!");
		m_VideoPlayer.Play();
	}

	private void ShowOutGlow()
	{
		LeanTween.cancel(m_OutGlowFadeId);
		m_OutGlowFadeId = LeanTween.alphaCanvas(m_OutGlowCanvasGroup, 1.0f, m_AnimInOutFadeTime).setEaseInOutCubic().uniqueId;
	}

	private void HideOutGlow()
	{
		LeanTween.cancel(m_OutGlowFadeId);
		m_OutGlowFadeId = LeanTween.alphaCanvas(m_OutGlowCanvasGroup, 0.0f, m_AnimInOutFadeTime).setEaseInOutCubic().uniqueId;
	}

	public void OnLevelNumSelected(int levelNum)
	{
		if (levelNum == m_LevelId)
		{
			if (!m_bIsSelected)
			{
				m_bIsSelected = true;
				ShowOutGlow();
				if (!m_bIsPointerIn)
					HideStarSplash();
			}
		}
		else
		{
			if (m_bIsSelected)
			{
				HideOutGlow();
				m_bIsSelected = false;
				if (!m_bIsPointerIn)
					ShowStarSplash();
			}
		}
	}
	#endregion

	#region Initialization
	public void SetupData(LevelData m_Data, bool isUnlocked)
	{
		m_bIsUnlocked = isUnlocked;
		m_LevelId = m_Data.GetLevelNumber;

		m_LevelSplashCanvasGroup.alpha = m_bIsUnlocked ? 1.0f : 0.0f;
		//m_StarUI.SetStarsVisible((int)m_Data.GetCurrentStarRating);
		m_BlurPlaneGo.SetActive(!m_bIsUnlocked);
		m_OutGlowCanvasGroup.alpha = 0.0f;

		float aspectRatio = (float)m_Data.GetLevelVideoClip.width / m_Data.GetLevelVideoClip.height;
		m_AspectRatioFitter.aspectRatio = aspectRatio;

		m_VideoPlayer.clip = m_Data.GetLevelVideoClip;
		m_VideoPlayer.renderMode = VideoRenderMode.RenderTexture;
		m_VideoPlayer.targetTexture = new RenderTexture((int)m_RectTransformForVideo.rect.height, (int)m_RectTransformForVideo.rect.width, 1);
		m_VideoPlayer.targetTexture.Create();
		m_RawImage.texture = m_VideoPlayer.targetTexture;
		m_VideoPlayer.Prepare();
		m_VideoPlayer.prepareCompleted += OnVideoPlayerPrepared;
		m_VideoPlayer.isLooping = true;
	}
	#endregion

	private void OnDestroy()
	{
		m_VideoPlayer.targetTexture.Release();
	}
}
