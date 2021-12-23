using UnityEngine;
using UnityEngine.AI;
[RequireComponent(typeof(NavMeshAgent))]
public class AnimalMovementComponent : MonoBehaviour
{
    [SerializeField] private float m_fMaximumWanderDistance;
    [SerializeField] private float m_fStuckTime;
    [SerializeField] private float m_fStuckSpeed;
    [SerializeField] private float m_RunSpeed;
    [SerializeField] private float m_IdleSpeed;
    [SerializeField] private float m_RotationSpeed;
    [SerializeField] private float m_IdleAcceleration;
    [SerializeField] private float m_RunAcceleration;
    [SerializeField] [Range(0f, 2f)] private float m_fChaseBufferSize = 0.5f;

    [SerializeField] private Rigidbody m_AnimalRigidBody;
    [SerializeField] private AnimationCurve m_UprightAccelerationScalar;

    public float TimeOnGround { get; private set; }

    private Vector3 m_vDestination;
    private float m_fCurrentTimeStuck = 0.0f;
    private Vector3 m_vPositionLastFrame;
    private PhysicalEntity m_PhysicalEntity;
    private Transform m_tObjectTransform;
    private NavMeshAgent m_NavMeshAgent;
    private LayerMask m_iLayerMask;

    //////////////////////////////////////////////////////////////////////////////////////////////
    void Awake()
    {
        m_iLayerMask = 1 << NavMesh.GetNavMeshLayerFromName("Default");
        m_tObjectTransform = GetComponent<Transform>();
        m_NavMeshAgent = GetComponent<NavMeshAgent>();
        m_PhysicalEntity = GetComponent<PhysicalEntity>();
        m_vPositionLastFrame = m_tObjectTransform.position;
        m_vDestination = m_tObjectTransform.position;
        m_fCurrentTimeStuck = 0.0f;
        enabled = false;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////
    private void Update()
    {
        if (Vector3.Distance(m_vPositionLastFrame, m_tObjectTransform.position)/Time.deltaTime < m_fStuckSpeed && !HasReachedDestination())
        {
            m_fCurrentTimeStuck += Time.deltaTime;
        }
        else 
        {
            m_fCurrentTimeStuck = 0;
        }

        m_vPositionLastFrame = m_tObjectTransform.position;
        Debug.DrawLine(m_vDestination, m_tObjectTransform.position, Color.red);

        float m_fStuckPercentage = m_fCurrentTimeStuck / m_fStuckTime;
        Vector3 stuck1 = m_tObjectTransform.up * 3 + m_tObjectTransform.right;
        Vector3 stuck2 = m_tObjectTransform.up * 3 + m_tObjectTransform.right - m_tObjectTransform.right * 2 * m_fStuckPercentage;
        Debug.DrawLine(stuck1, stuck2, Color.red * (1 - m_fStuckPercentage) + Color.green * m_fStuckPercentage);
    }

	//////////////////////////////////////////////////////////////////////////////////////////////
	// function chooses a random destination within range m_fMaximumWanderDistance on the navmesh
	public bool ChooseRandomDestination() 
    {
        enabled = true;
        m_fCurrentTimeStuck = 0.0f;
        var randomDirection = Random.insideUnitSphere * m_fMaximumWanderDistance;

        randomDirection += m_tObjectTransform.position;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, 30, m_iLayerMask)) 
        {
            if (m_NavMeshAgent.isOnNavMesh && m_NavMeshAgent.SetDestination(hit.position)) 
            {
                m_vDestination = hit.position;
                return true;
            }
        }
        return false;
    }

    public bool IsNearNavMesh() 
    {
        return NavMesh.SamplePosition(m_tObjectTransform.position, out NavMeshHit _, 1.0f, m_iLayerMask);
    }

    public void ClearDestination() 
    {
        if (m_NavMeshAgent.isOnNavMesh) 
        {
            m_NavMeshAgent.ResetPath();
        }
		m_vDestination = m_tObjectTransform.position;

	}

    public void RunInDirection(Vector3 dir) 
    {
        if (!m_PhysicalEntity.IsGrounded)
        {
            m_AnimalRigidBody.freezeRotation = false;
            return;
        }
		else 
        {
            m_AnimalRigidBody.freezeRotation = true;
        }
        Vector3 targetUp = m_PhysicalEntity.GetGroundedNorm;
        Vector3 targetForward = Vector3.ProjectOnPlane(dir, targetUp).normalized;
        Vector3 currentVelocity = (m_AnimalRigidBody.position - m_vPositionLastFrame)/Time.deltaTime;
        float percentageTowardsGoalVelocity = Mathf.Clamp(Vector3.Dot(targetForward, currentVelocity) / m_RunSpeed, 0, 2f);
        float parallelVelocityAccelerationScaler = 1 - percentageTowardsGoalVelocity;

        // also has trouble getting up edges - help it up a bit maybe?


        Vector3 currentVelocityPerp = currentVelocity - Vector3.Dot(targetForward, currentVelocity) * targetForward;
        float perpVelocityAccelerationScalar = -Mathf.Clamp01(currentVelocityPerp.magnitude / m_RunSpeed);
        // we also need to tilt to face the direction we want to go
        // as force will offset us and try to stop us :(

        Quaternion desiredRotation = Quaternion.LookRotation(targetForward, targetUp);
        m_AnimalRigidBody.rotation = Quaternion.Lerp(m_AnimalRigidBody.rotation, desiredRotation, m_RotationSpeed * Time.deltaTime);
       // m_AnimalRigidBody.rotation = desiredRotation;// UnityUtils.UnityUtils.SmoothDampQuat(m_AnimalRigidBody.rotation, desiredRotation, ref quatVelocity, 0.4f);

        Quaternion currentToDesired = Quaternion.Inverse(m_AnimalRigidBody.rotation) * desiredRotation;

        currentToDesired.ToAngleAxis(out float ang, out Vector3 axis);

        Vector3 parallelAngVel = Vector3.Dot(m_AnimalRigidBody.angularVelocity, axis) * axis;
        Vector3 unwantedAngVel = m_AnimalRigidBody.angularVelocity - parallelAngVel;

        Vector3 currentAngularVelocity = m_AnimalRigidBody.angularVelocity;



        //Vector3 torque = ;
        //m_AnimalRigidBody.AddTorque(torque, ForceMode.VelocityChange);

        float uprightAccelerationScalar = m_UprightAccelerationScalar.Evaluate(ang) * Time.deltaTime * m_RunAcceleration;

        // only apply forward velocity if we're on the ground (taper it off as we tilt?)
        Vector3 velocityChange = targetForward.normalized * uprightAccelerationScalar * parallelVelocityAccelerationScaler;// targetForward * m_RunAcceleration - currentVelocity.normalized * m_IdleAcceleration * (currentVelocity.magnitude / (m_RunSpeed * 10));
        velocityChange += currentVelocityPerp.normalized * uprightAccelerationScalar * perpVelocityAccelerationScalar;
        m_AnimalRigidBody.MovePosition(m_AnimalRigidBody.position + velocityChange);
        //m_AnimalRigidBody.velocity += velocityChange;
    }



    //////////////////////////////////////////////////////////////////////////////////////////////
    public void Idle() 
    {
        enabled = false;
    }
    public void SetWalking() 
    {
        m_NavMeshAgent.speed = m_IdleSpeed;
        m_NavMeshAgent.acceleration = m_IdleAcceleration;
    }

    public void SetRunning() 
    {
        m_NavMeshAgent.speed = m_RunSpeed;
        m_NavMeshAgent.acceleration = m_RunAcceleration;
    }
    //////////////////////////////////////////////////////////////////////////////////////////////
    public bool HasReachedDestination() 
    {
        return Vector3.SqrMagnitude(m_vDestination - m_tObjectTransform.position) < 0.5f || !m_NavMeshAgent.hasPath;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////
    public bool IsStuck() 
    { 
        return m_fCurrentTimeStuck > m_fStuckTime; 
    }

    //////////////////////////////////////////////////////////////////////////////////////////////
    // function chooses a destination within range m_fMaximumRunDistance directly away from objectTransform on the navmesh
    public bool RunAwayFromObject(Transform tRunAwayTransform, float runDistance)
    {
        enabled = true;
        m_fCurrentTimeStuck = 0.0f;
        Vector3 displacement = m_tObjectTransform.position - tRunAwayTransform.position;
        float distance = displacement.magnitude;
        Vector3 direction = displacement / distance;

        float distanceToRun = m_fChaseBufferSize + runDistance - distance;
        Vector3 runTo = direction * distanceToRun + m_tObjectTransform.position;

        if (NavMesh.SamplePosition(runTo, out NavMeshHit hit, distanceToRun, m_iLayerMask))
        {
            if (m_NavMeshAgent.SetDestination(hit.position)) 
            {
                m_vDestination = hit.position;
                return true;
            }
        }
        return false;
    }

    public bool CheckStoppingDistanceForChase(Transform tRunTowardTransform, float distanceFrom = 0f) 
    {
        Vector3 displacement = m_tObjectTransform.position - tRunTowardTransform.position;
        if ((Vector3.ProjectOnPlane(displacement, Vector3.up)).sqrMagnitude < distanceFrom * distanceFrom)
        {
            m_NavMeshAgent.isStopped = true;
            return true;
        }
        return false;
    }
    Transform targetTransform;
    //////////////////////////////////////////////////////////////////////////////////////////////
    // function chooses a destination within range m_fMaximumRunDistance directly away from objectTransform on the navmesh
    public bool RunTowardsObject(Transform tRunTowardTransform, float runDistance, float distanceFrom = 0f) 
    {
        enabled = true;
        m_fCurrentTimeStuck = 0.0f;
        if (CheckStoppingDistanceForChase(tRunTowardTransform, distanceFrom))
		{
            return true;
		}
        m_NavMeshAgent.isStopped = false;
        targetTransform = tRunTowardTransform;
        Vector3 target_localSpace = tRunTowardTransform.position - m_tObjectTransform.position;
        float targetDistance_localSpace = target_localSpace.magnitude;
        target_localSpace.Normalize();
        float desiredDistance_localSpace = Mathf.Max(targetDistance_localSpace - distanceFrom + m_fChaseBufferSize, 0);
        target_localSpace *= desiredDistance_localSpace;

        if(NavMesh.SamplePosition(m_tObjectTransform.position + target_localSpace, out NavMeshHit hit, runDistance, m_iLayerMask)) 
        {
            if (m_NavMeshAgent.SetDestination(hit.position)) 
            {
				m_vDestination = hit.position;
                return true;
            }
        }
        return false;
    }

	private void OnDrawGizmos()
	{
        if (!targetTransform)
            return;

        Gizmos.DrawLine(m_tObjectTransform.position, targetTransform.position);
	}
}
