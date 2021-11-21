using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class IThrowableObjectComponent : MonoBehaviour
{
    [SerializeField] private float m_GravityMultiplier = 1;
    [SerializeField] protected CowGameManager m_CowGameManager = null;
    [SerializeField] private bool m_CausesHeavyImpact = false;

    public event Action OnTuggedByLasso;
    public event Action OnStartSpinning;
    public event Action OnWrangled;
    public event Action OnReleased;
    public event Action OnLanded;
    public event Action<ProjectileParams> OnThrown;

    public abstract float GetMass();

    public float GetGravityMultiplier => m_GravityMultiplier;

    public abstract Transform GetCameraFocusTransform { get; }

	public virtual void ThrowObject(in ProjectileParams pParams)
    {
        OnThrown?.Invoke(pParams);
    }

    protected void OnObjectLanded() 
    {
        OnLanded?.Invoke();
        if (m_CausesHeavyImpact) 
        {

        }
    }

    public void TuggedByLasso() { OnTuggedByLasso?.Invoke(); }

    public void StartedSpinning() { OnStartSpinning?.Invoke(); }

    public void Released() { OnReleased?.Invoke(); }

    public void Wrangled() { OnWrangled?.Invoke(); }

    public abstract Transform GetAttachmentTransform { get; }

    public abstract Transform GetMainTransform { get; }

    public abstract void ApplyForceToObject(Vector3 force);
}
