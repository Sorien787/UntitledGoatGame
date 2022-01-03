using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicPlayer : MonoBehaviour, ILevelListener, IPauseListener
{
    [SerializeField] private AudioManager m_AudioManager;
    [SerializeField] private SoundObject m_MusicIdentifier;
	[SerializeField] private CowGameManager m_Manager;

	void Awake() 
	{
		m_Manager.AddToLevelStarted(this);
		m_Manager.AddToPauseUnpause(this);
	}

	private IEnumerator m_CurrentCoroutine;
	float m_CurrentVolume = 0.0f;
	private IEnumerator FadeMusic(SoundObject fadeObject, float target, float time) 
	{
		float start = m_CurrentVolume;
		while (m_CurrentVolume < time)
		{

			m_CurrentVolume += Time.deltaTime;
			float val = Mathf.Lerp(start, target, m_CurrentVolume / time);
			m_AudioManager.SetVolume(fadeObject, val);
			yield return null;
		}
	}
	void StartFade(float target, float time) 
	{
		if (m_CurrentCoroutine != null)
			StopCoroutine(m_CurrentCoroutine);
		m_CurrentCoroutine = FadeMusic(m_MusicIdentifier, target, time);
	}

	void OnDestroy() 
	{
		m_Manager.RemoveFromLevelStarted(this);
		m_Manager.RemoveFromPauseUnpause(this);
	}

	public void LevelFinished()
	{
		StartFade(0.0f, 2.0f);
	}

	public void LevelStarted() 
	{
		m_AudioManager.Play(m_MusicIdentifier);
	}

	public void Pause()
	{
		StartFade(0.3f, 0.3f);
	}

	public void PlayerPerspectiveBegin() {}

	public void Unpause()
	{
		StartFade(1.0f, 0.3f);
	}
}
