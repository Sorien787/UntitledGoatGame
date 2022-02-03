using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Video;

[CreateAssetMenu(menuName = "LevelData")]
public class LevelData : ScriptableObject
{
	[Header("Set Values")]
    [SerializeField] private string m_sLevelName = "";
	[SerializeField] private VideoClip m_ReferenceClip;
	[SerializeField] private List<LevelObjective> m_LevelObjectives = new List<LevelObjective>();

	[SerializeField] private float m_nAchievedTime = 0.0f;
	[SerializeField] private int m_SuccessTimerTime = 0;

	[SerializeField] private bool m_bHasEnteredLevel = false;



	public enum StarRating
	{
		Zero = 0,
		Half = 1,
		Two = 2,
		Three = 3
	}

	#region Properties

	public bool IsCompleted => m_nAchievedTime != 0.0f;

	public bool HasEnteredLevelBefore => m_bHasEnteredLevel;

	public int GetObjectiveCount => m_LevelObjectives.Count;

	public int GetSuccessTimerTime => m_SuccessTimerTime;

	public VideoClip GetLevelVideoClip => m_ReferenceClip;

	public string GetLevelName => m_sLevelName;

	public string GetBestTimeAsString => UnityUtils.UnityUtils.TurnTimeToString(m_nAchievedTime);

	public int GetLevelNumber { get; private set; } = 0;

	#endregion

	#region PublicFunctions

	public void OnEnterLevel()
	{
		m_bHasEnteredLevel = true;
	}

	public void Reset()
	{
		m_nAchievedTime = 0.0f;
		m_bHasEnteredLevel = false;
	}

	public void ForEachObjective(Action<LevelObjective> objectiveFunc)
	{
		foreach(LevelObjective objective in m_LevelObjectives)
		{
			objectiveFunc.Invoke(objective);
		}
	}

    public void TrySetNewTime(in float time) 
    {
        if (m_nAchievedTime > time || !IsCompleted) 
        {
            m_nAchievedTime = time;
        }
    }

	public void SetLevelNumber(in int num)
	{
		GetLevelNumber = num;
	}

	#endregion
}
