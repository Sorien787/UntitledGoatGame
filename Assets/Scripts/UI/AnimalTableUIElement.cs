using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnimalTableUIElement : MonoBehaviour
{
    [SerializeField] private Image m_LeftImage;
    [SerializeField] private Transform m_RightContainer;
    public void SetUpForCreature(EntityInformation creature) 
	{
        m_LeftImage.sprite = creature.GetAssociatedSprite;

        for (int i = 0; i < creature.GetHunts.Length; i++) 
        {
            EntityInformation huntedCreature = creature.GetHunts[i];
            Instantiate(m_LeftImage, m_RightContainer).GetComponent<Image>().sprite = huntedCreature.GetAssociatedSprite;
        }
	}
}
