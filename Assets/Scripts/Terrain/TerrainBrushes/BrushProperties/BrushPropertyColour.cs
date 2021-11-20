using UnityEngine;
using System;
[CreateAssetMenu(menuName = "Systems/ToolSystem/TerrainPainterTool/BrushPropertyColour")]
public class BrushPropertyColour : BrushProperty
{
    public override Type PropertyType => typeof(Color);

    public override object DefaultValue => m_DefaultValue;
    [SerializeField]
    private Color m_DefaultValue = Color.white;
}
