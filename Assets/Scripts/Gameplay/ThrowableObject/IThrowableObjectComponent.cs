using UnityEngine;
using System;
using EZCameraShake;

public abstract class IThrowableObjectComponent : MonoBehaviour
{
    [Header("Internal References")]
    [SerializeField] private ImpactEffectStrengthManager m_ParticleStrength;
    [SerializeField] private ParticleEffectsController m_DragFX;
    [SerializeField] private Transform m_Transform;
    [SerializeField] private PhysicalEntity m_Entity;

    [Header("Animation Settings")]
    [SerializeField] private AnimationCurve m_DragAnimationCurve;
    [SerializeField] private AnimationCurve m_ImpactMagnitudeByImpactMomentum;
    [SerializeField] private AnimationCurve m_HazardRadiusByImpactMomentum;
    [SerializeField] private AnimationCurve m_HazardLifetimeByImpactMomentum;

    [Header("External References")]
    [SerializeField] private GameObject m_HazardRef = null;
    [SerializeField] private CowGameManager m_Manager;
    [SerializeField] private GameObject m_GroundImpactEffectsPrefab;

    [Header("Settings Parameters")]
    [SerializeField] private float m_DelayBetweenImpacts = 0.3f;
    [SerializeField] private bool m_CausesDragging = false;
    [SerializeField] private bool m_CausesImpacts = false;
    [SerializeField] [Range(0.1f, 5f)] private float m_GravityMultiplier = 1.0f;

    public event Action OnTuggedByLasso;
    public event Action OnStartSpinning;
    public event Action OnWrangled;
    public event Action OnReleased;
    public event Action<ProjectileParams> OnThrown;

    private bool m_bParticleFXActive = false;
    private bool m_bIsWrangled = false;
    private float m_SpeedForDragFX;
    private float m_MomentumForImpactFX;
    private Transform m_DragFXTransform;

    virtual protected void Awake()
	{
        if (m_DragFX) 
        {
            m_DragFXTransform = m_DragFX.transform;
        }

        if (m_CausesImpacts)
            if (m_ImpactMagnitudeByImpactMomentum.keys.Length == 0) 
            {
            Debug.Log("There is no key(s) in Impact Animation Curve!", gameObject);
            }
		    else 
            {
            m_MomentumForImpactFX =  m_ImpactMagnitudeByImpactMomentum.keys[0].time;
            }

        if (m_CausesDragging)
            if (m_DragAnimationCurve.keys.Length == 0) 
            {
                Debug.Log("There is no key(s) in Drag Animation Curve!", gameObject);
            }
		    else 
            {
                m_SpeedForDragFX = m_DragAnimationCurve.keys[0].time;
            }
    }

    public void EnableImpacts(bool isEnabled) 
    {
        m_bImpactsDisabled = !isEnabled;
    }

    private bool m_bImpactsDisabled = false;

    float m_fImpactFXCooldown = 0.0f;

    virtual protected void Update() 
    {
        m_fImpactFXCooldown = Mathf.Max(0.0f, m_fImpactFXCooldown - Time.deltaTime);

        if (!m_CausesDragging)
            return;
        bool m_bShouldParticleFXBeActive = m_Entity.GetVelocity.sqrMagnitude > (m_SpeedForDragFX * m_SpeedForDragFX) && m_Entity.IsGrounded;
        if (m_bShouldParticleFXBeActive != m_bParticleFXActive)
        {
            m_bParticleFXActive = m_bShouldParticleFXBeActive;
            if (m_bParticleFXActive)
            {
                m_DragFX.TurnOnAllSystems();
            }
            else
            {
                m_DragFX.TurnOffAllSystems();
            }
        }
        if (m_bParticleFXActive)
        {
            m_ParticleStrength.SetParamsOfObject(m_DragAnimationCurve.Evaluate(m_Entity.GetVelocity.magnitude));
            m_DragFXTransform.position = m_Entity.GetGroundedPos;
            m_DragFXTransform.rotation = Quaternion.LookRotation(m_Entity.GetGroundedNorm);
        }
    }

    public virtual void ThrowObject(in ProjectileParams pParams)
    {
        OnThrown?.Invoke(pParams);
    }
    
	private void OnCollisionEnter(Collision collision)
	{
        CollisionEvent(collision);
    }

    protected void CollisionEvent(Collision collision) 
    {
        if (!m_CausesImpacts || m_bImpactsDisabled)
            return;
        Vector3 normal = collision.GetContact(0).normal;
        Quaternion rotation = Quaternion.LookRotation(Vector3.Cross(Vector3.up, normal), normal);
        OnObjectHitOtherWithMomentum(m_Entity.GetVelocity.magnitude * m_Entity.GetMass, collision.GetContact(0).point, rotation);
    }

    private void OnObjectHitOtherWithMomentum(float momentum, Vector3 position, Quaternion rotation) 
    {
        if (!m_CausesImpacts || momentum < m_MomentumForImpactFX || m_bIsWrangled || m_bImpactsDisabled)
            return;
        CreateImpactAtPosition(momentum, position, rotation);
    }

    private void CreateImpactAtPosition(float momentum, Vector3 position, Quaternion rotation) 
    {
        if (m_fImpactFXCooldown > Mathf.Epsilon)
            return;

        m_fImpactFXCooldown = m_DelayBetweenImpacts;

        float shakeStrength = Mathf.Clamp(momentum / Mathf.Sqrt((m_Transform.position - m_Manager.GetPlayer.transform.position).magnitude) / 10, 3, 200);
        CameraShaker.Instance.ShakeOnce(shakeStrength, shakeStrength, 0.1f, 1.0f);

        GameObject impactObject = Instantiate(m_GroundImpactEffectsPrefab, position, rotation);
        impactObject.GetComponent<ImpactEffectStrengthManager>().SetParamsOfObject(m_ImpactMagnitudeByImpactMomentum.Evaluate(momentum));

        GameObject hazardObject = Instantiate(m_HazardRef, transform.position, transform.rotation, null);
        HazardComponent hazard = hazardObject.GetComponent<HazardComponent>();
        hazard.SetLifetime(m_HazardRadiusByImpactMomentum.Evaluate(momentum));
        hazard.SetRadius(m_HazardLifetimeByImpactMomentum.Evaluate(momentum));
    }

    protected void OnObjectLanded()
    {
        if (m_CausesImpacts)
        {

        }
    }

    public abstract float GetMass();

    public float GetGravityMultiplier => m_GravityMultiplier;

    public abstract Transform GetCameraFocusTransform { get; }

    public void TuggedByLasso() { OnTuggedByLasso?.Invoke(); }

    public void StartedSpinning() { OnStartSpinning?.Invoke(); }

    public void Released() { OnReleased?.Invoke();  m_bIsWrangled = false; }

    public void Wrangled() { OnWrangled?.Invoke(); m_bIsWrangled = true; }

    public abstract Transform GetAttachmentTransform { get; }

    public abstract Transform GetMainTransform { get; }

    public abstract void ApplyForceToObject(Vector3 force);
}
