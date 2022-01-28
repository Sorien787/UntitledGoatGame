using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnimalTableUIElement : MonoBehaviour
{
    [SerializeField] private Image m_LeftImage;
    [SerializeField] private Transform m_RightContainer;
    public void SetUpForCreature(EntityInformation creature, in HashSet<EntityInformation> entitiesPresentInLevel) 
	{
        m_LeftImage.sprite = creature.GetAssociatedSprite;

        for (int i = 0; i < creature.GetHunts.Length; i++) 
        {
            EntityInformation huntedCreature = creature.GetHunts[i];
            if (!entitiesPresentInLevel.Contains(huntedCreature))
                continue;

            if (!huntedCreature.HasSprite())
                    continue;
            GameObject instantiatedImage = Instantiate(m_LeftImage.gameObject, m_RightContainer);
            instantiatedImage.GetComponent<Image>().sprite = huntedCreature.GetAssociatedSprite;
            Vector2 leftImageTransformSize = m_LeftImage.rectTransform.sizeDelta;
            leftImageTransformSize.y = leftImageTransformSize.x;
            instantiatedImage.GetComponent<RectTransform>().sizeDelta = leftImageTransformSize;
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(m_RightContainer.GetComponent<RectTransform>());
        StartCoroutine(FixLayoutGroup());
	}

    public IEnumerator FixLayoutGroup() 
    {
        m_RightContainer.gameObject.SetActive(false);
        yield return null;
        m_RightContainer.gameObject.SetActive(true);
    }
}
