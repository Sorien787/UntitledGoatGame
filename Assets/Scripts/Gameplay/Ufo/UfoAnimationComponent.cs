using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UfoAnimationComponent : MonoBehaviour, IPauseListener 
{
    [SerializeField] private float m_RotationalVelocity;
    [SerializeField] private float m_MaxRotationalAngle;
    [SerializeField] private float m_MaxAccelerationRotation;
    [SerializeField] private float m_AccelerationRequiredToTilt;
    [SerializeField] private Rigidbody m_Body;
    [SerializeField] private ParticleSystem m_SpeedParticleSystem;
    [SerializeField] private float m_StaggerAnimationTime;
    [SerializeField] private CowGameManager m_Manager;

    [SerializeField] private AnimationCurve m_AngularAccelerationDampening;
    [SerializeField] private AnimationCurve m_PitchAnimationCurve;
    [SerializeField] private AnimationCurve m_TiltAnimationCurve;

    public float GetMaxAngle => m_MaxRotationalAngle;
    public float GetMaxRotationSpeed => m_MaxAccelerationRotation;
    public float GetRotationalVelocity => m_RotationalVelocity;
    public float GetAccelerationRequiredToTilt => m_AccelerationRequiredToTilt;
    public float EvaluateTiltCurve(in float time) => m_TiltAnimationCurve.Evaluate(time);
    public float EvaluatePitchCurve(in float time) => m_PitchAnimationCurve.Evaluate(time);
    public float GetStaggerAnimationTime => m_StaggerAnimationTime;
    public float GetAccelerationDampingVal(in float angle) { return m_AngularAccelerationDampening.Evaluate(angle / m_MaxRotationalAngle); }
    public Rigidbody GetBody => m_Body;

    private StateMachine<UfoAnimationComponent> m_AnimationStateMachine;
    private void Awake()
    {
        m_AnimationStateMachine = new StateMachine<UfoAnimationComponent>(new UFOFlyAnimationState(), this);
        m_AnimationStateMachine.AddState(new UFOStaggeredAnimationState());
        m_AnimationStateMachine.AddState(new UFOAbductAnimationState());
        m_AnimationStateMachine.AddState(new UFODeathAnimationState());
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

    private void Update()
    {
        m_AnimationStateMachine.Tick(Time.deltaTime);
    }
    public void OnStaggered() 
    {
        m_AnimationStateMachine.RequestTransition(typeof(UFOStaggeredAnimationState));
    }

    public void OnFlying() 
    {
        m_AnimationStateMachine.RequestTransition(typeof(UFOFlyAnimationState));
    }

    public void OnAbducting() 
    {
        m_AnimationStateMachine.RequestTransition(typeof(UFOAbductAnimationState));
    }

    public void OnDeath() 
    {
        m_AnimationStateMachine.RequestTransition(typeof(UFODeathAnimationState));
    }
}

public class UFOStaggeredAnimationState : AStateBase<UfoAnimationComponent>
{

    private float m_CurrentAnimationTime = 0.0f;

    public override void OnEnter()
    {
        m_CurrentAnimationTime = 0.0f;
    }
    public override void Tick()
    {
        m_CurrentAnimationTime += Time.deltaTime;
        float scaledTime = m_CurrentAnimationTime / Host.GetStaggerAnimationTime;
        float pitch = Host.EvaluatePitchCurve(scaledTime);
        float roll = Host.EvaluateTiltCurve(scaledTime);

		Host.GetBody.rotation = Quaternion.RotateTowards(Host.GetBody.rotation, Quaternion.identity, Host.GetRotationalVelocity);
    }
}

public class UFOFlyAnimationState : AStateBase<UfoAnimationComponent>
{

    private Vector3 velocityLastFrame;
    private Vector3 accelerationLastFrame;
    public override void Tick()
    {
        Vector3 velocity = Host.GetBody.velocity;
        Vector3 acceleration = (velocity - velocityLastFrame) / Time.deltaTime;

        if (velocity.magnitude > 2.0f) 
        {

        }    


        float velocityContinuation = Vector3.Dot(accelerationLastFrame.normalized, acceleration.normalized);

        Vector3 accelerationInPlane = Vector3.ProjectOnPlane(acceleration, Vector3.up).normalized;
        float tiltAngle = 30;
        Quaternion targetQuat = Quaternion.identity;
        if (acceleration.magnitude > Host.GetAccelerationRequiredToTilt) 
        {
            targetQuat = Quaternion.AngleAxis(tiltAngle, Vector3.Cross(Vector3.up, accelerationInPlane).normalized);
        }

        // tilt towards acceleration

        // only tilt if acceleration is parallel/antiparallel to velocity

       // float angle = Vector3.Angle(acceleration, velocity);
        // now angleTime goes from 1 at parallel, 0 at perpendicular, and -1 at fully antiparallel
        //1 -  
        //      -
        //          -
        //              -
        //0 ============    -    ==============
        //                      -
        //                          -
        //                              -
        //-1                                -
        // paralell  perpendicular  antiparallel
        //float angleTime = 1 - angle/90;



        //break down acceleration direction into planes defined by x and z axis
        // take acceleration direction
        float angularVelocity = Host.GetRotationalVelocity * Time.deltaTime * Host.GetAccelerationDampingVal(Quaternion.Angle(Host.GetBody.rotation, targetQuat));

		Host.GetBody.rotation = Quaternion.RotateTowards(Host.GetBody.rotation, targetQuat, angularVelocity);

        accelerationLastFrame = acceleration;

        velocityLastFrame = velocity;
    }
}

public class UFOAbductAnimationState : AStateBase<UfoAnimationComponent>
{

    public override void Tick()
    {
        Host.GetBody.rotation = Quaternion.RotateTowards(Host.GetBody.rotation, Quaternion.identity, Host.GetRotationalVelocity * Time.deltaTime);
    }
}

public class UFODeathAnimationState : AStateBase<UfoAnimationComponent>
{

    public override void Tick()
    {
        
    }
}