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

	[Header("Control Bindings")]
	[SerializeField] private ControlBinding m_ForwardBinding;
	[SerializeField] private ControlBinding m_LeftBinding;
	[SerializeField] private ControlBinding m_RightBinding;
	[SerializeField] private ControlBinding m_BackBinding;
	[SerializeField] private ControlBinding m_JumpBinding;

    [SerializeField] private SoundObject m_ImpactSoundObject;
    [SerializeField] private SoundObject m_FootStepSoundObject;
    [SerializeField] private SoundObject m_JumpSoundObject;
    [SerializeField] private SoundObject m_ImpactLightSoundObject;

    public event Action OnSuccessfulJump;
    public event Action<float> OnHitGround;
    public event Action OnNotHitGround;
    public event Action<float> OnSetMovementSpeed;
	public event Action<Vector3> OnMovingInput;
    private Vector3 m_vVelocity;
    float m_fCurrentSpinningMassSpeedDecrease = 1.0f;
    float m_fCurrentSpinningStrengthSpeedDecrease = 1.0f;
    float m_fCurrentDraggingSpeedDecrease = 1.0f;
    private float m_fSpeed;

    bool m_bHasJumped = false;

    bool m_bIsGrounded;

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
    }

    public void Unpause()
    {
        enabled = true;
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
    bool m_bWasGroundedLastFrame = false;
    Vector3 positionLastFrame;
    // Update is called once per frame

    void OnThrown(ProjectileParams throwDetails) 
    {
        m_vVelocity = throwDetails.EvaluateVelocityAtTime(0);
    }

    Vector3 m_MoveAcceleration = Vector3.zero;
    Vector3 m_CurrentMoving = Vector3.zero;

    void FixedUpdate()
    {
        float currentMultiplier = m_fCurrentSpinningMassSpeedDecrease * m_fCurrentSpinningStrengthSpeedDecrease * m_fCurrentDraggingSpeedDecrease;
        m_fSpeed = m_fMaxSpeed * currentMultiplier;
        m_bIsGrounded = Physics.CheckSphere(m_tGroundTransform.position, m_fGroundDistance, groundMask);

		float forwardSpeed = m_ForwardBinding.GetBindingVal() - m_BackBinding.GetBindingVal();
		float sideSpeed = m_RightBinding.GetBindingVal() - m_LeftBinding.GetBindingVal();
		Vector3 playerInputMoveDir = Vector3.zero;
		if (Mathf.Abs(sideSpeed) > 0 || Mathf.Abs(forwardSpeed) > 0)
		{
			playerInputMoveDir = (forwardSpeed * m_tBodyTransform.forward + sideSpeed * m_tBodyTransform.right).normalized;
		}

		OnMovingInput?.Invoke(new Vector3(forwardSpeed, 0, sideSpeed));


		if (m_CharacterController.isGrounded)
        {
			Vector3 horizontalVelocity = Vector3.ProjectOnPlane(m_vVelocity, Vector3.up) / 1.1f;
			m_vVelocity.x = horizontalVelocity.x;
			m_vVelocity.z = horizontalVelocity.z;
            m_CurrentMoving = Vector3.SmoothDamp(m_CurrentMoving, playerInputMoveDir * m_fSpeed * currentMultiplier, ref m_MoveAcceleration, 0.1f);
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
            m_CurrentMoving = Vector3.SmoothDamp(m_CurrentMoving, playerInputMoveDir * m_fSpeed * currentMultiplier, ref m_MoveAcceleration, 0.5f);
            m_vVelocity.y += m_fGravity * Time.fixedDeltaTime;
        }

        m_bWasGroundedLastFrame = m_CharacterController.isGrounded;



        if (m_JumpBinding.IsBindingPressed() && m_CharacterController.isGrounded) 
        {
            m_AudioManager.PlayOneShot(m_JumpSoundObject);
            OnSuccessfulJump?.Invoke();
            m_vVelocity.y = Mathf.Sqrt(m_fJumpHeight * -2f * currentMultiplier * m_fGravity);
        }    

        m_CharacterController.Move(m_CurrentMoving * Time.fixedDeltaTime + m_vVelocity * Time.fixedDeltaTime);

        Vector3 posThisFrame = m_tBodyTransform.position;
        Vector3 movementThisFrame = posThisFrame - positionLastFrame;
        positionLastFrame = posThisFrame;

        OnSetMovementSpeed?.Invoke(Mathf.Clamp01(movementThisFrame.magnitude / (Time.deltaTime * m_fSpeed)));
    }
}
