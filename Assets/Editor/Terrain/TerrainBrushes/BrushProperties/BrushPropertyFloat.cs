using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Systems/ToolSystem/TerrainPainterTool/BrushPropertyFloat")]
public class BrushPropertyFloat : BrushProperty
{
    public override Type PropertyType => typeof(float);

    public override object DefaultValue => m_DefaultValue;
    [SerializeField]
    private float m_DefaultValue = 0.0f;
}
