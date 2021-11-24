using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicalEntity : MonoBehaviour, IPauseListener
{
    private ContactPoint[] m_Contacts = new ContactPoint[8];
    private Vector3 m_LastGroundedPosition = Vector3.zero;
    private Vector3 m_LastGroundedNormal = Vector3.up;
    private bool m_bIsGrounded = false;
    private Transform m_PhysTransform = null;
    private Collider[] m_colliderResults = new Collider[1];
    [SerializeField] private CowGameManager m_Manager = null;

    private Rigidbody m_Body = null;
    private bool m_bHadVelocity = false;
    private Vector3 m_cachedVelocity = Vector3.zero;
    private Vector3 m_cachedAngVelocity = Vector3.zero;

	private void Awake()
	{
        m_Body = GetComponent<Rigidbody>();
        m_PhysTransform = transform;
        m_Manager.AddToPauseUnpause(this);
    }

	private void Update()
    {
        m_bIsGrounded = Physics.OverlapSphereNonAlloc(m_PhysTransform.position, 0.5f, m_colliderResults, m_Manager.GetGroundLayer()) > 0;
    }

    public void Pause()
    {
        if (!m_Body.isKinematic)
        {
            m_bHadVelocity = true;
            m_cachedVelocity = m_Body.velocity;
            m_cachedAngVelocity = m_Body.angularVelocity;
            m_Body.isKinematic = true;
        }
    }

    public void Unpause()
    {
        if (m_bHadVelocity)
        {
            m_bHadVelocity = false;
            m_Body.angularVelocity = m_cachedAngVelocity;
            m_Body.velocity = m_cachedVelocity;
            m_Body.isKinematic = false;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (m_Manager.IsGroundLayer(collision.gameObject.layer))
        {
            collision.GetContacts(m_Contacts);

            if (collision.contactCount > 0) 
            {
                m_LastGroundedNormal = Vector3.zero;
                m_LastGroundedPosition = Vector3.zero;

                for (int i = 0; i < Mathf.Min(8, collision.contactCount); i++)
                {
                    m_LastGroundedPosition += m_Contacts[i].point;
                    m_LastGroundedNormal += m_Contacts[i].normal.normalized;
                }

                m_LastGroundedNormal /= collision.contactCount;
                m_LastGroundedPosition /= collision.contactCount;

                m_bIsGrounded = true;
            }


        }
    }
    private void OnCollisionExit(Collision collision)
    {
        if (m_Manager.IsGroundLayer(collision.gameObject.layer))
        {
            m_bIsGrounded = false;
        }
    }

    public void FixedUpdate()
    {
        m_vVelocity = (m_PhysTransform.position - m_vPositionLastFrame) / Time.fixedDeltaTime;
        m_vPositionLastFrame = m_PhysTransform.position;
    }

	private void OnDestroy()
	{
        m_Manager.RemoveFromPauseUnpause(this);
	}

	public Vector3 GetGroundedPos => m_LastGroundedPosition;
    public Vector3 GetGroundedNorm => m_LastGroundedNormal;
    public bool IsGrounded => m_bIsGrounded;
    public Vector3 GetPositionLastFrame => m_vPositionLastFrame;
    public Vector3 GetVelocity => m_vVelocity;
    public float GetMass => m_Body.mass;
    private Vector3 m_vPositionLastFrame;
    private Vector3 m_vVelocity;
}
