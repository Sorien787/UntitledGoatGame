using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Video;

[CreateAssetMenu(menuName = "LevelData")]
public class LevelData : ScriptableObject
{
	[Header("Set Values")]
    [SerializeField] private string m_sLevelName = "";
    [SerializeField] private float m_nTargetTime = 0.0f;
	[SerializeField] private VideoClip m_ReferenceClip;
	[SerializeField] private List<LevelObjective> m_LevelObjectives = new List<LevelObjective>();
	[SerializeField] private float[] m_Checkpoints = new float[] { 0f, 0f };

	[SerializeField] private float m_nAchievedTime = 0.0f;
	[SerializeField] private int m_LevelCompleteTime = 0;
	[SerializeField] private StarRating m_StarRating = StarRating.Zero;
	[SerializeField] private int m_AchievedScore = 0;
	[SerializeField] private bool m_bHasEnteredLevel = false;



	public enum StarRating
	{
		Zero = 0,
		Half = 1,
		Two = 2,
		Three = 3
	}

	#region Properties

	public bool IsCompleted => !m_StarRating.Equals(StarRating.Zero);

	public bool HasEnteredLevelBefore => m_bHasEnteredLevel;

	public int GetObjectiveCount => m_LevelObjectives.Count;

	public int GetSuccessTimerTime => m_LevelCompleteTime;

	public StarRating GetCurrentStarRating => m_StarRating;

	public VideoClip GetLevelVideoClip => m_ReferenceClip;

	public ref float[] GetCheckpoints => ref m_Checkpoints;

	public int GetScore => m_AchievedScore;

	public string GetLevelName => m_sLevelName;

	public float GetTargetTime => m_nTargetTime;

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
		m_StarRating = StarRating.Zero;
		m_AchievedScore = 0;
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
        if (m_nAchievedTime > time || IsCompleted) 
        {
            m_nAchievedTime = time;
        }
    }

	public void TrySetNewStarRating(StarRating newStarRating)
	{
		if (m_StarRating < newStarRating)
		{
			m_StarRating = newStarRating;
		}
	}

	public void TrySetNewScore(in int score)
	{
		if (m_AchievedScore < score)
		{
			m_AchievedScore = score;
		}
	}

	public void SetLevelNumber(in int num)
	{
		GetLevelNumber = num;
	}

	#endregion
}
