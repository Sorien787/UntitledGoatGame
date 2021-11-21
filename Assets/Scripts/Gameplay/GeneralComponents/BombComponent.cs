using UnityEngine;
using System.Collections.Generic;
[RequireComponent(typeof(ThrowableObjectComponent))]
[RequireComponent(typeof(FreeFallTrajectoryComponent))]
public class BombComponent : MonoBehaviour
{
    [SerializeField] private GameObject m_HazardRef;
    [SerializeField] private GameObject m_ExplosionRef;
    [SerializeField] private LayerMask m_ExplosionLayer;
    [SerializeField] private CowGameManager m_Manager;
    [SerializeField] private float m_MaxExplosionRadius;
    [SerializeField] private float m_MaxExplosionPower;
    [SerializeField] private AnimationCurve m_BombPowerScalarByDistance;
    [SerializeField] private ParticleEffectsController m_ParticleFXController;

    private FreeFallTrajectoryComponent m_FreeFallComponent;
    private bool m_bBombPrimed = false;
    private ThrowableObjectComponent m_ThrowableObject;
    private Transform m_Transform;
    // Start is called before the first frame update
    void Awake()
    {
        m_Transform = transform;
        m_FreeFallComponent = GetComponent<FreeFallTrajectoryComponent>();
        m_ThrowableObject = GetComponent<ThrowableObjectComponent>();
        m_FreeFallComponent.OnObjectHitGround += OnHitGround;
        m_ThrowableObject.OnThrown += OnThrown;
    }

	private void OnDrawGizmosSelected()
	{
        Gizmos.DrawWireSphere(transform.position, m_MaxExplosionRadius);
	}

	void OnThrown(ProjectileParams projectileParams) 
    {
        m_bBombPrimed = true;
        m_ParticleFXController.TurnOnAllSystems();
    }

	void OnHitGround(Collision _)
    {
        if (!m_bBombPrimed)
            return;

        m_bBombPrimed = false;
        m_ParticleFXController.TurnOffAllSystems();

        List<AnimalComponent> animals = new List<AnimalComponent>();

        m_Manager.ForEachAnimal((EntityToken token) => 
        {
            if (((token.GetEntityTransform.position - m_Transform.position).sqrMagnitude < m_MaxExplosionRadius * m_MaxExplosionRadius) && token.GetEntityTransform.TryGetComponent(out AnimalComponent animal)) 
            {
                animals.Add(animal);
            }
        });

        for (int i = 0; i < animals.Count; i++)
        {
            AnimalComponent animal = animals[i];
            Vector3 position = animal.GetBodyTransform.position;
            Vector3 offsetFromBlastCentre = position - m_Transform.position;
            float normalizedDistance = offsetFromBlastCentre.magnitude / m_MaxExplosionRadius;
            float bombPower = m_BombPowerScalarByDistance.Evaluate(normalizedDistance) * m_MaxExplosionPower;
            Vector3 explosionForce = offsetFromBlastCentre.normalized * bombPower;
            animal.OnReceiveImpulse(explosionForce);
        }
        EZCameraShake.CameraShaker.Instance.Shake(EZCameraShake.CameraShakePresets.Explosion);
        Instantiate(m_HazardRef, m_Transform.position, m_Transform.rotation);
        Instantiate(m_ExplosionRef, m_Transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
