using System.Collections;
using System.Collections.Generic;
using System.Security.Principal;
using UnityEngine;
using System;
public class PlayerMovement : MonoBehaviour, IPauseListener
{
	[Header("World Params")]
    [SerializeField] private float m_fGravity;
    [SerializeField] private float m_fGroundDistance = 0.1f;
    [SerializeField] private LayerMask groundMask;

	[Header("Movement Params")]
	[SerializeField] private float m_fMaxSpeed = 4;
	[SerializeField] private float m_fJumpHeight = 3.0f;
    [SerializeField] private float m_fImpactSpeedReductionPerSecondGrounded;

	[Header("Object References")]
	[SerializeField] private CharacterController m_CharacterController = null;
	[SerializeField] private Transform m_tBodyTransform = null;
	[SerializeField] private Transform m_tGroundTransform = null;
    [SerializeField] private AudioManager m_AudioManager = null;

	[SerializeField] private PlayerCameraComponent m_CameraComponent;
	[SerializeField] private CowGameManager m_Manager;
	[SerializeField] private ThrowablePlayerComponent m_throwableObjectComponent;
	[SerializeField] private AnimationCurve m_SpinningStrengthSlowCurve;
    [SerializeField] private AnimationCurve m_SpinningMassSlowCurve;
    [SerializeField] private AnimationCurve m_ImpactStrengthByImpactSpeed;
	[SerializeField] private LassoInputComponent m_LassoComponent;
	[SerializeField] private GameObject m_GroundImpactEffectsPrefab;
    [SerializeField] private ParticleEffectsController m_DragEffectsManager;
    [SerializeField] private ImpactEffectStrengthManager m_DragEffectsStrengthManager;
    
	[Header("Control Bindings")]
	[SerializeField] private ControlBinding m_ForwardBinding;
	[SerializeField] private ControlBinding m_LeftBinding;
	[SerializeField] private ControlBinding m_RightBinding;
	[SerializeField] private ControlBinding m_BackBinding;
	[SerializeField] private ControlBinding m_JumpBinding;

    [SerializeField] private SoundObject m_ImpactSoundObject;
    [SerializeField] private SoundObject m_JumpSoundObject;
    [SerializeField] private SoundObject m_DragSoundObject;
    [SerializeField] private SoundObject m_ImpactLightSoundObject;

    [SerializeField] private AnimationCurve m_angleWalkCurve;
    [SerializeField] private float m_angleAtMaxSlide = 10.0f;
    [SerializeField] private AnimationCurve m_slidingSpeedCurve;

    public event Action OnSuccessfulJump;
    public event Action<float> OnHitGround;
    public event Action OnNotHitGround;
    public event Action<float> OnSetMovementSpeed;
	public event Action<Vector3> OnMovingInput;
    private Vector3 m_externalVelocity;
    private bool m_bWasGroundedLastFrame = false;
    private Vector3 m_PositionLastFrame = Vector3.zero;
    private Vector3 m_MoveAcceleration = Vector3.zero;
    private Vector3 m_CurrentMoving = Vector3.zero;
    private Vector3 m_LastGroundedNormal = Vector3.up;
    private bool m_bIsSliding = false;
    private float m_fCurrentSpinningMassSpeedDecrease = 1.0f;
    private float m_fCurrentSpinningStrengthSpeedDecrease = 1.0f;
    private float m_fCurrentDraggingSpeedDecrease = 1.0f;
    private float m_fSpeed;
    private bool m_bIsGrounded;

    // Start is called before the first frame update
    void Start()
    {
        m_LassoComponent.OnSetPullingObject += OnIsPullingObject;
        m_LassoComponent.OnSetSwingingObject += OnIsSpinningObject;
        m_LassoComponent.OnSetSwingingStrength += OnSetSpinningStrength;

        m_LassoComponent.OnStoppedPullingObject += OnStoppedPulling;
        m_LassoComponent.OnStoppedSwingingObject += OnStoppedSpinning;

        m_throwableObjectComponent.OnThrown += OnThrown;

        OnHitGround += OnPlayerHitGround;
        m_Manager.AddToPauseUnpause(this);
    }
    private void OnDestroy()
    {
        m_Manager.RemoveFromPauseUnpause(this);
    }
    public void Pause()
    {
        enabled = false;
        m_AudioManager.StopPlaying(m_DragSoundObject);
    }

    public void Unpause()
    {
        enabled = true;
        m_AudioManager.Play(m_DragSoundObject);
    }

    void OnIsSpinningObject(ThrowableObjectComponent throwableObject) 
    {
        m_fCurrentSpinningStrengthSpeedDecrease = m_SpinningStrengthSlowCurve.Evaluate(throwableObject.GetMass());
    }

    void OnSetSpinningStrength(float spinningStrength) 
    {
        m_fCurrentSpinningMassSpeedDecrease = m_SpinningMassSlowCurve.Evaluate(spinningStrength);
    }

    void OnPlayerHitGround(float speed) 
    {
        float firstKey = m_ImpactStrengthByImpactSpeed.keys[0].time;
        if (Mathf.Abs(speed) > firstKey) 
        {
            float impactVal = m_ImpactStrengthByImpactSpeed.Evaluate(speed);
            m_AudioManager.PlayOneShot(m_ImpactSoundObject);
            GameObject resultObject = Instantiate(m_GroundImpactEffectsPrefab, m_tGroundTransform.position, m_tGroundTransform.rotation);
            resultObject.GetComponent<ImpactEffectStrengthManager>().SetParamsOfObject(impactVal);
        }
    }


    void OnStoppedSpinning() 
    {
        m_fCurrentSpinningMassSpeedDecrease = 1.0f;
        m_fCurrentSpinningStrengthSpeedDecrease = 1.0f;
    }

    void OnIsPullingObject(ThrowableObjectComponent throwableObject) 
    {
        m_fCurrentDraggingSpeedDecrease = 0.0f;
    }

    void OnStoppedPulling() 
    {
        m_fCurrentDraggingSpeedDecrease = 1.0f;
    }

    // Update is called once per frame

    void OnThrown(ProjectileParams throwDetails) 
    {
        m_externalVelocity = throwDetails.EvaluateVelocityAtTime(0);
    }


    private float m_fJumpCooldown = 0.0f;
    private bool m_bWasSlidingLastFrame = false;
    [SerializeField] [Range(0.0f, 45.0f)] private float m_SlopeLimit = 20.0f;

    void OnControllerColliderHit(ControllerColliderHit hit) 
    {
        m_LastGroundedNormal = hit.normal;
    }

	private void OnDrawGizmosSelected()
	{
        Gizmos.DrawWireSphere(m_tGroundTransform.position, m_fGroundDistance);
	}
    private float m_currentEffectsStrength = 0.0f;
    private float m_effectChangeTime = 0.6f;
    private float m_effectChangeVelocity = 0.0f;
    private const float m_slideFXThreshold = 0.1f;
    private bool m_bShowingSlidingFX = false;
	void FixedUpdate()
    {
        float swingingMultiplier = m_fCurrentSpinningMassSpeedDecrease * m_fCurrentSpinningStrengthSpeedDecrease * m_fCurrentDraggingSpeedDecrease;
        m_fSpeed = m_fMaxSpeed * swingingMultiplier;
        m_bIsGrounded = Physics.CheckSphere(m_tGroundTransform.position, m_fGroundDistance, groundMask) || m_CharacterController.isGrounded;

		float forwardSpeed = m_ForwardBinding.GetBindingVal() - m_BackBinding.GetBindingVal();
		float sideSpeed = m_RightBinding.GetBindingVal() - m_LeftBinding.GetBindingVal();
		Vector3 playerInputMoveDir = Vector3.zero;
		if (Mathf.Abs(sideSpeed) > 0 || Mathf.Abs(forwardSpeed) > 0)
		{
			playerInputMoveDir = (forwardSpeed * m_tBodyTransform.forward + sideSpeed * m_tBodyTransform.right).normalized;
		}

		OnMovingInput?.Invoke(new Vector3(forwardSpeed, 0, sideSpeed));

        float angleInternal = Vector3.Angle(Vector3.up, m_LastGroundedNormal);
        float angleMultiplier = 1.0f;
        if (angleInternal > m_SlopeLimit)
            angleMultiplier = m_angleWalkCurve.Evaluate((angleInternal - m_SlopeLimit) / m_angleAtMaxSlide);

        if (m_bIsGrounded)
        {
			Vector3 horizontalVelocity = Vector3.ProjectOnPlane(m_externalVelocity, Vector3.up) / 1.1f;
			m_externalVelocity.x = horizontalVelocity.x;
			m_externalVelocity.z = horizontalVelocity.z;



            m_CurrentMoving = Vector3.SmoothDamp(m_CurrentMoving, playerInputMoveDir * m_fSpeed * swingingMultiplier * angleMultiplier, ref m_MoveAcceleration, 0.1f);
            if (!m_bWasGroundedLastFrame) 
            {
                m_AudioManager.PlayOneShot(m_ImpactLightSoundObject);
                OnHitGround?.Invoke(m_CharacterController.velocity.y);
            }
        }
        else 
        {
            if (m_bWasGroundedLastFrame) 
            {
                OnNotHitGround?.Invoke();
            }
            m_CurrentMoving = Vector3.SmoothDamp(m_CurrentMoving, playerInputMoveDir * m_fSpeed * swingingMultiplier * angleMultiplier, ref m_MoveAcceleration, 0.5f);
            m_externalVelocity.y += m_fGravity * Time.fixedDeltaTime;
        }

        m_bWasGroundedLastFrame = m_bIsGrounded;

        if (m_JumpBinding.IsBindingPressed() && m_bIsGrounded && m_fJumpCooldown == 0.0f) 
        {
            m_AudioManager.PlayOneShot(m_JumpSoundObject);
            m_fJumpCooldown += 0.3f;
            OnSuccessfulJump?.Invoke();
            m_externalVelocity.y = Mathf.Sqrt(angleMultiplier * m_fJumpHeight * -2f * swingingMultiplier * m_fGravity);
        }

        m_fJumpCooldown = Mathf.Max(0, m_fJumpCooldown - Time.deltaTime);

        float angle = Vector3.Angle(Vector3.up, m_LastGroundedNormal);
        m_bIsSliding = (angle >= m_SlopeLimit) && m_bIsGrounded;

        float val = Mathf.Clamp01((angleInternal - m_SlopeLimit) / m_angleAtMaxSlide);
        m_currentEffectsStrength = Mathf.SmoothDamp(m_currentEffectsStrength, val, ref m_effectChangeVelocity, m_effectChangeTime);
        if (m_bIsSliding) 
        {
            m_externalVelocity += new Vector3(m_LastGroundedNormal.x, 0, m_LastGroundedNormal.z) * m_slidingSpeedCurve.Evaluate(val);
        }

        m_bShowingSlidingFX = (angleInternal - m_SlopeLimit) / m_angleAtMaxSlide > m_slideFXThreshold;
        m_AudioManager.SetVolume(m_DragSoundObject, m_currentEffectsStrength);
        m_DragEffectsStrengthManager.SetParamsOfObject(m_currentEffectsStrength);
        if (m_bShowingSlidingFX) 
        {
 

            if (!m_bWasSlidingLastFrame) 
            {
                m_DragEffectsManager.TurnOnAllSystems();
            }
        }
		else 
        {
            if (m_bWasSlidingLastFrame) 
            {
                m_DragEffectsManager.TurnOffAllSystems();
            }
        }
        m_bWasSlidingLastFrame = m_bShowingSlidingFX;
        m_CharacterController.Move(m_CurrentMoving * Time.fixedDeltaTime + m_externalVelocity * Time.fixedDeltaTime);

        Vector3 posThisFrame = m_tBodyTransform.position;
        Vector3 movementThisFrame = posThisFrame - m_PositionLastFrame;
        m_PositionLastFrame = posThisFrame;

        OnSetMovementSpeed?.Invoke(Mathf.Clamp01(movementThisFrame.magnitude / (Time.deltaTime * m_fSpeed)));
    }
}
