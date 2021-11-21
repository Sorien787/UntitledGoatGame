using UnityEngine;
using System;
using System.Collections.Generic;
public class FreeFallTrajectoryComponent : MonoBehaviour, IPauseListener
{
    ProjectileParams projectile;
    private float m_fCurrentTime = 0.0f;
 
    [SerializeField] private Rigidbody m_rMovingBody;
    [SerializeField] private CowGameManager m_Manager;
	[SerializeField] private DebugTextComponent m_debugTextComponent;
    [SerializeField] private List<GameObject> m_listOfObjsToChangeLayer = new List<GameObject>();
    [SerializeField] private LayerMask m_ThrownLayer = 0;

	public event Action<Collision> OnObjectHitGround;
    public event Action OnObjectNotInFreeFall;
	public bool m_bIsFalling = false;

	private void Awake()
	{
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
        foreach(GameObject go in m_listOfObjsToChangeLayer) 
        {
            m_ObjectsToChangeBack.Add(new Tuple<GameObject, int>(go, go.layer));
            go.layer = m_ThrownLayer;
        }
    }

    private List<Tuple<GameObject, int>> m_ObjectsToChangeBack = new List<Tuple<GameObject, int>>();

    public void StopThrowingObject() 
    {
        OnObjectNotInFreeFall?.Invoke();
        StopThrowingInternal();
    }

    private void StopThrowingInternal() 
    {
        m_bIsFalling = false;
        foreach(Tuple<GameObject, int> tuple in m_ObjectsToChangeBack) 
        {
            tuple.Item1.layer = tuple.Item2;
        }
    }

	private void OnCollisionStay(Collision collision)
    {
        if (m_fCurrentTime > 0.5f) 
        {
            if (m_bIsFalling)
            {
                OnObjectHitGround?.Invoke(collision);
                OnObjectNotInFreeFall?.Invoke();
                m_rMovingBody.velocity = projectile.EvaluateVelocityAtTime(m_fCurrentTime);
                m_rMovingBody.angularVelocity = projectile.m_vRotAxis * projectile.m_fAngVel;
            }
            StopThrowingInternal();
        }
    }

    void Update()
    {
		if (m_bIsFalling)
		{
			m_fCurrentTime += Time.deltaTime;
			m_rMovingBody.MovePosition(projectile.EvaluatePosAtTime(m_fCurrentTime));
			m_rMovingBody.MoveRotation(projectile.EvaluateRotAtTime(m_fCurrentTime));
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