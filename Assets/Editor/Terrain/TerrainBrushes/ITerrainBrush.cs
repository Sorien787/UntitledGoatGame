using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// TODO: change the OnSetData to set via an intermediary generic class that sets the compute shader's properties.
/// </summary>

public abstract class ITerrainBrush : IBrush
{
    [SerializeField] protected ComputeShader m_BrushShader;
    [SerializeField] protected BrushProperty m_SizeBrushProperty;

    [SerializeField] [HideInInspector] protected TerrainGenerator m_TerrainGenerator;

    [SerializeField] [HideInInspector] protected ComputeBuffer m_BrushDataBuffer;

    public virtual bool AffectsGeometry { get; }

    public void SetTerrainGenerator(TerrainGenerator terrainGenerator) 
    {
        m_TerrainGenerator = terrainGenerator;
    }

    public override void OnChooseBrush() 
    {
        InitializeBrush();
        SetBuffer(m_TerrainGenerator.workingTerrain);
    }
    public override void OnApplyBrush()
    {
        if (TryRaycastHit(out RaycastHit hit, m_TerrainGenerator.GetTerrainLayer)) 
        {
            m_TerrainGenerator.ApplyBrushToTerrain(hit, this, m_BrushData.GetValue<IBrushShaderProperty<float>>(m_SizeBrushProperty.GetIdentifier).GetProperty());
        }
    }

    public override void OnLeaveBrush() 
    {
        ResetBuffer();
    }
    public override void SetData<T>(in string identifier, in T value)
    {
        IBrushShaderProperty<T> val = m_BrushData.GetValue<IBrushShaderProperty<T>>(identifier);
        val.SetProperty(identifier, value, m_BrushShader);
    }

    public override void InitializeBrush()
    {
        for (int i = 0; i < m_BrushProperties.Count; i++)
        {
            if (!m_BrushData.ContainsKey(m_BrushProperties[i].GetIdentifier))
            {
                Type type = m_BrushProperties[i].DefaultValue.GetType();
                if (type == typeof(int)) 
                {
                    m_BrushData.Add(m_BrushProperties[i].GetIdentifier, new IntShaderProperty(m_BrushProperties[i].GetIdentifier, m_BrushProperties[i].DefaultValue, m_BrushShader));
                }
                else if (type == typeof(float)) 
                {
                    m_BrushData.Add(m_BrushProperties[i].GetIdentifier, new FloatShaderProperty(m_BrushProperties[i].GetIdentifier, m_BrushProperties[i].DefaultValue, m_BrushShader));
                }
                else if (type == typeof(Color)) 
                {
                    m_BrushData.Add(m_BrushProperties[i].GetIdentifier, new ColourShaderProperty(m_BrushProperties[i].GetIdentifier, m_BrushProperties[i].DefaultValue, m_BrushShader));
                }
                else if (type == typeof(bool)) 
                {
                    m_BrushData.Add(m_BrushProperties[i].GetIdentifier, new BoolShaderProperty(m_BrushProperties[i].GetIdentifier, m_BrushProperties[i].DefaultValue, m_BrushShader));
                }
            }
        }
    }

    public override T GetData<T>(in string identifier)
    {
        return m_BrushData.GetValue<IBrushShaderProperty<T>>(identifier).GetProperty();
    }

    public abstract void SetBuffer(in Terrain terrain);
    //Must call when brush is selected
    public abstract void BeginDispatchShader(in Terrain terrain, in RaycastHit hitPoint, in int xPos, in int yPos, in int zPos);
    //Must call when brush is deselected
    public void ResetBuffer()
    {
        if (m_BrushDataBuffer != null) 
        {
            m_BrushDataBuffer.Release();
            m_BrushDataBuffer = null;
        }
    }
    protected void DispatchShader(in Terrain terrain, in Vector3 hitPoint, in int xPos, in int yPos, in int zPos)
    {
        m_BrushShader.SetBuffer(m_BrushShader.FindKernel("ApplyBrush"), "dataBuffer", m_BrushDataBuffer);
        m_BrushShader.SetInt("extent", terrain.chunkSize);
        m_BrushShader.SetInt("chunkSize", terrain.chunkSize * terrain.chunkSize * terrain.chunkSize);
        m_BrushShader.SetFloat("brushPosX", hitPoint.x / terrain.scale - xPos * terrain.chunkSize);
        m_BrushShader.SetFloat("brushPosY", hitPoint.y / terrain.scale - yPos * terrain.chunkSize);
        m_BrushShader.SetFloat("brushPosZ", hitPoint.z / terrain.scale - zPos * terrain.chunkSize);
        int threadNum = (terrain.chunkSize) / 8;
        m_BrushShader.Dispatch(m_BrushShader.FindKernel("ApplyBrush"), threadNum, threadNum, threadNum);
    }

    public abstract class IBrushShaderProperty<T>
    {

        protected T m_Value;
        public abstract void SetProperty(string ID, in T value, in ComputeShader shader);

        public T GetProperty() { return m_Value; }
    }

    public class FloatShaderProperty : IBrushShaderProperty<float>
    {
        public FloatShaderProperty(string ID, object initVal, in ComputeShader shader) 
        {
            SetProperty(ID, (float)initVal, shader);
        }
        public override void SetProperty(string ID, in float value, in ComputeShader shader)
        {
            m_Value = value;
            shader.SetFloat(ID, value);
        }
    }

    public class BoolShaderProperty : IBrushShaderProperty<bool> 
    {
        public BoolShaderProperty(string ID, object initVal, in ComputeShader shader) 
        {
            SetProperty(ID, (bool)initVal, shader);
        }

        public override void SetProperty(string ID, in bool value, in ComputeShader shader)
        {
            m_Value = value;
        }
    }

    public class IntShaderProperty : IBrushShaderProperty<int>
    {
        public IntShaderProperty(string ID, object initVal, in ComputeShader shader)
        {
            SetProperty(ID, (int)initVal, shader);
        }
        public override void SetProperty(string ID, in int value, in ComputeShader shader)
        {
            m_Value = value;
            shader.SetInt(ID, value);
        }
    }

    public class ColourShaderProperty : IBrushShaderProperty<Color>
    {
        public ColourShaderProperty(string ID, object initVal, in ComputeShader shader)
        {
            SetProperty(ID, (Color)initVal, shader);
        }
        public override void SetProperty(string ID, in Color value, in ComputeShader shader)
        {
            m_Value = value;
            shader.SetFloats(ID, new float[] { value.r, value.g, value.b });
        }
    }
}
