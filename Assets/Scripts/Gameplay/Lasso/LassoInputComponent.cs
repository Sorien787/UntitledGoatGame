using UnityEngine;
using System;
using LassoStates;
using EZCameraShake;
using System.Collections.Generic;

public class LassoInputComponent : MonoBehaviour, IPauseListener, IFreeFallListener
{
	#region SerializedMemberVars

	[Header("Transform References")]
    [SerializeField] private Transform m_LassoStartTransform;
    [SerializeField] private Transform m_LassoEndTransform;
    [SerializeField] private Transform m_SwingPointTransform;
	[SerializeField] private Transform m_ProjectionPoint;
	[SerializeField] private Transform m_LassoGrabPoint;
	[SerializeField] private Transform m_LassoNormalContainerTransform;

	[Header("Animation References")]
	[SerializeField] private LineRenderer m_HandRopeLineRenderer;
	[SerializeField] private LineRenderer m_LassoSpinningLoopLineRenderer;
	[SerializeField] private LineRenderer m_TrajectoryLineRenderer;

	[Header("Gameplay References")]
	[SerializeField] private GameObject m_LassoEndPoint;
	[SerializeField] private GameObject m_PlayerObject;
    [SerializeField] private PlayerCameraComponent m_PlayerCam;
    [SerializeField] private AudioManager m_AudioManager;
	[SerializeField] private UIObjectReference m_PowerBarObjectReference;
	[SerializeField] private UIObjectReference m_CanGrabUIReference;

	[Header("Game System References")]
	[SerializeField] private ControlBinding m_TriggerBinding;
	[SerializeField] private ControlBinding m_CancelBinding;
    [SerializeField] private CowGameManager m_Manager;
	[SerializeField] private LassoParams m_LassoParams;
	#endregion

	#region Properties

	public AudioManager GetAudioManager => m_AudioManager;

	public ThrowableObjectComponent GetThrowableObject { get; private set; }

	public Rigidbody GetLassoBody { get; private set; }

	public Transform GetEndTransform { get; private set; }

	public Transform GetSwingingTransform => m_SwingPointTransform;

    public Transform GetLassoGrabPoint => m_LassoGrabPoint;

	public bool IsInIdle => m_StateMachine.GetCurrentState() == typeof(LassoStates.LassoIdleState);

	public float LassoReturnAcceleration => m_LassoParams.m_LassoReturnAcceleration;

	public float MaxLassoReturnSpeed => m_LassoParams.m_MaxLassoReturnSpeed;

	public float LassoLength => m_LassoParams.m_LassoLength;

	public float GrabDistance => m_LassoParams.m_GrabDistance;

	public float LassoRelaxScalar => m_LassoParams.m_LassoRelaxTime;

	public AnimationCurve ThrowForceCurve => m_LassoParams.m_ThrowForceCurve;

	public float MaxForceForPull => m_LassoParams.m_MaxForceForPull;
	public AnimationCurve ForceIncreasePerPull => m_LassoParams.m_ForceIncreasePerPull;
	public AnimationCurve ForceDecreasePerSecond => m_LassoParams.m_ForceDecreasePerSecond;
	public AnimationCurve JerkProfile => m_LassoParams.m_JerkProfile;

	public float JerkTimeForPull => m_LassoParams.m_JerkTimeForPull;
	public float TimeBeforeUserCanThrow => m_LassoParams.m_TimeBeforeUserCanThrow;
	public float MaxTimeSpinning => m_LassoParams.m_MaxTimeSpinning;

	public AnimationCurve SpinUpProfile => m_LassoParams.m_SpinUpProfile;
	public AnimationCurve SpinSidewaysProfile => m_LassoParams.m_SpinSidewaysProfile;
	public AnimationCurve SpinHeightProfile => m_LassoParams.m_SpinHeightProfile;
	public AnimationCurve SpinSizeProfile => m_LassoParams.m_SpinSizeProfile;
	public AnimationCurve SpinSpeedProfile => m_LassoParams.m_SpinSpeedProfile;
	public AnimationCurve GetThrowSpinSpeed => m_LassoParams.m_ThrowSpinSpeedTimeProfile;
	public AnimationCurve GetWaveLength => m_LassoParams.m_WaveLengthTimeProfile;
	public AnimationCurve GetUnravelSizeByTime => m_LassoParams.m_UnravelSizeTimeProfile;
	public AnimationCurve GetUnravelSizeByDistance => m_LassoParams.m_UnravelSizeDistanceProfile;
	public bool SpunUp { get; set; }
	public bool SpinningIsInitializing { get; set; }
	#endregion

	#region Events

	public event Action<ThrowableObjectComponent> OnSetPullingObject;
    public event Action OnStoppedPullingObject;
    public event Action<float, float> OnSetPullingStrength;
    public event Action<float> OnSetSwingingStrength;
    public event Action<ThrowableObjectComponent> OnSetSwingingObject;
    public event Action OnStoppedSwingingObject;
    public event Action<float> OnThrowObject;
	public event Action OnStartUsingLasso;
	public event Action OnStopUsingLasso;
	#endregion

	#region MemberVars

	private bool m_bIsAttachedToObject = false;
    private bool m_bCanPickUpObject = false;
    private bool m_bIsShowingGrabbableUI = false;
    private bool m_bPlayerThrown = false;
	private float m_fThrowStrength;
	private StateMachine<LassoInputComponent> m_StateMachine;
	private Animator m_PowerBarAnimator;
	private ProjectileParams m_projectileParams;
	private CanvasGroup m_CanGrabCanvasGroup;
	private Collider[] m_EndColliders;
	private PlayerComponent m_PlayerComponent;
	private IThrowableObjectComponent m_PlayerThrowableComponent;
	private FreeFallTrajectoryComponent m_LassoFreeFallComponent;
	private Transform m_LassoLoopTransform;
	private PlayerMovement m_PlayerMovementComponent;

	#endregion

	#region UnityEvents

	private void Start()
	{
		m_PowerBarAnimator = m_Manager.GetUIElementFromReference(m_PowerBarObjectReference).GetComponent<Animator>();
		m_CanGrabCanvasGroup = m_Manager.GetUIElementFromReference(m_CanGrabUIReference).GetComponent<CanvasGroup>();
	}

	private void OnDestroy()
	{
		m_LassoFreeFallComponent.RemoveListener(this);
	}

	private void LateUpdate()
    {
        m_StateMachine.Tick(Time.deltaTime);
    }
	[Header("Sound References")]
	[SerializeField] private SoundObject m_throwSoundRef;
	[SerializeField] private SoundObject m_returnSoundRef;
	[SerializeField] private SoundObject m_spinSoundRef;
	[SerializeField] private SoundObject m_pullSoundRef;
	[SerializeField] private SoundObject m_tugSoundRef;

	private void Awake()
    {
		m_EndColliders = m_LassoEndPoint.GetComponentsInChildren<Collider>();
		m_PlayerThrowableComponent = m_PlayerObject.GetComponent<ThrowablePlayerComponent>();
		m_PlayerMovementComponent = m_PlayerObject.GetComponent<PlayerMovement>();
		m_PlayerComponent = m_PlayerObject.GetComponent<PlayerComponent>();

		m_LassoFreeFallComponent = m_LassoEndPoint.GetComponent<FreeFallTrajectoryComponent>();
		m_LassoLoopTransform = m_LassoEndPoint.transform;
		GetLassoBody = m_LassoEndPoint.GetComponent<Rigidbody>();
		
		m_PlayerThrowableComponent.OnThrown += (ProjectileParams pparams) => OnThrown();
		m_LassoFreeFallComponent.AddListener(this);

        GetThrowableObject = m_LassoEndTransform.GetComponent<ThrowableObjectComponent>();

        GetEndTransform = m_LassoEndTransform;
        m_StateMachine = new StateMachine<LassoInputComponent>(new LassoIdleState(), this);
        m_StateMachine.AddState(new LassoReturnState(m_AudioManager.GetSoundBySoundObject(m_returnSoundRef)));
        m_StateMachine.AddState(new LassoThrowingState(m_AudioManager.GetSoundBySoundObject(m_throwSoundRef)));
        m_StateMachine.AddState(new LassoSpinningState(m_AudioManager.GetSoundBySoundObject(m_spinSoundRef)));
        m_StateMachine.AddState(new LassoAnimalAttachedState(m_TriggerBinding, m_AudioManager.GetSoundBySoundObject(m_tugSoundRef)));
        m_StateMachine.AddState(new LassoAnimalSpinningState(m_AudioManager.GetSoundBySoundObject(m_spinSoundRef)));


		m_StateMachine.AddStateGroup(StateGroup.Create(typeof(LassoThrowingState)).AddOnEnter(() => SetLassoHitColliders(true)).AddOnExit(() => SetLassoHitColliders(false)));
        // for if we want to start spinning
        m_StateMachine.AddTransition(typeof(LassoIdleState), typeof(LassoSpinningState), () => m_TriggerBinding.GetBindingDown() && !m_bIsAttachedToObject && !m_bPlayerThrown);

        m_StateMachine.AddTransition(typeof(LassoReturnState), typeof(LassoIdleState), () => Vector3.SqrMagnitude(GetEndTransform.position - m_LassoGrabPoint.position) < 1.0f, () => SetLassoAsChildOfPlayer(true));
        // for if we're spinning and want to cancel 
        m_StateMachine.AddTransition(typeof(LassoSpinningState), typeof(LassoIdleState), () => m_CancelBinding.GetBindingUp());
        // for if we're spinning and want to throw
        m_StateMachine.AddTransition(typeof(LassoSpinningState), typeof(LassoThrowingState), () => m_TriggerBinding.GetBindingUp(), () => { ProjectLasso(); SetLassoAsChildOfPlayer(false);});
        // for if we're spinning an animal and want to cancel
        m_StateMachine.AddTransition(typeof(LassoAnimalSpinningState), typeof(LassoIdleState), () => m_CancelBinding.GetBindingUp(), () => DetachFromObject());
        // for if we're throwing and want to cancel
        m_StateMachine.AddTransition(typeof(LassoThrowingState), typeof(LassoReturnState), () => (m_CancelBinding.GetBindingUp() || Vector3.SqrMagnitude(GetEndTransform.position - m_LassoGrabPoint.position) > LassoLength * LassoLength), () => { m_LassoEndTransform.GetComponent<FreeFallTrajectoryComponent>().StopThrowingObject(); } );
        // for if we've decided we want to unattach to our target
        m_StateMachine.AddTransition(typeof(LassoAnimalAttachedState), typeof(LassoReturnState), () => m_CancelBinding.GetBindingUp() || m_bPlayerThrown, () => DetachFromObject());

        m_StateMachine.AddTransition(typeof(LassoAnimalAttachedState), typeof(LassoReturnState), () => !m_bIsAttachedToObject);
        // for if the cow has reached us
        m_StateMachine.AddTransition(typeof(LassoAnimalAttachedState), typeof(LassoAnimalSpinningState), () => m_bCanPickUpObject && m_TriggerBinding.GetBindingDown());
        // for if we want to throw the animal
        m_StateMachine.AddTransition(typeof(LassoAnimalSpinningState), typeof(LassoIdleState), () => (!m_TriggerBinding.IsBindingPressed() && !SpinningIsInitializing), () => { ProjectObject(); SetLassoAsChildOfPlayer(true); });
        // instant transition back to idle state
        m_StateMachine.InitializeStateMachine();

        m_Manager.AddToPauseUnpause(this);
    }

	#endregion

	#region GameSystemCallbacks

	public void Pause()
    {
        enabled = false;
    }

    public void Unpause()
    {
        enabled = true;
    }

	#endregion GameSystemCallbacks

	#region StateMachineCallbacks

	private void SetLassoHitColliders(bool state)
	{
		for (int i = 0; i < m_EndColliders.Length; i++)
		{
			m_EndColliders[i].enabled = state;
		}
	}

	public void StartSwingingObject() 
    {
        OnSetSwingingObject(GetThrowableObject);
    }

    public void StopSwingingObject() 
    {
        OnStoppedSwingingObject();
    }

    public void StartDraggingObject() 
    {
        OnSetPullingObject(GetThrowableObject);
    }

    public void StopDraggingObject() 
    {
        OnStoppedPullingObject();
    }

	public void SetCanGrabEntity(bool canGrab)
	{
		if (m_bIsShowingGrabbableUI != canGrab)
		{
			m_bIsShowingGrabbableUI = canGrab;
			LeanTween.cancel(m_CanGrabCanvasGroup.gameObject);
			LeanTween.alphaCanvas(m_CanGrabCanvasGroup, canGrab ? 1 : 0, 0.20f);
		}
		m_bCanPickUpObject = canGrab;
	}
	#endregion

	#region LassoEndCallbacks

	public void OnThrown() 
    {
        m_bPlayerThrown = true;
    }

    public void OnStopFalling() 
    {
        m_bPlayerThrown = false;
    }

	public void OnCollide(Vector3 position, Vector3 rotation, GameObject hitObject)
	{
		ThrowableObjectComponent component = hitObject.GetComponentInParent<ThrowableObjectComponent>();

		if (component)
		{
			GetThrowableObject = component;
			GetThrowableObject.Wrangled();
			GetThrowableObject.OnDestroyed += OnThrowableObjectDestroyed;
			m_StateMachine.RequestTransition(typeof(LassoAnimalAttachedState));
			GetEndTransform = GetThrowableObject.GetAttachmentTransform;
			m_bIsAttachedToObject = true;
		}
		else
		{
			m_StateMachine.RequestTransition(typeof(LassoReturnState));
		}

	}

	public void OnImmediatelySpinObject(ThrowableObjectComponent throwableObject)
	{
		GetThrowableObject = throwableObject;
		GetThrowableObject.Wrangled();
		GetThrowableObject.OnDestroyed += OnThrowableObjectDestroyed;
		m_StateMachine.RequestTransition(typeof(LassoAnimalSpinningState));
		GetEndTransform = GetThrowableObject.GetAttachmentTransform;
		m_bIsAttachedToObject = true;
	}

	private void ProjectObject()
	{
		m_projectileParams.SetAngularVelocity(360);
		GetThrowableObject.ThrowObject(m_projectileParams);
		float throwForce = m_projectileParams.m_fThrowSpeed / GetThrowableObject.GetMass();
		OnThrowObject?.Invoke(throwForce);
		DetachFromObject();
	}

	private void ProjectLasso()
	{
		GetThrowableObject.ThrowObject(m_projectileParams);
		float throwForce = m_projectileParams.m_fThrowSpeed / GetThrowableObject.GetMass();
		OnThrowObject?.Invoke(throwForce);
	}
	private void SetLassoAsChildOfPlayer(bool set)
	{
		if (set)
		{
			GetEndTransform.SetParent(m_LassoNormalContainerTransform);
		}
		else
		{
			GetEndTransform.SetParent(null);
		}
	}

	private void DetachFromObject()
	{
		GetThrowableObject.Released();
		GetThrowableObject.OnDestroyed -= OnThrowableObjectDestroyed;
		GetThrowableObject.GetMainTransform.SetParent(null);
		m_LassoEndTransform.position = GetThrowableObject.GetAttachmentTransform.position;
		GetThrowableObject = GetLassoBody.GetComponent<ThrowableObjectComponent>();
		m_LassoEndTransform.SetParent(m_LassoNormalContainerTransform);
		GetEndTransform = m_LassoEndTransform;
		m_bIsAttachedToObject = false;
	}

	private float GetForceFromSwingTime()
	{
		return ThrowForceCurve.Evaluate(m_fThrowStrength);
	}

	public void SetPullStrength(float totalForce, float tugTime)
	{
		OnSetPullingStrength?.Invoke(totalForce, tugTime);
		OnChangePowerBarValue(totalForce * 0.5f + 0.5f * tugTime);
	}

	public void SetSpinStrength(float strength)
	{
		m_fThrowStrength = strength;
		OnChangePowerBarValue(strength);
	}

	#endregion

	public void TriggerPowerBarAnimIn() 
    {
        m_PowerBarAnimator.SetBool("AnimOut", false);
        m_PowerBarAnimator.Play("PowerBarInitAnimation", 0);
    }

    public void TriggerPowerBarAnimOut() 
    {
        m_PowerBarAnimator.SetBool("AnimOut", true);
    }

    public void OnChangePowerBarValue(float strength) 
    {
        m_PowerBarAnimator.SetFloat("SliderLength", strength);
    }

	#region ThrowableObjectCallbacks

	private void OnThrowableObjectDestroyed()
	{
		m_LassoEndTransform.position = GetThrowableObject.GetAttachmentTransform.position;
		GetThrowableObject = GetLassoBody.GetComponent<ThrowableObjectComponent>();
		m_LassoEndTransform.SetParent(m_LassoNormalContainerTransform);
		GetEndTransform = m_LassoEndTransform;
		m_bIsAttachedToObject = false;
	}

	#endregion


	#region Rendering

	public void SetRopeLineRenderer(bool enabled)
	{
		m_HandRopeLineRenderer.enabled = enabled;
	}

	public void SetLoopLineRenderer(bool enabled)
	{
		m_LassoSpinningLoopLineRenderer.positionCount = 0;
		m_LassoSpinningLoopLineRenderer.enabled = enabled;
	}

	public void SetTrajectoryRenderer(bool enabled)
	{
		m_TrajectoryLineRenderer.positionCount = 0;
		m_TrajectoryLineRenderer.enabled = enabled;
	}

	public void RenderThrownLoop()
    {
        Vector3 displacement = GetEndTransform.position - GetLassoGrabPoint.position;
        Vector3 midPoint = GetEndTransform.position + displacement.normalized * 0.8f;

        Quaternion colliderRotation = Quaternion.LookRotation(-displacement, Vector3.up);
		m_LassoLoopTransform.rotation = colliderRotation;

        RenderLoop(0.8f, midPoint, displacement.normalized, Vector3.Cross(displacement, Vector3.up).normalized);
    }

    public void RenderRope(in List<Vector3> positions)
    {
        m_HandRopeLineRenderer.positionCount = positions.Count;
		for (int i = 0; i < positions.Count; i++) 
		{
			m_HandRopeLineRenderer.SetPosition(i, positions[i]);
		}
    }

	public void RenderRope()
	{
		if (m_HandRopeLineRenderer.positionCount > 1) 
		{
			Vector3 oldStart = m_HandRopeLineRenderer.GetPosition(0);
			Vector3 oldEnd = m_HandRopeLineRenderer.GetPosition(m_HandRopeLineRenderer.positionCount - 1);

			List<Vector3> newPositions = new List<Vector3>();

			Vector3 newDir = GetEndTransform.position - GetLassoGrabPoint.position;
			Vector3 oldDir = oldEnd - oldStart;

			Quaternion rotationFromOldToNew = Quaternion.FromToRotation(oldDir, newDir);

			newPositions.Add(GetLassoGrabPoint.position);
			for (int i = 1; i < m_HandRopeLineRenderer.positionCount-1; i++) 
			{
				float percentageAlong = (float)i / (m_HandRopeLineRenderer.positionCount-1);

				Vector3 thisPos = m_HandRopeLineRenderer.GetPosition(i);
				Vector3 straightLinePos = percentageAlong * (oldDir) + oldStart;
				Vector3 straightToCurrent = thisPos - straightLinePos;
				if (straightToCurrent.sqrMagnitude < 0.01f)
					continue;
				newPositions.Add(newDir * percentageAlong + rotationFromOldToNew * straightToCurrent * (1 - Mathf.Min(1, LassoRelaxScalar * Time.deltaTime)) + GetLassoGrabPoint.position);
				// get the offset from where it is along the rope, reapply to new position.

			}
			newPositions.Add(GetEndTransform.position);
			m_HandRopeLineRenderer.positionCount = newPositions.Count;
			for (int i = 0; i < m_HandRopeLineRenderer.positionCount; i++) 
			{
				m_HandRopeLineRenderer.SetPosition(i, newPositions[i]);
			}
		}
	}

    public void RenderLoop(in float radius, in Vector3 centrePoint, in Vector3 normA, in Vector3 normB)
    {
        int numIterations = 30;
        m_LassoSpinningLoopLineRenderer.positionCount = numIterations+1;
        float angleIt = Mathf.Deg2Rad * 360 / numIterations;
        for (int i = 0; i <= numIterations; i++) 
        {
            float currAng = i * angleIt;
            Vector3 position = centrePoint + normA * Mathf.Cos(currAng) * radius + normB * Mathf.Sin(currAng) * radius;
            m_LassoSpinningLoopLineRenderer.SetPosition(i, position);
        }
    }

	public void RenderTrajectory() 
    {
        m_projectileParams = new ProjectileParams(GetThrowableObject, GetForceFromSwingTime(), m_ProjectionPoint.forward, m_LassoGrabPoint.position);
        int posCount = 40;
        m_TrajectoryLineRenderer.positionCount = posCount;
        for (int i = 0; i < posCount; i++) 
        {
            float time = (float)i /20;
            
            m_TrajectoryLineRenderer.SetPosition(i, m_projectileParams.EvaluatePosAtTime(time));
        }

    }

	#endregion
}

#region LassoStates

namespace LassoStates
{
	// raise from off to the side to above head
	// actual point is offset
	public class LassoSpinningState : AStateBase<LassoInputComponent>
	{
		float m_fCurrentAngle;
		float m_CurrentInitializeTime = 0.0f;
		private readonly ValueBasedEdgeTrigger m_LassoSwingSoundTrigger;

		public LassoSpinningState(Sound lassoSwingSound)
		{
			m_LassoSwingSoundTrigger = new ValueBasedEdgeTrigger(EdgeBehaviour.RisingEdge, 0.01f, lassoSwingSound);
			AddTimers(1);
		}

		public override void OnEnter()
		{
			Host.StartSwingingObject();
			m_CurrentInitializeTime = Host.TimeBeforeUserCanThrow;
			m_fCurrentAngle = 2 * Mathf.PI;
			Host.SetLoopLineRenderer(true);
			Host.SetRopeLineRenderer(true);
			Host.SetTrajectoryRenderer(true);
			Host.TriggerPowerBarAnimIn();
			Host.SpinningIsInitializing = true;
			Host.SpunUp = false;

			Vector3 forwardPlanar = Vector3.ProjectOnPlane(Host.GetLassoGrabPoint.forward, Vector3.up);
			Quaternion desiredGrabPointToSwingCentreQuat = Quaternion.AngleAxis(Host.SpinSidewaysProfile.Evaluate(0), forwardPlanar);
			Vector3 desiredSwingCentre_grabPointSpace = Host.SpinHeightProfile.Evaluate(0) * (desiredGrabPointToSwingCentreQuat * Vector3.up);
			m_lastActualSwingCentre_worldSpace = Host.GetLassoGrabPoint.transform.position + desiredSwingCentre_grabPointSpace;
		}

		public override void OnExit()
		{
			Host.TriggerPowerBarAnimOut();
			Host.StopSwingingObject();
			Host.SetLoopLineRenderer(false);
			Host.SetRopeLineRenderer(false);
			Host.SetTrajectoryRenderer(false);
			Host.SpinningIsInitializing = false;
		}

		Vector3 m_LoopCentrePointVelocity;
		Vector3 m_lastActualSwingCentre_worldSpace;

		public override void Tick()
		{
			float time = Mathf.Clamp01(GetTimerVal(0) / Host.MaxTimeSpinning);
			float spinStr = Host.SpinUpProfile.Evaluate(time);
			float r = Host.SpinSizeProfile.Evaluate(time);
			float height = Host.SpinHeightProfile.Evaluate(time);
			float sidewaysOffset = Host.SpinSidewaysProfile.Evaluate(time);
			Host.SetSpinStrength(spinStr);
			Host.RenderTrajectory();


			Vector3 forwardPlanar = Vector3.ProjectOnPlane(Host.GetLassoGrabPoint.forward, Vector3.up);

			Quaternion desiredGrabPointToSwingCentreQuat = Quaternion.AngleAxis(Host.SpinSidewaysProfile.Evaluate(time), forwardPlanar);
			Vector3 desiredSwingCentre_grabPointSpace = height * (desiredGrabPointToSwingCentreQuat * Vector3.up);
			Vector3 desiredSwingCentre_worldSpace = Host.GetLassoGrabPoint.position + desiredSwingCentre_grabPointSpace;

			Vector3 actualSwingCentre_worldSpace = Vector3.SmoothDamp(m_lastActualSwingCentre_worldSpace, desiredSwingCentre_worldSpace, ref m_LoopCentrePointVelocity, 0.2f);
			Vector3 actualSwingCentre_grabPointSpace = actualSwingCentre_worldSpace - Host.GetLassoGrabPoint.position;
			Quaternion actualGrabPointToSwingCentreQuat = Quaternion.FromToRotation(Vector3.up, actualSwingCentre_grabPointSpace);

			Vector3 ropePos_swingCentreSpace = actualGrabPointToSwingCentreQuat * (new Vector3(r * Mathf.Cos(m_fCurrentAngle), 0, r * Mathf.Sin(m_fCurrentAngle)));

			Host.GetEndTransform.position = actualSwingCentre_worldSpace + ropePos_swingCentreSpace;
			m_lastActualSwingCentre_worldSpace = actualSwingCentre_worldSpace;
			Host.RenderRope();

			Vector3 normA = ropePos_swingCentreSpace.normalized;
			Vector3 normB = Vector3.Cross(normA, actualSwingCentre_grabPointSpace.normalized);

			Host.RenderLoop(r, actualSwingCentre_worldSpace, normA, normB);

			m_fCurrentAngle += Host.SpinSpeedProfile.Evaluate(time) * Time.deltaTime;
			if (m_fCurrentAngle > 2 * Mathf.PI)
			{
				m_LassoSwingSoundTrigger.GetSound.SetPitch(spinStr);
				m_LassoSwingSoundTrigger.GetSound.PlayOneShot();

			}
			m_fCurrentAngle %= (2 * Mathf.PI);
			Debug.Log(m_fCurrentAngle);


			if (m_CurrentInitializeTime > 0)
			{
				m_CurrentInitializeTime = Mathf.Max(m_CurrentInitializeTime - Time.deltaTime, 0);
				if (m_CurrentInitializeTime == 0)
				{
					Host.SpinningIsInitializing = false;
				}
			}

			if (time >= 1 - Mathf.Epsilon)
			{
				Host.SpunUp = true;
			}
		}
	}

	public class LassoAnimalSpinningState : AStateBase<LassoInputComponent>
	{
		float m_CurrentAngle;
		float m_CurrentInitializeTime = 0.0f;
		private readonly ValueBasedEdgeTrigger m_LassoSwingSoundTrigger;

		public LassoAnimalSpinningState(Sound lassoSwingSound) 
		{
			m_LassoSwingSoundTrigger = new ValueBasedEdgeTrigger(EdgeBehaviour.RisingEdge, 1f, lassoSwingSound);
			AddTimers(1);
		}

		public override void OnEnter()
		{
			Host.StartSwingingObject();
			Host.SetTrajectoryRenderer(true);
			m_CurrentInitializeTime = Host.TimeBeforeUserCanThrow;
			m_CurrentAngle = 0.0f;
			Host.SetRopeLineRenderer(true);
			Host.GetThrowableObject.StartedSpinning();
			Host.TriggerPowerBarAnimIn();
			Host.SpinningIsInitializing = true;
			Host.SpunUp = false;
		}

		public override void OnExit()
		{
			Host.TriggerPowerBarAnimOut();
			Host.StopSwingingObject();
			Host.SetTrajectoryRenderer(false);
			Host.SetRopeLineRenderer(false);
			Host.SpinningIsInitializing = false;
		}

		public override void Tick()
		{
			float time = Mathf.Clamp01(GetTimerVal(0) / Host.MaxTimeSpinning);
			float spinStr = Host.SpinUpProfile.Evaluate(time);
			float r = Host.SpinSizeProfile.Evaluate(time);
			float height = Host.SpinHeightProfile.Evaluate(time);

			Host.GetThrowableObject.GetMainTransform.position = Host.GetSwingingTransform.position + new Vector3(r * Mathf.Cos(m_CurrentAngle), height, r * Mathf.Sin(m_CurrentAngle));
			Vector3 forward = Host.GetLassoGrabPoint.position - Host.GetThrowableObject.GetAttachmentTransform.position;
			Host.GetThrowableObject.GetMainTransform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
			Host.SetSpinStrength(spinStr);
			Host.RenderRope();
			Host.RenderTrajectory();

			m_CurrentAngle += (Host.SpinSpeedProfile.Evaluate(time) * Time.deltaTime);
			m_CurrentAngle %= 360f;

			m_LassoSwingSoundTrigger.Tick(m_CurrentAngle);
			m_LassoSwingSoundTrigger.GetSound.SetPitch(spinStr);

			if (m_CurrentInitializeTime > 0)
			{
				m_CurrentInitializeTime = Mathf.Max(m_CurrentInitializeTime - Time.deltaTime, 0);
				if (m_CurrentInitializeTime == 0)
				{
					Host.SpinningIsInitializing = false;
				}
			}
			if (time >= 1 - Mathf.Epsilon)
			{
				Host.SpunUp = true;
			}
		}
	}

	public class LassoThrowingState : AStateBase<LassoInputComponent>
	{
		private float m_fCurrentAngle = 0.0f;
		private float m_fRandomSizeMult = 1.0f;
		private readonly Sound m_ThrowSound;
		private float m_fRandomRotationSpeedMult = 1.0f;

		public LassoThrowingState(Sound throwSound) 
		{
			m_ThrowSound = throwSound;
		}

		public override void OnEnter()
		{
			m_fCurrentAngle = UnityEngine.Random.Range(0.0f, 360.0f);
			m_fRandomSizeMult = UnityEngine.Random.Range(0.7f, 1.3f);
			m_fRandomRotationSpeedMult = UnityEngine.Random.Range(0.7f, 1.3f) * Mathf.Sign(UnityEngine.Random.Range(-1.0f, 1.0f));
			m_ThrowSound.SetPitch(1.0f);
			m_ThrowSound.PlayOneShot();
			Host.SetRopeLineRenderer(true);
			Host.SetLoopLineRenderer(true);
			AddTimers(1);
		}
		public override void OnExit()
		{
			Host.SetRopeLineRenderer(false);
			Host.SetLoopLineRenderer(false);
		}

		public override void Tick()
		{

			Vector3 startPoint = Host.GetLassoGrabPoint.position;

			Vector3 endPoint = Host.GetEndTransform.position;

			Vector3 diff = endPoint - startPoint;

			int numPointsToDraw = (int)Mathf.Clamp( diff.magnitude*100, 1, 100);

			Vector3 up = Vector3.up;

			Vector3 side = Vector3.Cross(up, diff).normalized;

			Quaternion rotation = Quaternion.AngleAxis(m_fCurrentAngle, diff);

			float waveLength = Host.GetWaveLength.Evaluate(GetTimerVal(0));
			float size = m_fRandomSizeMult * Host.GetUnravelSizeByTime.Evaluate(GetTimerVal(0));

			List<Vector3> renderPoints = new List<Vector3>();

			for (int i = 0; i <= numPointsToDraw; i++)
			{
				float percentage = (float)i / numPointsToDraw;
				float sidewaysNess = Mathf.Cos(2 * Mathf.PI * percentage / waveLength);
				float upNess = Mathf.Sin(2 * Mathf.PI * percentage / waveLength);
				float envelope = Host.GetUnravelSizeByDistance.Evaluate(percentage);;
				float total = size * envelope;

				Vector3 displacement = diff * percentage;

				Vector3 offset = total * (rotation * (up * upNess + side * sidewaysNess));

				renderPoints.Add(startPoint + displacement + offset);
			}

			m_fCurrentAngle += m_fRandomRotationSpeedMult * Host.GetThrowSpinSpeed.Evaluate(GetTimerVal(0));

			Host.RenderRope(renderPoints);
			Host.RenderThrownLoop();
		}
	}

	public class LassoAnimalAttachedState : AStateBase<LassoInputComponent>
	{
		float m_fCurrentJerkTime;
		float m_fTotalCurrentForce;
		float m_fTimeSinceClicked;

		private readonly ControlBinding m_TriggerBinding;
		private readonly Sound m_lassoSound;

		public LassoAnimalAttachedState(ControlBinding triggerBinding, Sound lassoPullSound)
		{
			m_lassoSound = lassoPullSound;
			m_TriggerBinding = triggerBinding;
		}
		public override void OnEnter()
		{
			Host.StartDraggingObject();
			m_fTotalCurrentForce = 0.0f;
			m_fTimeSinceClicked = 1.0f;
			m_fCurrentJerkTime = 0.0f;
			Host.SetRopeLineRenderer(true);
			Host.TriggerPowerBarAnimIn();
		}

		public override void OnExit()
		{
			Host.StopDraggingObject();
			Host.SetRopeLineRenderer(false);
			Host.TriggerPowerBarAnimOut();
			Host.SetCanGrabEntity(false);
		}

		public override void Tick()
		{
			Host.RenderRope();
			m_fTimeSinceClicked += Time.deltaTime;
			Vector3 cowToPlayer = (Host.GetLassoGrabPoint.position - Host.GetEndTransform.position).normalized;
			float fForceDecrease = Host.ForceDecreasePerSecond.Evaluate(m_fTotalCurrentForce / Host.MaxForceForPull);
			m_fTotalCurrentForce = Mathf.Max(0.0f, m_fTotalCurrentForce - fForceDecrease * Time.deltaTime);
			m_fCurrentJerkTime = Mathf.Max(0.0f, m_fCurrentJerkTime - Time.deltaTime);

			if (m_TriggerBinding.GetBindingDown() && m_fTimeSinceClicked > 0.4f)
			{
				m_lassoSound.SetPitch(1 + (m_fTotalCurrentForce / Host.MaxForceForPull) / 3f);
				m_lassoSound.PlayOneShot();
				Host.GetThrowableObject.TuggedByLasso();
				m_fCurrentJerkTime = Host.JerkTimeForPull;
				float fForceIncrease = Host.ForceIncreasePerPull.Evaluate(m_fTotalCurrentForce / Host.MaxForceForPull);
				m_fTotalCurrentForce = Mathf.Min(m_fTotalCurrentForce + fForceIncrease, Host.MaxForceForPull);
				m_fTimeSinceClicked = 0.0f;
			}

			float jerkScale = Host.JerkProfile.Evaluate(m_fCurrentJerkTime / Host.JerkTimeForPull);

			if ((Host.GetThrowableObject.GetMainTransform.position - Host.GetSwingingTransform.position).sqrMagnitude < Host.GrabDistance * Host.GrabDistance)
			{
				Host.SetCanGrabEntity(true);
			}
			else
			{
				Host.SetCanGrabEntity(false);
			}

			Host.GetThrowableObject.ApplyForceToObject(cowToPlayer * m_fTotalCurrentForce * jerkScale * Time.deltaTime);
			Host.SetPullStrength(m_fTotalCurrentForce / Host.MaxForceForPull, m_fCurrentJerkTime / Host.JerkTimeForPull);
		}
	}

	public class LassoReturnState : AStateBase<LassoInputComponent>
	{
		private float m_LassoSpeed = 0.0f;
		private readonly Sound m_lassoReturnSound;
		public LassoReturnState(Sound returnBeginSound) 
		{
			m_lassoReturnSound = returnBeginSound;
		}

		public override void OnEnter()
		{
			m_LassoSpeed = 0.0f;
			m_lassoReturnSound.PlayOneShot();
			Host.SetRopeLineRenderer(true);
			Host.SetLoopLineRenderer(true);
		}

		public override void OnExit()
		{
			Host.SetRopeLineRenderer(false);
			Host.SetLoopLineRenderer(false);
		}

		public override void Tick()
		{
			// GetStateMachineParent.RenderLoop(0, Vector3.zero);
			Host.RenderRope();
			Host.RenderThrownLoop();
			m_LassoSpeed = (Mathf.Min(m_LassoSpeed + Time.deltaTime * Host.LassoReturnAcceleration, Host.LassoReturnAcceleration));
			Vector3 loopToPlayer = (Host.GetLassoGrabPoint.position - Host.GetEndTransform.position).normalized;
			Host.GetEndTransform.rotation = Quaternion.LookRotation(-loopToPlayer, Vector3.up);
			Host.GetEndTransform.position += m_LassoSpeed * loopToPlayer;
		}
	}

	public class LassoIdleState : AStateBase<LassoInputComponent>
	{

	}
}

#endregion