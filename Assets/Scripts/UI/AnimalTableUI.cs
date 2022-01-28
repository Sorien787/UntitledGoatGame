using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimalTableUI : MonoBehaviour
{
    [SerializeField] private CowGameManager m_Manager;
    [SerializeField] private GameObject m_AnimalTableUIElementPrefab;
    [SerializeField] private Transform m_TableTransform;

    void Start() 
    {
        HashSet<EntityInformation> entitiesPresent = m_Manager.GetEntitiesPresent();
        foreach(EntityInformation information in entitiesPresent) 
        {
            if (!information.CanDisplayInTable())
                continue;
            Instantiate(m_AnimalTableUIElementPrefab, m_TableTransform).GetComponent<AnimalTableUIElement>().SetUpForCreature(information, entitiesPresent);
        }
    }

}
