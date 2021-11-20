using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(FoodSourceComponent))]
public class GrainComponent : MonoBehaviour, IFoodSourceSizeListener
{
	[SerializeField] private float m_FoodMaxScale = 0.1f;
	[SerializeField] private float m_FoodMinScale = 1.0f;

	private class GrainPile 
	{
		public Transform m_Transform;
		public Vector3 m_InitialScale;
	}

	private List<GrainPile> m_GrainPiles = new List<GrainPile>();

	private float m_CurrentFoodSize = 1.0f;
	public void Awake()
	{
		GetComponent<FoodSourceComponent>().AddListener(this);
		Transform localTransform = transform;
		for (int i = 0; i < localTransform.childCount; i++) 
		{
			Transform childTransform = localTransform.GetChild(i);
			if (!childTransform.GetComponent<MeshFilter>())
				continue;

			GrainPile newPile = new GrainPile
			{
				m_Transform = childTransform,
				m_InitialScale = childTransform.localScale
			};

			m_GrainPiles.Add(newPile);
		}
	}
	public void OnSetFoodSize(float foodSize)
	{
		m_CurrentFoodSize = foodSize;
	}

	private void Update()
	{
		for (int i = 0; i < m_GrainPiles.Count; i++) 
		{
			float lerp = Mathf.Lerp(m_FoodMinScale, m_FoodMaxScale, m_CurrentFoodSize);
			Vector3 setScale = m_GrainPiles[i].m_InitialScale * lerp;
			m_GrainPiles[i].m_Transform.localScale = setScale;
		}
	}
}
