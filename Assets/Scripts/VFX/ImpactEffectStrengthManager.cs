using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImpactEffectStrengthManager : MonoBehaviour
{
    [SerializeField] private Animator m_ParticleAnimation;
	[SerializeField] [Range(0f, 1f)] private float m_Multiplier; 
	private void OnValidate()
	{
		if (m_ParticleAnimation) 
		{
			SetParamsOfObject(m_Multiplier);
		}
	}
	public void SetParamsOfObject(in float multiplier) 
    {
        m_ParticleAnimation.SetFloat("ParticleSize", multiplier);
    }
}
