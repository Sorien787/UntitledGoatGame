using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshRendererColorChanger : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private List<MeshRenderer> m_MeshRenderersToFade;
    [SerializeField] private Color m_BaseColor;
    [SerializeField] private float m_TimeTakenToReachOpacity;
    [SerializeField] private float m_TimeTakenToReachColour;

    private int m_ColourId;
    private int m_EmissionColourId;

    private MaterialPropertyBlock m_MatPropertyBlock;

    private float m_CurrentOpacity;
    private Vector3 m_CurrentColor;

    private float m_DesiredOpacity;
    private Vector3 m_DesiredColor;

    private float m_OpacityVelocity = 0.0f;
    private Vector3 m_ColorVelocity = Vector3.zero;

    void Start()
    {
        m_CurrentColor = new Vector3(m_BaseColor.r, m_BaseColor.g, m_BaseColor.b);
        m_DesiredColor = m_CurrentColor;

        m_DesiredOpacity = m_BaseColor.a;
        m_DesiredOpacity = m_CurrentOpacity;

        m_ColourId = Shader.PropertyToID("_Color");
        m_EmissionColourId = Shader.PropertyToID("_EmissionColor");

        m_MatPropertyBlock = new MaterialPropertyBlock();
    }

    // Update is called once per frame
    void Update()
    {
        m_CurrentOpacity = Mathf.SmoothDamp(m_CurrentOpacity, m_DesiredOpacity, ref m_OpacityVelocity, m_TimeTakenToReachOpacity);
        m_CurrentColor = Vector3.SmoothDamp(m_CurrentColor, m_DesiredColor, ref m_ColorVelocity, m_TimeTakenToReachColour);
        SetBeaconColour(new Color(1, 1, 1, m_CurrentOpacity), new Color(m_CurrentColor.x, m_CurrentColor.y, m_CurrentColor.z, 1));
    }

    public void SetDesiredOpacity(in float desiredOpacity) 
    {
        m_DesiredOpacity = desiredOpacity;
    }

    public void SetDesiredColour(in Vector3 color) 
    {
        m_DesiredColor = color;
    }

    private void SetBeaconColour(in Color colour, in Color emissiveColor) 
    {
        for (int i = 0; i < m_MeshRenderersToFade.Count; i++)
        {
            MeshRenderer currentMeshRenderer = m_MeshRenderersToFade[i];
            currentMeshRenderer.GetPropertyBlock(m_MatPropertyBlock);
            m_MatPropertyBlock.SetColor(m_ColourId, colour);
            m_MatPropertyBlock.SetColor(m_EmissionColourId, emissiveColor);
            currentMeshRenderer.SetPropertyBlock(m_MatPropertyBlock);
        }
    }
}
