using UnityEngine;

[RequireComponent(typeof(ThrowableObjectComponent))]
[RequireComponent(typeof(FreeFallTrajectoryComponent))]
[RequireComponent(typeof(PhysicalEntity))]
public class InanimateObjectComponent : MonoBehaviour
{
    private StateMachine<InanimateObjectComponent> m_StateMachine;

    [Header("Object References")]
    [SerializeField] private ThrowableObjectComponent m_throwableObjectComponent;
    [SerializeField] private FreeFallTrajectoryComponent m_freeFallTrajectoryComponent;
    [SerializeField] private Rigidbody m_objectRigidBody;
    [SerializeField] private GameObject m_ImpactEffectsPrefab;
    [SerializeField] private PhysicalEntity m_Entity;
    [SerializeField] private ParticleEffectsController m_DragFX;
    [SerializeField] private float m_SpeedForDragFX;

    private Transform m_DragFXTransform;

	private void Awake()
	{
        m_throwableObjectComponent.OnWrangled += () => m_StateMachine.RequestTransition(typeof(IObjectPhysicalizedState));
        m_throwableObjectComponent.OnStartSpinning += () => m_StateMachine.RequestTransition(typeof(IObjectControlledState));
        m_throwableObjectComponent.OnReleased += () => m_StateMachine.RequestTransition(typeof(IObjectPhysicalizedState));
        m_throwableObjectComponent.OnThrown += (ProjectileParams) => m_StateMachine.RequestTransition(typeof(IObjectControlledState));

        m_DragFXTransform = m_DragFX.transform;
        m_freeFallTrajectoryComponent.OnObjectHitGround += OnHitObject;

        m_Entity = GetComponent<PhysicalEntity>();

        m_StateMachine = new StateMachine<InanimateObjectComponent>(new IObjectPhysicalizedState(), this);

        m_StateMachine.AddState(new IObjectControlledState());
	}

    bool m_bParticleFXActive = false;

    private void Update()
	{
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
            m_DragFXTransform.position = m_Entity.GetGroundedPos;
            m_DragFXTransform.rotation = Quaternion.LookRotation(m_Entity.GetGroundedNorm);
        }
        m_StateMachine.Tick(Time.deltaTime);
    }

	public void SetPhysicsState(bool state) 
    {
        m_objectRigidBody.isKinematic = !state;
        m_objectRigidBody.useGravity = state;
    }

	private void Start()
	{
        m_StateMachine.InitializeStateMachine();
	}

    private void OnHitObject(Collision collision) 
    {
        AnimalComponent animal = collision.gameObject.GetComponentInParent<AnimalComponent>();
        if (animal) 
        {
            animal.OnStruckByObject(m_objectRigidBody.velocity, m_objectRigidBody.mass);
        }
		else 
        {
            Instantiate(m_ImpactEffectsPrefab, collision.GetContact(0).point, Quaternion.LookRotation(Vector3.forward, collision.GetContact(0).normal));
        }
        m_StateMachine.RequestTransition(typeof(IObjectPhysicalizedState));
    }
}

public class IObjectControlledState : AStateBase<InanimateObjectComponent>
{
	public override void OnEnter()
	{
		Host.SetPhysicsState(false);
	}
}

public class IObjectPhysicalizedState : AStateBase<InanimateObjectComponent>
{
    public override void OnEnter()
    {
		Host.SetPhysicsState(true);
    }
}
