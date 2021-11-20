using UnityEngine;
using System.Collections.Generic;

public class ParticleEffectAutoDestroy : MonoBehaviour
{
    private ParticleSystem[] ps;

    public void Start()
    {
        ps = GetComponentsInChildren<ParticleSystem>();
    }

    public void Update()
    {
        for (int i = 0; i < ps.Length; i++) 
        {
            if (ps[i].IsAlive())
                return;
        }
        Destroy(gameObject);
    }
}
