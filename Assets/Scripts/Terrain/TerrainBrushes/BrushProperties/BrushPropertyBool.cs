using UnityEngine;
using System;
using UnityEditor;

[CreateAssetMenu(menuName = "Systems/ToolSystem/TerrainPainterTool/BrushPropertyBool")]
public class BrushPropertyBool : BrushProperty
{
    public override Type PropertyType => typeof(bool);

    public override object DefaultValue => m_DefaultValue;
    [SerializeField]
    private bool m_DefaultValue = false;
}