using UnityEngine;

[CreateAssetMenu(menuName = "Systems/BrushPropertyAssociator")]
public class BrushPropertyAssociator : ScriptableObject
{
    [SerializeField] private BrushProperty m_SizeBrushProperty;
    public string GetSizeProperty => m_SizeBrushProperty.GetIdentifier;

    [SerializeField] private BrushProperty m_StrengthBrushProperty;
    public string GetStrengthProperty => m_StrengthBrushProperty.GetIdentifier;

    [SerializeField] private BrushProperty m_HardnessBrushProperty;
    public string GetHardnessProperty => m_HardnessBrushProperty.GetIdentifier;

    [SerializeField] private BrushProperty m_ColourBrushProperty;
    public string GetColourProperty => m_ColourBrushProperty.GetIdentifier;

    [SerializeField] private BrushProperty m_CacheNormalsBrushProperty;
    public string GetCacheNormalsProperty => m_CacheNormalsBrushProperty.GetIdentifier;

    [SerializeField] private BrushProperty m_UseVerticalProperty;
    public string GetUseVerticalProperty => m_UseVerticalProperty.GetIdentifier;

}
