using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIObjectElement : MonoBehaviour
{
    [SerializeField] private CowGameManager m_CowGameManager;
	[SerializeField] private UIObjectReference m_Reference;
	private void Awake()
	{
		m_CowGameManager.OnUIElementSpawned(this, m_Reference);
	}
}
