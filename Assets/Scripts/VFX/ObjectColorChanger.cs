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
    [HideInInspector] public bool RandomizeOnStart { get => m_bRandomizeOnStart; set { m_bRandomizeOnStart = value;} }


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
            if (setting.m_changeChildObjects) 
            {
                Transform thisTransform = transform;
                int childCount = transform.childCount;
                for(int i = 0; i < childCount; i++) 
                {
                    Transform childTransform = thisTransform.GetChild(i);
                    MeshRenderer meshRenderer = childTransform.GetComponent<MeshRenderer>();
                    if (!meshRenderer && ! childTransform.GetComponent<ObjectColorChanger>())
                        continue;
                    ChangeMaterialForRenderer(matPropertyBlock, setting, meshRenderer);
                }
            }
			else 
            {
                ChangeMaterialForRenderer(matPropertyBlock, setting, m_MeshRenderer);
            }

        }

    }

    private void ChangeMaterialForRenderer(in MaterialPropertyBlock matPropertyBlock, in ObjectColorChangeMaterialSetting setting, in MeshRenderer meshRenderer) 
    {
        meshRenderer.GetPropertyBlock(matPropertyBlock, setting.m_MaterialIndex);
        matPropertyBlock.SetColor(setting.m_MaterialColourId, setting.m_RolledColor);
        meshRenderer.SetPropertyBlock(matPropertyBlock, setting.m_MaterialIndex);
    }
}

[System.Serializable]
public class ObjectColorChangeMaterialSetting 
{
    public Gradient m_ColourGradient;
    public Color m_RolledColor;
    public int m_MaterialIndex;
    public string m_MaterialColourId;
    public bool m_changeChildObjects;
    public void RollColour() 
    {
        float rand = Random.Range(0.0f, 1.0f);
        m_RolledColor = m_ColourGradient.Evaluate(rand);
    }
}
