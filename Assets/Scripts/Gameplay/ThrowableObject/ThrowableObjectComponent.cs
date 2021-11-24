using UnityEngine;
using System;

[RequireComponent(typeof(FreeFallTrajectoryComponent))]
public class ThrowableObjectComponent : IThrowableObjectComponent, IHealthListener
{
    [SerializeField] private Rigidbody m_ThrowingBody;
	[SerializeField] private Transform m_CameraFocusTransform;
	[SerializeField] private Transform m_AttachmentTransform;
	[SerializeField] private Transform m_MainTransform;
	[SerializeField] private float m_fMassMultiplier = 1.0f;
	[SerializeField] protected FreeFallTrajectoryComponent m_FreeFallComponent;

	public event Action OnDestroyed;
	public bool IsImmediatelyThrowable { get; set; } = false;
	public override Transform GetCameraFocusTransform => m_CameraFocusTransform;
	public override Transform GetAttachmentTransform => m_AttachmentTransform;
	public override Transform GetMainTransform => m_MainTransform;

	public override void ThrowObject(in ProjectileParams pParams)
	{
		m_FreeFallComponent.ThrowObject(pParams);
		base.ThrowObject(pParams);
	}

	public override float GetMass()
	{
		return m_ThrowingBody.mass * m_fMassMultiplier;
	}

	public override void ApplyForceToObject(Vector3 force)
	{
		m_ThrowingBody.AddForce(force, ForceMode.Impulse);
	}

	public void OnEntityTakeDamage(GameObject go1, GameObject go2, DamageType type)
	{

	}

	public void OnEntityDied(GameObject go1, GameObject go2, DamageType type)
	{
		OnDestroyed?.Invoke();
	}


	protected override void Awake()
	{
		base.Awake();
		m_FreeFallComponent.OnObjectNotInFreeFall += OnObjectLanded;
		m_FreeFallComponent.OnObjectHitGround += CollisionEvent;
		if (TryGetComponent(out HealthComponent healthComponent))
		{
			healthComponent.AddListener(this);
		}
	}

	Vector3 m_cachedVelocity = Vector3.zero;
	bool m_bHadVelocity = false;
}
