using System;
using UnityEngine;

public class AttackTypeDamage : AttackBase
{
	[Range(0f, 5f)] [SerializeField] private float m_DamageAmount;
	[SerializeField] private Transform m_AttackPoint;

	public event Action<float, GameObject> OnDamagedTarget;
	public override void AttackTarget(in GameObject target, in Vector3 direction)
	{
		HealthComponent healthComponent = target.GetComponent<HealthComponent>();
		healthComponent.TakeDamageInstance(gameObject, DamageType.PredatorDamage, m_DamageAmount);
		GameObject attackFXObject = Instantiate(healthComponent.GetDamagedParticleType(), m_AttackPoint.position, m_AttackPoint.rotation, null);
		OnDamagedTarget?.Invoke(m_DamageAmount, target);
	}
}
