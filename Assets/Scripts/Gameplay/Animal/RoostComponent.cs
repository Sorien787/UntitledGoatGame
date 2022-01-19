using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoostComponent : MonoBehaviour
{
    [SerializeField] private Transform m_Transform;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Vector3 GetRoostingLocation => m_Transform.position;
}
