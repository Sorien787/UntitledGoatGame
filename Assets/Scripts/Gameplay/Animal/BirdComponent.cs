using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using BirdStates;

[RequireComponent(typeof(FlightComponent))]
[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(EntityTypeComponent))]
public class BirdComponent : InanimateObjectComponent
{
    [SerializeField] private CowGameManager m_Manager;
	[SerializeField] private EntityTypeComponent m_EntityInformation;
    [SerializeField] private StateMachine<BirdComponent> m_StateMachine;
    [SerializeField] private FlightComponent m_FlightComponent;
	[SerializeField] private float m_FlyTimeMin;
	[SerializeField] private float m_FlyTimeMax;
	[SerializeField] private float m_ScaredDistance;
	[SerializeField] float m_PatrolDistanceMin;
	[SerializeField] float m_PatrolDistanceMax;
	[SerializeField] private AudioManager m_AudioManager;

	private Transform m_Transform;



	[SerializeField] private SoundObject m_TakeoffSoundEffect;
	[SerializeField] private SoundObject m_FlapSoundEffect;

    // Start is called before the first frame update
    private RoostComponent m_CurrentRoost;
	bool m_bIsDead = false;
    public bool TryFindRoostingSpot() 
    {
        return m_Manager.GetRoostingSpot(m_Transform.position, ref m_CurrentRoost);
    }
    public void ClearRoostingSpot() 
    {
		m_Manager.RoostCleared(m_CurrentRoost);
        m_CurrentRoost = null;
    }
	private void Awake()
	{
		m_EntityInformation.RemoveFromTrackable();
		m_Transform = transform;
        m_throwableObjectComponent.OnWrangled += OnStopDoingBirdBehaviour;
	}

	public void OnHitByObject() 
	{

	}
	protected override void Start()
    {
        m_StateMachine = new StateMachine<BirdComponent>(new BirdFlyingState(), this);
        m_StateMachine.AddState(new BirdLimpState());
        m_StateMachine.AddState(new BirdRoostingState());
        m_StateMachine.AddState(new BirdTakeoffState());
        m_StateMachine.AddState(new BirdLandingState());

		m_StateMachine.AddStateGroup(StateGroup.Create(typeof(BirdLandingState), typeof(BirdRoostingState)).AddOnExit(ClearRoostingSpot));
		m_StateMachine.AddStateGroup(StateGroup.Create(typeof(BirdLandingState), typeof(BirdRoostingState), typeof(BirdTakeoffState)).AddOnEnter(StartFlyingSound).AddOnExit(StopFlyingSound));

		m_StateMachine.AddTransition(typeof(BirdRoostingState), typeof(BirdTakeoffState), ShouldTakeOff);
		m_StateMachine.AddTransition(typeof(BirdLandingState), typeof(BirdTakeoffState), ShouldTakeOff);
		m_StateMachine.AddTransition(typeof(BirdLandingState), typeof(BirdRoostingState), HasReachedRoost);

        m_StateMachine.InitializeStateMachine();
    }

	private void StartFlyingSound() 
	{
		m_AudioManager.Play(m_FlapSoundEffect);
		m_AudioManager.Play(m_TakeoffSoundEffect);
	}

	private void StopFlyingSound() 
	{
		m_AudioManager.StopPlaying(m_FlapSoundEffect);
	}

	private bool HasReachedRoost() 
	{
		return (m_CurrentRoost.GetRoostingLocation - m_Transform.position).sqrMagnitude < 0.2f;
	}

	public Vector3 GetRoostPosition() 
	{
		return m_CurrentRoost.GetRoostingLocation;
	}

	public void ShouldHoldPosition(bool hold)
	{
		m_FlightComponent.SetHold(true);
	}

	private bool ShouldTakeOff() 
	{
		if (m_Manager.GetClosestTransformMatchingList(m_Transform.position, out EntityToken token, false, m_EntityInformation.GetEntityInformation.GetScaredOf))	
			return ((m_Transform.position - token.GetEntityTransform.position).sqrMagnitude < m_ScaredDistance * m_ScaredDistance);
		return false;
	}

    private void OnStopDoingBirdBehaviour() 
    {
		m_throwableObjectComponent.OnWrangled -= OnStopDoingBirdBehaviour;
		m_EntityInformation.AddToTrackable();
        enabled = false;
        InitializeMachine();
    }

    // Update is called once per frame
    void Update()
    {
        m_StateMachine.Tick(Time.deltaTime);
    }

	public float GetRoamTimeMin => m_FlyTimeMin;
	public float GetRoamTimeMax => m_FlyTimeMax;
	public void SetTargetSpeedAtPathEnd(float speed) 
	{
		m_FlightComponent.SetTargetSpeed(speed);
	}
	public void SetOnReachedDestination(Action OnReachedDestination)
	{
		if (OnReachedDestination == null)
		{
			m_FlightComponent.ResetFlightCallback();
		}
		else
		{
			m_FlightComponent.OnAutopilotPositionCompleted += OnReachedDestination;
		}
	}
	public void SetDestination(Vector3 destination)
	{
		m_FlightComponent.SetLinearDestination(destination);
	}

	public void UpdateDestination(Vector3 destination) 
	{
		m_FlightComponent.UpdateLinearDestination(destination);
	}
	public Vector3 GetNewPatrolDestination()
	{
		float dst = UnityEngine.Random.Range(m_PatrolDistanceMin, m_PatrolDistanceMax);
		float height = UnityEngine.Random.Range(m_Manager.GetFlightPatrolHeightMin, m_Manager.GetFlightPatrolHeightMax);
		float randomDirection = UnityEngine.Random.Range(0, Mathf.Deg2Rad * 360);
		Vector2 currentPlanarPosition = new Vector2(m_Transform.position.x, m_Transform.position.z);
		Vector2 newPlanarPosition = new Vector3(Mathf.Sin(randomDirection) * dst + currentPlanarPosition.x, -Mathf.Cos(randomDirection) * dst + currentPlanarPosition.y);
		Vector2 limitedPlanarPosition = Mathf.Min(m_Manager.GetMapRadius, newPlanarPosition.magnitude) * newPlanarPosition.normalized;
		Vector3 newWorldPosition = new Vector3(limitedPlanarPosition.x, height, limitedPlanarPosition.y);
		return newWorldPosition;
	}
}
namespace BirdStates 
{

	public class BirdFlyingState : AStateBase<BirdComponent>
	{
		private float m_fTimeToPatrolFor;

		public BirdFlyingState()
		{
			AddTimers(1);
		}

		public override void OnEnter()
		{
			m_fTimeToPatrolFor = UnityEngine.Random.Range(Host.GetRoamTimeMin, Host.GetRoamTimeMax);
			StopTimer(1);
			Host.SetTargetSpeedAtPathEnd(1000.0f);
			Host.SetOnReachedDestination(OnReachedPatrolWaypoint);
			Host.SetDestination(Host.GetNewPatrolDestination());
		}

		public override void Tick()
		{
			// then if we're not finished patrolling, choose another destination
			if (GetTimerVal(0) >= m_fTimeToPatrolFor)
			{
				RequestTransition<BirdSearchingState>();
			}
		}
		private void OnReachedPatrolWaypoint()
		{
			Host.SetDestination(Host.GetNewPatrolDestination());
		}

		public override void OnExit()
		{
			Host.SetOnReachedDestination(null);
		}
	}

	public class BirdSearchingState : AStateBase<BirdComponent> 
	{
		public override void Tick()
		{
			if (Host.TryFindRoostingSpot())
				RequestTransition<BirdLandingState>();
			else
				RequestTransition<BirdFlyingState>();
		}
	}
	public class BirdLimpState : AStateBase<BirdComponent>
	{ }
	public class BirdRoostingState : AStateBase<BirdComponent>
	{ }
	public class BirdTakeoffState : AStateBase<BirdComponent>
	{ }
	public class BirdLandingState : AStateBase<BirdComponent>
	{
		public override void OnEnter()
		{
			Host.UpdateDestination(Host.GetRoostPosition());
			Host.ShouldHoldPosition(true);
			Host.SetTargetSpeedAtPathEnd(0.0f);
		}

		public override void OnExit()
		{
			Host.ShouldHoldPosition(false);
			Host.SetOnReachedDestination(null);
		}
	}

}