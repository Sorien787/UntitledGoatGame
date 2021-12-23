using System.Collections;
using UnityEngine;

public class HazardComponent : MonoBehaviour
{
    [SerializeField] private EntityTypeComponent m_EntityTypeComponent;
    [SerializeField] private CowGameManager m_GameManager;
    [SerializeField] private float m_HazardLifetime = 0.0f;
    [SerializeField] private float m_HazardRadius = 0.0f;

    private EntityInformation m_HazardSubtype;
    public EntityTypeComponent GetTypeComponent => m_EntityTypeComponent;
    public float GetHazardRadius => m_HazardRadius;
	private void Awake()
	{
        m_HazardSubtype = m_EntityTypeComponent.GetEntityInformation;
    }

	void Start()
    {
        StartCoroutine(StartDestroyTimer());
        m_GameManager.OnHazardSpawn(this);
    }

    public void SetHazardSubtype(EntityInformation hazardSubtype) 
    {
        m_HazardSubtype = hazardSubtype;
    }

    public Vector3 GetPosition 
    {
        get => m_EntityTypeComponent.GetTrackingTransform.position;
    }
    public bool Affects(EntityInformation entityInformation) 
    {
        foreach(EntityInformation hunted in entityInformation.GetHunts) 
        {
            if (hunted == m_HazardSubtype)
                return false;
        }
        return true;
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
