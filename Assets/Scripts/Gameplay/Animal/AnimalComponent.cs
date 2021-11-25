using UnityEngine;
using UnityEngine.AI;
using System;
using EZCameraShake;
using System.Collections.Generic;
using System.Collections;


[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(ThrowableObjectComponent))]
[RequireComponent(typeof(FreeFallTrajectoryComponent))]
public class AnimalComponent : MonoBehaviour, IPauseListener, IEntityTrackingListener, IHealthListener, ILevelListener
{
    [Header("Object references")]
    [SerializeField] protected CowGameManager m_Manager = default;

    [Header("Transforms")]
    [SerializeField] protected Transform m_AnimalMainTransform = default;
    [SerializeField] protected Transform m_AnimalLeashTransform = default;
    [SerializeField] protected Transform m_AnimalBodyTransform = default;
    [SerializeField] protected Collider m_AnimalBodyCollider = default;

    [Header("Stagger parameters")]
    [SerializeField] AnimationCurve m_StaggerTimeByImpactMomentum;
    [SerializeField] float m_StaggerCooldown = 0.2f;

    [Header("Breeding Parameters")]
    [SerializeField] private float m_fBreedingHungerCooldownTime = default;
    [SerializeField] private float m_fBreedingChaseStartRange = default;
    [SerializeField] private float m_fBreedingChaseEndRange = default;
    [SerializeField] private float m_fBreedingStartRange = default;
    [SerializeField] private float m_fBreedingCooldownTime = default;
    [SerializeField] private float m_fBreedingHungerUsage = default;
    [SerializeField] private float m_fBreedingDuration = default;
    [SerializeField] private float m_fBreedCheckInterval = default;
    [SerializeField] private float m_fMaximumFullness = default;

    [Header("Idle parameters")]
    [SerializeField] protected float m_LowIdleTimer = default;
    [SerializeField] protected float m_HighIdleTimer = default;

    [Header("Fleeing parameters")]
    [SerializeField] protected float m_ScaredDistance = default;
    [SerializeField] protected float m_EvadedDistance = default;
    [SerializeField] protected float m_FleeCheckInterval = default;
    [SerializeField] protected float m_AnimalResistanceToPull = default;

    [Header("Hunting parameters")]
    [SerializeField] private float m_HuntBeginDistance = default;
    [SerializeField] private float m_HuntEndDistance = default;
    [SerializeField] private float m_fHuntCheckInterval = default;
    [SerializeField] private float m_fViewFrustrumAngle = default;

    [Header("Born parameters")]
    [SerializeField] private float m_BornTime = default;

    [SerializeField] private AttackBase m_DamageAttackType;
    [SerializeField] private AttackTypeDamage m_EatAttackType;
    [SerializeField] private LayerMask m_GroundLayerMask;

    private AttackBase m_CurrentAttackComponent;

    private bool m_bIsHungry = false;
    private bool m_bShouldStagger = false;
    private float m_fFullness = 0.0f;
	private float m_TotalStaggerTime = 0.0f;

	public class Cooldown
	{
		private float m_Cooldown;
		public void DecrementCooldown(in float decrement) { m_Cooldown = Mathf.Max(0f, m_Cooldown - decrement); }
		public void SetCooldown(in float val) => m_Cooldown = val;
		public bool CooldownComplete => m_Cooldown == 0f;
	}
    private Cooldown m_CurrentStaggerCooldown = new Cooldown();
	private Cooldown m_CurrentBreedingCooldown = new Cooldown();

    private readonly Type[] m_CanStaggerStates = new Type[] {typeof(AnimalFreeFallState), typeof(AnimalLassoThrownState)};

	private StateMachine<AnimalComponent> m_StateMachine = default;
	private AnimalMovementComponent m_AnimalMovement = default;
	private FreeFallTrajectoryComponent m_FreeFallComponent = default;
	private Rigidbody m_AnimalRigidBody = default;
	private NavMeshAgent m_AnimalAgent = default;
	private AnimalAnimationComponent m_AnimalAnimator = default;
	private EntityTypeComponent m_EntityInformation = default;
	private AbductableComponent m_AbductableComponent = default;
	private HealthComponent m_AnimalHealthComponent = default;
    private AttackBase m_AttackableComponent = default;
	private ThrowableObjectComponent m_ThrowableComponent = default;

    public float GetBornDuration => m_BornTime;


    #region Component Event Handlers

    public void OnPulledByLasso()
    {
        m_AnimalAnimator.WasPulled();
    }

    private void OnBeginAbducted()
    {
        IsInTractorBeam = true;
    }

    private void OnFinishedAbducted()
    {
        IsInTractorBeam = false;
    }

    private void OnWrangledByLasso() 
    {
        IsWrangled = true;
    }

    private bool CanSeeObject(float range, Vector3 objectPosition) 
    {
        Vector3 fromThisToThat = objectPosition - m_AnimalMainTransform.position;
        if (fromThisToThat.sqrMagnitude - range * range > 0)
            return false;

        //Vector3 animalForward = m_AnimalBodyTransform.forward;
        //if (Vector3.Angle(animalForward, fromThisToThat) > m_fViewFrustrumAngle)
        //    return false;

        //if (Physics.Raycast(m_AnimalMainTransform.position, fromThisToThat, out RaycastHit hit, fromThisToThat.magnitude, m_GroundLayerMask, QueryTriggerInteraction.Ignore))
        //    return false;

        return true;
    }

    private void OnReleasedByLasso()
    {
        m_AnimalBodyCollider.enabled = true;
        IsWrangled = false;
    }

	private void OnThrownByLasso(ProjectileParams projectileParams)
    {
        m_AnimalBodyCollider.enabled = true;
        IsWrangled = false;
        m_StateMachine.RequestTransition(typeof(AnimalLassoThrownState));
    }

    private IEnumerator DelayedDeathDestroy() 
    {
        yield return new WaitForSeconds(2.0f);
        Destroy(gameObject);
    }

	private void OnStartedLassoSpinning()
    {
        m_AnimalBodyCollider.enabled = false;
        IsWrangled = true;
        DisablePhysics();
        m_AnimalAnimator.SetIdleAnimation();
        m_AnimalMainTransform.rotation = Quaternion.identity;
        m_StateMachine.RequestTransition(typeof(AnimalThrowingState));
    }

    private void OnHitGroundFromThrown(Collision collision)
    {
        OnHitGround(collision);
        m_StateMachine.RequestTransition(typeof(AnimalFreeFallState));
    }

    private void OnHitGround(Collision collision) 
    {
        Vector3 momentum = m_AnimalRigidBody.mass * m_AnimalRigidBody.velocity;
        float momentumInNormalDirection = -Vector3.Dot(collision.contacts[0].normal, momentum);
        OnHitByMomentum(momentumInNormalDirection);
    }

    private void OnHitByMomentum(float momentumMagnitude) 
    {
        if (momentumMagnitude > m_StaggerTimeByImpactMomentum.keys[0].time && CanImpactHard() && m_CurrentStaggerCooldown.CooldownComplete)
        {
            m_bShouldStagger = true;
            m_CurrentStaggerCooldown.SetCooldown(m_StaggerCooldown);
            m_TotalStaggerTime = Mathf.Max(m_StaggerTimeByImpactMomentum.Evaluate(momentumMagnitude), m_TotalStaggerTime);
        }
    }

    public void AddStaggerTime(float staggerTime) 
    {
        m_bShouldStagger = true;
        m_TotalStaggerTime = Mathf.Max(staggerTime, m_TotalStaggerTime);
    }

    public void OnReceiveImpulse(in Vector3 forceChange) 
    {
        ProjectileParams pParams = new ProjectileParams(m_ThrowableComponent, forceChange.magnitude, forceChange.normalized, m_AnimalBodyTransform.position, 180f);
        m_ThrowableComponent.ThrowObject(pParams);
    }

	public void OnStruckByObject(in Vector3 velocity, in float mass)
	{
		Vector3 momentum = velocity * mass;
		if (momentum.sqrMagnitude > m_StaggerTimeByImpactMomentum.keys[0].time * m_StaggerTimeByImpactMomentum.keys[0].time)
		{
			m_TotalStaggerTime = Mathf.Max(m_StaggerTimeByImpactMomentum.Evaluate(momentum.magnitude), m_TotalStaggerTime);
            m_bShouldStagger = true;
        }
		m_AnimalRigidBody.velocity += GetGroundDir() * momentum.magnitude / m_AnimalRigidBody.mass;
	}
	#endregion

	#region State Machine Callbacks
    public void SetManagedByAgent(bool enable) 
    {
        m_AnimalAgent.enabled = enable;
        if (m_AnimalAgent.isOnNavMesh)
            m_AnimalAgent.isStopped = false;
        m_AnimalAgent.updatePosition = enable;
        m_AnimalAgent.updateUpAxis = false;
        m_AnimalAgent.updateRotation = false;
    }

    public void DisablePhysics() 
    {
        m_AnimalRigidBody.isKinematic = true;
        m_AnimalRigidBody.useGravity = false;
    }

	public void SetGeneralPhysics() 
    {
        m_AnimalRigidBody.isKinematic = false;
        m_AnimalRigidBody.useGravity = true;
    }

	public void SetAbductionPhysics()
	{
		m_AnimalRigidBody.isKinematic = false;
		m_AnimalRigidBody.useGravity = false;
	}

	public void OnStaggered() 
    {
        m_bShouldStagger = false;
    }

    public void OnBreedingComplete() 
    {
        if (m_fFullness > m_fBreedingHungerUsage)
        {
            Transform otherTrans = GetTargetEntity.GetTrackingTransform;
            Vector3 inBetween = (m_EntityInformation.GetTrackingTransform.position + otherTrans.position) / 2f;
            Instantiate(gameObject, inBetween, Quaternion.identity, null);
            GetTargetEntity.GetComponent<AnimalComponent>().OnSuccessfullyBred();
            OnSuccessfullyBred();
		}
    }
    #endregion

    #region State Machine Transitions
    protected bool IsWrangled { get; private set; }
    protected bool IsInTractorBeam { get; private set; }
    protected bool IsDead { get; private set; }

	#region evading logic
	protected bool ShouldStopActionToEvadeNext() 
    {
        return GetEntityTokenToEscapeFrom(out EntityToken _);
    }

    private bool GetEntityTokenToEscapeFrom(out EntityToken token) 
    {
        if (m_Manager.GetClosestTransformMatchingList(m_AnimalMainTransform.position, out token, null, m_EntityInformation.GetEntityInformation.GetScaredOf))
        {
            if (Vector3.SqrMagnitude(token.GetEntityTransform.position - m_AnimalMainTransform.position) < m_ScaredDistance * m_ScaredDistance)
            {
                return true;
            }
        }


        if (m_Manager.GetClosestTransformMatchingList(m_AnimalMainTransform.position, out token, null, m_Manager.GetHazardType))
		{
            if (token.GetEntityTransform.TryGetComponent(out HazardComponent hazard)) 
            {
                if (Vector3.SqrMagnitude(token.GetEntityTransform.position - m_AnimalMainTransform.position) < hazard.GetHazardRadius * hazard.GetHazardRadius) 
                {
                    return true;
                }         
            }
		}
        return false;
    }

    protected bool ShouldEvade()
    {
       if (GetEntityTokenToEscapeFrom(out EntityToken token)) 
       {
            m_AnimalAnimator.IsScared();
            SetRelevantTargetAnimal(token.GetEntityType);
            return true;
       }
        return false;
    }

    protected bool HasEvadedEnemy()
    { 
        if (!GetTargetEntity)
            return true;

        float distSq = Vector3.SqrMagnitude(GetTargetEntity.GetTrackingTransform.position - m_AnimalMainTransform.position);
		float distToEscSq;
		if (GetTargetEntity.GetEntityInformation == m_Manager.GetHazardType && GetTargetEntity.TryGetComponent(out HazardComponent hazard))
        {
            distToEscSq = hazard.GetHazardRadius * hazard.GetHazardRadius;
        }
		else 
        {
            distToEscSq = m_EvadedDistance * m_EvadedDistance * 1.0f;
        }

        return distSq > distToEscSq;
    }

    // the call that this listener makes removes the listener anyway, so this is just valid.
    public void OnTargetInvalidated()
    {
        if (m_bIsMated) 
        {
            m_bIsMated = false;
        }
        if (GetTargetEntity) 
        {
            GetTargetEntity.RemoveListener(this);
            GetTargetEntity = null;
        }
    }

    protected void SetRelevantTargetAnimal(EntityTypeComponent evadingAnimal)
	{
		GetTargetEntity = evadingAnimal;
		GetTargetEntity.AddListener(this);
	}

    private void OnResetAttackBool() 
    {
        m_bHasAttackBeenTriggered = false;
    }

    #endregion

    #region chasing logic
    /////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// Chasing Data
    private float m_fLastAttackTime = -Mathf.Infinity;

    private bool CanHuntEnemy()
    {
        if (m_Manager.GetClosestTransformMatchingList(m_AnimalMainTransform.position, out EntityToken objAtkToken, null, m_EntityInformation.GetEntityInformation.GetAttacks) && TryHuntObject(objAtkToken))
        {
			SetCurrentAttack(m_DamageAttackType);
			m_AnimalAnimator.HasSeenEnemy();
            return true;
        }

        if (!IsFull && m_bIsHungry && m_Manager.GetClosestTransformMatchingList(m_AnimalMainTransform.position, out EntityToken objToken, null, m_EntityInformation.GetEntityInformation.GetHunts) && TryHuntObject(objToken))
        { 
		    SetCurrentAttack(m_EatAttackType);
		    m_AnimalAnimator.HasSeenFood();
            return true;
        }
        return false;
    }
	private void SetCurrentAttack(in AttackBase attack)
	{
		m_CurrentAttackComponent = attack;
		m_AnimalAnimator.SetCurrentAttackAnimation(attack);
	}

    bool TryHuntObject(in EntityToken objToken)
    {
        if (CanSeeObject(m_HuntBeginDistance, objToken.GetEntityType.GetTrackingTransform.position))
        {
            SetRelevantTargetAnimal(objToken.GetEntityTransform.GetComponent<EntityTypeComponent>());
            return true;
        }
        return false;
    }

	#endregion

	#region attacking logic

	private bool CanAttackEnemy()
    {
        float distSq = Vector3.SqrMagnitude(Vector3.ProjectOnPlane(GetTargetEntity.GetTrackingTransform.position - m_AnimalMainTransform.position, Vector3.up));
        float distToEscSq = Mathf.Pow(m_CurrentAttackComponent.GetAttackRange + GetTargetEntity.GetTrackableRadius, 2);
        if (distSq < distToEscSq )
        {
            m_AnimalAgent.isStopped = true;
            if (Time.time - m_fLastAttackTime > m_CurrentAttackComponent.GetAttackCooldownTime)
            {
                m_fLastAttackTime = Time.time;
                return true;
            }
        }
        return false;
    }

    private bool CanAttackPlayerFromWrangle() 
    {
        EntityTypeComponent targetEntity = m_Manager.GetPlayer;
        float distSq = Vector3.SqrMagnitude(Vector3.ProjectOnPlane(targetEntity.GetTrackingTransform.position - m_AnimalMainTransform.position, Vector3.up));
        float distToEscSq = Mathf.Pow(m_CurrentAttackComponent.GetAttackRange + GetTargetEntity.GetTrackableRadius, 2);
        if (distSq < distToEscSq)
        {
            if (Time.time - m_fLastAttackTime > m_CurrentAttackComponent.GetAttackCooldownTime)
            {
                m_CurrentAttackComponent = m_DamageAttackType;
                m_AnimalAnimator.SetCurrentAttackAnimation(m_DamageAttackType);

                SetRelevantTargetAnimal(targetEntity);
                m_fLastAttackTime = Time.time;
                return true;
            }
        }
        return false;
    }

    private void AttackAnimationComplete()
    {
        m_StateMachine.RequestTransition(typeof(AnimalIdleState));
        m_StateMachine.RequestTransition(typeof(AnimalIdleState));
    }

	Vector3 m_CachedTargetDirection = Vector3.zero;
	public void CacheTargetDirection()
	{
		m_CachedTargetDirection = (GetTargetEntity.GetTrackingTransform.position - m_AnimalMainTransform.position).normalized;
		m_AnimalAnimator.SetDesiredLookDirection(m_CachedTargetDirection);
	}

	public void TriggerAttack()
	{
		m_CurrentAttackComponent.AttackTarget(GetTargetEntity.gameObject, m_CachedTargetDirection);
        m_bHasAttackBeenTriggered = true;
	}

    bool m_bHasAttackBeenTriggered = false;

	#endregion
	#region targetting logic (general)

    private bool HasLostAttackTarget(float attackTargetRange) 
    {
        if (m_bHasAttackBeenTriggered) 
        {
            return false;
        }
        return HasLostTarget(attackTargetRange);
    }

    private bool HasLostTarget(float targetLostDistance)
    {
        if (!GetTargetEntity)
        {
            return true;
        }
        targetLostDistance += GetTargetEntity.GetTrackableRadius;
        float currentDistance = Vector3.SqrMagnitude(GetTargetEntity.GetTrackingTransform.position - m_AnimalMainTransform.position);
        float maxDistance = targetLostDistance * targetLostDistance;
        return currentDistance > maxDistance;
    }
	#endregion

	/////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// Breeding Data
	#region breeding logic
	private bool m_bIsMated = false;
    private float m_fLastBreedingTime = Mathf.NegativeInfinity;
    private bool FoundBreedingPartner()
    {
        if (!IsReadyToBreed())
            return false;

        m_Manager.GetTransformsMatchingType(m_EntityInformation.GetEntityInformation, out List<EntityToken> objTokens);

        for (int i = 0; i < objTokens.Count; i++)
        {
            if ((objTokens[i].GetEntityTransform.gameObject == gameObject))
                continue;

            if (Vector3.SqrMagnitude(objTokens[i].GetEntityType.GetTrackingTransform.transform.position - m_AnimalMainTransform.position) > m_fBreedingChaseStartRange * m_fBreedingChaseStartRange)
                continue;

            if (objTokens[i].GetEntityType.TryGetComponent(out AnimalComponent animal) && animal.OnRequestedAsBreedingPartner(gameObject))
            {
                return true;
            }
        }
        return false;
    }

    public bool OnRequestedAsBreedingPartner(GameObject partner) 
    {
        if (IsReadyToBreed() && !IsMated()) 
        {
            partner.GetComponent<AnimalComponent>().CompleteStartBreeding(gameObject);
            CompleteStartBreeding(partner);
            m_StateMachine.RequestTransition(typeof(AnimalBreedingChaseState));
            return true;
        }
        return false;
    }

    private void CompleteStartBreeding(GameObject partner) 
    {
        SetRelevantTargetAnimal(partner.GetComponent<EntityTypeComponent>());
        m_AnimalAnimator.HasSeenMate();
        m_bIsMated = true;
    }

    public void OnSuccessfullyBred() 
    {
        m_fFullness -= m_fBreedingHungerUsage;
        m_fLastBreedingTime = Time.time;
        StartCoroutine(WaitForHunger());
        OnTargetInvalidated();
    }

    private IEnumerator WaitForHunger() 
    {
        m_bIsHungry = false;
        yield return new WaitForSecondsRealtime(m_fBreedingHungerCooldownTime);
        m_bIsHungry = true;
    }

    // TODO:
    // Rethink logic on breeding cancellation - make it identical of finishing or interrupted.
    private bool CanBreedPartner()
    {
        float distSq = Vector3.SqrMagnitude(Vector3.ProjectOnPlane(GetTargetEntity.GetTrackingTransform.position - m_AnimalMainTransform.position, Vector3.up));
        float distToEscSq = m_fBreedingStartRange * m_fBreedingStartRange;
        if (distSq < distToEscSq)
		{
            m_AnimalAgent.isStopped = true;
            return true;
        }
        return false;
    }

    public bool IsReadyToBreed()
    {
        return m_fFullness > m_fBreedingHungerUsage && Time.time - m_fLastBreedingTime > m_fBreedingCooldownTime &&  m_StateMachine != null && m_StateMachine.GetCurrentState() == typeof(AnimalIdleState) && (m_StateMachine.TimeBeenInstate() > 4.0f);
    }

    public bool IsMated()
    {
        return m_bIsMated;
    }

	#endregion

	/////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// Staggered Data
	private bool IsStaggered()
    {
        return m_StateMachine.GetCurrentState() == typeof(AnimalStaggeredState);
    }

	private bool CanImpactHard()
	{
		for (int i = 0; i < m_CanStaggerStates.Length; i++)
		{
			if (m_StateMachine.GetCurrentState() == m_CanStaggerStates[i])
			{
				return true;
			}
		}

		return false;
	}

	protected bool ShouldEnterIdleFromWrangled()
	{
		return (Vector3.Angle(GetGroundDir(), Vector3.up) < 20.0f);
	}

	public Vector3 GetGroundDir()
	{
		if (Physics.Raycast(m_AnimalMainTransform.position + 0.5f * Vector3.up, -Vector3.up, out RaycastHit hit, 1, layerMask: (1 << 8)))
		{
			return hit.normal;
		}
		return Vector3.up;
	}

	/////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// Free Fall Data
	protected bool CanLeaveFreeFall()
    {
        return m_AnimalRigidBody.velocity.magnitude < 0.2f && m_AnimalMovement.IsNearNavMesh();
    }

    #endregion

    #region Unity Functions
    private readonly List<ContactPoint> m_Contacts = new List<ContactPoint>();

    public bool IsFull => m_fFullness > m_fMaximumFullness;


    private void OnDamageObject(float damageAmount, GameObject target) 
    {
        m_fFullness += damageAmount;
    }

    private Vector3 m_BodyVelocity = Vector3.zero;
    private bool m_bWasUsingNavmeshAgent = false;


    public void Pause()
    {
        if (m_AnimalAgent.enabled)
        {
            m_bWasUsingNavmeshAgent = true;
            m_BodyVelocity = m_AnimalAgent.velocity;
        }

        m_AnimalAgent.enabled = false;
        m_AnimalAnimator.enabled = false;
        m_AnimalMovement.enabled = false;
        enabled = false;
    }

    public void Unpause()
    {
        if (m_bWasUsingNavmeshAgent)
        {
            m_AnimalAgent.velocity = m_BodyVelocity;
            m_AnimalAgent.enabled = true;
            m_bWasUsingNavmeshAgent = false;
        }

        m_AnimalAnimator.enabled = true;
        m_AnimalMovement.enabled = true;
        enabled = true;
    }

	public float GetLowIdleTime => m_LowIdleTimer;
	public float GetHighIdleTime => m_HighIdleTimer;
	public EntityTypeComponent GetTargetEntity { get; private set; }
	public float GetHuntCheckInterval => m_fHuntCheckInterval;
    public float GetBreedCheckInterval => m_fBreedCheckInterval;
    public float GetHuntDistance => m_HuntBeginDistance;
	public float GetEvadedDistance => m_EvadedDistance;
	public Transform GetBodyTransform => m_AnimalBodyTransform;
	public float GetTotalStaggerTime => m_TotalStaggerTime;
	public float GetBreedingDuration => m_fBreedingDuration;
	public float GetBreedingStartDistance => m_fBreedingStartRange;
	public float GetBreedingChaseDistance => m_fBreedingChaseStartRange;
	public float GetAttackDamageTime => m_CurrentAttackComponent.GetAttackTriggerTime * m_CurrentAttackComponent.GetAttackDuration;
	public float GetAttackDuration => m_CurrentAttackComponent.GetAttackDuration;
	public float GetAttackRange => m_CurrentAttackComponent.GetAttackRange;
	public AttackBase GetAttack => m_CurrentAttackComponent;
	protected void Start()
    {
        m_bIsHungry = m_fFullness == m_fMaximumFullness ? false : true;

        m_AbductableComponent = GetComponent<AbductableComponent>();
        m_AnimalMovement = GetComponent<AnimalMovementComponent>();
        m_FreeFallComponent = GetComponent<FreeFallTrajectoryComponent>();
        m_AnimalRigidBody = GetComponent<Rigidbody>();
        m_AnimalAgent = GetComponent<NavMeshAgent>();
        m_AnimalAnimator = GetComponent<AnimalAnimationComponent>();
        m_EntityInformation = GetComponent<EntityTypeComponent>();
        m_AnimalHealthComponent = GetComponent<HealthComponent>();
        m_ThrowableComponent = GetComponent<ThrowableObjectComponent>();
        m_AttackableComponent = GetComponent<AttackBase>();
        DisableImpactFX();

        m_ThrowableComponent.OnTuggedByLasso += OnPulledByLasso;
        m_ThrowableComponent.OnStartSpinning += OnStartedLassoSpinning;
        m_ThrowableComponent.OnThrown += OnThrownByLasso;
        m_ThrowableComponent.OnReleased += OnReleasedByLasso;
        m_ThrowableComponent.OnWrangled += OnWrangledByLasso;

        m_EatAttackType.OnDamagedTarget += OnDamageObject;

        m_FreeFallComponent.OnObjectHitGround += OnHitGroundFromThrown;

        m_AbductableComponent.OnStartedAbducting += (UfoMain ufo, AbductableComponent abductable) => OnBeginAbducted();
        m_AbductableComponent.OnEndedAbducting += (UfoMain ufo, AbductableComponent abductable) => OnFinishedAbducted();
        m_AnimalHealthComponent.AddListener(this);

        if (m_Manager.HasLevelStarted()) 
        {
            m_StateMachine = new StateMachine<AnimalComponent>(new AnimalGrowingState(m_AnimalAnimator), this);
        }
		else 
        {
            m_StateMachine = new StateMachine<AnimalComponent>(new AnimalPrePlayState(m_AnimalAnimator), this);
        }

        m_Manager.AddToLevelStarted(this);

        m_StateMachine.AddState(new AnimalEvadingState(m_AnimalMovement, m_AnimalAnimator));
        bool huntsPlayer = false;
        for (int i = 0; i < m_EntityInformation.GetEntityInformation.GetHunts.Length; i++) 
        {
            huntsPlayer |= m_Manager.GetPlayer.GetEntityInformation == m_EntityInformation.GetEntityInformation.GetHunts[i];
        }

        if (huntsPlayer)
        {
            m_StateMachine.AddState(new AnimalWrangledAttackState(m_AnimalMovement, m_AnimalAnimator));
        }
        else
        {
            m_StateMachine.AddState(new AnimalWrangledRunState(m_AnimalMovement, m_AnimalAnimator));
        }
        m_StateMachine.AddState(new AnimalAbductedState(m_AnimalMovement));
        m_StateMachine.AddState(new AnimalThrowingState(m_AnimalAnimator));
        m_StateMachine.AddState(new AnimalFreeFallState(m_AnimalAnimator));
        m_StateMachine.AddState(new AnimalStaggeredState(m_AnimalAnimator));
        m_StateMachine.AddState(new AnimalDamagedState(m_AnimalAnimator));
        m_StateMachine.AddState(new AnimalLassoThrownState(m_AnimalAnimator));
        m_StateMachine.AddState(new AnimalDeathState(m_AnimalAnimator));
        m_StateMachine.AddState(new AnimalPredatorChaseState(m_AnimalMovement, m_AnimalAnimator));
        m_StateMachine.AddState(new AnimalAttackState(m_AnimalAnimator));
        m_StateMachine.AddState(new AnimalBreedingChaseState(m_AnimalMovement, m_AnimalAnimator));
        m_StateMachine.AddState(new AnimalBreedingState(m_AnimalAnimator));
        m_StateMachine.AddState(new AnimalIdleState(m_AnimalMovement, m_AnimalAnimator));

        m_StateMachine.AddStateGroup(StateGroup.Create(typeof(AnimalBreedingChaseState), typeof(AnimalBreedingState)).AddOnExit(OnTargetInvalidated));
        m_StateMachine.AddStateGroup(StateGroup.Create(typeof(AnimalWrangledState), typeof(AnimalAbductedState), typeof(AnimalAbductedAndWrangledState)).AddOnEnter(m_EntityInformation.RemoveFromTrackable).AddOnExit(m_EntityInformation.AddToTrackable));
        m_StateMachine.AddStateGroup(StateGroup.Create(typeof(AnimalPredatorChaseState), typeof(AnimalAttackState)).AddOnExit(OnTargetInvalidated).AddOnEnter(OnResetAttackBool));
        m_StateMachine.AddStateGroup(StateGroup.Create(typeof(AnimalEvadingState)).AddOnExit(OnTargetInvalidated));
        m_StateMachine.AddStateGroup(StateGroup.Create(typeof(AnimalWrangledState)).AddOnExit(OnTargetInvalidated));

        m_StateMachine.AddStateGroup(StateGroup.Create(typeof(AnimalFreeFallState), typeof(AnimalStaggeredState)).AddOnEnter(EnableImpactFX).AddOnExit(DisableImpactFX));

        m_StateMachine.AddTransition(typeof(AnimalIdleState), typeof(AnimalDeathState), () => IsDead);
        m_StateMachine.AddAnyTransition(typeof(AnimalAbductedState), ShouldEnterAbducted);
        m_StateMachine.AddAnyTransition(typeof(AnimalWrangledState), ShouldEnterWrangled);
        m_StateMachine.AddTransition(typeof(AnimalThrowingState), typeof(AnimalFreeFallState), () => !IsWrangled);
        m_StateMachine.AddTransition(typeof(AnimalWrangledState), typeof(AnimalStaggeredState), () => m_bShouldStagger);
        m_StateMachine.AddTransition(typeof(AnimalWrangledState), typeof(AnimalAbductedAndWrangledState), () => IsInTractorBeam);
        m_StateMachine.AddTransition(typeof(AnimalAbductedState), typeof(AnimalAbductedAndWrangledState), () => IsWrangled);
        m_StateMachine.AddTransition(typeof(AnimalAbductedState), typeof(AnimalFreeFallState), () => !IsInTractorBeam);
        m_StateMachine.AddTransition(typeof(AnimalWrangledState), typeof(AnimalFreeFallState), () => !IsWrangled && CanLeaveFreeFall() && m_bShouldStagger);
        m_StateMachine.AddTransition(typeof(AnimalWrangledState), typeof(AnimalIdleState), () => !IsWrangled && CanLeaveFreeFall() && !m_bShouldStagger);
        m_StateMachine.AddTransition(typeof(AnimalAbductedState), typeof(AnimalFreeFallState), () => !IsInTractorBeam);
        m_StateMachine.AddTransition(typeof(AnimalGrowingState), typeof(AnimalIdleState), () => m_StateMachine.TimeBeenInstate() > m_BornTime);

        // evading states
        m_StateMachine.AddTransition(typeof(AnimalIdleState), typeof(AnimalEvadingState), ShouldEvade);
        m_StateMachine.AddTransition(typeof(AnimalEvadingState), typeof(AnimalIdleState), HasEvadedEnemy);

        // breeding and breeding chase states
        m_StateMachine.AddTransition(typeof(AnimalIdleState), typeof(AnimalBreedingChaseState), FoundBreedingPartner);

        m_StateMachine.AddTransition(typeof(AnimalBreedingState), typeof(AnimalIdleState), () => HasLostTarget(m_fBreedingChaseEndRange));
        m_StateMachine.AddTransition(typeof(AnimalBreedingChaseState), typeof(AnimalIdleState), () => HasLostTarget(m_fBreedingChaseEndRange));
        m_StateMachine.AddTransition(typeof(AnimalBreedingChaseState), typeof(AnimalIdleState), ShouldStopActionToEvadeNext);
        m_StateMachine.AddTransition(typeof(AnimalBreedingState), typeof(AnimalIdleState), ShouldStopActionToEvadeNext);
        m_StateMachine.AddTransition(typeof(AnimalBreedingChaseState), typeof(AnimalBreedingState), CanBreedPartner);

        // free fall transitionary states
        m_StateMachine.AddTransition(typeof(AnimalFreeFallState), typeof(AnimalStaggeredState), () => CanLeaveFreeFall() && m_bShouldStagger);
        m_StateMachine.AddTransition(typeof(AnimalFreeFallState), typeof(AnimalIdleState), () => CanLeaveFreeFall() && !m_bShouldStagger);

        // attack and attack chase states
        m_StateMachine.AddTransition(typeof(AnimalPredatorChaseState), typeof(AnimalIdleState), () => HasLostTarget(m_HuntEndDistance));
        m_StateMachine.AddTransition(typeof(AnimalPredatorChaseState), typeof(AnimalIdleState), ShouldStopActionToEvadeNext);
        m_StateMachine.AddTransition(typeof(AnimalIdleState), typeof(AnimalPredatorChaseState), CanHuntEnemy);
        m_StateMachine.AddTransition(typeof(AnimalPredatorChaseState), typeof(AnimalAttackState), CanAttackEnemy);
        m_StateMachine.AddTransition(typeof(AnimalAttackState), typeof(AnimalIdleState), () => HasLostAttackTarget(m_CurrentAttackComponent.GetAttackRange * 3.0f));

        // only aggressive creatures can attack players from wrangled state
        m_StateMachine.AddTransition(typeof(AnimalWrangledAttackState), typeof(AnimalAttackState), CanAttackPlayerFromWrangle);

        m_Manager.AddToPauseUnpause(this);
        m_StateMachine.InitializeStateMachine();
    }

    private void EnableImpactFX() 
    {
        m_ThrowableComponent.EnableImpacts(true);
    }

    private void DisableImpactFX() 
    {
        m_ThrowableComponent.EnableImpacts(false);
    }

	private void OnDestroy()
	{
        m_Manager.RemoveFromPauseUnpause(this);
    }

	private bool ShouldEnterWrangled() 
    {
        if (IsWrangled && !IsInTractorBeam && !IsStaggered()) 
        {
            SetRelevantTargetAnimal(m_Manager.GetPlayer);
            return true;
        }
        return false;
    }

    private bool ShouldEnterAbducted() 
    {
        if (!IsWrangled && IsInTractorBeam && !IsStaggered()) 
        {
            return true;
        }
        return false;
    }

    public void FixedUpdate()
    {
		m_CurrentStaggerCooldown.DecrementCooldown(Time.fixedDeltaTime);
        m_CurrentBreedingCooldown.DecrementCooldown(Time.fixedDeltaTime);
		m_StateMachine.Tick(Time.fixedDeltaTime);
    }

	public void Update()
	{
        m_debugTextComponent?.AddLine(string.Format("Current animal state: {0}", m_StateMachine.GetCurrentState().ToString()));
        m_debugTextComponent?.AddLine(string.Format("Current target entity: {0}", GetTargetEntity ? GetTargetEntity.name : "No Target"));
        m_debugTextComponent?.AddLine(string.Format("Current food: {0} target food: {1}", m_fFullness, m_fMaximumFullness));
    }

	public void OnEntityTakeDamage(GameObject go1, GameObject go2, DamageType type)
	{
        if (type == DamageType.PredatorDamage)
        {
            m_StateMachine.RequestTransition(typeof(AnimalDamagedState));
        }
    }

	public void OnEntityDied(GameObject go1, GameObject go2, DamageType type)
	{
        switch (type)
        {
            case (DamageType.UFODamage):
                Destroy(gameObject);
                return;
            case (DamageType.PredatorDamage):
                break;
            case (DamageType.FallDamage):
                StartCoroutine(DelayedDeathDestroy());
                break;
            default:
                Destroy(gameObject);
                break;
        }
        m_StateMachine.RequestTransition(typeof(AnimalDamagedState));
        IsDead = true;
        m_EntityInformation.OnKilled();
    }

	public void LevelStarted()
	{
        m_StateMachine.RequestTransition(typeof(AnimalIdleState));
	}

	public void LevelFinished()
	{
		throw new NotImplementedException();
	}

	[SerializeField] private DebugTextComponent m_debugTextComponent;
    #endregion
}


public class AnimalIdleState : AStateBase<AnimalComponent>
{
    private readonly AnimalMovementComponent m_animalMovement;
    private readonly AnimalAnimationComponent m_animalMovementAnimator;

    public AnimalIdleState(AnimalMovementComponent animalMovement, AnimalAnimationComponent animalAnimator)
    {
        m_animalMovement = animalMovement;
        m_animalMovementAnimator = animalAnimator;
		AddTimers(1);
    }

	public override void Tick()
	{
		if (m_animalMovement.HasReachedDestination())
		{
			StartTimer(0);
		}
		else if (m_animalMovement.IsStuck())
		{
			StartTimer(0);
			if (GetTimerVal(0) < -Host.GetLowIdleTime)
				SetTimer(0, -Host.GetLowIdleTime);
		}

		if (GetTimerVal(0) > 0f)
		{
			if (m_animalMovement.ChooseRandomDestination())
			{
				SetTimer(0, -UnityEngine.Random.Range(Host.GetLowIdleTime, Host.GetHighIdleTime));
				StopTimer(0);
			}
		}
	}
	public override void OnEnter()
	{
		StartTimer(0);
		Host.SetManagedByAgent(true);
		Host.DisablePhysics();
		m_animalMovementAnimator.SetWalkAnimation();
		m_animalMovementAnimator.IsIdling();
		m_animalMovement.SetWalking();
		m_animalMovement.ClearDestination();
	}
}

public class AnimalPrePlayState : AStateBase<AnimalComponent> 
{
    private readonly AnimalAnimationComponent m_animalAnimator;
    public AnimalPrePlayState(AnimalAnimationComponent animalAnimator)
    {
        m_animalAnimator = animalAnimator;
    }

    public override void OnEnter()
    {
        m_animalAnimator.SetSizeOnStartup();
    }
}

public class AnimalEvadingState : AStateBase<AnimalComponent>
{
    private readonly AnimalAnimationComponent animalAnimator;
    private readonly AnimalMovementComponent animalMovement;
    public AnimalEvadingState(AnimalMovementComponent animalMovement, AnimalAnimationComponent animalAnimator)
    {
        this.animalMovement = animalMovement;
        this.animalAnimator = animalAnimator;
        AddTimers(1);
    }

    public override void OnEnter()
    {
        animalMovement.RunAwayFromObject(Host.GetTargetEntity.GetTrackingTransform, Host.GetEvadedDistance);
        animalAnimator.SetRunAnimation();
        animalMovement.SetRunning();

		Host.SetManagedByAgent(true);
		Host.DisablePhysics();

    }
    public override void Tick()
    {
        if (GetTimerVal(0) > Host.GetHuntCheckInterval) 
        {
            animalMovement.RunAwayFromObject(Host.GetTargetEntity.GetTrackingTransform, Host.GetEvadedDistance);
			ClearTimer(0);
        }
        if (animalMovement.IsStuck()) 
        {
            animalMovement.RunAwayFromObject(Host.GetTargetEntity.GetTrackingTransform, Host.GetEvadedDistance);
			SetTimer(0, Host.GetHuntCheckInterval / 2f);
        }
    }
}

public class AnimalWrangledRunState : AnimalWrangledState
{
    private readonly AnimalMovementComponent animalMovement;
    private readonly AnimalAnimationComponent animalMovementAnimator;
    public AnimalWrangledRunState(AnimalMovementComponent animalMovement, AnimalAnimationComponent animalAnimator)
    {
        this.animalMovement = animalMovement;
        this.animalMovementAnimator = animalAnimator;
    }

    public override void Tick()
    {
        Vector3 dir = (Host.GetBodyTransform.position - Host.GetTargetEntity.GetTrackingTransform.position).normalized;
        animalMovement.RunInDirection(dir);
        animalMovementAnimator.SetDesiredLookDirection(dir);
    }
    public override void OnEnter()
    {
		Host.SetManagedByAgent(false);
		Host.SetGeneralPhysics();

        animalMovement.enabled = false;
        animalMovementAnimator.SetEscapingAnimation();
    }
}

public class AnimalWrangledAttackState : AnimalWrangledState
{
    private readonly AnimalMovementComponent animalMovement;
    private readonly AnimalAnimationComponent animalAnimator;
    public AnimalWrangledAttackState(AnimalMovementComponent animalMovement, AnimalAnimationComponent animalAnimator)
    {
        this.animalMovement = animalMovement;
        this.animalAnimator = animalAnimator;
    }

    public override void Tick()
    {
        Vector3 dir = -(Host.GetBodyTransform.position - Host.GetTargetEntity.GetTrackingTransform.position).normalized;
        animalMovement.RunInDirection(dir);
        animalAnimator.SetDesiredLookDirection(dir);
    }
    public override void OnEnter()
    {
		Host.SetManagedByAgent(false);
		Host.SetGeneralPhysics();

        animalMovement.enabled = false;
        animalAnimator.SetRunAnimation();
    }
}

public abstract class AnimalWrangledState : AStateBase<AnimalComponent> {  }

public class AnimalThrowingState : AStateBase<AnimalComponent>
{
    private readonly AnimalAnimationComponent animalAnimator;
    public AnimalThrowingState(AnimalAnimationComponent animalAnimator)
    {
        this.animalAnimator = animalAnimator;
    }
}

public class AnimalDamagedState : AStateBase<AnimalComponent>
{
    private readonly AnimalAnimationComponent animalAnimator;
    public AnimalDamagedState(AnimalAnimationComponent animalAnimator)
    {
        this.animalAnimator = animalAnimator;
    }
}

public class AnimalStaggeredState : AStateBase<AnimalComponent>
{
    private readonly AnimalAnimationComponent animalAnimator;

    public AnimalStaggeredState(AnimalAnimationComponent animalAnimator)
    {       
        this.animalAnimator = animalAnimator;
        AddTimers(1);
    }

    public override void OnEnter()
    {
        animalAnimator.SetIdleAnimation();
        animalAnimator.SetStaggeredAnimation();

		Host.SetGeneralPhysics();
    }

    public override void Tick()
    {
        if (GetTimerVal(0) > Host.GetTotalStaggerTime) 
        {
			RequestTransition<AnimalIdleState>();
        }
    }
}

public class AnimalLassoThrownState : AStateBase<AnimalComponent> 
{
    private readonly AnimalAnimationComponent animalAnimator;
    public AnimalLassoThrownState(AnimalAnimationComponent animalAnimator)
    {
        this.animalAnimator = animalAnimator;
    }

    public override void OnEnter()
    {
		Host.SetManagedByAgent(false);
		Host.SetGeneralPhysics();

		animalAnimator.SetIdleAnimation();
    }
}

public class AnimalBreedingState : AStateBase<AnimalComponent>
{
    private readonly AnimalAnimationComponent animalAnimator;

    public AnimalBreedingState(AnimalAnimationComponent animalAnimator)
    {
        this.animalAnimator = animalAnimator;
		AddTimers(1);
	}

	public override void OnEnter()
	{
        Host.CacheTargetDirection();
        animalAnimator.TriggerBreedingAnimation();
        StartTimer(0);
	}

	public override void Tick()
    {
        if (GetTimerVal(0) > Host.GetBreedingDuration) 
        {
            Host.OnBreedingComplete();
            ClearTimer(0);
            StopTimer(0);
		}
    }
}

public class AnimalBreedingChaseState : AStateBase<AnimalComponent> 
{
    private readonly AnimalAnimationComponent animalAnimator;
    private readonly AnimalMovementComponent animalMovement;

    public AnimalBreedingChaseState(AnimalMovementComponent animalMovement, AnimalAnimationComponent animalAnimator)
    {
        this.animalMovement = animalMovement;
        this.animalAnimator = animalAnimator;
        AddTimers(1);
    }

    public override void OnEnter()
    {
		animalMovement.RunTowardsObject(Host.GetTargetEntity.GetTrackingTransform, Host.GetBreedingChaseDistance, 0.8f * Host.GetBreedingStartDistance);
		animalAnimator.SetRunAnimation();
        animalMovement.SetRunning();

		Host.SetManagedByAgent(true);
		Host.DisablePhysics();
        StartTimer(0);
	}
    public override void Tick()
    {
		if (GetTimerVal(0) > Host.GetBreedCheckInterval)
		{
			animalMovement.RunTowardsObject(Host.GetTargetEntity.GetTrackingTransform, Host.GetBreedingChaseDistance, 0.8f * Host.GetBreedingStartDistance);
			ClearTimer(0);
		}
		if (animalMovement.IsStuck())
		{
			animalMovement.RunTowardsObject(Host.GetTargetEntity.GetTrackingTransform, Host.GetBreedingChaseDistance, 0.8f * Host.GetBreedingStartDistance);
			SetTimer(0, Host.GetBreedCheckInterval / 2f);
		}
	}
}


public class AnimalFreeFallState : AStateBase<AnimalComponent> 
{
    private readonly AnimalAnimationComponent animalAnimator;
    public AnimalFreeFallState(AnimalAnimationComponent animalAnimator) 
    {
        this.animalAnimator = animalAnimator;
    }

    public override void OnEnter()
    {
		Host.SetManagedByAgent(false);
		Host.SetGeneralPhysics();

		animalAnimator.SetFreeFallAnimation();
    }
}

public class AnimalDeathState : AStateBase<AnimalComponent>
{
        private readonly AnimalAnimationComponent animalAnimator;
    public AnimalDeathState(AnimalAnimationComponent animalAnimator) 
    {
        this.animalAnimator = animalAnimator;
    }

    public override void OnEnter()
    {
		Host.SetManagedByAgent(false);
		Host.SetGeneralPhysics();

        animalAnimator.SetIdleAnimation();
    }
}
    
public class AnimalAbductedState : AStateBase<AnimalComponent>
{
    private readonly AnimalMovementComponent animalMovement;

    public AnimalAbductedState(AnimalMovementComponent animalMovement)
    {
       this.animalMovement = animalMovement;
    }

    public override void OnEnter()
    {
        animalMovement.enabled = false;

		Host.SetManagedByAgent(false);
		Host.SetAbductionPhysics();
	}
}

public class AnimalAbductedAndWrangledState : AStateBase<AnimalComponent>
{

}

public class AnimalAttackState : AStateBase<AnimalComponent>
{
    private readonly AnimalAnimationComponent m_animalAnimator;
    public AnimalAttackState(AnimalAnimationComponent animalAnimator)
    {
        m_animalAnimator = animalAnimator;
		AddTimers(2);
    }

    public override void OnEnter()
    {
		m_animalAnimator.TriggerAttackAnimation();
		StartTimer(0);
		StartTimer(1);
		Host.CacheTargetDirection();
	}

    private void SomeMemeFunction() { }

	public override void OnExit()
	{
        SomeMemeFunction();
	}

	public override void Tick()
	{
		if (GetTimerVal(0) > Host.GetAttackDamageTime)
		{
			Host.TriggerAttack();
			ClearTimer(0);
			StopTimer(0);
		}
		if (GetTimerVal(1) > Host.GetAttackDuration)
		{
			RequestTransition<AnimalIdleState>();
		}
	}
}

public class AnimalGrowingState : AStateBase<AnimalComponent>
{
    private readonly AnimalAnimationComponent m_animalAnimator;
    public AnimalGrowingState(AnimalAnimationComponent animalAnimator) 
    {
        m_animalAnimator = animalAnimator;
    }
	public override void OnEnter()
	{
        m_animalAnimator.OnBorn();
	}

}

public class AnimalPredatorChaseState : AStateBase<AnimalComponent>
{
    private readonly AnimalAnimationComponent m_animalAnimator;
    private readonly AnimalMovementComponent m_animalMovement;

    public AnimalPredatorChaseState(AnimalMovementComponent animalMovement, AnimalAnimationComponent animalAnimator)
    {
        m_animalMovement = animalMovement;
        m_animalAnimator = animalAnimator;
		AddTimers(1);
    }

    public override void OnEnter()
    {
        m_animalMovement.RunTowardsObject(Host.GetTargetEntity.GetTrackingTransform, Host.GetHuntDistance, Host.GetAttackRange + Host.GetTargetEntity.GetTrackableRadius);
        m_animalAnimator.SetRunAnimation();
        m_animalMovement.SetRunning();

		Host.SetManagedByAgent(true);
		Host.DisablePhysics();
	}
    public override void Tick()
    {
		EntityTypeComponent targetTransform = Host.GetTargetEntity;
		float moveDist = Host.GetHuntDistance;
		float attackDist = Host.GetAttackRange + targetTransform.GetTrackableRadius;

		if (GetTimerVal(0) > Host.GetHuntCheckInterval)
        {
            m_animalMovement.RunTowardsObject(targetTransform.GetTrackingTransform, moveDist, attackDist);
			ClearTimer(0);
		}
        else if (m_animalMovement.IsStuck())
        {
            m_animalMovement.RunTowardsObject(targetTransform.GetTrackingTransform, Host.GetHuntDistance, attackDist);
            SetTimer(0, Host.GetHuntCheckInterval / 2f);
        }
		else 
        {
            m_animalMovement.CheckStoppingDistanceForChase(targetTransform.GetTrackingTransform, attackDist);
        }
    }
}
