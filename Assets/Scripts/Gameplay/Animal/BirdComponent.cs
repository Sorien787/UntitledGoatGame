using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using BirdStates;

[RequireComponent(typeof(FlightComponent))]
[RequireComponent(typeof(HealthComponent))]
public class BirdComponent : InanimateObjectComponent
{
    [SerializeField] private CowGameManager m_Manager;
    [SerializeField] private StateMachine<BirdComponent> m_StateMachine;
    [SerializeField] private FlightComponent m_FlightComponent;
	[SerializeField] private float m_FlyTimeMin;
	[SerializeField] private float m_FlyTimeMax;
    // Start is called before the first frame update
    private RoostComponent m_CurrentRoost;
    public bool TryFindRoostingSpot() 
    {
        return m_Manager.GetRoostingSpot(ref m_CurrentRoost);
    }
    public void ClearRoostingSpot() 
    {
        m_CurrentRoost = null;
    }
	private void Awake()
	{
        m_throwableObjectComponent.OnWrangled += OnStopDoingBirdBehaviour;
	}
	protected override void Start()
    {
        m_StateMachine = new StateMachine<BirdComponent>(new BirdFlyingState(), this);
        m_StateMachine.AddState(new BirdLimpState());
        m_StateMachine.AddState(new BirdRoostingState());
        m_StateMachine.AddState(new BirdTakeoffState());
        m_StateMachine.AddState(new BirdLandingState());

		m_StateMachine.AddStateGroup(StateGroup.Create(typeof(BirdRoostingState), typeof(BirdLimpState)).AddOnExit(ClearRoostingSpot));

		m_StateMachine.AddTransition(typeof(BirdRoostingState), typeof(BirdTakeoffState), );
		m_StateMachine.AddTransition(typeof(BirdLandingState), typeof(BirdTakeoffState), );

        m_StateMachine.InitializeStateMachine();
    }



    private void OnStopDoingBirdBehaviour() 
    {
		m_throwableObjectComponent.OnWrangled -= OnStopDoingBirdBehaviour;
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

	}
	public void SetOnReachedDestination(Action onReached)
	{

	}
	public void SetDestination(Vector3 destination)
	{

	}
	public Vector3 GetNewPatrolDestination()
	{

	}

	public void OnReachedPatrolWaypoint() 
	{

	}
}
namespace BirdStates 
{

	public class BirdFlyingState : AStateBase<BirdComponent>
	{
		private float m_fTimeToPatrolFor;

		public BirdFlyingState()
		{
			AddTimers(2);
		}

		public override void OnEnter()
		{
			m_fTimeToPatrolFor = UnityEngine.Random.Range(Host.GetRoamTimeMin, Host.GetRoamTimeMax);
			StopTimer(1);
			Host.SetTargetSpeedAtPathEnd(0.0f);
			Host.SetOnReachedDestination(OnReachedPatrolWaypoint);
			Host.SetDestination(Host.GetNewPatrolDestination());
		}

		public override void Tick()
		{
			// if the waypoint timer has expired
			// and we're not currently patrolling, I.E we're at a waypoint
			if (GetTimerVal(1) > 1.0f)
			{
				// then if we're not finished patrolling, choose another destination
				if (GetTimerVal(0) < m_fTimeToPatrolFor)
				{
					ClearTimer(1);
					StopTimer(1);
					Host.SetDestination(Host.GetNewPatrolDestination());

				}
				// else we change states
				else
				{
					RequestTransition<BirdSearchingState>();
				}
			}
		}
		private void OnReachedPatrolWaypoint()
		{
			StartTimer(1);
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
	{ }

}