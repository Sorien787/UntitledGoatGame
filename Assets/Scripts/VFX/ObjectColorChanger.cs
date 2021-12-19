using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ObjectColorChanger : MonoBehaviour
{
    [SerializeField] private string m_DefaultShaderId = "_Color";
    [SerializeField] [HideInInspector] public int m_MaterialColourSettingReference = 0;
    [SerializeField] [HideInInspector] private List<ObjectColorChangeMaterialSetting> m_ColourSettings = new List<ObjectColorChangeMaterialSetting>();
    [SerializeField] private bool m_bRandomizeOnStart = false;
    private MeshRenderer m_MeshRenderer = default;
    public ref List<ObjectColorChangeMaterialSetting> GetMaterialColourSettings() 
    {
        return ref m_ColourSettings;
    }

    public bool RandomizeOnStart { get => m_bRandomizeOnStart; set { m_bRandomizeOnStart = value;} }


    public string GetDefaultShaderId { get => m_DefaultShaderId; }

    private void Awake()
	{
        m_MeshRenderer = GetComponent<MeshRenderer>();

        if (m_bRandomizeOnStart)
        {
            foreach(ObjectColorChangeMaterialSetting setting in m_ColourSettings) 
            {
                setting.RollColour();
            }
        }
        SetColours();
    }

    public void SetColours() 
    {
        MaterialPropertyBlock matPropertyBlock = new MaterialPropertyBlock();
        foreach(ObjectColorChangeMaterialSetting setting in m_ColourSettings) 
        {
            m_MeshRenderer.GetPropertyBlock(matPropertyBlock, setting.m_MaterialIndex);
            matPropertyBlock.SetColor(setting.m_MaterialColourId, setting.m_RolledColor);
            m_MeshRenderer.SetPropertyBlock(matPropertyBlock, setting.m_MaterialIndex);
        }

    }
}

[System.Serializable]
public class ObjectColorChangeMaterialSetting 
{
    public Gradient m_ColourGradient;
    public Color m_RolledColor;
    public int m_MaterialIndex;
    public string m_MaterialColourId;
    public void RollColour() 
    {
        float rand = Random.Range(0.0f, 1.0f);
        m_RolledColor = m_ColourGradient.Evaluate(rand);
    }
}
