using System.Collections.Generic;
using UnityEngine;

public class PlayerBeaconComponent : MonoBehaviour, ILevelListener
{
    [SerializeField] private List<MeshRendererColorChanger> m_BeaconColourChangers;
    [SerializeField] private CowGameManager m_Manager;
    [SerializeField] private float m_InitialOpacity;

	void Start()
    {
		for (int i = 0; i < m_BeaconColourChangers.Count; i++)
		{
			m_BeaconColourChangers[i].SetDesiredOpacity(m_InitialOpacity);
		}
		m_Manager.AddToLevelStarted(this);
    }

    public void PlayerPerspectiveBegin() 
    {
        for (int i = 0; i< m_BeaconColourChangers.Count; i++) 
        {
            m_BeaconColourChangers[i].SetDesiredOpacity(0f);
        }
    }


	public void LevelFinished()
	{

	}

	public void OnExitLevel(float transitionTime)
	{

	}

	public void LevelStarted()
	{

	}
}
