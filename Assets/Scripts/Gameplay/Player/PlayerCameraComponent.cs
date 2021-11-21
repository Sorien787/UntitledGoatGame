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

	[Header("--- Anim Strings ---")]
	[SerializeField] private string m_JumpString;
	[SerializeField] private string m_GroundedAnimString;
	[SerializeField] private string m_MovementSpeedAnimString;
	[SerializeField] private string m_LRTiltAnimString;
	[SerializeField] private string m_FBTiltAnimString;

	[Header("--- Sensitivity Multipliers ---")]
	[SerializeField] private float m_fMouseSensitivityMultiplier = 100.0f;

	private float m_fCamPoint;
    float m_fTargetFOV;
    float m_fCurrentFOV;
    private Transform m_tCamTransform;
    private Transform m_tFocusTransform;
    private StateMachine<PlayerCameraComponent> m_CameraStateMachine;
    private Type m_CachedType;

    private void OnSetPullStrength(float force, float yankSize) 
    {
        m_fTargetFOV = m_FOVTugAnimator.Evaluate(yankSize) * force + m_FOVForceAnimator.Evaluate(force) + m_fDefaultFOV;
    }

    private void OnSetPullingObject(ThrowableObjectComponent pullingObject) 
    {
        SetFocusedTransform(pullingObject.GetCameraFocusTransform);
    }

    private void OnStoppedPullingObject() 
    {
        ClearFocusedTransform();
    }
    public void SetFocusedTransform(Transform focusTransform) 
    {
        m_tFocusTransform = focusTransform;
        m_CameraStateMachine.RequestTransition(typeof(ObjectFocusLook));
        m_CachedType = typeof(ObjectFocusLook);
    }

    private void OnJumped() 
    {
        m_CameraAnimator.SetBool(m_JumpString, true);
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

    private float m_CurrentTime = 0.0f;
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

	float currentMovement = 0.0f;
	float currentMovementAcceleration = 0.0f;
    public void OnSetMovementSpeed(float speed) 
    {
		currentMovement = Mathf.SmoothDamp(currentMovement, speed, ref currentMovementAcceleration, 0.15f);
        m_CameraAnimator.SetFloat(m_MovementSpeedAnimString, currentMovement);
    }

	Vector3 current = Vector3.zero;
	Vector3 velocity = Vector3.zero;
	public void OnMovingInput(Vector3 input)
	{
		current = Vector3.SmoothDamp(current, input, ref velocity, 0.1f);
		m_CameraAnimator.SetFloat(m_LRTiltAnimString, current.x);
		m_CameraAnimator.SetFloat(m_FBTiltAnimString, current.z);
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
			m_fDefaultFOV = m_SettingsManager.FoV;
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
        m_PlayerMovement.OnSuccessfulJump += OnJumped;
        m_PlayerMovement.OnSetMovementSpeed += OnSetMovementSpeed;
		m_PlayerMovement.OnMovingInput += OnMovingInput;
    }
    public void Pause()
    {
        enabled = false;
    }

    public void Unpause()
    {
        enabled = true;
    }

	private void OnDestroy()
	{
		m_SettingsManager.PropertyChanged -= OnPropertyChanged;
	}

	public void ProcessTargetFOV() 
    {
        m_fCurrentFOV += Mathf.Clamp(m_fTargetFOV - m_fCurrentFOV, -Time.deltaTime * m_fMaxFOVChangePerSecond, Time.deltaTime * m_fMaxFOVChangePerSecond);
        m_PlayerCamera.fieldOfView = m_fCurrentFOV;
    }

    public void ProcessMouseInput() 
    {
        float mouseX = Input.GetAxis("Mouse X") * m_fMouseSensitivityMultiplier * m_SettingsManager.MouseSensitivityX * Time.deltaTime;
		float invertY = m_SettingsManager.InvertY ? -1 : 1;
		float mouseY = invertY * Input.GetAxis("Mouse Y") * m_fMouseSensitivityMultiplier * m_SettingsManager.MouseSensitivityY * Time.deltaTime;

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

        Quaternion targetCamQuat = Quaternion.FromToRotation(m_tCamTransform.forward, Vector3.ProjectOnPlane(lookDir, m_tCamTransform.right)) * m_tCamTransform.rotation;
        m_tCamTransform.rotation = Quaternion.RotateTowards(m_tCamTransform.rotation, targetCamQuat, 60.0f * Time.deltaTime);

        Quaternion targetBodyQuat = Quaternion.FromToRotation(m_tBodyTransform.forward, Vector3.ProjectOnPlane(lookDir, Vector3.up)) * m_tBodyTransform.rotation;
        m_tBodyTransform.rotation = Quaternion.RotateTowards(m_tBodyTransform.rotation, targetBodyQuat, 60.0f * Time.deltaTime);
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
}

public class CameraIdleState : AStateBase <PlayerCameraComponent>
{
    
}
