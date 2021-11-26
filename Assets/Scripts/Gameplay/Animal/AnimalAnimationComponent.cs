using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;
using EZCameraShake;
using UnityUtils;

public class AnimalAnimationComponent : MonoBehaviour
{

    [Header("Animation Curves")]
    [Header("Walk Animation Curves")]
    [SerializeField] public AnimationCurve m_HopAnimationCurve;
    [SerializeField] public AnimationCurve m_TiltAnimationCurve;
    [SerializeField] public AnimationCurve m_YawAnimationCurve;
    [SerializeField] public AnimationCurve m_ForwardBackwardAnimationCurve;
    [SerializeField] public AnimationCurve m_WalkHorizontalAnimationCurve;
    [SerializeField] public AnimationCurve m_StepSoundCurve;
    [Header("Damaged Animation Curves")]
    [SerializeField] public AnimationCurve m_DamagedHopAnimationCurve;
    [SerializeField] private AnimationCurve m_DamagedVisualsAnimationCurve;
    [Header("Breeding Animation Curves")]
    [SerializeField] private AnimationCurve m_BreedingHopAnimationCurve;
    [SerializeField] private AnimationCurve m_BreedingPitchAnimationCurve;
    [Header("Born Animation Curves")]
    [SerializeField] private AnimationCurve m_BornSizeAnimationCurve;
    [Header("Movement Animation Durations")]
    [Range(0f, 2f)] [SerializeField] private float m_RunAnimationTime;
    [Range(0f, 2f)] [SerializeField] private float m_WalkAnimationTime;
    [Range(0f, 2f)] [SerializeField] private float m_EscapingAnimationTime;
    [Range(0f, 2f)] [SerializeField] private float m_WalkWindupTime = 1.0f;
    [Range(0f, 1f)] [SerializeField] private float m_Phase = 1.0f;
    [Range(0f, 720f)] [SerializeField] private float m_AnimRotationSpeed;
    [Range(0f, 60f)] [SerializeField] private float m_AnimMoveSpeed;

    [Header("Movement Animation Sizes")]
    [SerializeField] private float m_TiltSizeMultiplier = 1.0f;
    [SerializeField] private float m_HopHeightMultiplier = 1.0f;
    [SerializeField] private float m_YawSizeMultiplier = 1.0f;
    [SerializeField] private float m_HorizontalMovementMultiplier = 1.0f;
    [SerializeField] private float m_ForwardBackwardMovementMultiplier = 1.0f;
    [SerializeField] private float m_MaximumRunAnimationSpeed = 0.01f;


    [Header("Miscellaneous Params")]
    [SerializeField] private float m_AttackAnimationDuration = 1.0f;
    [SerializeField] private float m_DamagedAnimationDuration = 1.0f;
    [Range(0f, 0.5f)][SerializeField] private float m_AnimationSpeedRandom;
    [SerializeField] private float m_fAnimationSizeScalar;
    [SerializeField] private float m_fPullTime;

    [Header("Variation Parameters")]
    [Range(0f, 0.3f)] [SerializeField] protected float m_SizeVariation;

    [Header("Confusion and Damage Params")]
    [SerializeField] private float m_ConfusionAnimWindupTime;
    [SerializeField] private float m_ConfusionRotationSpeed;
    [ColorUsage(true, true)] [SerializeField] private Color m_DamagedColor;

    [Header("Breeding References")]
    [SerializeField] private float m_BreedHopDuration;
    [SerializeField] private AnimationCurve m_BreedHopProbability;

    [Header("Audio Identifiers")]
    [SerializeField] private string m_AnimalCallSoundIdentifier;
    [SerializeField] private string m_AnimalStepSoundIdentifier;
    [SerializeField] private string m_AnimalImpactSoundIdentifier;

    [Header("Object references")]
    [SerializeField] private Transform m_tAnimationTransform;
    [SerializeField] private Transform m_tParentObjectTransform;
    [SerializeField] private Transform m_tScaleObjectTransform;
    [SerializeField] private Rigidbody m_vCowRigidBody;
    [SerializeField] private NavMeshAgent m_Agent;
    [SerializeField] private Transform m_ConfusionEffectTiltTransform;
    [SerializeField] private Transform m_ConfusionEffectRotationTransform;
    [SerializeField] private Transform m_DraggingParticlesTransform;
    [SerializeField] private GameObject m_BornEffectsPrefab;

    [SerializeField] private ParticleEffectsController m_AlertAttackEffectsController;
    [SerializeField] private ParticleEffectsController m_AlertFoodEffectsController;
    [SerializeField] private ParticleEffectsController m_AlertFleeEffectsController;
    [SerializeField] private ParticleEffectsController m_AlertBreedEffectsController;
    [SerializeField] private ParticleEffectsController m_AlertIdleEffectsController;
    private ParticleEffectsController m_ActiveController;

    [SerializeField] private ParticleEffectsController m_FreeFallingParticleController;
    [SerializeField] private ParticleEffectsController m_BashedParticleController;

    [SerializeField] private AudioManager m_AudioManager;
    [SerializeField] private List<MeshRenderer> m_DamagedMeshRenderers;
    [SerializeField] private CowGameManager m_Manager;
    [SerializeField] private PhysicalEntity m_PhysicalEntity;

    private Vector3 m_vTargetForward;

	private StateMachine<AnimalAnimationComponent> m_AnimatorStateMachine;
	private AnimalComponent m_AnimalComponent;
    private float m_TotalAnimationTime = 1.0f;
    private float m_CurrentAnimationTime;
    private float m_fCurrentConfusionAnimTime;
    private float m_fSizeVariationActual = 1.0f;
    private float m_fAnimationSpeedRandomMult;

    public float GetCurrentHopHeight => m_HopHeightMultiplier * m_HopAnimationCurve.Evaluate((m_CurrentAnimationTime + m_Phase) % 1);
    public float GetCurrentTilt => m_TiltSizeMultiplier * m_TiltAnimationCurve.Evaluate(m_CurrentAnimationTime);
    public float GetCurrentForwardBackward => m_ForwardBackwardMovementMultiplier * m_ForwardBackwardAnimationCurve.Evaluate(m_CurrentAnimationTime);
    public float GetCurrentYaw => m_YawSizeMultiplier * m_YawAnimationCurve.Evaluate(m_CurrentAnimationTime);
    public float GetCurrentStepSound => m_StepSoundCurve.Evaluate((m_CurrentAnimationTime + m_Phase) % 1);
    public float GetCurrentHorizontalMovement => m_HorizontalMovementMultiplier * m_WalkHorizontalAnimationCurve.Evaluate((m_CurrentAnimationTime + m_Phase) % 1);
    public AudioManager GetAudioManager => m_AudioManager;

	public Transform MainTransform => m_tParentObjectTransform;
    public Transform ScaleTransform => m_tScaleObjectTransform;
	public Transform AnimTransform => m_tAnimationTransform;
	public float AnimRotationSpeed => m_AnimRotationSpeed;
	public float AnimLinearSpeed => m_AnimMoveSpeed;
	public Rigidbody AnimalBody => m_vCowRigidBody;

    public PhysicalEntity AnimalPhysEnt => m_PhysicalEntity;

	public AnimationCurve GetAttackPitchCurve => m_CurrentAttack.GetPitchCurve;
	public AnimationCurve GetAttackTiltCurve => m_CurrentAttack.GetTiltCurve;
	public AnimationCurve GetAttackForwardCurve => m_CurrentAttack.GetForwardCurve;
	public AnimationCurve GetAttackHopCurve => m_CurrentAttack.GetHopCurve;
	public float GetAttackAnimDuration => m_CurrentAttack.GetAttackDuration;

    public float GetBreedingDuration => m_AnimalComponent.GetBreedingDuration;
    public float GetBornDuration => m_AnimalComponent.GetBornDuration;
    public float GetBreedHopDuration => m_BreedHopDuration;
    public AnimationCurve GetBreedHopProbabilityCurve => m_BreedHopProbability;
    public float GetSizeMult => m_fSizeVariationActual;

    public float GetPullTime => m_fPullTime;

	public float DamagedAnimTime => m_AnimalComponent.GetDamagedDuration;
	public AnimationCurve DamagedColorCurve => m_DamagedVisualsAnimationCurve;
	public AnimationCurve DamagedHopCurve => m_DamagedHopAnimationCurve;
	public Color DamagedFlashColor => m_DamagedColor;


    public AnimationCurve BreedingHopCurve => m_BreedingHopAnimationCurve;
    public AnimationCurve BreedingPitchCurve => m_BreedingPitchAnimationCurve;

    public AnimationCurve BornSizeCurve => m_BornSizeAnimationCurve;

    public float WalkWindup => m_WalkWindupTime;
	public NavMeshAgent Agent => m_Agent;

	public Vector3 TargetDirection => m_vTargetForward;

	private AttackBase m_CurrentAttack;


	public void PlayAnimalStepSound(float strength) 
    {
        m_AudioManager.Play(m_AnimalStepSoundIdentifier);
    }

    public void PlayAnimalCall() 
    {
        m_AudioManager.Play(m_AnimalCallSoundIdentifier);
    }

    public void PlayAnimalImpactSound() 
    {
        m_AudioManager.Play(m_AnimalImpactSoundIdentifier);
    }

    public void EnableDraggingParticles(bool enabled) 
    {
        
    }

    public void SetSizeOnStartup() 
    {
        ScaleTransform.localScale = GetSizeMult * Vector3.one;
    }


    public void SetCurrentAttackAnimation(in AttackBase m_NewAttack) 
    {
		m_CurrentAttack = m_NewAttack;
    }

    public Quaternion GetOrientation(Vector3 forward) 
    {
        if (forward.sqrMagnitude < Mathf.Epsilon) 
            forward = AnimTransform.forward;
        Vector3 up = AnimTransform.up;
        if (Physics.Raycast(MainTransform.position + 0.5f * Vector3.up, -Vector3.up, out RaycastHit hit, 1, layerMask: (1 << 8)))
        {
            up = hit.normal;
            forward = Vector3.ProjectOnPlane(forward, up).normalized;
        }
        return Quaternion.LookRotation(forward, up);
    }

    private void Awake()
    {
        m_fAnimationSpeedRandomMult = 1 + UnityEngine.Random.Range(-m_AnimationSpeedRandom, m_AnimationSpeedRandom);

        m_AnimatorStateMachine = new StateMachine<AnimalAnimationComponent>(new AnimalIdleAnimationState(), this);
        m_AnimatorStateMachine.AddState(new AnimalStaggeredAnimationState());
        m_AnimatorStateMachine.AddState(new AnimalWalkingAnimationState());
        m_AnimatorStateMachine.AddState(new AnimalCapturedAnimationState());
        m_AnimatorStateMachine.AddState(new AnimalFreeFallAnimationState(m_FreeFallingParticleController));
        m_AnimatorStateMachine.AddState(new AnimalAttackAnimationState());
        m_AnimatorStateMachine.AddState(new AnimalDamagedAnimationState());
        m_AnimatorStateMachine.AddState(new AnimalBreedingAnimationState());
        m_AnimatorStateMachine.AddState(new AnimalBornAnimationState());
        m_AnimatorStateMachine.AddState(new AnimalCapturedPulledState());
        m_AnimalComponent = GetComponent<AnimalComponent>();

        m_fSizeVariationActual = (1 + UnityEngine.Random.Range(-m_SizeVariation, m_SizeVariation));
    }

    void FixedUpdate()
    {
        m_AnimatorStateMachine.Tick(Time.fixedDeltaTime);

        m_CurrentAnimationTime = (m_CurrentAnimationTime + (Time.fixedDeltaTime * m_fAnimationSpeedRandomMult) / m_TotalAnimationTime) % 1;
	}

	private void Update()
	{
        m_debugTextComponent?.AddLine(string.Format("current animal animation state: {0}", m_AnimatorStateMachine.GetCurrentState().ToString()));
    }

	[SerializeField] private DebugTextComponent m_debugTextComponent;

	void Start()
    {
        m_AnimatorStateMachine.InitializeStateMachine();
        m_CurrentAnimationTime = UnityEngine.Random.Range(0.0f, 1.0f);
        m_ActiveController = m_AlertIdleEffectsController;
    }

    public void OnBorn() 
    {
        Instantiate(m_BornEffectsPrefab, MainTransform.position, Quaternion.identity, null);
        m_AnimatorStateMachine.RequestTransition(typeof(AnimalBornAnimationState));
    }

    public void SetIdleAnimation() 
    {
        m_AnimatorStateMachine.RequestTransition(typeof(AnimalIdleAnimationState));
    }

    private enum AnimalMood 
    {
        Fleeing,
        Attacking,
        Hunting,
        Horny,
        UnSet
    }

    private AnimalMood m_LastMood = AnimalMood.UnSet;

    bool TrySetAnimalMood(AnimalMood mood, bool overrideMood = false) 
    {
        if (!m_LastMood.Equals(mood) || overrideMood) 
        {
            m_ActiveController.TurnOffAllSystems();
            m_LastMood = mood;
            return true;
        }
        return false;
    }

    public void IsScared() 
    {
        if (TrySetAnimalMood(AnimalMood.Fleeing, true)) 
        {
            m_ActiveController = m_AlertAttackEffectsController;
            m_AlertFleeEffectsController.PlayOneShot();
        }
    }

    public void HasSeenEnemy() 
    {
        if (TrySetAnimalMood(AnimalMood.Attacking, true))
        {
            m_ActiveController = m_AlertAttackEffectsController;
            m_AlertAttackEffectsController.PlayOneShot();
		}
    }

    public void HasSeenFood() 
    {
        if (TrySetAnimalMood(AnimalMood.Hunting))
        {
            m_ActiveController = m_AlertFoodEffectsController;
            m_AlertFoodEffectsController.PlayOneShot();
        }
    }

    public void HasSeenMate() 
    {
        if (TrySetAnimalMood(AnimalMood.Horny, true)) 
        {
            m_ActiveController = m_AlertBreedEffectsController;
            m_AlertBreedEffectsController.TurnOnAllSystems();
        }
    }

    public void SetWalkAnimation() 
    {
		enabled = true;
        m_TotalAnimationTime = m_WalkAnimationTime;
        m_AnimatorStateMachine.RequestTransition(typeof(AnimalWalkingAnimationState));
    }

    private void DeathAnimationFinished()   
    {
        Destroy(m_tParentObjectTransform.gameObject);
    }

    public void SetRunAnimation() 
    {
		enabled = true;
		m_TotalAnimationTime = m_RunAnimationTime;
        m_AnimatorStateMachine.RequestTransition(typeof(AnimalWalkingAnimationState));
    }

    public void SetFreeFallAnimation() 
    {
        m_AnimatorStateMachine.RequestTransition(typeof(AnimalFreeFallAnimationState));
    }

    public void SetEscapingAnimation() 
    {
		enabled = true;
		m_TotalAnimationTime = m_RunAnimationTime;
        m_AnimatorStateMachine.RequestTransition(typeof(AnimalCapturedAnimationState));
    }

    public void SetStaggeredAnimation() 
    {
        m_AnimatorStateMachine.RequestTransition(typeof(AnimalStaggeredAnimationState));
    }
    
    public void TriggerAttackAnimation() 
    {
        m_AnimatorStateMachine.RequestTransition(typeof(AnimalAttackAnimationState));
    }

    public void TriggerDamagedAnimation() 
    {
        m_AnimatorStateMachine.RequestTransition(typeof(AnimalDamagedAnimationState));
    }

    public void TriggerBreedingAnimation() 
    {
        m_AnimatorStateMachine.RequestTransition(typeof(AnimalBreedingAnimationState));
    }

    public void StopBreedingEffects() 
    {

    }

    public void TriggerBashedAnimation() 
    {

    }

    public void TriggerConfuseAnim() 
    {
        m_fConfusionAnimMultiplier = 1.0f;
        StartCoroutine(ConfusionRotationCoroutine());
    }

    public void RemoveConfuseAnim() 
    {
        m_fConfusionAnimMultiplier = -1.0f;
    }

    float m_fConfusionAnimMultiplier = 1.0f;

    public void SetDesiredLookDirection(Vector3 dir) 
    {
        m_vTargetForward = dir;
    }

    public void WasPulled() 
    {
        m_AnimatorStateMachine.RequestTransition(typeof(AnimalCapturedPulledState));
    }

    public void OnDead() 
    {
        m_AnimatorStateMachine.RequestTransition(typeof(AnimalIdleAnimationState));
    }

    private IEnumerator ConfusionRotationCoroutine() 
    {
         m_fCurrentConfusionAnimTime = Mathf.Clamp(m_fCurrentConfusionAnimTime + m_fConfusionAnimMultiplier * Time.deltaTime, 0, m_ConfusionAnimWindupTime);
        while (m_fCurrentConfusionAnimTime != 0.0f) 
        {        
            float time = m_fCurrentConfusionAnimTime / m_ConfusionAnimWindupTime;
            m_ConfusionEffectRotationTransform.localScale = Vector3.one * time;
            m_ConfusionEffectRotationTransform.localRotation = Quaternion.AngleAxis(Time.deltaTime * m_ConfusionRotationSpeed, Vector3.up) * m_ConfusionEffectRotationTransform.localRotation;
            m_ConfusionEffectTiltTransform.localRotation = Quaternion.AngleAxis(Time.deltaTime * m_ConfusionRotationSpeed/2, -Vector3.up) * m_ConfusionEffectTiltTransform.localRotation;
            m_fCurrentConfusionAnimTime = Mathf.Clamp(m_fCurrentConfusionAnimTime + m_fConfusionAnimMultiplier * Time.deltaTime, 0, m_ConfusionAnimWindupTime);
            yield return null;
        }
        m_ConfusionEffectRotationTransform.localScale = Vector3.zero;
    }

    public void RunAnimationTick(in Quaternion desiredDir, ref float windup, in float currentSpeed) 
    {
        float moveMult = Mathf.Clamp(Quaternion.Angle(desiredDir, AnimTransform.rotation) / 20.0f, 0.4f, 1.0f);
        Quaternion currentQuat = Quaternion.RotateTowards(AnimTransform.rotation, desiredDir, AnimRotationSpeed * Time.deltaTime * moveMult);

        float hopHeight = GetCurrentHopHeight;

        float tiltSize = GetCurrentTilt;

        float horizontalMovement = 0;// animator.GetCurrentHorizontalMovement;

        float forwardBackwardMovement = GetCurrentForwardBackward;

        float yawSize = GetCurrentYaw;

        float multiplier = Mathf.Sign(currentSpeed - m_MaximumRunAnimationSpeed);
        windup = Mathf.Clamp(windup + multiplier * Time.deltaTime, 0.0f, WalkWindup);

        float bounceMult = windup / WalkWindup;
        AnimTransform.rotation = currentQuat * Quaternion.Euler(yawSize * bounceMult, 0, bounceMult * tiltSize);
        AnimTransform.localPosition = AnimTransform.forward * bounceMult * forwardBackwardMovement + AnimTransform.right * bounceMult * horizontalMovement + AnimTransform.up * bounceMult * hopHeight;
    }
}

public class AnimalIdleAnimationState : AStateBase<AnimalAnimationComponent> { }

public class AnimalBreedingAnimationState : AStateBase<AnimalAnimationComponent>
{
    public AnimalBreedingAnimationState() 
    {
        AddTimers(3);
    }

    private Quaternion m_targetQuat = Quaternion.identity;
    private Quaternion m_currentBaseRotation = Quaternion.identity;
    private Quaternion m_quatVelocity = Quaternion.identity;

    private Vector3 m_currentBaseTranslation = Vector3.zero;
    private Vector3 m_smoothDampVelocity = Vector3.zero;

    private float m_HeightMult = 0.0f;
    private float m_TimeMult = 0.0f;

    public override void OnEnter()
    {
        m_smoothDampVelocity = Vector3.zero;
        m_quatVelocity = Quaternion.identity;
        m_currentBaseTranslation = Vector3.zero;
        m_currentBaseRotation = Host.AnimTransform.localRotation;
        m_targetQuat = Host.GetOrientation(Host.TargetDirection);
        SetTimer(2, UnityEngine.Random.Range(0.0f, Host.GetBreedHopProbabilityCurve.keys[Host.GetBreedHopProbabilityCurve.keys.Length - 1].time));
        StartTimer(2);
        StopTimer(1);
        ClearTimer(1);
	}
	public override void Tick()
	{
        // if we're currently hopping, run that logic.
        m_currentBaseRotation = UnityUtils.UnityUtils.SmoothDampQuat(m_currentBaseRotation, m_targetQuat, ref m_quatVelocity, 0.3f);
        m_currentBaseTranslation = Vector3.SmoothDamp(m_currentBaseTranslation, Vector3.zero, ref m_smoothDampVelocity, 0.3f);
        if (GetTimerVal(1) != 0)
        {
            float hopPercentage = GetTimerVal(1) /(m_TimeMult * Host.GetBreedHopDuration);

            float pitchAmount = m_HeightMult * Host.BreedingPitchCurve.Evaluate(hopPercentage);
            float hopAmount = m_HeightMult * Host.BreedingHopCurve.Evaluate(hopPercentage);

            Host.AnimTransform.localRotation = m_currentBaseRotation * Quaternion.Euler(pitchAmount, 0, 0);

            Vector3 targetPos = m_targetQuat * (new Vector3(0, hopAmount, 0));
            Host.AnimTransform.localPosition = targetPos + m_currentBaseTranslation;

            if (hopPercentage > 1.0f)
            {
                ClearTimer(1);
                StopTimer(1);
                StartTimer(2);
            }
            return;
        }
		else 
        {
            Host.AnimTransform.localPosition = m_currentBaseTranslation;
            Host.AnimTransform.localRotation = m_currentBaseRotation;
        }

        // if time in state is such that we cant complete a hop, or we're currently hopping, don't hop.
        if (GetTimerVal(0) < Host.GetBornDuration - Host.GetBreedHopDuration)
            return;

        float breedHopProbability = Host.GetBreedHopProbabilityCurve.Evaluate(GetTimerVal(2));
        if (UnityEngine.Random.Range(0.0f, 1.0f) < breedHopProbability)
        {
            m_HeightMult = UnityEngine.Random.Range(0.9f, 1.1f);
            m_TimeMult = m_HeightMult * UnityEngine.Random.Range(0.9f, 1.1f);
            ClearTimer(2);
            StopTimer(2);
            StartTimer(1);
        }
    }
}

public class AnimalFreeFallAnimationState : AStateBase<AnimalAnimationComponent>
{
    private readonly ParticleEffectsController dragController;
    bool m_bGroundedStateLastFrame;
    public AnimalFreeFallAnimationState(ParticleEffectsController dragController)
    {
        this.dragController = dragController;
    }

	public override void OnEnter()
	{
        m_bGroundedStateLastFrame = Host.AnimalPhysEnt.IsGrounded;
        if (m_bGroundedStateLastFrame) dragController.TurnOnAllSystems();
    }
	public override void Tick()
	{
		if (Host.AnimalPhysEnt.IsGrounded != m_bGroundedStateLastFrame) 
        {
            m_bGroundedStateLastFrame = Host.AnimalPhysEnt.IsGrounded;
            if (m_bGroundedStateLastFrame) dragController.TurnOnAllSystems();
            else dragController.TurnOffAllSystems();
        }

        if (Host.AnimalPhysEnt.IsGrounded) 
        {
            dragController.SetWorldPos(Host.AnimalPhysEnt.GetGroundedPos);
            dragController.SetLookDirection(Host.AnimalPhysEnt.GetGroundedNorm);
        }
	}


	public override void OnExit()
	{
        dragController.TurnOffAllSystems();
    }
}

public class AnimalStaggeredAnimationState : AStateBase<AnimalAnimationComponent>
{
    private readonly AnimalAnimationComponent animator;

    public override void OnEnter()
    {
		Host.TriggerConfuseAnim();
    }
    public override void OnExit()
    {
		Host.RemoveConfuseAnim();
    }
}
public class AnimalWalkingAnimationState : AStateBase<AnimalAnimationComponent>
{
	float m_fCurrentWindup;
    public override void Tick()
    {
        Quaternion targetBodyQuat = Host.GetOrientation(Host.Agent.velocity);
        Host.RunAnimationTick(targetBodyQuat, ref m_fCurrentWindup, Host.Agent.velocity.magnitude);  
    }
    public override void OnEnter()
    {
        Quaternion savedrot = Host.AnimTransform.rotation;
		Host.AnimalBody.transform.rotation = Quaternion.identity;
		Host.AnimTransform.rotation = savedrot;
        m_fCurrentWindup = 0.0f;
    }
}

public class AnimalDamagedAnimationState : AStateBase<AnimalAnimationComponent>
{
    private List<MeshRenderer> m_VisualMeshRenderers = new List<MeshRenderer>();

    public AnimalDamagedAnimationState()
    {
        AddTimers(1);
    }

    public override void OnEnter()
    {
        ForEachMeshRenderer(m_VisualMeshRenderers, (MeshRenderer meshRenderer) =>
        {
            meshRenderer.enabled = true;
        });
        localOffset = Host.AnimTransform.localPosition;
        localRotation = Host.AnimTransform.localRotation;
    }
    private void ForEachMeshRenderer(List<MeshRenderer> renderers, Action<MeshRenderer> action)
    {
        foreach (MeshRenderer renderer in renderers)
        {
            action.Invoke(renderer);
        }
    }

    Quaternion localRotation = Quaternion.identity;
    Quaternion rotVelocity = Quaternion.identity;

    Vector3 localOffset = Vector3.zero;
    Vector3 velocity = Vector3.zero;

    public override void Tick()
    {
        if (GetTimerVal(0) < Host.DamagedAnimTime)
        {
			float animTime = GetTimerVal(0) / Host.DamagedAnimTime;

			float colorSlider = Host.DamagedColorCurve.Evaluate(animTime);
            ForEachMeshRenderer(m_VisualMeshRenderers, (MeshRenderer renderer) =>
            {
                List<Material> rendererMats = new List<Material>();
                renderer.GetSharedMaterials(rendererMats);
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                for (int i = 0; i < rendererMats.Count; i++)
                {
                    renderer.GetPropertyBlock(block, i);
                    block.SetColor("_EmissionColor", Host.DamagedFlashColor * colorSlider);
                    renderer.SetPropertyBlock(block, i);
                }
            });

            localRotation = UnityUtils.UnityUtils.SmoothDampQuat(localRotation, Quaternion.identity, ref rotVelocity, 0.25f);
            Host.AnimTransform.localRotation = localRotation;

            localOffset = Vector3.SmoothDamp(localOffset, Vector3.zero, ref velocity, 0.25f);
            Vector3 desiredPosition = new Vector3(0, Host.DamagedHopCurve.Evaluate(animTime), 0);
            Host.AnimTransform.localPosition = localOffset + desiredPosition;
        }
    }

	public override void OnExit()
	{
        ForEachMeshRenderer(m_VisualMeshRenderers, (MeshRenderer meshRenderer) =>
        {
            meshRenderer.enabled = false;
        });
    }
}

public class AnimalBornAnimationState : AStateBase<AnimalAnimationComponent> 
{
    public AnimalBornAnimationState() 
    {
        AddTimers(1);
    }

	public override void Tick()
	{
        float percentage = GetTimerVal(0) / Host.GetBornDuration;
        float size = Host.BornSizeCurve.Evaluate(percentage) * Host.GetSizeMult;
        Host.ScaleTransform.localScale = Vector3.one * size;
	}
}

public class AnimalAttackAnimationState : AStateBase<AnimalAnimationComponent>
{
    private Quaternion m_startQuat;

    public override void OnEnter()
    {
        m_startQuat = Host.AnimTransform.localRotation;
		AddTimers(1);
    }

    public override void Tick()
    {
        if (GetTimerVal(0) < Host.GetAttackAnimDuration) 
        {
			Quaternion lookQuat = Quaternion.LookRotation(Host.TargetDirection, Vector3.up);
			float animTime = GetTimerVal(0) / Host.GetAttackAnimDuration;

			float pitchAng = Host.GetAttackPitchCurve.Evaluate(animTime);
			float tiltAng = Host.GetAttackTiltCurve.Evaluate(animTime);
			float forwardAmount = Host.GetAttackForwardCurve.Evaluate(animTime);
			float hopAmount = Host.GetAttackHopCurve.Evaluate(animTime);

			Quaternion targetQuat = lookQuat * Quaternion.Euler(pitchAng, 0, tiltAng);
			Host.AnimTransform.localRotation = Quaternion.RotateTowards(Host.AnimTransform.localRotation, targetQuat, Host.AnimRotationSpeed);

			Vector3 targetPos = m_startQuat * (new Vector3(0, hopAmount, forwardAmount));
			Host.AnimTransform.localPosition = Vector3.MoveTowards(Host.AnimTransform.localPosition, targetPos, Host.AnimLinearSpeed);
		}
    }
}

public class AnimalCapturedPulledState : AStateBase<AnimalAnimationComponent> 
{
    Quaternion m_InitQuat;
    public AnimalCapturedPulledState()
    {
        AddTimers(1);
    }
	public override void OnEnter()
	{
        Host.EnableDraggingParticles(true);
        m_InitQuat = Host.AnimTransform.localRotation;
    }
	public override void Tick()
    {

        float time = GetTimerVal(0) / Host.GetPullTime;
        if (time > 1)
        {
            RequestTransition<AnimalCapturedAnimationState>();
        }
		else 
        {
            Host.AnimTransform.localRotation = Quaternion.RotateTowards(Host.AnimTransform.localRotation, m_InitQuat * Quaternion.Euler(time * 60.0f, 0, 0), Host.AnimRotationSpeed * Time.fixedDeltaTime);
        }      
    }
	public override void OnExit()
	{
        Host.EnableDraggingParticles(false);
    }
}

public class AnimalCapturedAnimationState : AStateBase<AnimalAnimationComponent>
{

    float m_fWindup = 0.0f;
    public override void Tick()
    {
        Quaternion desiredQuat = Host.GetOrientation(Host.TargetDirection);
        Host.RunAnimationTick(desiredQuat, ref m_fWindup, Host.AnimalPhysEnt.GetVelocity.magnitude);
    }

	public override void OnEnter()
	{
        m_fWindup = 0.0f;
	}
}
