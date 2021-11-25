using System.Collections;
using UnityEngine;

public class HazardComponent : MonoBehaviour
{
    [SerializeField] private EntityTypeComponent m_EntityTypeComponent;
    [SerializeField] private float m_HazardLifetime = 0.0f;
    [SerializeField] private float m_HazardRadius = 0.0f;

    public float GetHazardRadius => m_HazardRadius;

    void Start()
    {
        StartCoroutine(StartDestroyTimer());
    }

    public void SetRadius(in float radius) 
    {
        m_HazardRadius = radius;
    }

    public void SetLifetime(in float lifetime) 
    {
        m_HazardLifetime = lifetime;
    }

	private void OnDrawGizmosSelected()
	{
        Gizmos.DrawWireSphere(transform.position, m_HazardRadius);
	}

	private IEnumerator StartDestroyTimer() 
    {
        yield return new WaitForSecondsRealtime(m_HazardLifetime);
        m_EntityTypeComponent.OnKilled();
        Destroy(gameObject);
    }
}
