using UnityEngine;
using System.Collections.Generic;
[RequireComponent(typeof(AudioManager))]
public class ParticleFXPlayer : MonoBehaviour
{
    [SerializeField] private List<SoundObject> m_SoundsToPlay;
	// Start is called before the first frame update
	private void Awake()
	{
		AudioManager audioManager = GetComponent<AudioManager>();
		for (int i = 0; i < m_SoundsToPlay.Count; i++) 
		{
			audioManager.PlayOneShot(m_SoundsToPlay[i]);
		}
	}
}
