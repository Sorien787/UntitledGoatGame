using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimalTableUI : MonoBehaviour, IEntityListener
{
    [SerializeField] private CowGameManager m_Manager;

    private HashSet<EntityInformation> m_AddedEntities = new HashSet<EntityInformation>();

    [SerializeField] private GameObject m_AnimalTableUIElementPrefab;
    [SerializeField] private Transform m_TableTransform;

	public void OnEntityAdded(EntityToken token)
	{
        EntityInformation entityInformation = token.GetEntityType.GetEntityInformation;
        if (m_AddedEntities.Contains(entityInformation))
            return;
        m_AddedEntities.Add(entityInformation);

        if (!entityInformation.CanDisplayInTable())
            return;

        Instantiate(m_AnimalTableUIElementPrefab, m_TableTransform).GetComponent<AnimalTableUIElement>().SetUpForCreature(entityInformation);
	}

	public void OnEntityRemoved(EntityToken token) {}

	// Start is called before the first frame update
	void Awake()
    {
        m_Manager.AddEntityAddedListener(this);
    }
}
