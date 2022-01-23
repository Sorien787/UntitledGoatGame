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
			AddSoundToDict(sound);
		}
	}

	private void AddSoundToDict(SoundObject sound) 
	{
		AudioSource source = gameObject.AddComponent<AudioSource>();
		source.loop = sound.loop;
		source.rolloffMode = AudioRolloffMode.Linear;
		source.maxDistance = sound.distance;
		source.playOnAwake = false;
		source.spatialBlend = sound.is3DSound ? 1.0f : 0.0f;
		Sound newSound = new Sound(sound, source);
		m_SoundDict.Add(sound, newSound);
	}

	public Sound GetSoundBySoundObject(SoundObject soundObject)
	{
		if (!m_SoundDict.ContainsKey(soundObject))
			AddSoundToDict(soundObject);
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

	public void PlayOneShot(SoundObject soundIdentifier) 
	{
		ApplyToSound(soundIdentifier, (Sound sound) => sound.PlayOneShot());
	}

	public void SetPitch(SoundObject soundIdentifier, float newPitch) 
	{
		ApplyToSound(soundIdentifier, (Sound sound) => { sound.SetPitch(newPitch); sound.UpdateAudioVolume(); });
	
	}

	public void SetVolume(SoundObject soundIdentifier, float volume) 
	{
		ApplyToSound(soundIdentifier, (Sound sound) => { sound.SetVolume(volume); sound.UpdateAudioVolume(); });
	}

	public void StopPlaying(SoundObject soundIdentifier)
	{
		ApplyToSound(soundIdentifier, (Sound sound) => sound.Stop());
	}

	public void ApplyToSound(in SoundObject sound, in Action<Sound> soundAction) 
	{
		Sound soundToPlay = GetSoundBySoundObject(sound);
		if (!m_SoundDict.ContainsKey(sound))
			AddSoundToDict(sound);
		soundAction.Invoke(soundToPlay);
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
	protected readonly Sound m_Sound;
	public SoundTrigger(Sound sound) 
	{
		m_Sound = sound;
	}
	public void SetVolumeModifier(float volumeModifier)
	{
		m_Sound.SetVolume(volumeModifier);
	}


	protected void TriggerSound() 
	{
		m_Sound.PlayOneShot();
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
		ResetTriggerTime();

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

public enum EdgeBehaviour : int
{
	FallingEdge = 1,
	RisingEdge = 2,
	Both = 3
}

[SerializeField]
public class EdgeTrigger : SoundTrigger 
{
	[SerializeField] private EdgeBehaviour m_EdgeBehaviour;
	private float m_LastValue = 0f;
	private EdgeBehaviour m_lastEdgeValue;
	private readonly AnimationCurve m_CurveTrigger;


	public EdgeTrigger(EdgeBehaviour edgeBehaviour, AnimationCurve curveTrigger, Sound sound) : base(sound)
	{
		m_EdgeBehaviour = edgeBehaviour;
		m_CurveTrigger = curveTrigger;
		m_LastValue = m_CurveTrigger.Evaluate(0);
		m_EdgeBehaviour = GetEdgeBasedOnTime(0.001f);
	}


	private EdgeBehaviour GetEdgeBasedOnTime(in float time)
	{
		float val = m_CurveTrigger.Evaluate(time);
		EdgeBehaviour edgeBehaviour = (val - m_LastValue > 0) ? EdgeBehaviour.RisingEdge : EdgeBehaviour.FallingEdge;
		m_LastValue = val;
		return edgeBehaviour;
	}


	public override void Tick(float tickVal)
	{
		EdgeBehaviour edgeValue = GetEdgeBasedOnTime(tickVal);

		if (edgeValue == m_lastEdgeValue)
			return;

		m_lastEdgeValue = edgeValue;
		if (((int)m_EdgeBehaviour & (int)m_lastEdgeValue) != 0)
			return;
	
		TriggerSound();
	}
}


[SerializeField]
public class ValueBasedEdgeTrigger : SoundTrigger 
{
	[SerializeField] private EdgeBehaviour m_EdgeBehaviour;
	[SerializeField] private float m_TriggerValue = 0f;
	private EdgeBehaviour m_lastEdgeValue;
	public ValueBasedEdgeTrigger(EdgeBehaviour edgeBehaviour, float value, Sound sound) : base(sound)
	{
		m_EdgeBehaviour = edgeBehaviour;
		m_TriggerValue = value;
		m_lastEdgeValue = GetEdgeBasedOnTime(0);
	}
	private EdgeBehaviour GetEdgeBasedOnTime(in float time)
	{
		return time > m_TriggerValue ? EdgeBehaviour.RisingEdge : EdgeBehaviour.FallingEdge;
	}

	public override void Tick(float tickVal)
	{
		EdgeBehaviour edgeValue = GetEdgeBasedOnTime(tickVal);
		if (edgeValue == m_lastEdgeValue)
			return;

		m_lastEdgeValue = edgeValue;
		if (((int)m_EdgeBehaviour & (int)edgeValue) == 0)
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

	private float m_fVolumeModifierInternal = 1.0f;
	private float m_fPitchModifierInternal = 1.0f;
	private float m_MutedModifer = 1.0f;

	public float GetVolume => m_fVolumeModifierInternal * m_MutedModifer;
	public Sound(in SoundObject soundObject, in AudioSource sourceToplayFrom) 
	{
		m_SoundObject = soundObject;
		m_AudioSource = sourceToplayFrom;
		m_AudioType = soundObject.m_AudioType;
		SettingsManager manager = m_AudioType.GetViewModelAsSettingsManager();
		bool isMuted = AudioManager.GetIsMuted(manager);
		MuteSound(isMuted);

	}
	public void UpdateAudioVolume()
	{
		m_AudioSource.volume = GetAudioVol();
	}

	private float GetAudioVol() 
	{
		float volumeValMod = m_AudioType.GetVolumeValModifier();// From game system
		float defaultVolume = m_SoundObject.defaultVolume;                   // never changes - static val
		float randomVolumeAddition = UnityEngine.Random.Range(-m_SoundObject.volRandomize, m_SoundObject.volRandomize);// random addition
		float volumeModifierInternal = (m_fVolumeModifierInternal - 1) * m_SoundObject.maxVolumeModifier;// volume modifier - left is percentage, right is amount the percentage affects
		return volumeValMod * defaultVolume * (1 + randomVolumeAddition + volumeModifierInternal);	
	}

	public void MuteSound(bool mute)
	{
		if (mute)
		{
			m_MutedModifer = 0.0f;
		}
		else
		{
			m_MutedModifer = 1.0f;	
		}
		UpdateAudioVolume();
	}

	public void UpdateAudioPitch()
	{
		m_AudioSource.pitch = GetAudioPitch();
	}

	private float GetAudioPitch() 
	{
		float defaultPitch = m_SoundObject.defaultPitch;
		float randomPitchAddition = UnityEngine.Random.Range(-m_SoundObject.pitchRandomize, m_SoundObject.pitchRandomize);
		float pitchModifierInternal = (m_fPitchModifierInternal - 1) * m_SoundObject.maxPitchModifier;
		return defaultPitch + randomPitchAddition + pitchModifierInternal;
	}

	public void Start() 
	{
		m_AudioSource.clip = m_SoundObject.GetAudioClip();
		m_AudioSource.volume = GetAudioVol();
		m_AudioSource.Play();
	}

	public void PlayOneShot() 
	{
		m_AudioSource.pitch = GetAudioPitch();
		float volume = GetAudioVol();
		m_AudioSource.PlayOneShot(m_SoundObject.GetAudioClip(), volume);
	}

	public void Stop() 
	{
		m_AudioSource.Stop();
	}

	public AudioType GetAudioType => m_AudioType;

	public void SetPitch(in float pitchPercent) 
	{
		m_fPitchModifierInternal = pitchPercent;
	}

	public void SetVolume(in float volumePercent) 
	{
		m_fVolumeModifierInternal = volumePercent;
	}
}
