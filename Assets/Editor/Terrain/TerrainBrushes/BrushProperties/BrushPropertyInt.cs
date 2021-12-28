using UnityEngine;
using System;
[CreateAssetMenu(menuName = "Systems/ToolSystem/TerrainPainterTool/BrushPropertyInt")]
public class BrushPropertyInt : BrushProperty
{
    public override Type PropertyType => typeof(int);

    public override object DefaultValue => m_DefaultValue;
    [SerializeField]
    private int m_DefaultValue = 0;
}