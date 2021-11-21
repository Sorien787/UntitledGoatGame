using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UFOStates;

public class UfoMain : MonoBehaviour, IPauseListener, IHealthListener
{
	[Header("Object references")]
	[SerializeField] private CowGameManager m_Manager;
	[SerializeField] private TractorBeamComponent m_TractorBeamComponent;
	[SerializeField] private UfoAnimationComponent m_AnimationComponent;
	[SerializeField] private Transform m_UfoTransform;
	[SerializeField] private FlightComponent m_FlightComponent;
	[SerializeField] private EntityInformation m_EntityInformation;
	[Header("Gameplay params")]
	[SerializeField] private float m_UfoStaggerTime;
	[SerializeField] private float m_UfoRoamTimeMin;
	[SerializeField] private float m_UfoRoamTimeMax;
	[SerializeField] private float m_PatrolDistanceMax;
	[SerializeField] private float m_PatrolDistanceMin;
	[SerializeField] private float m_UfoInvulnerableTime;
	[SerializeField] private float m_PatrolHeight;
	[Header("Debug params")]
	[SerializeField] private bool m_bDebugSkipIntro;

	public float GetUfoRoamTimeMin => m_UfoRoamTimeMin;
	public float GetUfoRoamTimeMax => m_UfoRoamTimeMax;
	public Transform GetTargetCowTransform => m_TargetCow.transform;

	private StateMachine<UfoMain> m_UfoStateMachine;
	private GameObject m_TargetCow;



	public void SetOnStoppedCallback(Action OnStopped) 
	{

		if (OnStopped == null) 
		{
			m_FlightComponent.ResetStoppedCallback();
		}
		else 
		{
			m_FlightComponent.OnAutopilotArrested += OnStopped;		
		}
	}

	public void SetShouldHoldFlight(in bool shouldHold) 
	{
		m_FlightComponent.SetHold(shouldHold);
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

	public void SetTractorBeam(in bool state) 
	{
		if (state) 
		{
			m_TractorBeamComponent.OnBeginTractorBeam();
		}
		else 
		{
			m_TractorBeamComponent.OnStopTractorBeam();
		}
	}


	private void Awake()
	{
		m_UfoStateMachine = new StateMachine<UfoMain>(new UFOEnterMapState(), this);
		m_UfoStateMachine.AddState(new UFOAbductState( m_AnimationComponent));
		m_UfoStateMachine.AddState(new UFOPatrolState());
		m_UfoStateMachine.AddState(new UFOSearchState());
		m_UfoStateMachine.AddState(new UFOSwoopDownState());
		m_UfoStateMachine.AddState(new UFOStaggeredState(m_AnimationComponent));
		m_UfoStateMachine.AddState(new UFODeathState(m_AnimationComponent));
		m_UfoStateMachine.AddState(new UFOSwoopUpState());
		m_TractorBeamComponent.SetParent(this);
		m_TractorBeamComponent.OnTractorBeamFinished += () => OnCowDied(null, null, DamageType.Undefined);
		m_Manager.AddToPauseUnpause(this);
	}
	private void OnDestroy()
	{
		m_Manager.RemoveFromPauseUnpause(this);
	}
	public void Pause()
	{
		enabled = false;
	}

	public void Unpause()
	{
		enabled = true;
	}

	private void Start()
	{
		m_UfoStateMachine.InitializeStateMachine();
		m_FlightComponent.SetLinearDestination(GetStartingDestination());
	}

	private void Update()
	{
		m_UfoStateMachine.Tick(Time.deltaTime);
	}

	private Vector3 GetStartingDestination() 
	{
		float mapRadius = m_Manager.GetMapRadius;
		float radiusOut = Mathf.Sqrt(UnityEngine.Random.Range(0f, 1f)) * mapRadius;
		float angle = UnityEngine.Random.Range(0f, 1f) * Mathf.Deg2Rad * 360;
		return new Vector3( Mathf.Cos(angle) * radiusOut, m_PatrolHeight, Mathf.Sin(angle));
	}

	public void SetDestination(in Vector3 destination) 
	{
		m_FlightComponent.SetLinearDestination(destination);
	}

	public void UpdateDestination(in Vector3 destination) 
	{
		m_FlightComponent.UpdateLinearDestination(destination);
	}

	public void SetTargetSpeedAtPathEnd(in float speed) 
	{
		m_FlightComponent.SetTargetSpeed(speed);
	}

	public void ResetMotion() 
	{
		m_FlightComponent.StopFlight();
	}

	public Vector3 GetNewPatrolDestination() 
	{
		float dst = UnityEngine.Random.Range(m_PatrolDistanceMin, m_PatrolDistanceMax);
		float randomDirection = UnityEngine.Random.Range(0, Mathf.Deg2Rad * 360);
		Vector2 currentPlanarPosition = new Vector2(m_UfoTransform.position.x, m_UfoTransform.position.z);
		Vector2 newPlanarPosition = new Vector3(Mathf.Sin(randomDirection) * dst + currentPlanarPosition.x, -Mathf.Cos(randomDirection) * dst + currentPlanarPosition.y);
		Vector2 limitedPlanarPosition = Mathf.Min(m_Manager.GetMapRadius, newPlanarPosition.magnitude) * newPlanarPosition.normalized;
		Vector3 newWorldPosition = new Vector3(limitedPlanarPosition.x, m_PatrolHeight, limitedPlanarPosition.y);
		return newWorldPosition;
	}

	public void SetSwoopingUp() 
	{
		SetDestination(GetNewPatrolDestination());
	}
	private void OnCollisionEnter(Collision collision)
	{
		if (collision.gameObject.GetComponent<DamagingProjectileComponent>()) 
		{
			if (m_bCanBeHit)
			{
				StartCoroutine(InvulnerabilityCoroutine());
				StartCoroutine(StaggerCoroutine());
				m_UfoStateMachine.RequestTransition(typeof(UFOStaggeredState));
			}
		}
	}
	private bool m_bCanBeHit = true;

	private IEnumerator InvulnerabilityCoroutine() 
	{
		m_bCanBeHit = false;
		yield return new WaitForSecondsRealtime(m_UfoInvulnerableTime);
		m_bCanBeHit = true;
	}

	private IEnumerator StaggerCoroutine() 
	{
		yield return new WaitForSecondsRealtime(m_UfoStaggerTime);
		m_UfoStateMachine.RequestTransition(typeof(UFOSwoopUpState));
	}

	public void OnCowDied(GameObject cow, GameObject target, DamageType killingType) 
	{

	}

	public void OnTargetedCowStartedAbducted(UfoMain ufo, AbductableComponent cow) 
	{
		if (ufo != this) 
		{
			m_UfoStateMachine.RequestTransition(typeof(UFOSwoopUpState));
			cow.GetComponent<HealthComponent>().RemoveListener(this);
			cow.OnStartedAbducting -= OnTargetedCowStartedAbducted;
		}
	}

	private static readonly List<CowGameManager.EntityState> validEntitiesToFind = new List<CowGameManager.EntityState>() { CowGameManager.EntityState.Free };

	public bool FindCowToAbduct() 
	{
		if (m_Manager.GetClosestTransformMatchingList(m_UfoTransform.position, out EntityToken outEntityToken, validEntitiesToFind, m_EntityInformation.GetHunts)) 
		{
			// in case it dies before we get to it
			outEntityToken.GetEntityType.GetComponent<HealthComponent>().AddListener(this);
			outEntityToken.GetEntityType.GetComponent<AbductableComponent>().OnStartedAbducting += OnTargetedCowStartedAbducted;
			m_TargetCow = outEntityToken.GetEntityType.gameObject;
			outEntityToken.SetAbductionState(CowGameManager.EntityState.Hunted);
			return true;
		}
		return false;
	}

	public void OnEntityTakeDamage(GameObject go1, GameObject go2, DamageType type)
	{
		throw new NotImplementedException();
	}

	public void OnEntityDied(GameObject go1, GameObject go2, DamageType type)
	{
		if (m_TargetCow)
		{
			EntityToken cowToken = m_Manager.GetTokenForEntity(m_TargetCow.GetComponent<EntityTypeComponent>(), m_TargetCow.GetComponent<EntityTypeComponent>().GetEntityInformation);
			cowToken.SetAbductionState(CowGameManager.EntityState.Free);
		}


		m_UfoStateMachine.RequestTransition(typeof(UFOSwoopUpState));
	}
}

namespace UFOStates
{
	public class UFOAbductState : AStateBase<UfoMain>
	{
		private readonly UfoAnimationComponent m_UfoAnimation;
		Vector3 hoverPos;
		public UFOAbductState(UfoAnimationComponent ufoAnimation)
		{
			m_UfoAnimation = ufoAnimation;
		}

		public override void OnEnter()
		{
			Debug.Log("On enter abduct state");
			hoverPos = Host.transform.position;
			Host.SetDestination(hoverPos);
			Host.SetTargetSpeedAtPathEnd(0.0f);
			Host.SetShouldHoldFlight(true);
			m_UfoAnimation.OnAbducting();
			Host.SetTractorBeam(true);
		}

		public override void OnExit()
		{
			m_UfoAnimation.OnFlying();
			Host.SetTractorBeam(false);
			Host.SetShouldHoldFlight(false);
		}
	}

	public class UFOSearchState : AStateBase<UfoMain>
	{

		public override void OnEnter()
		{
			if (Host.FindCowToAbduct())
			{
				RequestTransition<UFOSwoopDownState>();
			}
			else
			{
				RequestTransition<UFOPatrolState>();
			}
		}

		public override void OnExit()
		{
			Host.SetOnStoppedCallback(null);
		}
	}

	public class UFOSwoopUpState : AStateBase<UfoMain>
	{
		public override void OnEnter()
		{
			Debug.Log("On enter swoop up state");
			Host.SetTargetSpeedAtPathEnd(0.0f);
			Host.SetSwoopingUp();
			Host.SetOnReachedDestination(() =>
			{
				RequestTransition<UFOPatrolState>();
			});
		}

		public override void OnExit()
		{
			Host.SetOnReachedDestination(null);
		}
	}

	public class UFOSwoopDownState : AStateBase<UfoMain>
	{
		public override void OnEnter()
		{
			Debug.Log("On enter swoop down state");
			Host.SetTargetSpeedAtPathEnd(1.0f);
			Host.SetOnReachedDestination(() =>
			{
				RequestTransition<UFOAbductState>();
				Host.GetTargetCowTransform.GetComponent<HealthComponent>().AddListener(Host);
			});
			Host.SetDestination(Host.GetTargetCowTransform.position);
		}

		public override void Tick()
		{
			Host.UpdateDestination(Host.GetTargetCowTransform.position + Vector3.up * 10.0f);
		}

		public override void OnExit()
		{
			Host.SetOnReachedDestination(null);
		}
	}

	public class UFOEnterMapState : AStateBase<UfoMain>
	{
		public override void OnEnter()
		{
			Debug.Log("On enter map state");
			Host.SetTargetSpeedAtPathEnd(0.0f);
			Host.SetOnReachedDestination(() => RequestTransition<UFOPatrolState>());
		}

		public override void OnExit()
		{
			Host.SetOnReachedDestination(null);
		}
	}

	public class UFOStaggeredState : AStateBase<UfoMain>
	{
		private readonly UfoAnimationComponent m_UfoAnimation;
		public UFOStaggeredState(UfoAnimationComponent ufoAnimation)
		{
			m_UfoAnimation = ufoAnimation;
		}

		public override void OnEnter()
		{
			Debug.Log("On enter staggered state");
			m_UfoAnimation.OnStaggered();
			Host.ResetMotion();
		}

		public override void OnExit()
		{
			m_UfoAnimation.OnFlying();
		}
	}

	public class UFODeathState : AStateBase<UfoMain>
	{
		private readonly UfoAnimationComponent m_UfoAnimation;

		public UFODeathState(UfoAnimationComponent ufoAnimation)
		{
			m_UfoAnimation = ufoAnimation;
		}

		public override void OnEnter()
		{
			m_UfoAnimation.OnDeath();
		}
	}

	public class UFOPatrolState : AStateBase<UfoMain>
	{
		private float m_fTimeToPatrolFor;

		public UFOPatrolState()
		{
			AddTimers(2);
		}

		public override void OnEnter()
		{
			Debug.Log("On entered patrol state");
			m_fTimeToPatrolFor = UnityEngine.Random.Range(Host.GetUfoRoamTimeMin, Host.GetUfoRoamTimeMax);
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
					RequestTransition<UFOSearchState>();
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
}
