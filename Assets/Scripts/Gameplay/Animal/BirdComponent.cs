using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using BirdStates;

[RequireComponent(typeof(FlightComponent))]
[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(EntityTypeComponent))]
public class BirdComponent : InanimateObjectComponent, IPauseListener, IHealthListener
{
	[Header("References")]
    [SerializeField] private CowGameManager m_Manager;
	[SerializeField] private SoundObject m_TakeoffSoundEffect;
	[SerializeField] private SoundObject m_FlapSoundEffect;
	[SerializeField] private SoundObject m_DeathSound;

	[Header("Anim Params")]
	[SerializeField] private float m_DeathDuration = 1.0f;
	[SerializeField] private float m_SizeVariation = 0.0f;
	[SerializeField] private AnimationCurve m_DeathSizeAnimationCurve = default;
	[SerializeField] private float m_FlapWindUpDistance = 3.0f;
	[SerializeField] private float m_TurnTime = 0.7f;
	[SerializeField] private Transform m_AnimRotationTransform; 
	[SerializeField] private Animator m_BirdAnimator = default;

	[Header("Gameplay Params")]
	[SerializeField] private float m_ScaredDistance;
	[SerializeField] float m_PatrolDistanceMin;
	[SerializeField] float m_PatrolDistanceMax;
	[SerializeField] private float m_FlyTimeMin;
	[SerializeField] private float m_FlyTimeMax;

	private StateMachine<BirdComponent> m_BirdStateMachine;
	private EntityTypeComponent m_EntityInformation;
	private Transform m_Transform;
	private HealthComponent m_HealthComponent;
	private FlightComponent m_FlightComponent;
	private AudioManager m_AudioManager;
	private Rigidbody m_Body;



	private float m_DeathAnimTime = 0.0f;
	private float m_SizeMult;
	private RoostComponent m_CurrentRoost;
	private Vector3 m_RoostSpotWorldSpace;

	Quaternion m_CurrentAngVel = Quaternion.identity;
	public void UpdateLookDirToVelocity() 
	{
		Quaternion target = Quaternion.LookRotation(Vector3.ProjectOnPlane(m_Body.velocity, Vector3.up), Vector3.up);
		m_AnimRotationTransform.rotation = UnityUtils.UnityUtils.SmoothDampQuat(m_AnimRotationTransform.rotation, target, ref m_CurrentAngVel, m_TurnTime);
	}

	public void TakeOff() 
	{
		m_AudioManager.PlayOneShot(m_TakeoffSoundEffect);
	}

	float m_currentFlapStrength = 0.0f;
	public void PlayFlapSoundEffect() 
	{
		m_AudioManager.SetVolume(m_FlapSoundEffect, m_currentFlapStrength);
		m_AudioManager.PlayOneShot(m_FlapSoundEffect);
	}

	public void UpdateFlapStrength() 
	{
		float strUnclamped = (m_Transform.position - m_RoostSpotWorldSpace).magnitude / m_FlapWindUpDistance;
		SetFlapStrength(strUnclamped);
	}

	public void SetFlapStrength(float strength) 
	{
		m_currentFlapStrength = Mathf.Clamp01(strength);
		m_BirdAnimator.SetFloat("flapStrength", m_currentFlapStrength);
	}

    public bool TryFindRoostingSpot() 
    {
		if (!m_Manager.GetRoostingSpot(m_Transform.position, ref m_CurrentRoost))
			return false;
		m_RoostSpotWorldSpace = m_CurrentRoost.GetRoostingLocation;
		return true;
    }
    public void ClearRoostingSpot() 
    {
		m_Manager.RoostCleared(m_CurrentRoost);
        m_CurrentRoost = null;
    }
	protected override void Awake()
	{
		base.Awake();
		m_SizeMult = 1.0f + UnityEngine.Random.Range(-m_SizeVariation, m_SizeVariation);

		m_EntityInformation = GetComponent<EntityTypeComponent>();
		m_Transform = GetComponent<Transform>();
		m_HealthComponent = GetComponent<HealthComponent>();
		m_FlightComponent = GetComponent<FlightComponent>();
		m_AudioManager = GetComponent<AudioManager>();
		m_Body = GetComponent<Rigidbody>();

		m_Manager.AddToPauseUnpause(this);
		m_Transform = transform;
        m_throwableObjectComponent.OnWrangled += OnStopDoingBirdBehaviour;
		m_throwableObjectComponent.EnableImpacts(false);
		m_throwableObjectComponent.EnableDragging(false);
		m_HealthComponent.AddListener(this);
		m_Transform.localScale = Vector3.one * m_SizeMult;
	}


	public void OnHitByObject() 
	{
		OnStopDoingBirdBehaviour();
	}
	protected override void Start()
    {
		m_EntityInformation.RemoveFromTrackable();
		m_BirdStateMachine = new StateMachine<BirdComponent>(new BirdFlyingState(), this);
        m_BirdStateMachine.AddState(new BirdLimpState());
        m_BirdStateMachine.AddState(new BirdRoostingState());
        m_BirdStateMachine.AddState(new BirdTakeoffState());
        m_BirdStateMachine.AddState(new BirdLandingState());
		m_BirdStateMachine.AddState(new BirdSearchingState());

		m_BirdStateMachine.AddStateGroup(StateGroup.Create(typeof(BirdLandingState), typeof(BirdRoostingState)).AddOnExit(ClearRoostingSpot));
		m_BirdStateMachine.AddStateGroup(StateGroup.Create(typeof(BirdLandingState), typeof(BirdRoostingState), typeof(BirdTakeoffState)).AddOnEnter(StartFlyingSound).AddOnExit(StopFlyingSound));

		m_BirdStateMachine.AddTransition(typeof(BirdRoostingState), typeof(BirdTakeoffState), ShouldTakeOffFromRoost);
		m_BirdStateMachine.AddTransition(typeof(BirdLandingState), typeof(BirdTakeoffState), ShouldTakeOff);
		m_BirdStateMachine.AddTransition(typeof(BirdLandingState), typeof(BirdRoostingState), HasReachedRoost);

        m_BirdStateMachine.InitializeStateMachine();
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
		m_FlightComponent.SetHold(hold);
	}

	private bool ShouldTakeOff() 
	{
		if (m_Manager.GetClosestTransformMatchingList(m_Transform.position, out EntityToken token, false, m_EntityInformation.GetEntityInformation.GetScaredOf))	
			return ((m_Transform.position - token.GetEntityTransform.position).sqrMagnitude < m_ScaredDistance * m_ScaredDistance);
		return false;
	}

	private bool ShouldTakeOffFromRoost() 
	{
		if (m_Manager.GetClosestTransformMatchingList(m_Transform.position, out EntityToken token, false, m_EntityInformation.GetEntityInformation.GetScaredOf))
		{
			bool takeOff = ((m_Transform.position - token.GetEntityTransform.position).sqrMagnitude < m_ScaredDistance * m_ScaredDistance);
			if (takeOff) 
			{
				m_AudioManager.PlayOneShot(m_TakeoffSoundEffect);
			}
			return takeOff;
		}
			
		return false;
	}

    private void OnStopDoingBirdBehaviour() 
    {
		m_throwableObjectComponent.OnWrangled -= OnStopDoingBirdBehaviour;
		m_throwableObjectComponent.EnableImpacts(true);
		m_throwableObjectComponent.EnableDragging(true);
		m_EntityInformation.AddToTrackable();
		m_BirdAnimator.enabled = false;
		m_AudioManager.PlayOneShot(m_DeathSound);
		SetFlapStrength(0.0f);
        enabled = false;
        InitializeMachine();
    }

    // Update is called once per frame
    void Update()
    {
        m_BirdStateMachine.Tick(Time.deltaTime);
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
	private bool m_bIsActive = true;
	public void Pause()
	{
		enabled = false;
	}

	public void Unpause()
	{
		if (m_bIsActive)
			enabled = true;
	}

	public void OnEntityTakeDamage(GameObject go1, GameObject go2, DamageType type)
	{

	}

	IEnumerator PredatorEatCoroutine()
	{
		while (m_DeathAnimTime < m_DeathDuration)
		{
			m_DeathAnimTime += Time.deltaTime;
			float deathSize = m_DeathSizeAnimationCurve.Evaluate(m_DeathAnimTime / m_DeathDuration) * m_SizeMult;
			m_Transform.localScale = Vector3.one * deathSize;
			yield return null;

		}
		m_HealthComponent.RemoveListener(this);
		m_Manager.RemoveFromPauseUnpause(this);
		Destroy(gameObject);
	}

	public void OnEntityHealthPercentageChange(float currentHealthPercentage)
	{

	}

	public void OnEntityDied(GameObject go1, GameObject go2, DamageType type)
	{
		m_EntityInformation.OnRemovedFromGame();

		StartCoroutine(PredatorEatCoroutine());
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
			Host.SetFlapStrength(1.0f);
			m_fTimeToPatrolFor = UnityEngine.Random.Range(Host.GetRoamTimeMin, Host.GetRoamTimeMax);
			StartTimer(0);
			Host.SetTargetSpeedAtPathEnd(1000.0f);
			Host.SetOnReachedDestination(OnReachedPatrolWaypoint);
			Host.SetDestination(Host.GetNewPatrolDestination());
		}

		public override void Tick()
		{
			Host.UpdateLookDirToVelocity();
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
	{
		public override void OnEnter()
		{
			Host.SetFlapStrength(0.0f);
		}
	}
	public class BirdTakeoffState : AStateBase<BirdComponent>
	{
		public override void OnEnter()
		{
			Host.SetTargetSpeedAtPathEnd(1000.0f);
			Host.SetOnReachedDestination(OnReachedSky);
			Host.SetDestination(Host.GetNewPatrolDestination());
		}

		public override void Tick()
		{
			Host.UpdateFlapStrength();
			Host.UpdateLookDirToVelocity();
		}

		private void OnReachedSky() 
		{
			RequestTransition<BirdFlyingState>();
		}

		public override void OnExit()
		{
			Host.SetOnReachedDestination(null);
		}

	}
	public class BirdLandingState : AStateBase<BirdComponent>
	{
		public override void OnEnter()
		{
			Host.UpdateDestination(Host.GetRoostPosition());
			Host.ShouldHoldPosition(true);
			Host.SetTargetSpeedAtPathEnd(0.0f);
		}

		public override void Tick()
		{
			Host.UpdateFlapStrength();
			Host.UpdateLookDirToVelocity();
		}
		public override void OnExit()
		{
			Host.ShouldHoldPosition(false);
			Host.SetOnReachedDestination(null);
		}
	}

}