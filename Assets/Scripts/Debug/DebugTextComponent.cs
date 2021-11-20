using UnityEngine;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(TextMesh))]
public class DebugTextComponent : MonoBehaviour
{
	private TextMesh m_TextMesh;
	private Transform m_CameraTransform;
	private Transform m_Transform;
	private int m_CurrentFrameCount = -1;
	// Start is called before the first frame update
	private string m_TotalText = "";

	private void Awake()
	{
		m_CameraTransform = Camera.main.transform;
		m_TextMesh = GetComponent<TextMesh>();
		m_Transform = transform;
	}

	public void AddLine(string line)
	{
		if (Time.renderedFrameCount != m_CurrentFrameCount) 
		{
			m_CurrentFrameCount = Time.renderedFrameCount;
			m_TotalText = "";
		}
		m_TotalText += line + "\n";
	}
	private void Update()
	{
		m_Transform.rotation =  m_CameraTransform.rotation;
		m_TextMesh.text = m_TotalText;
	}
}
