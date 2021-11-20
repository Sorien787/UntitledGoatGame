using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicalEntity : MonoBehaviour
{
    private ContactPoint[] m_Contacts = new ContactPoint[8];
    private Vector3 m_LastGroundedPosition = Vector3.zero;
    private Vector3 m_LastGroundedNormal = Vector3.zero;
    private bool m_bIsGrounded = false;
    private Transform m_PhysTransform = null;
    private Collider[] m_colliderResults = new Collider[1];
    [SerializeField] private CowGameManager m_Manager = null;

	private void Awake()
	{
        m_PhysTransform = transform;
	}

	private void Update()
    {
        m_bIsGrounded = Physics.OverlapSphereNonAlloc(m_PhysTransform.position, 0.5f, m_colliderResults, m_Manager.GetGroundLayer()) > 0;
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

    public Vector3 GetGroundedPos => m_LastGroundedPosition;
    public Vector3 GetGroundedNorm => m_LastGroundedNormal;
    public bool IsGrounded => m_bIsGrounded;
    public Vector3 GetPositionLastFrame => m_vPositionLastFrame;
    public Vector3 GetVelocity => m_vVelocity;

    private Vector3 m_vPositionLastFrame;
    private Vector3 m_vVelocity;
}
