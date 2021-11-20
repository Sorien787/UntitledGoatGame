using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillFloorDelayedDestroy : MonoBehaviour
{
    IEnumerator DelayedKill() 
    {
        yield return new WaitForSeconds(2.0f);
        Destroy(gameObject);
    }

    public void OnHitKillFloor() 
    {
        StartCoroutine(DelayedKill());
    } 
}
