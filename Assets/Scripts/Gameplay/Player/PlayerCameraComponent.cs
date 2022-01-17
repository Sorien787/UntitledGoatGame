using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerCameraComponent : MonoBehaviour, IPauseListener
{
	[Header("--- References ---")]
	[SerializeField] private Transform m_tBodyTransform;
    [SerializeField] private Camera m_PlayerCamera;
    [SerializeField] private CowGameManager m_Manager;
	[SerializeField] private SettingsManager m_SettingsManager;
	[SerializeField] private Animator m_CameraAnimator;
	[SerializeField] private PlayerMovement m_PlayerMovement;
	[SerializeField] private LassoInputComponent m_LassoStart;
    [SerializeField] private AudioManager m_AudioManager = default;
    [SerializeField] private Transform m_CameraLookRotatorTransform = default;

    [Header("--- Cam Shake ---")]
	[SerializeField] private EZCameraShake.CameraShaker m_CameraShaker;
	[SerializeField] private AnimationCurve m_GroundImpactSpeedSize;
    [SerializeField] private AnimationCurve m_ThrowForceCamShake;

	[Header("--- FOV Animation ---")]
	[SerializeField] private float m_fMaxFOVChangePerSecond = 1.0f;
    [SerializeField] private float m_fDefaultFOV;
    [SerializeField] private float m_ThrowAnimPulseTime;
    [SerializeField] private AnimationCurve m_FOVTugAnimator;
    [SerializeField] private AnimationCurve m_FOVForceAnimator;
    [SerializeField] private AnimationCurve m_FOVThrowAnimator;
    [SerializeField] private AnimationCurve m_FOVThrowPulseAnimator;
    [SerializeField] private SoundObject m_StepSoundObject;

    [Header("--- Anim Strings ---")]
	[SerializeField] private string m_JumpString;
	[SerializeField] private string m_GroundedAnimString;
	[SerializeField] private string m_MovementSpeedAnimString;
	[SerializeField] private string m_LRTiltAnimString;
	[SerializeField] private string m_FBTiltAnimString;

	[Header("--- Sensitivity Multipliers ---")]
	[SerializeField] private float m_fMouseSensitivityMultiplier = 100.0f;

	private float m_fCamPoint;
    private float m_fTargetFOV;
    private  float m_fCurrentFOV;
    private Transform m_tCamTransform;
    private Transform m_tFocusTransform;
    private StateMachine<PlayerCameraComponent> m_CameraStateMachine;
    private Type m_CachedType;

    private float m_CurrentTime = 0.0f;
    private float currentMovement = 0.0f;
    private float currentMovementAcceleration = 0.0f;
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 targetVelocity = Vector3.zero;
    private float m_fTimeToFocus = 1.0f;
    private Quaternion m_CurrentQuatVelocity = Quaternion.identity;

    private void OnSetPullStrength(float force, float yankSize) 
    {
        m_fTargetFOV = m_FOVTugAnimator.Evaluate(yankSize) * force + m_FOVForceAnimator.Evaluate(force) + m_fDefaultFOV;
    }

    private void OnSetPullingObject(ThrowableObjectComponent pullingObject) 
    {
        SetFocusedTransform(pullingObject.GetCameraFocusTransform, typeof(ObjectFocusLook), 0.5f);
    }

    private void OnStoppedPullingObject() 
    {
        ClearFocusedTransform();
    }

    public void SetFocusedTransform(Transform focusTransform, Type stateToTransitionTo, float focusTime) 
    {
        m_fTimeToFocus = focusTime;
        m_CurrentQuatVelocity = Quaternion.identity;
        m_tFocusTransform = focusTransform;
        m_CameraStateMachine.RequestTransition(stateToTransitionTo);
        m_CachedType = stateToTransitionTo;
    }

    private void OnJumped() 
    {
        m_CameraAnimator.SetBool(m_JumpString, true);
    }
    private void OnNotHitGround() 
    {
        m_CameraAnimator.SetBool(m_GroundedAnimString, false);
    }
    private void OnHitGround(float impactSpeed) 
    {
        m_CameraAnimator.SetBool(m_JumpString, false);
        m_CameraAnimator.SetBool(m_GroundedAnimString, true);

        float animationSize = m_GroundImpactSpeedSize.Evaluate(Mathf.Abs(impactSpeed));
        m_CameraShaker.ShakeOnce(animationSize, animationSize / 2, 0.15f, 0.45f);
    }

	private void OnThrowObject(float throwForce)
	{
        float animationSize = m_ThrowForceCamShake.Evaluate(throwForce);
		m_CameraShaker.ShakeOnce(animationSize/3, animationSize, 0.3f, 0.3f);
        StartCoroutine(AnimCoroutine(throwForce));
	}


    private IEnumerator AnimCoroutine(float throwForce) 
    {
        m_CurrentTime = 0.0f;
        while (m_CurrentTime < m_ThrowAnimPulseTime) 
        {
            m_fTargetFOV = m_fDefaultFOV * (1 +  m_FOVThrowPulseAnimator.Evaluate(m_CurrentTime / m_ThrowAnimPulseTime) * m_FOVThrowAnimator.Evaluate(throwForce)); 
            m_CurrentTime += Time.deltaTime;
            yield return null;
        }
        m_fTargetFOV = m_fDefaultFOV;
    }

    public void OnSetMovementSpeed(float speed) 
    {
		currentMovement = Mathf.SmoothDamp(currentMovement, speed, ref currentMovementAcceleration, 0.15f);
        m_CameraAnimator.SetFloat(m_MovementSpeedAnimString, currentMovement * (m_SettingsManager.ViewBobbing ? 1 : 0.001f));
        m_AudioManager.SetVolume(m_StepSoundObject, currentMovement);
    }
	public void OnStep()
	{
        m_AudioManager.PlayOneShot(m_StepSoundObject);
	}


	public void OnMovingInput(Vector3 input)
	{
		currentVelocity = Vector3.SmoothDamp(currentVelocity, input, ref targetVelocity, 0.1f);
		m_CameraAnimator.SetFloat(m_LRTiltAnimString, currentVelocity.x);
		m_CameraAnimator.SetFloat(m_FBTiltAnimString, currentVelocity.z);
	}

    public void ClearFocusedTransform() 
    {
        m_tFocusTransform = null;
        m_fTargetFOV = m_fDefaultFOV;
        m_CameraStateMachine.RequestTransition(typeof(PlayerControlledLook));
        m_CachedType = typeof(PlayerControlledLook);
    }

	private void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == UnityUtils.UnityUtils.GetPropertyName(() => m_SettingsManager.FoV))
		{
			float FoVDifference = m_SettingsManager.FoV - m_fDefaultFOV;
			m_fCurrentFOV += FoVDifference;
			m_fDefaultFOV = m_SettingsManager.FoV;
			m_PlayerCamera.fieldOfView = m_fCurrentFOV;
		}
	}

	public void SetCameraIdle() 
    {
        m_CameraStateMachine.RequestTransition(typeof(CameraIdleState));
    }

    public void UnsetCameraIdle() 
    {
        m_CameraStateMachine.RequestTransition(m_CachedType);
    }

    void Awake()
    {
		m_SettingsManager.PropertyChanged += OnPropertyChanged;

        m_CameraStateMachine = new StateMachine<PlayerCameraComponent>(new PlayerControlledLook(), this);
        m_CameraStateMachine.AddState(new ObjectFocusLook());
        m_CameraStateMachine.AddState(new ObjectFocusLookWithControls());
        m_CameraStateMachine.AddState(new CameraIdleState());
        m_tCamTransform = transform;
		m_fDefaultFOV = m_SettingsManager.FoV;
        m_fTargetFOV = m_fDefaultFOV;
        m_fCurrentFOV = m_fDefaultFOV;
        m_LassoStart.OnSetPullingStrength += OnSetPullStrength;
        m_LassoStart.OnSetPullingObject += OnSetPullingObject;
        m_LassoStart.OnStoppedPullingObject += OnStoppedPullingObject;
        m_LassoStart.OnThrowObject += OnThrowObject;

        m_Manager.AddToPauseUnpause(this);
        m_PlayerMovement.OnHitGround += OnHitGround;
        m_PlayerMovement.OnNotHitGround += OnNotHitGround;
        m_PlayerMovement.OnSuccessfulJump += OnJumped;
        m_PlayerMovement.OnSetMovementSpeed += OnSetMovementSpeed;
		m_PlayerMovement.OnMovingInput += OnMovingInput;
    }
    public void Pause()
    {
        enabled = false;
        m_CameraAnimator.enabled = false;
    }

    public void Unpause()
    {
        m_CameraAnimator.enabled = true;
        enabled = true;
    }

	private void OnDestroy()
	{
		m_SettingsManager.PropertyChanged -= OnPropertyChanged;
        m_Manager.RemoveFromPauseUnpause(this);
    }

	public void ProcessTargetFOV() 
    {
        m_fCurrentFOV += Mathf.Clamp(m_fTargetFOV - m_fCurrentFOV, -Time.deltaTime * m_fMaxFOVChangePerSecond, Time.deltaTime * m_fMaxFOVChangePerSecond);
        m_PlayerCamera.fieldOfView = m_fCurrentFOV;
    }

    public void AlignBodyAndTiltAndResetCameraLook() 
    {
        Quaternion m_TotalQuat = m_CameraLookRotatorTransform.rotation;
        float elevationAng = m_TotalQuat.eulerAngles.x;
        float yawAng = m_TotalQuat.eulerAngles.y;

        m_tBodyTransform.rotation = Quaternion.Euler(0, yawAng, 0);
        m_tCamTransform.rotation = Quaternion.Euler(elevationAng, 0, 0);
        m_CameraLookRotatorTransform.localRotation = Quaternion.Euler(0, 0, 0);

    }


    public void ProcessMouseInput() 
    {
        float mouseX = Input.GetAxis("Mouse X") * m_fMouseSensitivityMultiplier * m_SettingsManager.MouseSensitivity * Time.deltaTime;
		float invertY = m_SettingsManager.InvertY ? -1 : 1;
		float mouseY = invertY * Input.GetAxis("Mouse Y") * m_fMouseSensitivityMultiplier * m_SettingsManager.MouseSensitivity * Time.deltaTime;

        m_fCamPoint -= mouseY;
        m_fCamPoint = Mathf.Clamp(m_fCamPoint, -80f, 80f);

        m_tCamTransform.localRotation = Quaternion.Euler(m_fCamPoint, 0.0f, 0.0f);
        m_tBodyTransform.Rotate(Vector3.up * mouseX);
    }

    public void ProcessLookTowardsTransform() 
    {
        // rotate body by z in plane towards object
        // rotate cam around x towards object
        Vector3 lookDir = m_tFocusTransform.position - m_tCamTransform.position;
        Vector3 right = Vector3.Cross(lookDir, Vector3.up);
        Vector3 up = Vector3.Cross(right, lookDir);
        Quaternion targetCamQuat = Quaternion.LookRotation(lookDir, up);

        Quaternion currentCamQuat = m_CameraLookRotatorTransform.rotation;
        Quaternion rotationThisFrame = UnityUtils.UnityUtils.SmoothDampQuat(currentCamQuat, targetCamQuat, ref m_CurrentQuatVelocity, m_fTimeToFocus);
        m_CameraLookRotatorTransform.rotation = rotationThisFrame;

        //Quaternion targetCamQuat = Quaternion.FromToRotation(m_tCamTransform.forward, Vector3.ProjectOnPlane(lookDir, m_tCamTransform.right)) * m_tCamTransform.rotation;
        //m_tCamTransform.rotation = Quaternion.RotateTowards(m_tCamTransform.rotation, targetCamQuat, 60.0f * Time.deltaTime);

        //Quaternion targetBodyQuat = Quaternion.FromToRotation(m_tBodyTransform.forward, Vector3.ProjectOnPlane(lookDir, Vector3.up)) * m_tBodyTransform.rotation;
        //m_tBodyTransform.rotation = Quaternion.RotateTowards(m_tBodyTransform.rotation, targetBodyQuat, 60.0f * Time.deltaTime);
    }

    // Update is called once per frame
    void Update()
    {
        m_CameraStateMachine.Tick(Time.deltaTime);
    }
}

public class PlayerControlledLook : AStateBase<PlayerCameraComponent>
{
    public override void Tick()
    {
        Host.ProcessMouseInput();
		Host.ProcessTargetFOV();
    }
}

public class ObjectFocusLook : AStateBase<PlayerCameraComponent>
{
    public override void Tick()
    {
		Host.ProcessLookTowardsTransform();
		Host.ProcessTargetFOV();
    }

	public override void OnExit()
	{
        Host.AlignBodyAndTiltAndResetCameraLook();
	}
}

public class ObjectFocusLookWithControls : AStateBase<PlayerCameraComponent>
{
    public override void Tick()
    {
        Host.ProcessMouseInput();
        Host.ProcessLookTowardsTransform();
        Host.ProcessTargetFOV();
    }

	public override void OnExit()
	{
        Host.AlignBodyAndTiltAndResetCameraLook();
    }
}


public class CameraIdleState : AStateBase <PlayerCameraComponent>
{
    
}
