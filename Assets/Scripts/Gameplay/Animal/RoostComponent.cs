using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoostComponent : MonoBehaviour
{
	private EntityTypeComponent m_EntityType;
	private Transform m_Transform;

	private void Awake()
	{
		m_Transform = transform;
		m_EntityType = GetComponent<EntityTypeComponent>();
	}

	private void Start()
	{
		m_EntityType.AddToTrackable();
	}
	public Vector3 GetRoostingLocation => m_Transform.position;
}
