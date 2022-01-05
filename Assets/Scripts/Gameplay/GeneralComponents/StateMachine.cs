using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;


public class StateGroup : IStateGroup
{
    private Action m_OnEnterGroup;
    private Action m_OnExitGroup;
    private readonly Type[] m_StatesInGroup;

    public static StateGroup Create(params Type[] typesInGroup) { return new StateGroup(typesInGroup); }
    
    private StateGroup(Type[] typesInGroup)
    {
        m_StatesInGroup = typesInGroup;
    }

    public StateGroup AddOnEnter(in Action OnEnter) 
    {
        m_OnEnterGroup += OnEnter;
        return this;
    }

    public StateGroup AddOnExit(in Action OnExit) 
    {
        m_OnExitGroup += OnExit;
        return this;
    }

    public bool CheckIfShouldEnterStateGroupForTransition(Type toState) 
    {
        foreach(Type type in m_StatesInGroup) 
        {
            if (toState.IsAssignableFrom(type)) 
            {
                m_OnEnterGroup?.Invoke();
                return true;
            }
        }
        return false;
    }
    public bool CheckIfShouldExitStateGroupForTransition(Type toState) 
    {
        foreach (Type type in m_StatesInGroup)
        {
            if (toState.IsAssignableFrom(type))
            {
                return false;
            }
        }
        m_OnExitGroup?.Invoke();
        return true;
    }    
}

public class BaseStateGroup : IStateGroup
{
	public bool CheckIfShouldEnterStateGroupForTransition(Type toState)
	{
        return false;
	}

	public bool CheckIfShouldExitStateGroupForTransition(Type toState)
	{
        return false;
	}
}

public interface IStateGroup 
{
    bool CheckIfShouldEnterStateGroupForTransition(Type toState);

    bool CheckIfShouldExitStateGroupForTransition(Type toState);
}

public class StateMachine<J>
{
    private readonly List<AStateBase<J>> m_States = new List<AStateBase<J>>();
    private readonly List<IStateGroup> m_StateGroups = new List<IStateGroup>();
    private readonly Dictionary<Type, List<IStateTransition>> m_StateTransitions = new Dictionary<Type, List<IStateTransition>>();
    private readonly Dictionary<string, object> m_StateMachineParams = new Dictionary<string, object>();
    private readonly Dictionary<string, Action> m_StateMachineCallbacks = new Dictionary<string, Action>();
    private readonly List<IStateTransition> m_AnyTransitions = new List<IStateTransition>();

    private readonly Stack<IStateGroup> m_CurrentStateGroupQueue = new Stack<IStateGroup>(new[] { new BaseStateGroup() });
    private static readonly List<IStateTransition> m_EmptyTransitionsList = new List<IStateTransition>();
	private AStateBase<J> m_CurrentState;
    private List<IStateTransition> m_SpecificTransitions;


    public void AddState<T>(T newState) where T : AStateBase<J> 
    {
        newState.SetParent(this);
        m_States.Add(newState);
    }

    public Type GetCurrentState() 
    {
        return m_CurrentState.GetType();
    }

    public void AddStateGroup(StateGroup newStateGroup) 
    {
        if (newStateGroup.CheckIfShouldEnterStateGroupForTransition(m_CurrentState.GetType())) 
        {
            m_CurrentStateGroupQueue.Push(newStateGroup);
        }
		else 
        {
            m_StateGroups.Add(newStateGroup);
        }
    }

	public J GetParentBase { get; }

	public StateMachine(AStateBase<J> initialState, J parentType) 
    {
		GetParentBase = parentType;
        m_SpecificTransitions = m_EmptyTransitionsList;
        m_States.Add(initialState);
        initialState.SetParent(this);
        m_CurrentState = initialState;
    }

    public void InitializeStateMachine() 
    {
        m_CurrentState.OnEnter();
    }

    public void AddTransition(Type from, Type to, Func<bool> transition) 
    {
        if (!m_StateTransitions.TryGetValue(from, out List<IStateTransition> transitions))
        {
            transitions = new List<IStateTransition>();
            m_StateTransitions.Add(from, transitions);
            if (m_CurrentState.GetType() == from) 
            {
                m_SpecificTransitions = transitions;
            }
        }
        StateTransition stateTransition = new StateTransition(string.Format("{0}to{1}", from.Name, to.Name), to, transition);

        transitions.Add(stateTransition);

    }

    public void AddTransition(Type from, Type to, Func<bool> transition, Action delegateOnTransition) 
    {
        if (!m_StateTransitions.TryGetValue(from, out List<IStateTransition> transitions))
        {
            transitions = new List<IStateTransition>();
            m_StateTransitions.Add(from, transitions);
        }
        transitions.Add(new StateTransitionWithCallback(to, transition, delegateOnTransition));
    }

    public void AddAnyTransition(Type to, Func<bool> transition) 
    {
        m_AnyTransitions.Add(new StateTransition(string.Format("AnyTo{0}", to.Name), to, transition));
    }

    private float m_timeInState = 0.0f;

    public float TimeBeenInstate() 
    {
        return m_timeInState;
    }

    public void RequestTransition(Type newState)
    {
        for (int i = 0; i < m_States.Count; i++)
        {
            if (newState.IsAssignableFrom(m_States[i].GetType()) && !newState.IsAssignableFrom(m_CurrentState.GetType()))
            {
                if (m_StateTransitions.TryGetValue(newState, out List<IStateTransition> newList))
                {
                    m_SpecificTransitions = newList;
                }
                else
                {
                    m_SpecificTransitions = m_EmptyTransitionsList;
                }
                for(int j = 0; j < m_StateGroups.Count; j++) 
                {
                    IStateGroup stateGroup = m_StateGroups[j];
                    if (stateGroup.CheckIfShouldEnterStateGroupForTransition(newState)) 
                    {
                        m_CurrentStateGroupQueue.Push(stateGroup);
                        m_StateGroups.RemoveAt(j);
                        break;
                    }
                }
                while (true)
                {
                    if (m_CurrentStateGroupQueue.Peek().CheckIfShouldExitStateGroupForTransition(newState))
                    {
                        m_StateGroups.Add(m_CurrentStateGroupQueue.Pop());
                        continue;
                    }
                    break;
                }
                m_CurrentState.OnExit();
                m_CurrentState = m_States[i];
                m_timeInState = Time.time;
                m_CurrentState.ClearTimers();
                m_CurrentState.OnEnter();
                break;
            }
        }
    }

    public void Tick(in float deltaTime)
    {
        m_timeInState += deltaTime;
        for (int i = 0; i < m_AnyTransitions.Count; i++) 
        {
            if (m_AnyTransitions[i].TypeToTransitionTo != m_CurrentState.GetType() && m_AnyTransitions[i].AttemptTransition)
            {
                RequestTransition(m_AnyTransitions[i].TypeToTransitionTo);
                m_CurrentState.Tick();
                return;
            }
        }

        for (int i = 0; i < m_SpecificTransitions.Count; i++) 
        {
            if (m_SpecificTransitions[i].AttemptTransition) 
            {
                RequestTransition(m_SpecificTransitions[i].TypeToTransitionTo);
                m_CurrentState.Tick();
                return;
            }
        }
		m_CurrentState.IncrementInternalTimers(Time.deltaTime);
        m_CurrentState.Tick();
    }
}

public abstract class IStateTransition 
{
    protected Func<bool> m_StateTransition;
    protected Type m_ToState;

    public void OverrideFunc(bool val) 
    {
        m_StateTransition = () => val;
    }

    public abstract bool AttemptTransition { get; }

    public  Type TypeToTransitionTo { get => m_ToState; }

}

public class StateTransition : IStateTransition
{
    private string name;
    public StateTransition(string transitionName, Type toState, Func<bool> stateTransition)
    {
        name = transitionName;
        this.m_ToState = toState;
        this.m_StateTransition = stateTransition;
    }
    public override bool AttemptTransition { get => m_StateTransition(); }

    
}

public class StateTransitionWithCallback : IStateTransition
{
    private Action m_TransitionAction;
    public StateTransitionWithCallback(Type toState, Func<bool> stateTransition, Action transitionAction)
    {
        this.m_ToState = toState;
        this.m_StateTransition = stateTransition;
        this.m_TransitionAction = transitionAction;
    }
    public override bool AttemptTransition { get { if (m_StateTransition()) { m_TransitionAction(); return true; } return false; } }

}

public abstract class AStateBase<J>
{
    private StateMachine<J> m_ParentStateMachine;

	private List<bool> m_TimersActive;
	private List<float> m_InternalTimers;

	public J Host => m_ParentStateMachine.GetParentBase;

	public AStateBase(uint timers = 0u)
	{
		AddTimers(timers);
	}

    public void SetParent(in StateMachine<J> parentStateMachine) 
    {
        m_ParentStateMachine = parentStateMachine;
    }
    protected void RequestTransition<T>()
    {
        m_ParentStateMachine.RequestTransition(typeof(T));
    }

	protected void AddTimers(uint timerNum)
	{
		if (timerNum != 0)
		{
			m_InternalTimers = new List<float>();
			m_TimersActive = new List<bool>();
			while (timerNum != 0)
			{
				m_TimersActive.Add(true);
				m_InternalTimers.Add(0f);
				timerNum--;
			} 
		}
	}
	protected void StartTimer(int timerId)
	{
		m_TimersActive[timerId] = true;
	}

	protected void StopTimer(int timerId)
	{
		m_TimersActive[timerId] = false;
	}

	protected void SetTimer(int timerId, float timerVal) { m_InternalTimers[timerId] = timerVal; }

	protected void ClearTimer(int timerId) { m_InternalTimers[timerId] = 0f; }

	public void ClearTimers()
	{
		if (m_InternalTimers == null)
			return;
		for (int i = 0; i < m_InternalTimers.Count; i++)
		{
			if (!m_TimersActive[i])
				continue;

			m_InternalTimers[i] = 0;
		}
	}

	protected float GetTimerVal(int timerId) { return m_InternalTimers[timerId]; }

	protected void IncrementInternalTimer(in float timeAdd, in int timer)
	{
		m_InternalTimers[timer] += timeAdd;
	}

	public void IncrementInternalTimers(in float timeAdd)
	{
		if (m_InternalTimers == null)
			return;
		for (int i = 0; i < m_InternalTimers.Count; i++)
		{
			if (m_TimersActive[i])
				m_InternalTimers[i] += timeAdd;
		}
	}
    public virtual void Tick() { }

    public virtual void LateTick() { }
    public virtual void OnEnter() { }
    public virtual void OnExit() { }
}

