﻿using UnityEngine;

public abstract class AttackBase : MonoBehaviour
{
    public abstract void AttackTarget(in GameObject target, in Vector3 direction);

	[SerializeField] private AnimationCurve m_AttackPitchAnimationCurve;
	[SerializeField] private AnimationCurve m_AttackForwardAnimationCurve;
	[SerializeField] private AnimationCurve m_AttackHopAnimationCurve;
	[SerializeField] private AnimationCurve m_AttackTiltAnimationCurve;

	[Range(0f, 3f)][SerializeField] private float m_Duration = 1.0f;
	[Range(0f, 3f)] [SerializeField] private float m_AttackRange = 1.0f;
	[Range(0f, 3f)] [SerializeField] private float m_AttackCooldownTime = 1.0f;
	[Range(0f, 1f)] [SerializeField] private float m_AttackTriggerTime = 0.3f;

	public AnimationCurve GetPitchCurve => m_AttackPitchAnimationCurve;
	public AnimationCurve GetForwardCurve => m_AttackForwardAnimationCurve;
	public AnimationCurve GetHopCurve => m_AttackHopAnimationCurve;
	public AnimationCurve GetTiltCurve => m_AttackTiltAnimationCurve;
	public float GetAttackRange => m_AttackRange;
	public float GetAttackCooldownTime => m_AttackCooldownTime;
	public float GetAttackDuration => m_Duration;
	public float GetAttackTriggerTime => m_AttackTriggerTime;
}
