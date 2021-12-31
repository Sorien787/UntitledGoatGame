using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class CountdownTimerUI : MonoBehaviour, IPauseListener
{
	[Header("Internal References")]
	[SerializeField] private TextMeshProUGUI m_TimerText;
	[SerializeField] private RectTransform m_TimerRect;
	[SerializeField] private CanvasGroup m_TextCanvasGroup;
	[SerializeField] private CowGameManager m_Manager;
	[SerializeField] private AudioManager m_AudioManager;

	[Header("Animation and Audio references")]
	[SerializeField] private string m_TimerCompleteAudioIdentifier;
	[SerializeField] private string m_TimerTickAudioIdentifier;
	[SerializeField] private AnimationCurve m_TextPulseSizeByTimer;
	[SerializeField] private AnimationCurve m_TextPulseOpacityByTimer;
	[SerializeField] private float m_TimerFadeTime;
	[SerializeField] private string m_FinalTimerTickString = "0";
	[SerializeField] private bool m_bAffectedByPause = false;
	// Start is called before the first frame update
	private int m_CurrentTime;
	private IEnumerator m_TimerCoroutine;
	public event Action OnTimerComplete;
	public event Action<float> OnTimerTick;

	private void Start()
	{
		if (!m_bAffectedByPause)
			return;
		m_Manager.AddToPauseUnpause(this);
	}

	public void ShowTimer()
	{
		LeanTween.alphaCanvas(m_TextCanvasGroup, 1.0f, m_TimerFadeTime).setEaseInCubic();
	}

	public void StartTimerFromTime(in float time)
    {
		m_TimeTimerStarted = Time.time;
		m_InitialTime = time;
		m_TimerCoroutine = StartTimer(time);
		StartCoroutine(m_TimerCoroutine);
    }

	public void StopTimer(bool hideTimer = true)
	{
		if (hideTimer)
			LeanTween.alphaCanvas(m_TextCanvasGroup, 0.0f, m_TimerFadeTime).setEaseInCubic();
		if (m_TimerCoroutine != null)
			StopCoroutine(m_TimerCoroutine);
		OnTimerComplete = null;
		m_TimeTimerStarted = 0.0f;
		m_TimeRemainingWhenTimerPaused = 0.0f;
		m_InitialTime = 0.0f;
		m_bIsTimerPaused = false;
	}

	private bool m_bIsTimerPaused = false;
	private float m_TimeRemainingWhenTimerPaused = 0.0f;
	public void PauseTimer() 
	{
		if (m_InitialTime != 0) 
		{
			m_TimeRemainingWhenTimerPaused = m_InitialTime - (Time.time - m_TimeTimerStarted);
			m_bIsTimerPaused = true;
			StopCoroutine(m_TimerCoroutine);
		}
	}

	public void ContinueTimer() 
	{
		if (!m_bIsTimerPaused)
			return;

		m_TimerCoroutine = StartTimer(m_TimeRemainingWhenTimerPaused);
		StartCoroutine(m_TimerCoroutine);
		m_bIsTimerPaused = false;
		m_TimeTimerStarted = Time.time;
		m_TimeRemainingWhenTimerPaused = 0;
		
	}

	private IEnumerator StartTimer(float time)
	{
		float remainder = time % 1;
		m_CurrentTime = Mathf.FloorToInt(time);
		if (remainder > 0.01f)
		{
			yield return new WaitForSecondsRealtime(remainder);
		}

		
		while (m_CurrentTime > 0)
		{
			TimerTick(m_CurrentTime.ToString(), m_TimerTickAudioIdentifier);
			yield return new WaitForSecondsRealtime(1.0f);
		}
		TimerTick(m_FinalTimerTickString, m_TimerCompleteAudioIdentifier);
		yield return new WaitForSecondsRealtime(0.5f);
		OnTimerComplete?.Invoke();
		StopTimer(false);
	}

	private float m_InitialTime = 0;
	private float m_TimeTimerStarted = 0;

	private void TimerTick(in string timerText, in string audioIdentifier)
	{
		m_AudioManager.Play(audioIdentifier);
		OnTimerTick?.Invoke(1 - m_CurrentTime / m_InitialTime);
		m_TimerRect.localScale = Vector3.one * (1 + m_TextPulseSizeByTimer.Evaluate(m_CurrentTime / m_InitialTime));
		LeanTween.scale(m_TimerRect.gameObject, Vector3.one, 1.0f).setEaseInOutCubic();
		m_TextCanvasGroup.alpha = 1.0f;
		LeanTween.alphaCanvas(m_TextCanvasGroup, m_TextPulseOpacityByTimer.Evaluate(m_CurrentTime), 1.0f).setEaseInOutCubic();
		m_TimerText.text = timerText;
		m_CurrentTime--;
	}

	public void Pause()
	{
		PauseTimer();
	}

	public void Unpause()
	{
		ContinueTimer();
	}

	void OnDestroy()
	{
		m_Manager.RemoveFromPauseUnpause(this);
	}
}
