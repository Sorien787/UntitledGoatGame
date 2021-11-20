using System.Collections.Generic;
using UnityEngine;

public class PenComponent : MonoBehaviour
{
    [SerializeField]
    private CowGameManager m_Manager;

    [SerializeField]
    private List<EntityInformation> m_PennableAnimalTags;

    [SerializeField]
    private PenBeaconComponent m_PenBeaconComponent;

    private readonly HashSet<GameObject> m_ContainedObjects = new HashSet<GameObject>();

    private void OnTriggerEnter(Collider objCollided)
    {
        if (IsObjectPennable(objCollided.gameObject) && !ContainsAnimal(objCollided.gameObject))
        {
            m_ContainedObjects.Add(objCollided.gameObject);
            m_PenBeaconComponent.OnObjectEnterPen();
            m_Manager.OnEntityEnterPen(objCollided.gameObject);
        }
    }

    private void OnTriggerExit(Collider objCollided)
    {
        if (IsObjectPennable(objCollided.gameObject) && ContainsAnimal(objCollided.gameObject)) 
        {
            m_PenBeaconComponent.OnObjectLeavePen();
            m_Manager.OnEntityLeavePen(objCollided.gameObject);
        }
    }

    private bool IsObjectPennable(in GameObject targetGameObject) 
    {
        if (targetGameObject.TryGetComponent(out EntityTypeComponent gameTagComponent))
        {
            for (int i = 0; i < m_PennableAnimalTags.Count; i++) 
            {
                if (gameTagComponent.GetEntityInformation == m_PennableAnimalTags[i]) 
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool ContainsAnimal(GameObject go) 
    {
        return m_ContainedObjects.Contains(go);
    }
}
