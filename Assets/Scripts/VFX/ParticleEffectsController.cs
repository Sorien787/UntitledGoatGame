using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ParticleEffectsController : MonoBehaviour
{
	[SerializeField]
	private bool m_bShouldStartInitialized = false;
	[SerializeField]
	private List<ParticleSystem> m_ParticleSystems;

	private Transform m_Transform;
	private void Awake()
	{
		m_Transform = transform;
		if (!m_bShouldStartInitialized)
			IterateParticleSystems((ParticleSystem particleSystem) => { particleSystem.Stop(); });
	}
	public void IterateParticleSystems(Action<ParticleSystem> ParticleAction) 
	{
		foreach (ParticleSystem particleSystem in m_ParticleSystems)
			ParticleAction(particleSystem);
	}

	public void SetWorldPos(Vector3 pos) 
	{
		m_Transform.position = pos;
	}

	public void SetLookDirection(Vector3 lookDirection) 
	{
		m_Transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
	}

	public void TurnOffAllSystems() 
	{
		IterateParticleSystems((ParticleSystem particleSystem) => { particleSystem.Stop(); });
	}

	public void PlayOneShot() 
	{
		IterateParticleSystems((ParticleSystem particleSystem) => 
		{ 
			particleSystem.Stop();
			particleSystem.Play();
		});
	}

	public void TurnOnAllSystems() 
	{
		IterateParticleSystems((ParticleSystem particleSystem) => { particleSystem.Play(); });
	}
}
