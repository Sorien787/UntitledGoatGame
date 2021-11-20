using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ISelectToolBase : ScriptableObject, ITool
{
	[SerializeField] private float activateDraggingTime = 1.0f;

	private GameObject m_CachedHoveredGameObject;
	protected RaycastHit m_CachedHit;
	private float m_fTimeDragging = 0.0f;
	private bool m_bIsDragging = false;

	protected abstract void OnTick();
	protected abstract void OnObjectHovered(GameObject gameObject);
	protected abstract void OnObjectNotHovered(GameObject gameObject);
	protected abstract void OnRightClick();
	protected abstract void OnLeftClick();
	protected abstract void OnLeftClickDraggingStarted();
	protected abstract void OnLeftCLickDragging();
	protected abstract void OnLeftClickDraggingEnd();

	public void OnToolActivated()
	{

	}

	public void OnToolDeactivated()
	{
		m_CachedHoveredGameObject = null;
		m_fTimeDragging = 0.0f;
		m_bIsDragging = false;
	}

	public void OnUpdate()
	{
		bool ya = Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out m_CachedHit, maxDistance: Mathf.Infinity);
		if (!m_bIsDragging)
		{
			GameObject hoveredObject = ya ? m_CachedHit.transform.gameObject : null;
			if (m_CachedHoveredGameObject != hoveredObject)
			{
				if (m_CachedHoveredGameObject)
				{
					OnObjectNotHovered(m_CachedHoveredGameObject);
				}

				m_CachedHoveredGameObject = hoveredObject;
				if (m_CachedHoveredGameObject)
				{
					OnObjectHovered(m_CachedHoveredGameObject);
				}
			}
		}

		OnTick();

		if (Input.GetMouseButton(0))
		{
			m_fTimeDragging += Time.deltaTime;
			if (!m_bIsDragging && m_fTimeDragging > activateDraggingTime)
			{
				m_bIsDragging = true;
				m_fTimeDragging = 0;
				OnLeftClickDraggingStarted();
			}
		}



		if (m_bIsDragging)
		{
			OnLeftCLickDragging();
		}
		if (Input.GetMouseButtonUp(0))
		{
			m_fTimeDragging = 0;
			if (m_bIsDragging)
			{
				OnLeftClickDraggingEnd();
				m_bIsDragging = false;
			}
			else
			{
				OnLeftClick();
			}
		}
		if (Input.GetMouseButtonUp(1))
		{
			OnRightClick();
		}
	}
}

