using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class FlightComponent : MonoBehaviour, IPauseListener
{
    [SerializeField]
    private float m_MaximumAcceleration;
    [SerializeField]
    private float m_CruiseSpeed;
    [SerializeField]
    private float m_DistanceTolerance;
    [SerializeField]
    private float m_FinalSpeed;

    [SerializeField]
    private float m_StoppedTolerance;
    [SerializeField] private CowGameManager m_Manager;

    bool m_bHasStopped = false;
    bool m_bHoldCommand = false;

    private Transform m_Transform;
    private Rigidbody m_Body;
    private Vector3 m_Destination;
    public IEnumerator m_FlightCoroutine;

    private void Awake()
    {
        m_Body = GetComponent<Rigidbody>();
        m_Transform = transform;
        UpdateAction = HasReachedDestination;
        position = m_Transform.position;
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

    public void UpdateLinearDestination(in Vector3 destination) 
    {
        m_Destination = destination;
    }

    public void SetLinearDestination(in Vector3 destination) 
    {
        StartCoroutine(DelayedStartFollowDestination(destination));
    }


    // for some reason this doesnt work unless it's started delayed in an enumerator...
    private IEnumerator DelayedStartFollowDestination(Vector3 destination) 
    {
        yield return null;
        OnAutopilotCancelled?.Invoke();
        m_Destination = destination;
        UpdateAction = MovingToDestination;
    }

    public void SetHold(bool hold) 
    {
        m_bHoldCommand = hold;
    }

    public void SetTargetSpeed(in float finalVelocity) 
    {
        m_FinalSpeed = finalVelocity;
    }

    public void SetCruiseSpeed(in float cruiseVelocity) 
    {
        m_CruiseSpeed = cruiseVelocity;
    }

    public void StopFlight() 
    {
        UpdateAction = HasReachedDestination;
        accelDirection = Vector3.zero;
    }

    public void ResetFlightCallback()
    {
        OnAutopilotPositionCompleted = null;
    }

    public void ResetStoppedCallback() 
    {
        OnAutopilotArrested = null;
    }

    Vector3 accelDirection = Vector3.zero;

    Vector3 velocity = Vector3.zero;
    Vector3 position = Vector3.zero;
    private void MovingToDestination() 
    {
        Vector3 offsetFromDestination = m_Destination - m_Transform.position;
        // we need to both decellerate perpendicular velocity and accelerate linear velocity.
        // linear velocity can be accelerated/decellerate depending on wheth

        Vector3 normalizedTargetDirection = offsetFromDestination.normalized;


        Vector3 currentVelocity = m_Body.velocity;
        Vector3 velParallel = normalizedTargetDirection * Vector3.Dot(normalizedTargetDirection, currentVelocity);
        Vector3 velPerpendicular = currentVelocity - velParallel;
        Vector3 velocityChangeThisFrame = Vector3.zero;




            // slow down in that direction
        velocityChangeThisFrame -= velPerpendicular.normalized * Mathf.Clamp(m_MaximumAcceleration * Time.fixedDeltaTime, 0.0f, velPerpendicular.magnitude);

        // if we need to slow down to reach target
        float desiredSpeedChange = m_FinalSpeed - currentVelocity.magnitude;


        float distanceToDecellerate = 0.0f;
        float USqMinusVSq = velParallel.sqrMagnitude - m_FinalSpeed * m_FinalSpeed;
        if (USqMinusVSq > 0)     
        {
            distanceToDecellerate = USqMinusVSq / (2 * m_MaximumAcceleration);
        }

        if (Vector3.Dot(normalizedTargetDirection, currentVelocity) > 0 && distanceToDecellerate * distanceToDecellerate > offsetFromDestination.sqrMagnitude)
        {
            // slow down
            float currentDesired = m_MaximumAcceleration * Time.fixedDeltaTime;
            float clamped = Mathf.Clamp(m_MaximumAcceleration * Time.fixedDeltaTime, 0.0f, Mathf.Abs(velParallel.magnitude - m_FinalSpeed));
            Vector3 addition = normalizedTargetDirection * clamped;
            velocityChangeThisFrame -= addition;
        }
        // if we're over max speed
        else if (velParallel.sqrMagnitude > m_CruiseSpeed * m_CruiseSpeed)
        {
            velocityChangeThisFrame -= normalizedTargetDirection * Mathf.Clamp(m_MaximumAcceleration * Time.fixedDeltaTime, 0.0f, Mathf.Abs(velParallel.magnitude - m_CruiseSpeed));
        }
        // if we're under max speed
        else if (velParallel.sqrMagnitude < m_CruiseSpeed * m_CruiseSpeed)
        {
            velocityChangeThisFrame += normalizedTargetDirection * Mathf.Clamp(m_MaximumAcceleration * Time.fixedDeltaTime, 0.0f, Mathf.Abs(m_CruiseSpeed - velParallel.magnitude));
        }

        accelDirection = velocityChangeThisFrame.normalized;

        m_Body.velocity += velocityChangeThisFrame;

        m_bHasStopped = currentVelocity.sqrMagnitude < m_StoppedTolerance * m_StoppedTolerance;

        if (!m_bHoldCommand && offsetFromDestination.sqrMagnitude < m_DistanceTolerance * m_DistanceTolerance) 
        {
            Vector3 destination = m_Destination;
            Vector3 currentLocation = m_Transform.position;
            Vector3 offset = destination - currentLocation;
            float dist = offset.sqrMagnitude;

            accelDirection = Vector3.zero;
            OnAutopilotPositionCompleted?.Invoke();
            UpdateAction = HasReachedDestination;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;

        Gizmos.DrawLine(transform.position, transform.position + accelDirection * 10);

        Vector3 offsetFromDestination = m_Destination - transform.position;

        if (offsetFromDestination.sqrMagnitude > m_DistanceTolerance * m_DistanceTolerance) 
        {
            Gizmos.DrawWireSphere(m_Destination, 2.0f);
        }

    }

    private void HasReachedDestination() 
    {
        // get current velocity, slow down to zero.
        Vector3 currentVelocity = m_Body.velocity;

        float velocityChange = m_MaximumAcceleration * Time.fixedDeltaTime;

        float maximumVelocityChange = currentVelocity.magnitude;

        Vector3 acceleration = -currentVelocity.normalized * Mathf.Min(velocityChange, maximumVelocityChange);

        m_Body.velocity += acceleration;

        bool hasStopped = m_Body.velocity.sqrMagnitude < m_StoppedTolerance * m_StoppedTolerance;
        if (hasStopped != m_bHasStopped) 
        {
            if (hasStopped) 
            {
                OnAutopilotArrested?.Invoke();
            }
            m_bHasStopped = hasStopped;
        }
    }

    private void Update()
    {
        UpdateAction();
    }

    private Action UpdateAction;

    public event Action OnAutopilotPositionCompleted;

    public event Action OnAutopilotCancelled;

    public event Action OnAutopilotArrested;
}
