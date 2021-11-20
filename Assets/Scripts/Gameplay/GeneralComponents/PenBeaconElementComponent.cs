using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRendererColorChanger))]
public class PenBeaconElementComponent : MonoBehaviour
{
    [SerializeField] private CowGameManager m_GameManager;
    [SerializeField] private AnimationCurve m_OpacityDistanceAnimationCurve;
    [SerializeField] private AnimationCurve m_OpacityAngleMultiplierAnimationCurve;
    private Transform m_BeaconTransform;

    // Start is called before the first frame update
    void Awake()
    {
        m_BeaconTransform = transform;
    }


	public float GetPlayerOpacity(in Transform playerTransform) 
    {
        Vector3 offset = Vector3.ProjectOnPlane(m_BeaconTransform.position - playerTransform.position, Vector3.up);
        Vector3 forwardLookDir = Vector3.ProjectOnPlane(playerTransform.forward, Vector3.up).normalized;
        float dottedDir = Vector3.Dot(forwardLookDir, offset.normalized);

        float distance = offset.magnitude;
        float positiveDotted = 1 - Mathf.Max(0, dottedDir);

        return m_OpacityDistanceAnimationCurve.Evaluate(distance) * m_OpacityAngleMultiplierAnimationCurve.Evaluate(positiveDotted);
    }
}

