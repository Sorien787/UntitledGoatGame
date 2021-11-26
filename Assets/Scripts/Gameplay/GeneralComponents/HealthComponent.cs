using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(EntityTypeComponent))]
public class HealthComponent : MonoBehaviour
{
    [SerializeField] private float m_MaxHealth = 3;

    [SerializeField] private CowGameManager m_Manager;

    [SerializeField] private float m_InvulnerabilityTime = 1.0f;

    [SerializeField] private bool m_bCanDie = true;

    [SerializeField] private float m_CurrentHealth = 0;

    [SerializeField] private GameObject m_DamagedParticleType = default;

    private bool m_bIsKilled = false;

    private bool m_IsInvulnerable = false;

    private UnityUtils.ListenerSet<IHealthListener> m_HealthListeners = new UnityUtils.ListenerSet<IHealthListener>();

    public void AddListener(IHealthListener listener) 
    {
        m_HealthListeners.Add(listener);
    }

    public void RemoveListener(IHealthListener listener) 
    {
        m_HealthListeners.Remove(listener);
    }


    private void Awake()
    {
        m_CurrentHealth = m_MaxHealth;
    }

    public GameObject GetDamagedParticleType(DamageType type) => m_DamagedParticleType;

    public void Revive(in float health) 
    {
        m_CurrentHealth = health;

        m_bIsKilled = false;
    }

    public void ReplenishHealth(in float healthAmount) 
    {
        if (GetCurrentHealthPercentage == 0) 
        {
            Revive(healthAmount);
        }
		else
        {
            m_CurrentHealth = Mathf.Min(m_CurrentHealth + healthAmount, m_MaxHealth);
        }
    }

    public void Revive() 
    {
        Revive(m_MaxHealth);
    }

    private IEnumerator SetInvulnerability() 
    {
        m_IsInvulnerable = true;
        yield return new WaitForSecondsRealtime(m_InvulnerabilityTime);
        m_IsInvulnerable = false;
    }

    protected virtual void OnKilled(GameObject killed, GameObject damagedBy, DamageType damageType) 
    {
        if (!m_bIsKilled) 
        {
            m_CurrentHealth = 0;
            m_bIsKilled = true;
            m_HealthListeners.ForEachListener((IHealthListener listener) => listener.OnEntityDied(gameObject, damagedBy, damageType));
        }
    }

    public float GetCurrentHealthPercentage => m_CurrentHealth / m_MaxHealth;

    public bool TakeDamageInstance(GameObject damagedBy, DamageType damageType, float damageAmount = 1f) 
    {
        if (!m_IsInvulnerable)
        {
            m_CurrentHealth -= damageAmount;
            if (m_CurrentHealth <= 0)
            {
                OnKilled(gameObject, damagedBy, damageType);
            }
            else
            {
                m_HealthListeners.ForEachListener((IHealthListener listener) => listener.OnEntityTakeDamage(gameObject, damagedBy, damageType));
                StartCoroutine(SetInvulnerability());
            }
            return true;
        }
        return false;
    }

    public void OnTakeLethalDamage(DamageType damageType) 
    {
        OnKilled(gameObject, null, damageType);
    }
}

public interface IHealthListener 
{
    void OnEntityTakeDamage(GameObject go1, GameObject go2, DamageType type);
    void OnEntityDied(GameObject go1, GameObject go2, DamageType type);
}

public enum DamageType 
{
    FallDamage,
    ImpactDamage,
    Undefined,
    UFODamage,
    PredatorDamage
}