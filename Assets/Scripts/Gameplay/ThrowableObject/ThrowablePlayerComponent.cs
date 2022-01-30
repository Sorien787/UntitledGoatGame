using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class ThrowablePlayerComponent : IThrowableObjectComponent
{
	[SerializeField] private float m_ObjectMass;

	public override Transform GetCameraFocusTransform => transform;

	public override Transform GetAttachmentTransform => transform;

	public override Transform GetMainTransform => transform;

	private bool m_bWasThrown = false;

	public Action OnPlayerCanUseLasso;

	public override void ThrowObject(in ProjectileParams pParams)
	{
		base.ThrowObject(pParams);
		StartCoroutine(OnThrownRoutine());
	}

	private IEnumerator OnThrownRoutine() 
	{
		yield return new WaitForSeconds(0.2f);
		OnPlayerCanUseLasso();
	}

	public override void ApplyForceToObject(Vector3 force)
	{
		return;
	}

	public override float GetMass()
	{
		return m_ObjectMass;
	}
}
