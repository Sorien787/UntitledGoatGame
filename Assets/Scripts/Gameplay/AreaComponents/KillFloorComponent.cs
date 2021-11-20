using UnityEngine;
public class KillFloorComponent : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        HealthComponent health = other.GetComponentInParent<HealthComponent>();
        if (health) 
        {
            health.OnTakeLethalDamage(DamageType.FallDamage);
        }
    }
}
