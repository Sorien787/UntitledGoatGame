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


    private void Awake()
	{
        m_throwableObjectComponent.OnWrangled += () => m_StateMachine.RequestTransition(typeof(IObjectPhysicalizedState));
        m_throwableObjectComponent.OnStartSpinning += () => m_StateMachine.RequestTransition(typeof(IObjectControlledState));
        m_throwableObjectComponent.OnReleased += () => m_StateMachine.RequestTransition(typeof(IObjectPhysicalizedState));
        m_throwableObjectComponent.OnThrown += (ProjectileParams) => m_StateMachine.RequestTransition(typeof(IObjectControlledState));

 
        m_freeFallTrajectoryComponent.OnObjectHitGround += OnHitObject;

        m_StateMachine = new StateMachine<InanimateObjectComponent>(new IObjectPhysicalizedState(), this);

        m_StateMachine.AddState(new IObjectControlledState());
        m_StateMachine.InitializeStateMachine();
    }

    private void Update()
	{
        m_StateMachine.Tick(Time.deltaTime);
    }

	public void SetPhysicsState(bool state) 
    {
        m_objectRigidBody.isKinematic = !state;
        m_objectRigidBody.useGravity = state;
    }


    private void OnHitObject(Vector3 pos, Vector3 norm, GameObject go) 
    {
        AnimalComponent animal = go.GetComponentInParent<AnimalComponent>();
        if (animal) 
        {
            animal.OnStruckByObject(m_objectRigidBody.velocity, m_objectRigidBody.mass);
        }
		else 
        {
            Instantiate(m_ImpactEffectsPrefab, pos, Quaternion.LookRotation(Vector3.forward, norm));
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
