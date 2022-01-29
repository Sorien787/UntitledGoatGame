using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Tutorial Data")]
public class TutorialSystem : ScriptableObject
{
	[SerializeField] private HashSet<int> m_AlreadyCompletedActions = new HashSet<int>();
	[SerializeField] private List<string> m_TextsToShow = new List<string>();
	[SerializeField] private int m_CurrentShowingText = 0;

	public void AddCompletedStage(int stage) 
	{
		m_AlreadyCompletedActions.Add(stage);
	}
	public string GetTutorialStage() 
	{
		return m_TextsToShow[m_CurrentShowingText];
	}
	public bool HasTutorialStageQueued() 
	{
		while (m_CurrentShowingText++ < m_TextsToShow.Count)
		{
			if (m_AlreadyCompletedActions.Contains(m_CurrentShowingText))
				continue;
			return true;
		}
		return false;
	}
}
