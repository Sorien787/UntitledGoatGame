using System.Collections;
using UnityEngine;
using System;
[RequireComponent(typeof(HealthComponent))]
public class AbductableComponent : MonoBehaviour
{
    [Header("References to External GameObjects")]
    [SerializeField]
    private ParticleSystem m_AbductionParticleEffects;
    [SerializeField]
    private Transform m_AbductionParticleEffectsTransform;
    [SerializeField]
    private Transform m_CowGeometryTransform;
    [SerializeField]
    private HealthComponent m_HealthComponent;
    [SerializeField]
    private CowGameManager m_Manager;
    [SerializeField]
    private EntityTypeComponent m_TypeComponent;
    [Header("Animation Params")]
    [SerializeField]
    private float m_fAbductionResistance;
    [SerializeField]
    private float m_fRotationResistance;
    [SerializeField]
    private AnimationCurve m_OnAbductedAnimCurve;
    [SerializeField]
    private float m_OnAbductedAnimTime;


    private Rigidbody m_Body;

    private Transform m_Transform;

    private Vector3 m_ChosenRotationAxis;

    private bool m_bIsBeingAbducted;

    public Transform GetTransform => m_Transform;
    public Transform GetBodyTransform => m_CowGeometryTransform;
    public Rigidbody GetBody => m_Body;
    public float GetAbductionResistance => m_fAbductionResistance;
    public float GetRotationResitance => m_fRotationResistance;
    public Vector3 GetRotationAxis => m_ChosenRotationAxis;

    public event Action<UfoMain, AbductableComponent> OnStartedAbducting;
    public event Action<UfoMain, AbductableComponent> OnEndedAbducting;


    private void Awake()
    {
        if (m_AbductionParticleEffects)
        m_AbductionParticleEffects.Stop();
        m_Body = GetComponent<Rigidbody>();
        m_Transform = GetComponent<Transform>();
    }

    public void OnBeginAbducting(UfoMain ufo) 
    {
        m_AbductionParticleEffects.Play();
        m_bIsBeingAbducted = true;
        m_ChosenRotationAxis = UnityEngine.Random.onUnitSphere;
        OnStartedAbducting?.Invoke(ufo, this);
        m_Manager.GetTokenForEntity(m_TypeComponent, m_TypeComponent.GetEntityInformation).SetAbductionState(CowGameManager.EntityState.Abducted);
    }

    public Vector3 CurrentDesiredVelocity;

    private void OnDrawGizmos()
    {
        if (m_bIsBeingAbducted) 
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(GetBody.position, GetBody.position + CurrentDesiredVelocity.normalized * 5);
        }
    }


    public void SetAbductionEffectsQuat(in Quaternion quat)
    {
        m_AbductionParticleEffectsTransform.rotation = quat;
    }

    public void OnEndAbducting(UfoMain ufo) 
    {
        m_AbductionParticleEffects.Stop();
        m_bIsBeingAbducted = false;
        OnEndedAbducting?.Invoke(ufo, this);
        m_Manager.GetTokenForEntity(m_TypeComponent, m_TypeComponent.GetEntityInformation).SetAbductionState(CowGameManager.EntityState.Free);
    }

    public bool HasRegisteredAbduction => hasRegisteredAbduction;
    private float abductedTime = 0;
    private bool hasRegisteredAbduction = false;
    private Vector3 cachedInitialCowScale;
    private IEnumerator AbductionDeathAnimation() 
    {
        cachedInitialCowScale = m_CowGeometryTransform.localScale;
        while (abductedTime < m_OnAbductedAnimTime) 
        {
            m_CowGeometryTransform.localScale = cachedInitialCowScale * m_OnAbductedAnimCurve.Evaluate(abductedTime / m_OnAbductedAnimTime);
            abductedTime += Time.deltaTime;
            yield return null;
        }
        m_HealthComponent.OnTakeLethalDamage(DamageType.UFODamage);
    }

    public void OnAbducted() 
    {
        hasRegisteredAbduction = true;
        StartCoroutine(AbductionDeathAnimation());
    }
}
