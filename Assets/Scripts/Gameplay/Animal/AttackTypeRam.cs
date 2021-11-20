using UnityEngine;
using EZCameraShake;

public class AttackTypeRam : AttackBase
{
	[SerializeField] private float m_RamForce;
	[SerializeField] private float m_MinimumElevationAngle;
	[SerializeField] private float m_RamFXStrength = 0.5f;
	[SerializeField] private Transform m_RamPoint;
	[SerializeField] private GameObject m_RamFXPrefabs;
	[SerializeField] private PhysicalEntity m_AnimalEntity;
	public override void AttackTarget(in GameObject target, in Vector3 attackDirection)
	{
		//m_AnimalEntity.GetGroundedPos;
		//m_AnimalEntity.GetGroundedNorm;
		//m_


		IThrowableObjectComponent throwableComponent = target.GetComponent<IThrowableObjectComponent>();
		Vector3 planeOfAttackNormal = Vector3.Cross(attackDirection, Vector3.up);
		Vector3 rammingOffset = (throwableComponent.GetMainTransform.position - m_RamPoint.position).normalized;
		Vector3 rammingDirection = Vector3.ProjectOnPlane(rammingOffset, planeOfAttackNormal);

		Vector3 rammingDirectionPlanar = Vector3.ProjectOnPlane(rammingDirection, Vector3.up);
		Vector3 attackDirectionPlanar = Vector3.ProjectOnPlane(attackDirection, Vector3.up);
		float dot = Vector3.Dot(rammingDirectionPlanar, attackDirectionPlanar);

		if (dot < 0) 
		{
			rammingDirection = Vector3.up;
		}

		if (Vector3.Angle(rammingDirection, rammingDirectionPlanar) < m_MinimumElevationAngle) 
		{
			rammingDirection = Quaternion.AngleAxis(m_MinimumElevationAngle, planeOfAttackNormal) * rammingDirectionPlanar;
		}

		GameObject ramFXObject = Instantiate(m_RamFXPrefabs, m_RamPoint.position, Quaternion.LookRotation(rammingDirection), null);
		ramFXObject.GetComponent<ImpactEffectStrengthManager>().SetParamsOfObject(m_RamFXStrength);
		if (target.TryGetComponent(out PlayerComponent _)) 
		{
			CameraShaker.Instance.ShakeOnce(5.0f, 5.0f, 0.1f, 1.0f);
		}
		ProjectileParams throwParams = new ProjectileParams(m_RamForce, rammingDirection, throwableComponent.GetMainTransform.position, 0);
		target.GetComponent<FreeFallTrajectoryComponent>().ThrowObject(throwParams);
		target.GetComponent<IThrowableObjectComponent>().ThrowObject(throwParams);
	}
}
