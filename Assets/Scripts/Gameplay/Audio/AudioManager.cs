using UnityEngine;
using System.Collections.Generic;
using System;

public class AudioManager : MonoBehaviour
{
	[SerializeField] private SettingsManager m_Settings;
	[SerializeField] private SoundObject[] sounds;
	private float m_fCachedVolume;
	
	private readonly Dictionary<string, Sound> m_SoundDict = new Dictionary<string, Sound>();

	void Awake()
	{
		m_Settings.PropertyChanged += OnPropertyChanged;

		foreach (SoundObject sound in sounds) 
		{
			Sound newSound = new Sound(sound.m_AudioType, sound.defaultVolume, sound.defaultPitch, sound.loop, sound.clip, gameObject.AddComponent<AudioSource>());
			m_SoundDict.Add(sound.m_Identifier, newSound);
		}
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

	public void Play(string soundIdentifier)
	{
		ApplyToSound(soundIdentifier, (Sound sound) => sound.Start());
	}

	public void SetPitch(string soundIdentifier, float newPitch) 
	{
		ApplyToSound(soundIdentifier, (Sound sound) => sound.SetPitch(newPitch));
	
	}

	public void SetVolume(string soundIdentifier, float volume) 
	{
		m_fCachedVolume = volume;
		ApplyToSound(soundIdentifier, (Sound sound) => sound.SetVolume(volume));
	}

	public void StopPlaying(string soundIdentifier)
	{
		ApplyToSound(soundIdentifier, (Sound sound) => sound.Stop());
	}

	public void ApplyToSound(in string sound, in Action<Sound> soundAction) 
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

public class Sound 
{
	private readonly AudioClip m_AudioClip;
	private readonly AudioSource m_AudioSource;
	private readonly AudioType m_AudioType;
	private readonly float m_fDefaultVolume;
	private readonly float m_fDefaultPitch;

	private float m_fVolumeModifierInternal = 1.0f;
	private float m_fPitchModifierInternal = 1.0f;
	public Sound(in AudioType type, in float defaultVolume, in float defaultPitch, in bool doesLoop, in AudioClip clipToPlay, in AudioSource sourceToplayFrom) 
	{
		m_AudioType = type;
		m_AudioClip = clipToPlay;
		m_AudioSource = sourceToplayFrom;
		m_fDefaultPitch = defaultPitch;
		m_fDefaultVolume = defaultVolume;
		m_AudioSource.loop = doesLoop;
		m_AudioSource.clip = m_AudioClip;
		MuteSound(AudioManager.GetIsMuted(m_AudioType.GetViewModelAsSettingsManager()));

	}
	public void UpdateAudioVolume()
	{
		m_AudioSource.volume = (m_AudioType.GetVolumeValModifier() * m_fDefaultVolume * m_fVolumeModifierInternal);
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
		m_AudioSource.pitch = (m_fDefaultPitch * m_fPitchModifierInternal);
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
