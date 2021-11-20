using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[ExecuteAlways]
public class MaterialInstancerComponent : MonoBehaviour
{
    [SerializeField]
    private Gradient m_Colors;
    private Renderer m_Renderer;
    private MaterialPropertyBlock m_PropertyBlock;
    void Awake()
    {
        m_PropertyBlock = new MaterialPropertyBlock();
        m_Renderer = GetComponent<Renderer>();
        UpdateSettings();
    }

    // Update is called once per frame
    public void UpdateSettings()
    {
        m_Renderer.GetPropertyBlock(m_PropertyBlock);
        m_PropertyBlock.SetColor("_Color", m_Colors.Evaluate(Random.Range(0.0f, 1.0f)));
    }
}
