using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolsManager : ScriptableObject
{
    private ITool m_CurrentTool;

    private ITool m_NullTool = new NullTool();

    public void OnActivated() 
    {
        m_CurrentTool = m_NullTool;
    }

    public void ActivateTool(in ITool tool) 
    {
        m_CurrentTool.OnToolDeactivated();
        m_CurrentTool = tool;
        m_CurrentTool.OnToolActivated();
    }

    public void DeactivateTools() 
    {
        m_CurrentTool.OnToolDeactivated();
        m_CurrentTool = m_NullTool;
    }

    public void Update() 
    {
        m_CurrentTool.OnUpdate();
    }
}

public class NullTool : ITool
{
	public void OnToolActivated(){}

	public void OnToolDeactivated(){}

	public void OnUpdate(){}
}

public interface ITool : IUpdateableElement
{
    void OnToolActivated();
    void OnToolDeactivated();
}


public interface IUpdateableElement 
{
    void OnUpdate();
}
