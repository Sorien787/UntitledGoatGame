using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotator : MonoBehaviour
{
    [SerializeField]
    private float m_MaxRotationRPM = 1.0f;
    private Transform m_Transform;
    // Start is called before the first frame update
    void Start()
    {
        m_Transform = transform;
    }

    // Update is called once per frame
    void Update()
    {
        m_Transform.Rotate(m_Transform.up, Time.deltaTime * 360 * m_MaxRotationRPM);
    }

    public void SetActive(in bool state) 
    {
        enabled = state;
    }
}
