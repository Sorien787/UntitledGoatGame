using UnityEngine;
using System;
using System.Collections.Generic;
public class FreeFallTrajectoryComponent : MonoBehaviour, IPauseListener
{

    [Header("Internal References")]
    [SerializeField] private Rigidbody m_rMovingBody;
	[SerializeField] private DebugTextComponent m_debugTextComponent;
    [Header("External References")]
    [SerializeField] private CowGameManager m_Manager;
    [Header("Settings")]
    [SerializeField] private List<GameObject> m_listOfObjsToChangeLayer = new List<GameObject>();
    [SerializeField] private int m_ThrownLayer = 0;
    [SerializeField] private LayerMask m_GroundImpactLayermask;
    [SerializeField] private SoundObject m_ThrowinSound;
    [SerializeField] private AudioManager m_Audio;
    [SerializeField] private bool m_bUsesTriggers = false;

    private bool m_bIsFalling = false;
    private ProjectileParams projectile;
    private Transform m_Transform;
    private float m_fCurrentTime = 0.0f;

	private UnityUtils.ListenerSet<IFreeFallListener> m_Listeners = new UnityUtils.ListenerSet<IFreeFallListener>();

	public void AddListener(IFreeFallListener listener)
	{
		m_Listeners.Add(listener);
	}

	public void RemoveListener(IFreeFallListener listener)
	{
		m_Listeners.Remove(listener);
	}

	private void Awake()
	{
        m_Transform = transform;
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

    public void ThrowObject(in ProjectileParams projectileParams) 
    {
        m_fCurrentTime = 0.0f;
        projectile = projectileParams;
        m_rMovingBody.isKinematic = true;
		m_bIsFalling = true;
        m_rMovingBody.position = projectile.EvaluatePosAtTime(0.0f);
        m_rMovingBody.rotation = projectile.EvaluateRotAtTime(0.0f);
		m_positionLastFrame = m_rMovingBody.position;
        m_Audio.Play(m_ThrowinSound);

        foreach (GameObject go in m_listOfObjsToChangeLayer) 
        {
            m_ObjectsToChangeBack.Add(new Tuple<GameObject, int>(go, go.layer));
            go.layer = m_ThrownLayer;
        }
    }

    private List<Tuple<GameObject, int>> m_ObjectsToChangeBack = new List<Tuple<GameObject, int>>();

    public void StopThrowingObject() 
    {
		StopThrowingInternal();
        m_Audio.StopPlaying(m_ThrowinSound);
    }

    private void StopThrowingInternal() 
    {
        m_bIsFalling = false;
        foreach(Tuple<GameObject, int> tuple in m_ObjectsToChangeBack) 
        {
            tuple.Item1.layer = tuple.Item2;
        }
    }

	private void OnTriggerStay(Collider other)
	{
        if (m_bUsesTriggers)
		{
            var collisionPoint = other.ClosestPoint(m_Transform.position);
            OnCollide(collisionPoint, Vector3.up, other.gameObject);
        }
	}

	private void OnCollisionEnter(Collision collision)
    {
        OnCollide(collision.GetContact(0).point, collision.GetContact(0).normal, collision.gameObject);
    }

	private void OnCollide(Vector3 pos, Vector3 norm, GameObject go)
	{   
		if (m_bIsFalling)
        {
            m_rMovingBody.velocity = projectile.EvaluateVelocityAtTime(m_fCurrentTime);
            m_rMovingBody.angularVelocity = projectile.m_vRotAxis * projectile.m_fAngVel;
            m_Listeners.ForEachListener((IFreeFallListener listener) => 
			{
				listener.OnCollide(pos, norm, go);
			});
            m_rMovingBody.velocity = projectile.EvaluateVelocityAtTime(m_fCurrentTime);
            m_rMovingBody.angularVelocity = projectile.m_vRotAxis * projectile.m_fAngVel;
            StopThrowingInternal();
        }
    }
	private Vector3 m_positionLastFrame = Vector3.zero;
	void FixedUpdate()
    {
		if (m_bIsFalling)
		{
			m_fCurrentTime += Time.fixedDeltaTime;
            Vector3 desiredPos = projectile.EvaluatePosAtTime(m_fCurrentTime);
            Vector3 offset = m_rMovingBody.position - m_positionLastFrame;
            if (Physics.Raycast(m_positionLastFrame, offset.normalized, out RaycastHit hit, offset.magnitude, m_GroundImpactLayermask, QueryTriggerInteraction.Ignore)) 
            {
                OnCollide(hit.point, hit.normal, hit.collider.gameObject);
            }
			else 
            {
				m_rMovingBody.MovePosition(projectile.EvaluatePosAtTime(m_fCurrentTime));
				m_rMovingBody.MoveRotation(projectile.EvaluateRotAtTime(m_fCurrentTime));
			}

			m_positionLastFrame = m_rMovingBody.position;

		}
		if (m_debugTextComponent)
			m_debugTextComponent.AddLine(string.Format("Free fall: {0} \n Time free falling: {1}", m_bIsFalling ? "active" : "inactive", m_fCurrentTime.ToString()));
    }
}

public struct ProjectileParams 
{
    public float m_fThrowSpeed;
    public Vector3 m_vStartPos;
    public Vector3 m_vRotAxis;
    public Vector3 m_vForwardDir;
    public float m_fElevationAngle;
    public float m_fAngVel;
    public float m_fGravityMult;

    public ProjectileParams(IThrowableObjectComponent throwable, float force, Vector3 throwDirection, Vector3 origin, float angularVelocity = 0)
    {
        m_fThrowSpeed = force/throwable.GetMass();
        m_vStartPos = origin;
        m_fGravityMult = throwable.GetGravityMultiplier;
        m_vRotAxis = UnityEngine.Random.insideUnitSphere;
        m_vForwardDir = Vector3.ProjectOnPlane(throwDirection, Vector3.up).normalized;
        m_fElevationAngle = Mathf.Deg2Rad * (90 - Vector3.Angle(Vector3.up, throwDirection));
        m_fAngVel = angularVelocity;
    }

    public ProjectileParams(float speed, Vector3 throwDirection, Vector3 origin, float angularVelocity = 0) 
    {
        m_fGravityMult = 1;
        m_fThrowSpeed = speed;
        m_vStartPos = origin;
        m_vRotAxis = UnityEngine.Random.insideUnitSphere;
        m_vForwardDir = Vector3.ProjectOnPlane(throwDirection, Vector3.up).normalized;
        m_fElevationAngle = Mathf.Deg2Rad * (90 - Vector3.Angle(Vector3.up, throwDirection));
        m_fAngVel = angularVelocity;
    }

    public void SetAngularVelocity(in float angularVelocity) 
    {
        m_fAngVel = angularVelocity;
    }

    public Vector3 EvaluatePosAtTime(in float time) 
    {
        return m_vStartPos
             + Vector3.up * (-0.5f * UnityEngine.Physics.gravity.magnitude * m_fGravityMult * time * time + m_fThrowSpeed * Mathf.Sin(m_fElevationAngle) * time)
             + m_vForwardDir * (Mathf.Cos(m_fElevationAngle) * m_fThrowSpeed * time);
    }

    public Vector3 EvaluateVelocityAtTime(in float time) 
    {
        return Vector3.up * (-UnityEngine.Physics.gravity.magnitude * m_fGravityMult * time + m_fThrowSpeed * Mathf.Sin(m_fElevationAngle)) + m_vForwardDir * Mathf.Cos(m_fElevationAngle) * m_fThrowSpeed;
    }

    public Quaternion EvaluateRotAtTime(in float time) 
    {
        return Quaternion.AngleAxis(time * m_fAngVel, time * m_vRotAxis);
    }
}

public interface IFreeFallListener
{
	void OnCollide(Vector3 position, Vector3 rotation, GameObject go);
}