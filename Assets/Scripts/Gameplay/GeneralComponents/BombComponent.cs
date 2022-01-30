using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(ThrowableObjectComponent))]
[RequireComponent(typeof(FreeFallTrajectoryComponent))]
public class BombComponent : MonoBehaviour, IFreeFallListener
{
    [Header("External References")]
    [SerializeField] private GameObject m_HazardRef;
    [SerializeField] private GameObject m_ExplosionRef;
    [SerializeField] private CowGameManager m_Manager;

    [Header("Settings")]
    [SerializeField] private float m_MaxExplosionRadius;
    [SerializeField] private float m_MaxStunRadius;
	[SerializeField] private float m_MaxExplosionPower;
    [SerializeField] private AnimationCurve m_BombStunTimeByRadius;
    [SerializeField] private AnimationCurve m_BombPowerScalarByDistance;

    [Header("Internal References")]
    [SerializeField] private ParticleEffectsController m_ParticleFXController;
    [SerializeField] private AudioManager m_AudioManager;
    [SerializeField] private SoundObject m_Fizzle;
    [SerializeField] private SoundObject m_Splosion;
    [SerializeField] private Collider m_Collider;
    [SerializeField] private Rigidbody m_Body;
    [SerializeField] private MeshRenderer m_Renderer;

    private FreeFallTrajectoryComponent m_FreeFallComponent;
    private bool m_bBombPrimed = false;
    private ThrowableObjectComponent m_ThrowableObject;
    private Transform m_Transform;
	// Start is called before the first frame update

	private void OnValidate()
	{
        m_MaxStunRadius = Mathf.Max(m_MaxStunRadius, m_MaxExplosionRadius);
	}

	void Awake()
    {
        m_Transform = transform;
        m_FreeFallComponent = GetComponent<FreeFallTrajectoryComponent>();
        m_ThrowableObject = GetComponent<ThrowableObjectComponent>();
		m_FreeFallComponent.AddListener(this);
        m_ThrowableObject.OnThrown += OnThrown;
    }

	private void OnDrawGizmosSelected()
	{
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, m_MaxExplosionRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, m_MaxStunRadius);
	}

	void OnThrown(ProjectileParams projectileParams) 
    {
        m_bBombPrimed = true;
        m_AudioManager.Play(m_Fizzle);
        m_ParticleFXController.TurnOnAllSystems();
    }

	public void OnCollide(Vector3 pos, Vector3 norm, GameObject go)
    {
        if (!m_bBombPrimed)
            return;

        m_bBombPrimed = false;
        m_ParticleFXController.TurnOffAllSystems();

        List<AnimalComponent> animals = new List<AnimalComponent>();

        m_Manager.ForEachAnimal((EntityToken token) => 
        {
            if (((token.GetEntityTransform.position - m_Transform.position).sqrMagnitude < m_MaxStunRadius * m_MaxStunRadius) && token.GetEntityTransform.TryGetComponent(out AnimalComponent animal)) 
            {
                animals.Add(animal);
            }
        });

        for (int i = 0; i < animals.Count; i++)
        {         
            AnimalComponent animal = animals[i];
            Vector3 position = animal.GetBodyTransform.position;
            Vector3 offsetFromBlastCentre = position - m_Transform.position;
            float offsetFromCentreLength = offsetFromBlastCentre.magnitude;
            if ((animal.GetBodyTransform.position - m_Transform.position).sqrMagnitude < m_MaxExplosionRadius * m_MaxExplosionRadius) 
            {
                float normalizedDistance = offsetFromCentreLength / m_MaxExplosionRadius;
                float bombPower = m_BombPowerScalarByDistance.Evaluate(normalizedDistance) * m_MaxExplosionPower;
                Vector3 explosionForce = offsetFromBlastCentre.normalized * bombPower;
                animal.OnReceiveImpulse(explosionForce);
            }
            animal.AddStaggerTime(m_BombStunTimeByRadius.Evaluate(offsetFromCentreLength / m_MaxStunRadius));
        }
        EZCameraShake.CameraShaker.Instance.Shake(EZCameraShake.CameraShakePresets.Explosion);
        Instantiate(m_HazardRef, m_Transform.position, m_Transform.rotation);
        m_AudioManager.PlayOneShot(m_Splosion);
        m_AudioManager.StopPlaying(m_Fizzle);
        Vector3 forward = Vector3.forward;
        if (Vector3.Dot(norm, Vector3.up ) != 1.0f) 
        {
            forward = Vector3.Cross(norm, Vector3.up);
        }
        Quaternion upRot = Quaternion.LookRotation(forward, norm);
        Instantiate(m_ExplosionRef, m_Transform.position, upRot);
        StartCoroutine(DelayedDestroy());
    }

    private IEnumerator DelayedDestroy() 
    {
        m_Renderer.enabled = false;
        m_Collider.enabled = false;
        m_Body.isKinematic = true;
        yield return new WaitForSecondsRealtime(3.0f);
        Destroy(gameObject);
    }

	public void OnStopFalling() {}
}
