using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicPlayer : MonoBehaviour, ILevelListener, IPauseListener
{
    [SerializeField] private AudioManager m_AudioManager;
    [SerializeField] private SoundObject m_MusicIdentifier;
	[SerializeField] private CowGameManager m_Manager;

	[SerializeField] private float m_AudioFadeTimeEnd = 2.0f;
	[SerializeField] private float m_AudioFadeTime = 0.3f;
	[SerializeField] private float m_AudioFadeVolume = 0.3f;

	[SerializeField] private bool m_bRequireLevelStart = false;

	void Awake() 
	{
		StartFade(1.0f, 1.0f);
		m_Manager.AddToLevelStarted(this);
		if (!m_bRequireLevelStart)
			m_AudioManager.Play(m_MusicIdentifier);
	}

	private IEnumerator m_CurrentCoroutine;
	float m_CurrentVolume = 0.0f;
	private IEnumerator FadeMusic(SoundObject fadeObject, float target, float timeToChange) 
	{
		float start = m_CurrentVolume;
		float currentTime = 0.0f;
		while (currentTime < timeToChange)
		{

			currentTime += Time.deltaTime;
			float val = Mathf.Lerp(start, target, currentTime / timeToChange);
			m_CurrentVolume = val;
			m_AudioManager.SetVolume(fadeObject, val);
			yield return null;
		}
	}
	void StartFade(float target, float time) 
	{
		if (m_CurrentCoroutine != null)
			StopCoroutine(m_CurrentCoroutine);
		m_CurrentCoroutine = FadeMusic(m_MusicIdentifier, target, time);
		StartCoroutine(m_CurrentCoroutine);
	}

	void OnDestroy() 
	{
		m_Manager.RemoveFromLevelStarted(this);
		m_Manager.RemoveFromPauseUnpause(this);
	}

	public void LevelFinished()
	{
		StartFade(0.0f, m_AudioFadeTimeEnd);
	}

	public void LevelStarted() 
	{
		m_Manager.AddToPauseUnpause(this);
		if (m_bRequireLevelStart)
			m_AudioManager.Play(m_MusicIdentifier);
	}

	public void Pause()
	{
		StartFade(m_AudioFadeVolume, m_AudioFadeTime);
	}

	public void PlayerPerspectiveBegin() {}

	public void Unpause()
	{
		StartFade(1.0f, m_AudioFadeTime);
	}

	public void OnExitLevel(float transitionTime)
	{
		StartFade(0.0f, transitionTime);
	}
}
