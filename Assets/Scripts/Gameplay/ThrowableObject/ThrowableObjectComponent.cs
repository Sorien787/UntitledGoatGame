using UnityEngine;
using System;

[RequireComponent(typeof(FreeFallTrajectoryComponent))]
public class ThrowableObjectComponent : IThrowableObjectComponent, IHealthListener, IPauseListener
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

	public void Update()
	{
		
	}

	public void Awake()
	{
		m_CowGameManager.AddToPauseUnpause(this);
		m_FreeFallComponent.OnObjectNotInFreeFall += OnObjectLanded;
		if (TryGetComponent(out HealthComponent healthComponent))
		{
			healthComponent.AddListener(this);
		}
	}

	private void OnDestroy()
	{
		m_CowGameManager.RemoveFromPauseUnpause(this);
	}

	Vector3 m_cachedVelocity = Vector3.zero;
	bool m_bHadVelocity = false;

	public void Pause()
	{
		if (!m_ThrowingBody.isKinematic)
		{
			m_bHadVelocity = true;
			m_cachedVelocity = m_ThrowingBody.velocity;
			m_ThrowingBody.isKinematic = true;
		}
	}

	public void Unpause()
	{
		if (m_bHadVelocity) 
		{
			m_bHadVelocity = false;
			m_ThrowingBody.velocity = m_cachedVelocity;
			m_ThrowingBody.isKinematic = false;
		}
	}
}
