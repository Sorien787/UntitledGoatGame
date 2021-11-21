using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerComponent : MonoBehaviour, IPauseListener, IHealthListener
{
	[Header("Object References")]
	[SerializeField] private CowGameManager m_GameManager;
	[SerializeField] private HealthComponent m_HealthComponent;
	[SerializeField] private LassoInputComponent m_LassoComponent;
	[SerializeField] private Transform m_CamContainer;
	[SerializeField] private BoxCollider m_GrapplingBufferCollider;
	[SerializeField] private LassoInputComponent m_LassoInput;

	[Header("Object References")]
	[SerializeField] private LayerMask m_OnThrowLayer;
	[SerializeField] private float m_GrabDistance;

	[Header("Control Bindings")]
	[SerializeField] private ControlBinding m_GrabBinding;


	private void Start()
	{
		m_GameManager.AddToPauseUnpause(this);
		m_LassoComponent.OnSetPullingObject += (ThrowableObjectComponent throwable) => OnStartGrappling();
		m_LassoComponent.OnStoppedPullingObject += OnStopGrappling;
		m_HealthComponent.AddListener(this);
		m_GrapplingBufferCollider.enabled = false;
	}

	private void Update()
	{
		// Add validation to the control bindings - essentially a "Type" (or control token?) that it can respond to/is valid for
		// which can live in the game manager?
		// I.E, ControlToken in here and in lasso
		// by default, lasso control token is active
		// this switches when lasso is idle and there's something to grab

		if (!m_LassoInput.IsInIdle)
			return;
		if (!m_GrabBinding.GetBindingDown())
			return;
		if (!Physics.Raycast(m_CamContainer.position, m_CamContainer.forward, out RaycastHit hit, m_GrabDistance, m_OnThrowLayer, QueryTriggerInteraction.Ignore))
			return;
		if (hit.collider.gameObject.TryGetComponent(out ThrowableObjectComponent throwableObject))
			return;
		if (!throwableObject.IsImmediatelyThrowable)
			return;
		m_LassoInput.OnImmediatelySpinObject(throwableObject);

	}

	public void Pause() 
	{
		enabled = false;
	}

	public void Unpause() 
	{
		enabled = true;
	}

	private void OnStartGrappling() 
	{
		m_GrapplingBufferCollider.enabled = true;
	}

	private void OnStopGrappling() 
	{
		m_GrapplingBufferCollider.enabled = false;
	}

	public void OnEntityTakeDamage(GameObject go1, GameObject go2, DamageType type)
	{

	}

	public void OnEntityDied(GameObject go1, GameObject go2, DamageType type)
	{
		m_GameManager.OnPlayerKilled();
	}
}
