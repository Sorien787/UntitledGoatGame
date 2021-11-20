using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TractorBeamComponent : MonoBehaviour
{
    [SerializeField]
    private Transform m_Transform;
    
    [SerializeField]
    private Collider m_BeamCollider;

    [SerializeField]
    private CapsuleCollider m_CapsuleCollider;

    [SerializeField]
    private MeshRenderer m_MeshRenderer;

    [SerializeField] private CowGameManager m_Manager;

    [SerializeField]
    private TractorBeamEndComponent m_TractorBeamEnd;

    [SerializeField]
    private ParticleSystem m_GroundCubesParticleSystem;

    [SerializeField]
    private ParticleSystem m_MovementStreaksParticleSystem;

    [SerializeField]
    private AnimationCurve m_AccelerationDirectionHorizontalityCurve;

    [SerializeField]
    private AnimationCurve m_AccelerationMagnitudeHorizontalityCurve;

    [SerializeField]
    private float m_TargetAbductionVelocity;

    [SerializeField]
    private bool m_bDebugTractorBeamState;

    [SerializeField]
    private float m_TargetRotationVelocity;

    [SerializeField]
    private float m_AngularAcceleration;

    [SerializeField]
    private float m_AbductionRadius;

    public event Action OnTractorBeamFinished;

    private readonly List<AbductableComponent> m_Abducting = new List<AbductableComponent>();

    private bool m_bTractorBeamState;

    private IEnumerator AbductingCoroutine;

    private void OnValidate()
    {
        EvaluateParams();
    }

    private UfoMain ufo;

    public void SetParent(UfoMain ufo) 
    {
        this.ufo = ufo;
    }

    private void OnPaused() 
    {
        m_bIsPaused = true;
    }

    private void OnUnpaused() 
    {
        m_bIsPaused = false;
    }

    private void EvaluateParams() 
    {
        if (AbductingCoroutine == null) 
        {
            AbductingCoroutine = Abducting();
        }
        if (m_bDebugTractorBeamState != m_bTractorBeamState)
        {
            if (m_bDebugTractorBeamState) 
            {
                OnBeginTractorBeam();
            }
            else 
            {
                OnStopTractorBeam();
            }
            m_bTractorBeamState = m_bDebugTractorBeamState;
        }
    }

    private void Awake()
    {
        m_MeshRenderer.enabled = false;
        m_BeamCollider.enabled = false;
        m_GroundCubesParticleSystem.Stop();
        m_MovementStreaksParticleSystem.Stop();
        m_TractorBeamEnd.OnAbductableAbducted += OnObjectAbducted;
        EvaluateParams();
    }

    public void OnBeginTractorBeam() 
    {
        m_BeamCollider.enabled = true;
        m_MeshRenderer.enabled = true;
        SetParticlesActive();
        AbductingCoroutine = Abducting();
        m_abductingTime = 1.0f;
        StartCoroutine(AbductingCoroutine);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(m_Transform.position, m_AbductionRadius);
    }

    public void OnStopTractorBeam() 
    {
        m_MeshRenderer.enabled = false;
        m_BeamCollider.enabled = false;
        SetParticlesInactive();
        StopCoroutine(AbductingCoroutine);
    }

    private void SetParticlesActive() 
    {
        m_GroundCubesParticleSystem.Play();
        m_MovementStreaksParticleSystem.Play();
    }

    private void SetParticlesInactive() 
    {
        m_GroundCubesParticleSystem.Stop();
        m_MovementStreaksParticleSystem.Stop();
    }

    private void OnObjectAbducted(AbductableComponent abductable) 
    {
        m_Abducting.Remove(abductable);
        abductable.OnAbducted();
    }

    private void StartAbductingObject(in AbductableComponent abductable) 
    {
        m_Abducting.Add(abductable);
        abductable.OnBeginAbducting(ufo);
    }

    private void StopAbductingObject(in AbductableComponent abductable) 
    {
        m_Abducting.Remove(abductable);
        abductable.OnEndAbducting(ufo);
    }


    private void OnTriggerEnter(Collider other)
    {
        AbductableComponent abductable = other.GetComponentInParent<AbductableComponent>();
        if (abductable) 
        {
            StartAbductingObject(abductable);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        AbductableComponent abductable = other.GetComponentInParent<AbductableComponent>();
        if (abductable)
        {
            StopAbductingObject(abductable);
        }
    }

    private float GetDistanceFromAbductionAxis(in Transform abductableTransform) 
    {
        Vector3 axis = -m_Transform.up;
        Vector3 offset = abductableTransform.position - m_Transform.position;
        return Vector3.Magnitude(offset - Vector3.Dot(axis, offset) * axis);
    }

    private Vector3 GetDirectionFromAbductionAxis(in Transform abductableTransform) 
    {
        Vector3 axis = -m_Transform.up;
        Vector3 offset = abductableTransform.position - m_Transform.position;
        return (offset - Vector3.Dot(axis, offset) * axis).normalized;
    }
    float m_abductingTime = 0.0f;
    bool m_bIsPaused = false;
    public IEnumerator Abducting() 
    {
        m_bTractorBeamState = true;
        while (m_bDebugTractorBeamState || m_abductingTime > 0.0f) 
        {
            if (m_bIsPaused)
                yield return new WaitForFixedUpdate();
            if (m_Abducting.Count > 0)
                m_abductingTime = 1.0f;
            else
                m_abductingTime -= Time.fixedDeltaTime;
            for (int i = 0; i < m_Abducting.Count; i++) 
            {
                float scaledDistanceFromAbductionAxis = Mathf.Clamp(GetDistanceFromAbductionAxis(m_Abducting[i].GetTransform) / m_AbductionRadius, 0, 1);
                float desiredVelocityAngleFromAbductionAxis = m_AccelerationDirectionHorizontalityCurve.Evaluate( scaledDistanceFromAbductionAxis) * 90 * Mathf.Deg2Rad;
                float allowedAcceleration = m_AccelerationMagnitudeHorizontalityCurve.Evaluate(scaledDistanceFromAbductionAxis);
                // distance out from central axis of the tractor beam
                Vector3 outDir = GetDirectionFromAbductionAxis(m_Abducting[i].transform);

                Vector3 desiredVelocity = (m_Transform.up * Mathf.Cos(desiredVelocityAngleFromAbductionAxis) + -outDir * Mathf.Sin(desiredVelocityAngleFromAbductionAxis)).normalized * m_TargetAbductionVelocity;
                Vector3 desiredVelocityDifference = desiredVelocity - m_Abducting[i].GetBody.velocity;
                float accelerationMagnitudeDelta = Mathf.Min(Time.fixedDeltaTime * allowedAcceleration, desiredVelocityDifference.magnitude);
                Vector3 accelerationDelta = desiredVelocityDifference.normalized * accelerationMagnitudeDelta;
                float resistiveMagnitudeDelta = m_Abducting[i].GetBody.velocity.sqrMagnitude * m_Abducting[i].GetAbductionResistance * Time.fixedDeltaTime;
                Vector3 resistiveDeccelerationDelta = -m_Abducting[i].GetBody.velocity.normalized * resistiveMagnitudeDelta;

                m_Abducting[i].GetBody.AddForce(accelerationDelta + resistiveDeccelerationDelta, ForceMode.VelocityChange);

                Vector3 desiredAngularVelocity = m_Abducting[i].GetRotationAxis * m_TargetRotationVelocity;
                Vector3 desiredAngularVelocityDifference = desiredAngularVelocity - m_Abducting[i].GetBody.angularVelocity;
                float angularAccelerationMagnitudeDelta = Mathf.Clamp(desiredAngularVelocityDifference.magnitude, - Time.fixedDeltaTime * m_AngularAcceleration, Time.fixedDeltaTime * m_AngularAcceleration);
                Vector3 angularAccelerationDelta = desiredAngularVelocityDifference.normalized * angularAccelerationMagnitudeDelta;
                float resistiveAngularMagnitudeDelta = m_Abducting[i].GetBody.angularVelocity.sqrMagnitude * m_Abducting[i].GetRotationResitance * Time.fixedDeltaTime;
                Vector3 resistiveAngularDeccelerationDelta = -m_Abducting[i].GetBody.angularVelocity.normalized * resistiveAngularMagnitudeDelta;

                m_Abducting[i].GetBody.AddTorque(angularAccelerationDelta + resistiveAngularDeccelerationDelta, ForceMode.VelocityChange);

                m_Abducting[i].CurrentDesiredVelocity = desiredVelocity;
                m_Abducting[i].SetAbductionEffectsQuat(m_Transform.rotation);
            }
            yield return new WaitForFixedUpdate();
        }
        SetParticlesInactive();
        yield return new WaitForSecondsRealtime(1.0f);
        OnTractorBeamFinished?.Invoke();
        m_bTractorBeamState = false;
    }
}
