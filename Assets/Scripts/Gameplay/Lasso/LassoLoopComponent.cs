using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class LassoLoopComponent : MonoBehaviour
{
    public event Action OnHitGround;

    public event Action<ThrowableObjectComponent> OnHitObject;

    [SerializeField]
    private Rigidbody m_RigidBody;

    private List<Collider> m_Colliders = new List<Collider>();

    bool m_bRegisteredCollisionThisFrame;

	private void Awake()
	{
        GetComponentsInChildren<Collider>(m_Colliders);
	}
	void OnCollisionEnter(Collision collision){
        if (!m_bRegisteredCollisionThisFrame) 
        {

            m_bRegisteredCollisionThisFrame = true;
            GameObject hitObject = collision.gameObject;
            m_RigidBody.velocity = Vector3.zero;
            if (hitObject.TryGetComponent(out ThrowableObjectComponent throwable)) 
            {
                OnHitObject(throwable);
            }
            else 
            {
                OnHitGround();
            }
        }
    }

	private void FixedUpdate()
	{
        m_bRegisteredCollisionThisFrame = false;
	}

    public void EnableColliders(bool activate) 
    { 
        foreach(Collider collider in m_Colliders) 
        {
            collider.enabled = activate;
        }
        m_RigidBody.isKinematic = !activate;
    }
}
