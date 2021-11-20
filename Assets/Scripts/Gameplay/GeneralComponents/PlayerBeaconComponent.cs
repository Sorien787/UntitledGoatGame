using System.Collections.Generic;
using UnityEngine;

public class PlayerBeaconComponent : MonoBehaviour
{
    [SerializeField] private List<MeshRendererColorChanger> m_BeaconColourChangers;
    [SerializeField] private CowGameManager m_Manager;
    [SerializeField] private float m_InitialOpacity;
    // Start is called before the first frame update
    void Start()
    {
        m_Manager.GetCurrentLevel.OnLevelStarted += OnLevelStarted;
        for (int i = 0; i < m_BeaconColourChangers.Count; i++)
        {
            m_BeaconColourChangers[i].SetDesiredOpacity(m_InitialOpacity);
        }
    }

    void OnLevelStarted() 
    {
        for (int i = 0; i< m_BeaconColourChangers.Count; i++) 
        {
            m_BeaconColourChangers[i].SetDesiredOpacity(0f);
        }
    }
}
