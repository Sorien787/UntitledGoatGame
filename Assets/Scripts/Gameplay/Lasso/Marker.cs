using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class Marker : MonoBehaviour
{
    private MeshRenderer[] m_MarkerRenderers = default;
    private Transform m_Transform = default;

    [SerializeField] private bool m_bOnByDefault = false;
    private void Awake()
	{
        m_MarkerRenderers = GetComponentsInChildren<MeshRenderer>();
        m_Transform = transform;
        if (m_bOnByDefault)
            Enable();
        else
            Disable();
	}

    public Transform GetTransform => m_Transform;

    public void Enable() 
    {
        for (int i = 0; i < m_MarkerRenderers.Length; i++) 
        {
            m_MarkerRenderers[i].enabled = true;
        }
    }

    public void Disable() 
    {
        for (int i = 0; i < m_MarkerRenderers.Length; i++)
        {
            m_MarkerRenderers[i].enabled = false;
        }

    }

    public void SetPosition(Vector3 position) 
    {
        m_Transform.position = position;
    }

    public void SetRotation(Vector3 rotation) 
    {
        SetRotation(Quaternion.LookRotation(Vector3.forward, rotation));
    }

    public void SetRotation(Quaternion quat) 
    {
        m_Transform.rotation = quat;
    }
}
