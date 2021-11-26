using UnityEngine;
using EZCameraShake;

public class AttackTypeRam : AttackBase
{
	[SerializeField] private float m_RamForce;
	[SerializeField] private float m_MinimumElevationAngle;
	[SerializeField] private float m_RamFXStrength = 0.5f;
	[SerializeField] private float m_LowAngVel = 160f;
	[SerializeField] private float m_HighAngVel = 360f;
	[SerializeField] private Transform m_RamPoint;
	[SerializeField] private Transform m_RamFXPoint;
	[SerializeField] private GameObject m_RamFXPrefabs;
	[SerializeField] private PhysicalEntity m_AnimalEntity;
	public override void AttackTarget(in GameObject target, in Vector3 attackDirection)
	{

		IThrowableObjectComponent throwableComponent = target.GetComponent<IThrowableObjectComponent>();

		// We've got the attack direction - let's turn it into a plane of attack
		Vector3 planeOfAttackNormal = Vector3.Cross(attackDirection, Vector3.up);
		Vector3 rammingOffset = (throwableComponent.GetMainTransform.position - m_RamPoint.position).normalized;
		// Now our actual ram direction is based on the ramming offset within that plane
		Vector3 rammingDirection = Vector3.ProjectOnPlane(rammingOffset, planeOfAttackNormal);

		Vector3 rammingDirectionPlanar = Vector3.ProjectOnPlane(rammingDirection, Vector3.up);
		Vector3 attackDirectionPlanar = Vector3.ProjectOnPlane(attackDirection, Vector3.up);

		// if the ram direction is opposite to attack direction, I.E object has moved behind
		float dot = Vector3.Dot(rammingDirectionPlanar, attackDirectionPlanar);
		if (dot < 0) 
		{
			// let's just hit us straight upwards
			rammingDirection = Vector3.up;
		}

		// make sure we're hitting at a minimum angle above the ground 
		Vector3 groundNorm = m_AnimalEntity.GetGroundedNorm;
		Vector3 groundDirectionRamming = Vector3.ProjectOnPlane(rammingDirection, groundNorm);

		float angleFromUpFromGroundDirection = Vector3.Angle(Vector3.up, groundDirectionRamming);
		float angleFromUpFromRammingDirection = Vector3.Angle(Vector3.up, rammingDirection);

		float elevationAngle = angleFromUpFromGroundDirection - angleFromUpFromRammingDirection;

		// if elevation angle lower than minimum, then let's go a bit above that :) 
		if (elevationAngle < m_MinimumElevationAngle) 
		{
			rammingDirection = Quaternion.AngleAxis(m_MinimumElevationAngle, planeOfAttackNormal) * groundDirectionRamming;
		}


		GameObject ramFXObject = Instantiate(m_RamFXPrefabs, m_RamFXPoint.position, Quaternion.LookRotation(rammingDirection), null);
		ramFXObject.GetComponent<ImpactEffectStrengthManager>().SetParamsOfObject(m_RamFXStrength);
		if (target.TryGetComponent(out PlayerComponent _)) 
		{
			CameraShaker.Instance.ShakeOnce(5.0f, 5.0f, 0.1f, 1.0f);
		}


		ProjectileParams throwParams = new ProjectileParams(m_RamForce, rammingDirection, throwableComponent.GetMainTransform.position, UnityEngine.Random.Range(m_LowAngVel, m_HighAngVel));
		target.GetComponent<IThrowableObjectComponent>().ThrowObject(throwParams);
	}
}
