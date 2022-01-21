using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "New Sound Object", fileName = "New Sound Object")]
public class SoundObject : ScriptableObject
{
	public AudioClip GetAudioClip() 
	{
		if (m_clips.Count == 0)
			return null;
		int val = UnityEngine.Random.Range(0, m_clips.Count - 1);
		return m_clips[val];
	}

	[SerializeField] private List<AudioClip> m_clips;

	public AudioType m_AudioType;

	[Range(0.1f, 4f)]
	public float defaultVolume = 1.0f;

	[Range(0.1f, 4f)]
	public float defaultPitch = 1.0f;

	[Range(0.1f, 4f)]
	public float maxPitchModifier = 1.0f;

	[Range(0.1f, 4f)]
	public float maxVolumeModifier = 1.0f;

	[Range(0.0f, 1f)]
	public float pitchRandomize = 0.0f;

	[Range(0.0f, 1f)]
	public float volRandomize = 0.0f;

	[Range(0.0f, 100f)]
	public float distance = 20f;

	public bool loop = false;
}

[System.Serializable]
public class ClipWeight
{
	[SerializeField] private AudioClip m_AudioClip;

	[SerializeField] private uint m_RelativeWeight;
}