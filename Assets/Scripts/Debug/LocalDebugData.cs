using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalDebugData : MonoBehaviour
{
    [SerializeField] private Vector2 m_InitOffset;
    [SerializeField] private float m_TextSpacing;
    private Transform m_Transform;
    private float m_numCurrentLines = 0;

    public void AddDebugLine(string text, Color? color = null) 
    {
        if (color == null) color = Color.white;
        CustomDebug.DrawString(text, m_Transform.position, m_InitOffset.x, m_InitOffset.y + m_TextSpacing * m_numCurrentLines, color);
        m_numCurrentLines++;
    }

	private void Awake()
	{
        m_Transform = transform;
	}

	void LateUpdate()
    {
        m_numCurrentLines = 0;
    }
}
