using UnityEngine;
using System;

public enum ObjectiveType
{
	Population,
	Capturing
}

[CreateAssetMenu(menuName = "Level Objective")]
public class LevelObjective : ScriptableObject
{
	[Header("Gameplay Settings")]
	[SerializeField] private bool m_HasMinimumFailure;
	[SerializeField] private int m_MinimumValue;
	[SerializeField] private bool m_HasMaximumFailure;
	[SerializeField] private int m_MaximumValue;
	[SerializeField] private int m_MinimumGoal;
	[SerializeField] private int m_MaximumGoal;
	[SerializeField] private int m_TimerMaximum;
	[SerializeField] private ObjectiveType m_ObjectiveType;

	[Header("Static References")]
	[SerializeField] private CowGameManager m_Manager;
	[SerializeField] private EntityInformation m_CounterAnimalType;

	private UnityUtils.ListenerSet<IObjectiveListener> m_ObjectiveListeners = new UnityUtils.ListenerSet<IObjectiveListener>();

	private bool m_bIsCurrentlyFailing = false;
	private bool m_bIsCurrentlyWithinGoal = false;
	private int m_InternalCounterVal = 0;

	// Properties for external access of data by the game manager
	#region AccessibleProperties

	public EntityInformation GetEntityInformation => m_CounterAnimalType;
	public ObjectiveType GetObjectiveType => m_ObjectiveType;

	public float GetStartGoalPos => (float)(m_MinimumGoal - 0.5f - m_MinimumValue) / (m_MaximumValue - m_MinimumValue);
	public float GetEndGoalPos => (float)(m_MaximumGoal + 0.5f - m_MinimumValue) / (m_MaximumValue - m_MinimumValue);
	public float GetLowestValue => m_MinimumValue;
	public float GetHighestValue => m_MaximumValue;
	public bool HasMaximumFailure => m_HasMaximumFailure;
	public bool HasMinimumFailure => m_HasMinimumFailure;

	#endregion

	#region UnityFunctions
	private void Awake()
	{
		OnValidate();
	}

	private void OnValidate()
	{
		// its only valid to have the maximum greater than the minimum in both cases
		m_MaximumValue = Mathf.Max(m_MaximumValue, m_MinimumValue);
		m_MaximumGoal = Mathf.Max(m_MaximumGoal, m_MinimumGoal);

		// it's only valid to have the goal within the minimum
		if (m_HasMinimumFailure)
			m_MinimumGoal = Mathf.Max(m_MinimumValue, m_MinimumGoal);
		if (m_HasMaximumFailure)
			m_MaximumGoal = Mathf.Min(m_MaximumGoal, m_MaximumValue);
		if (!m_HasMinimumFailure && !m_HasMaximumFailure)
			Debug.LogError("Gameplay UI has no failure states", this);
		if ((!m_HasMinimumFailure &&  m_MinimumValue == m_MinimumGoal)|| (!m_HasMaximumFailure && m_MaximumValue == m_MaximumGoal))
			Debug.LogError("Gameplay UI failure state is the same as the goal minimum - they should be at least slightly different", this);
	}
	#endregion

	// pretty much entirely called by the game manager
	#region PublicFunctions

	public void Reset()
	{
		m_bIsCurrentlyFailing = false;
		m_bIsCurrentlyWithinGoal = false;
		m_InternalCounterVal = 0;
	}

	public void AddObjectiveListener(IObjectiveListener listener)
	{
		m_ObjectiveListeners.Add(listener);
		listener.InitializeData(this);
	}

	public int GetInternalCounterVal() 
	{
		return m_InternalCounterVal;
	}

	public void RemoveObjectiveListener(IObjectiveListener listener)
	{
		m_ObjectiveListeners.Remove(listener);
	}

	public void ClearListeners()
	{
		m_ObjectiveListeners.Clear();
	}

	public void IncrementCounter()
	{
		m_InternalCounterVal++;
		m_ObjectiveListeners.ForEachListener((IObjectiveListener listener) => listener.OnCounterChanged(m_InternalCounterVal));
		CheckChanged();
	}

	public void IncrementCounter(int counterChanged) 
	{
		m_InternalCounterVal += counterChanged;
		m_ObjectiveListeners.ForEachListener((IObjectiveListener listener) => listener.OnCounterChanged(m_InternalCounterVal));
		CheckChanged();
	}

	public void DecrementCounter()
	{
		m_InternalCounterVal--;
		m_ObjectiveListeners.ForEachListener((IObjectiveListener listener) => listener.OnCounterChanged(m_InternalCounterVal));
		CheckChanged();
	}

	public void StartLevel() 
	{
		m_ObjectiveListeners.ForEachListener((IObjectiveListener listener) => listener.OnObjectiveValidated());
	}

	#endregion

	// internal functions for setting UI and telling the game manager what state this objective is in
	#region PrivateFunctions

	// for each listener, trigger the timer (only one will actually start a timer)
	// and tell it to trigger ObjectiveFailed at the end of it.
	private void StartFailureTimer()
	{
		m_ObjectiveListeners.ForEachListener( (IObjectiveListener listener) => {
			listener.OnTimerTriggered(OnObjectiveFailed, m_TimerMaximum);
		});
	}

	private void OnObjectiveFailed()
	{
		m_ObjectiveListeners.ForEachListener((IObjectiveListener listener) => {
			listener.OnObjectiveFailed();
		});
	}

	private void HaltFailureTimer()
	{
		m_ObjectiveListeners.ForEachListener((IObjectiveListener listener) => listener.OnTimerRemoved());
	}

	private void EnteredGoal()
	{
		m_ObjectiveListeners.ForEachListener((IObjectiveListener listener) => listener.OnObjectiveEnteredGoal());
	}

	private void LeftGoal()
	{
		m_ObjectiveListeners.ForEachListener((IObjectiveListener listener) => listener.OnObjectiveLeftGoal());
	}

	public void CheckChanged()
	{
		bool withinGoal = false;
		bool withininFailure = false;

		if (m_InternalCounterVal >= m_MinimumGoal && m_InternalCounterVal <= m_MaximumGoal)
			withinGoal = true;
		if ((m_HasMaximumFailure && m_InternalCounterVal >= m_MaximumValue) || (m_HasMinimumFailure && m_InternalCounterVal <= m_MinimumValue))
			withininFailure = true;

		if (withinGoal != m_bIsCurrentlyWithinGoal)
		{
			if (withinGoal)
			{
				EnteredGoal();
			}
			else
			{
				LeftGoal();
			}
			m_bIsCurrentlyWithinGoal = withinGoal;
		}

		if (withininFailure != m_bIsCurrentlyFailing)
		{
			if (withininFailure)
			{
				StartFailureTimer();
			}
			else
			{
				HaltFailureTimer();
			}
			m_bIsCurrentlyFailing = withininFailure;
		}
	}

	#endregion
}

public interface IObjectiveListener
{
	void OnCounterChanged(in int val);

	void OnTimerTriggered( in Action totalTime, in int time);

	void OnTimerRemoved();

	void OnObjectiveEnteredGoal();

	void OnObjectiveLeftGoal();

	void OnObjectiveFailed();

	void InitializeData(LevelObjective objective);

	void OnObjectiveValidated();
}
