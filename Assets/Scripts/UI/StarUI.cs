using System.Collections.Generic;
using UnityEngine;

public class StarUI : MonoBehaviour
{
	[SerializeField] private List<CanvasGroup> m_StarCanvasGroup;

	[SerializeField] private float m_fLowOpacity;
	[SerializeField] private float m_fHighOpacity;

	public void SetStarsVisible(in int numStarsVisible)
	{
		for (int i = 0; i < m_StarCanvasGroup.Count; i++)
		{
			m_StarCanvasGroup[i].alpha = numStarsVisible > i ? m_fHighOpacity : m_fLowOpacity;
		}
	}
}
