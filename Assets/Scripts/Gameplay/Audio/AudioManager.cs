using UnityEngine;
using System.Collections.Generic;
using System;

public class AudioManager : MonoBehaviour
{
	[SerializeField] private SettingsManager m_Settings;
	[SerializeField] private SoundObject[] sounds;
	
	private readonly Dictionary<SoundObject, Sound> m_SoundDict = new Dictionary<SoundObject, Sound>();

	void Awake()
	{
		m_Settings.PropertyChanged += OnPropertyChanged;

		foreach (SoundObject sound in sounds) 
		{
			Sound newSound = new Sound(sound, gameObject.AddComponent<AudioSource>());
			m_SoundDict.Add(sound, newSound);
		}
	}

	public Sound GetSoundBySoundObject(SoundObject soundObject) 
	{
		return m_SoundDict[soundObject];
	}

	private void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "IsMuted")
		{
			System.Reflection.PropertyInfo info = sender.GetType().GetProperty(e.PropertyName);
			bool isMuted = (bool)info.GetValue(sender);

			foreach (Sound sound in m_SoundDict.Values)
			{
				sound.MuteSound(isMuted);
			}

		}
		else
		{
			foreach (Sound sound in m_SoundDict.Values)
			{
				if (!sound.GetAudioType.WasValidPropertyChanged(e.PropertyName))
					continue;
				sound.UpdateAudioVolume();
			}
		}
	}

	public static bool GetIsMuted(SettingsManager settings)
	{
		System.Reflection.PropertyInfo info = settings.GetType().GetProperty("IsMuted");
		return (bool)info.GetValue(settings);
	}

	public void Play(SoundObject soundIdentifier)
	{
		ApplyToSound(soundIdentifier, (Sound sound) => sound.Start());
	}

	public void SetPitch(SoundObject soundIdentifier, float newPitch) 
	{
		ApplyToSound(soundIdentifier, (Sound sound) => sound.SetPitch(newPitch));
	
	}

	public void SetVolume(SoundObject soundIdentifier, float volume) 
	{
		ApplyToSound(soundIdentifier, (Sound sound) => sound.SetVolume(volume));
	}

	public void StopPlaying(SoundObject soundIdentifier)
	{
		ApplyToSound(soundIdentifier, (Sound sound) => sound.Stop());
	}

	public void ApplyToSound(in SoundObject sound, in Action<Sound> soundAction) 
	{
		if (m_SoundDict.TryGetValue(sound, out Sound value))
		{
			soundAction.Invoke(value);
		}
		else
		{
			Debug.Log("Could not find sound with identifier " + sound + " in object " + gameObject.name, gameObject);
		}
	}

	private void OnDestroy()
	{
		m_Settings.PropertyChanged -= OnPropertyChanged;
	}
}


// needs to trigger off of an anim
// or also randomly?

[Serializable]
public abstract class SoundTrigger 
{
	private readonly Sound m_Sound;
	public SoundTrigger(Sound sound) 
	{
		m_Sound = sound;
	}

	protected void TriggerSound() 
	{
		m_Sound.Start();
	}
	public Sound GetSound => m_Sound;
	public abstract void Tick(float tickVal);
}

[Serializable]
public class RandomSoundTrigger : SoundTrigger
{
	[SerializeField] private float m_MinimumTime;
	[SerializeField] private float m_MaximumTime;

	private float m_CurrentTime = 0.0f;
	private float m_TriggerTime = 0.0f;

	public RandomSoundTrigger(float minTime, float maxTime, Sound sound) : base(sound) 
	{
		m_MinimumTime = minTime;
		m_MaximumTime = maxTime;

	}

	public void Reset() 
	{
		m_CurrentTime = 0.0f;
	}

	private void ResetTriggerTime() 
	{
		m_TriggerTime = UnityEngine.Random.Range(m_MinimumTime, m_MaximumTime);
	}

	public override void Tick(float tickVal)
	{
		m_CurrentTime += tickVal;
		if (m_CurrentTime < m_TriggerTime)
			return;
		ResetTriggerTime();
		TriggerSound();
		m_CurrentTime = 0.0f;
	}
}

public enum EdgeBehaviour
{
	RisingEdge = 1,
	FallingEdge = 2,
	Both = 3
}

[SerializeField]
public class EdgeTrigger : SoundTrigger 
{
	[SerializeField] private EdgeBehaviour m_EdgeBehaviour;
	private float m_LastValue = 0f;
	private int m_lastEdgeValue;
	private readonly AnimationCurve m_CurveTrigger;

	public EdgeTrigger(EdgeBehaviour edgeBehaviour, AnimationCurve curveTrigger, Sound sound) : base(sound)
	{
		m_EdgeBehaviour = edgeBehaviour;
		m_CurveTrigger = curveTrigger;
		m_LastValue = m_CurveTrigger.Evaluate(0);
	}

	private int GetEdgeBasedOnTime(in float time)
	{
		float val = m_CurveTrigger.Evaluate(time);
		int edgeValue = Mathf.FloorToInt( 2 + Mathf.Clamp(val - m_LastValue, -0.5f, 0.5f));
		m_LastValue = val;
		return edgeValue;
	}


	public override void Tick(float tickVal)
	{
		int edgeValue = GetEdgeBasedOnTime(tickVal);

		if (edgeValue == m_lastEdgeValue)
			return;

		m_lastEdgeValue = edgeValue;
		if (((int)m_EdgeBehaviour & m_lastEdgeValue) == 0)
			return;

		TriggerSound();
	}
}


[SerializeField]
public class ValueBasedEdgeTrigger : SoundTrigger 
{
	[SerializeField] private EdgeBehaviour m_EdgeBehaviour;
	[SerializeField] private float m_TriggerValue = 0f;
	private int m_lastEdgeValue;
	public ValueBasedEdgeTrigger(EdgeBehaviour edgeBehaviour, float value, Sound sound) : base(sound)
	{
		m_EdgeBehaviour = edgeBehaviour;
		m_TriggerValue = value;
		m_lastEdgeValue = GetEdgeBasedOnTime(0);
	}
	private int GetEdgeBasedOnTime(in float time)
	{
		return time > m_TriggerValue ? 1 : 2;
	}

	public override void Tick(float tickVal)
	{
		int edgeValue = GetEdgeBasedOnTime(tickVal);

		if (edgeValue == m_lastEdgeValue)
			return;

		m_lastEdgeValue = edgeValue;
		if (((int)m_EdgeBehaviour & m_lastEdgeValue) == 0)
			return;

		TriggerSound();
	}
}

[Serializable]
public class AnimEdgeValueTrigger : SoundTrigger
{
	[SerializeField] private EdgeBehaviour m_EdgeBehaviour;
	[SerializeField] private float m_TriggerValue = 0f;

	private readonly AnimationCurve m_CurveTrigger;
	private int m_lastEdgeValue;
	public AnimEdgeValueTrigger(EdgeBehaviour edgeBehaviour, AnimationCurve curveTrigger, Sound sound, float triggerValue) : base(sound) 
	{
		m_EdgeBehaviour = edgeBehaviour;
		m_TriggerValue = triggerValue;
		m_CurveTrigger = curveTrigger;
		m_lastEdgeValue = GetEdgeBasedOnTime(0);
	}

	private int GetEdgeBasedOnTime(in float time) 
	{
		return m_CurveTrigger.Evaluate(time) > m_TriggerValue ? 1 : 2;
	}

	public override void Tick(float tickVal)
	{
		int edgeValue = GetEdgeBasedOnTime(tickVal);

		if (edgeValue == m_lastEdgeValue)
			return;

		m_lastEdgeValue = edgeValue;
		if (((int)m_EdgeBehaviour & m_lastEdgeValue) == 0)
			return;

		TriggerSound();
	}
}


public class Sound 
{
	private readonly AudioSource m_AudioSource;
	private readonly AudioType m_AudioType;
	private readonly SoundObject m_SoundObject;
	private readonly float m_fDefaultVolume;
	private readonly float m_fDefaultPitch;

	private float m_fVolumeModifierInternal = 1.0f;
	private float m_fPitchModifierInternal = 1.0f;
	public Sound(in SoundObject soundObject, in AudioSource sourceToplayFrom) 
	{
		m_SoundObject = soundObject;
		m_AudioSource = sourceToplayFrom;
		m_AudioSource.clip = soundObject.clip;
		MuteSound(AudioManager.GetIsMuted(m_AudioType.GetViewModelAsSettingsManager()));

	}
	public void UpdateAudioVolume()
	{
		m_AudioSource.volume = m_AudioType.GetVolumeValModifier() * ( m_fDefaultVolume + (m_fVolumeModifierInternal - 1) * m_SoundObject.maxVolumeModifier);
	}

	public void MuteSound(bool mute)
	{
		if (mute)
		{
			m_AudioSource.volume = 0.0f;
		}
		else
		{
			UpdateAudioVolume();
		}
	}

	public void UpdateAudioPitch()
	{
		m_AudioSource.pitch = (m_fDefaultPitch + (m_fPitchModifierInternal - 1) * m_SoundObject.maxPitchModifier);
	}

	public void Start() 
	{
		m_AudioSource.Play();
	}

	public void Stop() 
	{
		m_AudioSource.Stop();
	}

	public AudioType GetAudioType => m_AudioType;

	public void SetPitch(in float pitchPercent) 
	{
		m_fPitchModifierInternal = pitchPercent;
		UpdateAudioPitch();
	}

	public void SetVolume(in float volumePercent) 
	{
		m_fVolumeModifierInternal = volumePercent;
		UpdateAudioVolume();
	}

	public float GetVolume()
	{
		return m_AudioSource.volume / m_fDefaultVolume;
	}
}
