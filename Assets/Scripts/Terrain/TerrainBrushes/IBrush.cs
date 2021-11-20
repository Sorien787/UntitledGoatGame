
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
public enum TerrainBrushDataParams
{
    brushSize,
    brushStrength,
    brushHardness,
    negativeModifier,
    brushColour
}


public interface IBrushStart 
{
    void OnBrushStarted(RaycastHit hit);
}

public interface IBrushFinish 
{
    void OnBrushFinished();
}
public class GenericDictionary
{
    private Dictionary<string, object> _dict = new Dictionary<string, object>();

    public void Add<T>(string key, T value)
    {
        _dict.Add(key, value);
    }

    public void Set<T>(string key, T value)
    {
        if (!_dict.ContainsKey(key)) 
        {
            _dict.Add(key, value);
        }
        else 
        {
            _dict[key] = value;
        }
    }

    public bool TryGetValue<T>(string key, out T value)
    {
        value = default(T);
        if (_dict.ContainsKey(key)) 
        {
            value = (T)_dict[key];
            return true;
        }
        return false;
    }

    public bool ContainsKey(string key) 
    {
        return _dict.ContainsKey(key);
    }

    public T GetValue<T>(string key)
    {
        Type type = typeof(T);
        object returnVal = _dict[key];
        return (T)_dict[key];
    }
}

[Serializable]
public abstract class IBrush : ScriptableObject
{
    // serializeable list of brushproperties that this brush will extend, can be set in inspector
    [SerializeField]
    protected List<BrushProperty> m_BrushProperties = null;

    protected GenericDictionary m_BrushData = new GenericDictionary();

    // non-serializeable dictionary to store player-defined values of brush properties

    ////////////////////////////////////////////////////////////////////////
    /// public methods for getting and setting brush information

    // we're guaranteed to only be setting identifier which this brush has, since we've defined which brush properties are visible when this brush is loaded.
    public virtual void SetData<T>(in string identifier, in T value) 
    {
        m_BrushData.Set(identifier, value);
    }

    // we're guaranteed to only be getting data with an identifier that exists, as we check for existence when brush loads - so no null check necessary
    public virtual T GetData<T>(in string identifier) 
    {
        return m_BrushData.GetValue<T>(identifier);
    }

    // used to get the properties that this brush extends, so that they can be set as interactable/uninteractable for the player
    public List<string> GetExtendedProperties()
    {
        List<string> brushPropertiesIdentifiers = new List<string>();
        for (int i = 0; i < m_BrushProperties.Count; i++) 
        {
            brushPropertiesIdentifiers.Add(m_BrushProperties[i].GetIdentifier);
        }
        return brushPropertiesIdentifiers;
    }

    public bool ExtendsProperty(in string property)
    {
        List<string> properties = GetExtendedProperties();
        for (int i = 0; i < properties.Count; i++) 
        {
            if (properties[i] == property)
                return true;
        }
        return false;
    }

    // called at game startup, transfer data from the cached (ScriptableObject, effectively static readonly) properties into a malleable dataset
    public virtual void InitializeBrush()
    {
        for (int i = 0; i < m_BrushProperties.Count; i++)
        {
            if (!m_BrushData.ContainsKey(m_BrushProperties[i].GetIdentifier)) 
            {
                m_BrushData.Add(m_BrushProperties[i].GetIdentifier, m_BrushProperties[i].DefaultValue);
            }
        }
    }

    protected bool TryRaycastHit(out RaycastHit hit, in LayerMask layerMask) 
    {
        Vector3 mousePos = Event.current.mousePosition;
        mousePos.y = SceneView.lastActiveSceneView.camera.pixelHeight - mousePos.y;
        Ray ray = SceneView.lastActiveSceneView.camera.ScreenPointToRay(mousePos);

        bool hashit = Physics.Raycast(ray, out hit, Mathf.Infinity);
        return hashit;
    }

    // needed for setting shader values for brushes that work off of shaders
    public abstract void OnApplyBrush();

    public virtual void OnStartApplyingBrush() { }

    public virtual void OnChooseBrush() { InitializeBrush(); }

    public virtual void OnLeaveBrush() { }
}
