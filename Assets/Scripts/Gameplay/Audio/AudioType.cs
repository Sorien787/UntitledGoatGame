using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName ="AudioType")]
public class AudioType : ScriptableObject
{
	[SerializeField] private string m_AudioTypeIdentifier;

	public string GetAudioTypeIdentifier;
}
