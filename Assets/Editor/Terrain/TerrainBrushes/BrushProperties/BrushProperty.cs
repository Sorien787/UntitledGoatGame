using UnityEngine;
using System;
using UnityEditor;

public abstract class BrushProperty : ScriptableObject
{
    [SerializeField]
    private string m_PropertyIdentifier = "default";

    public abstract Type PropertyType { get; }

    public abstract object DefaultValue { get; }

    public string GetIdentifier => m_PropertyIdentifier;
}
