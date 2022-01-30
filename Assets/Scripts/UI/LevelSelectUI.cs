using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.Video;

public class LevelSelectUI : MonoBehaviour
{
	[Space]
	[Header("Anim Params")]
	[SerializeField] [Range(0.1f, 1.0f)] private float m_TextFadeInOutTime;
	[SerializeField] [Range(0.1f, 1.0f)] private float m_TextFadeNextDelay = 0.3f;

	[Space]
	[Header("UI references")]
	[SerializeField] private TextMeshProUGUI m_LevelNameLeft;
	[SerializeField] private TextMeshProUGUI m_LevelNameRight;
	[SerializeField] private TextMeshProUGUI m_LevelTime;
	[SerializeField] private StarUI m_StarUI;
	[Space]
	[SerializeField] private CanvasGroup m_LevelNameCanvasGroup;
	[SerializeField] private CanvasGroup m_LevelTimeCanvasGroup;
	[SerializeField] private CanvasGroup m_LevelScoreCanvasGroup;
	[SerializeField] private Transform m_LevelDataUITransform;

	[Space]
	[Header("Game System References")]
	[SerializeField] private GameObject m_LevelDataUIPrefab;
	[SerializeField] private CowGameManager m_GameManager;

	private int m_SelectedLevelId;
	public event Action<int> m_OnLevelSelected;

	public int GetChosenLevelId => m_SelectedLevelId;

	private void Awake()
	{
		bool lastLevelCompleted = true;
		// TODO: re-implement score based on time
		m_StarUI.enabled = false;

		for (int i = 0; i < m_GameManager.GetNumLevels; i++)
		{
			LevelData levelDatum = m_GameManager.GetLevelDataByLevelIndex(i);
			levelDatum.SetLevelNumber(i);

			LevelDataUI levelDataUI = Instantiate(m_LevelDataUIPrefab, m_LevelDataUITransform).GetComponent<LevelDataUI>();
			levelDataUI.OnSelectLevel += UpdateSelectedLevelData;
			m_OnLevelSelected += levelDataUI.OnLevelNumSelected;
			levelDataUI.SetupData(levelDatum, lastLevelCompleted);
			lastLevelCompleted = levelDatum.IsCompleted;
		}
		UpdateSelectedLevelData(0);
	}

	private void EditField(Action OnEdit, CanvasGroup canvas, in float delay, float editTime, ref int tweenId)
	{
		LeanTween.cancel(tweenId);
		LTDescr tween = LeanTween.alphaCanvas(canvas, 0.0f, editTime).setEaseInOutCubic().setOnComplete(() =>
		{
			OnEdit();
			tween = LeanTween.alphaCanvas(canvas, 1.0f, editTime).setEaseInOutCubic();
		}).setDelay(delay);
		tweenId = tween.uniqueId;
	}

	int[] animIDs = new int[]{0, 0, 0 };

	private void UpdateSelectedLevelData(int levelId)
	{
		m_SelectedLevelId = levelId;
		LevelData levelData = m_GameManager.GetLevelDataByLevelIndex(m_SelectedLevelId);
		m_OnLevelSelected.Invoke(m_SelectedLevelId);

		int currentPoint = 0;

		EditField(() => { m_LevelNameLeft.text = "Level " + UnityUtils.UnityUtils.NumberToWords(levelData.GetLevelNumber+1); m_LevelNameRight.text = levelData.GetLevelName; },
			m_LevelNameCanvasGroup,
			currentPoint * m_TextFadeNextDelay,
			m_TextFadeInOutTime,
			ref animIDs[currentPoint]);
		currentPoint++;
		EditField(() => m_LevelTime.text = levelData.GetBestTimeAsString,
			m_LevelTimeCanvasGroup,
			currentPoint * m_TextFadeNextDelay,
			m_TextFadeInOutTime,
			ref animIDs[currentPoint]);
		currentPoint++;
		//EditField(() => m_StarUI.SetStarsVisible((int)levelData.GetCurrentStarRating),
		//	m_LevelScoreCanvasGroup,
		//	currentPoint * m_TextFadeNextDelay,
		//	m_TextFadeInOutTime,
		//	ref animIDs[currentPoint]);
	}
}
