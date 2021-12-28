using System.Collections;
using UnityEngine;
using System;
using MenuManagerStates;
using UnityWeld.Binding;
using System.Collections.Generic;
using TMPro;

[Binding]
public class MenuManager : MonoBehaviour
{
	[Header("Animation parameters")]
	[SerializeField] private float m_fTransitionTime;

	[Space]
	[Header("Object references")]
	[SerializeField] private CowGameManager m_Manager;
	[SerializeField] private LevelSelectUI m_LevelSelectUI;

	[Space]
	[Header("Canvas references")]
	[SerializeField] private CanvasGroup m_MainCanvas;
	[SerializeField] private CanvasGroup m_SettingsCanvas;
	[SerializeField] private CanvasGroup m_LevelSelectCanvas;
	[SerializeField] private CanvasGroup m_QuitCanvas;

	[Space]
	[Header("Misc.")]
	[SerializeField] private List<CanvasGroup> m_MenuButtons;
	[SerializeField] private List<Animator> m_SettingsAnimators;

	[Space]
	[Header("Animator References")]
	[SerializeField] private Animator m_LevelTransitionAnimator;
	[SerializeField] private Animator m_MainScreenAnimator;

	private StateMachine<MenuManager> m_MenuStateMachine;

	#region UnityFunctions

	void Awake()
    {
		m_MenuStateMachine = new StateMachine<MenuManager>(new MenuManagerStates.MainState(m_MainCanvas, m_MainScreenAnimator), this);
		m_MenuStateMachine.AddState(new MenuManagerStates.LevelSelectState(m_LevelSelectCanvas, m_MainScreenAnimator));
		m_MenuStateMachine.AddState(new MenuManagerStates.SettingsState(m_SettingsCanvas, m_MainScreenAnimator));
		m_MenuStateMachine.AddState(new MenuManagerStates.PreQuitState(m_QuitCanvas, m_MainScreenAnimator));
		m_MenuStateMachine.InitializeStateMachine();
	}

    void Update()
    {
		m_MenuStateMachine.Tick(Time.deltaTime);
    }

	public void ShowMenuStuff(bool shouldShow)
	{
		StartCoroutine(ChangeMenuCoroutine(shouldShow));
	}

	private IEnumerator ChangeMenuCoroutine(bool menuState)
	{
		for (int i = 0; i < m_MenuButtons.Count; i++)
		{
			m_MenuButtons[i].interactable = menuState;
			yield return new WaitForSecondsRealtime(0.15f);
		}
	}

	public void ShowSettingsStuff(bool shouldShow)
	{
		StartCoroutine(ChangeSettingsCoroutine(shouldShow));
	}

	private IEnumerator ChangeSettingsCoroutine(bool settingsState)
	{
		string toPlay = settingsState ? "AnimIn" : "AnimOut";
		for (int i = 0; i < m_SettingsAnimators.Count; i++)
		{
			m_SettingsAnimators[i].Play(toPlay, -1);
			yield return new WaitForSecondsRealtime(0.15f);
		}
	}

	#endregion

	#region CanvasSceneFunctions

	private IEnumerator BeginSceneTransition(Action queuedOnFinish)
	{
		m_LevelTransitionAnimator.Play("ExitLevelAnimation", -1);
		yield return new WaitForSeconds(m_fTransitionTime);
		queuedOnFinish();
	}
	#endregion

	#region UIFunctions

	public void OpenSettingsMenu()
	{
		m_MenuStateMachine.RequestTransition(typeof(SettingsState));
	}

	public void OpenLevelSelect()
	{
		m_MenuStateMachine.RequestTransition(typeof(LevelSelectState));
	}

	public void ReturnToMainMenu()
	{
		m_MenuStateMachine.RequestTransition(typeof(MainState));
	}

	public void OpenQuitScreen()
	{
		m_MenuStateMachine.RequestTransition(typeof(PreQuitState));
	}

	public void Quit()
	{
		m_LevelSelectCanvas.blocksRaycasts = false;
		StartCoroutine(BeginSceneTransition(() => Application.Quit(0)));
	}

	public void OnClickPlay()
	{
		m_LevelSelectCanvas.blocksRaycasts = false;
		int sceneId = m_LevelSelectUI.GetChosenLevelId;
		StartCoroutine(BeginSceneTransition(() => m_Manager.MoveToSceneWithSceneId(sceneId+1)));
		if (!m_Manager.GetLevelDataByLevelIndex(m_LevelSelectUI.GetChosenLevelId).HasEnteredLevelBefore)
		{
			m_Manager.SetDefaultEntry();
			m_Manager.GetLevelDataByLevelIndex(m_LevelSelectUI.GetChosenLevelId).OnEnterLevel();
		}
		else
		{
			m_Manager.SetQuickEntry();
		}
	}

	#endregion
}

namespace MenuManagerStates
{
	public class SettingsState : AStateBase<MenuManager>
	{
		private readonly Animator m_Animator;
		private readonly CanvasGroup m_CanvasGroup;
		public SettingsState(CanvasGroup settingsGroup, Animator animator)
		{
			m_CanvasGroup = settingsGroup;
			m_Animator = animator;
		}
		public override void OnEnter()
		{
			m_Animator.Play("AnimSettingsIn", -1);
		}

		public override void OnExit()
		{
			m_Animator.Play("AnimSettingsOut", -1);
			Host.ShowSettingsStuff(false);
		}

		public override void Tick()
		{
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				RequestTransition<MenuManagerStates.MainState>();
			}
		}
	}

	public class MainState : AStateBase<MenuManager>
	{
		private readonly Animator m_Animator;
		private readonly CanvasGroup m_CanvasGroup;

		public MainState(CanvasGroup levelSelectGroup, Animator animator)
		{
			m_CanvasGroup = levelSelectGroup;
			m_Animator = animator;
		}

		public override void OnEnter()
		{
			Host.ShowMenuStuff(true);
		}

		public override void OnExit()
		{
			Host.ShowMenuStuff(false);	
		}
	}

	public class PreQuitState : AStateBase<MenuManager>
	{
		private readonly CanvasGroup m_CanvasGroup;
		private readonly Animator m_Animator;

		public PreQuitState(CanvasGroup levelSelectGroup, Animator animator)
		{
			m_CanvasGroup = levelSelectGroup;
			m_Animator = animator;
		}

		public override void OnEnter()
		{
			m_Animator.Play("AnimQuitIn", -1);
		}

		public override void OnExit()
		{
			m_Animator.Play("AnimQuitOut", -1);
		}

		public override void Tick()
		{
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				RequestTransition<MenuManagerStates.MainState>();
			}
		}
	}

	public class LevelSelectState : AStateBase<MenuManager>
	{
		private readonly Animator m_Animator;
		private readonly CanvasGroup m_CanvasGroup;

		public LevelSelectState(CanvasGroup levelSelectGroup, Animator animator)
		{
			m_CanvasGroup = levelSelectGroup;
			m_Animator = animator;
		}
		public override void OnEnter()
		{
			m_Animator.Play("AnimLevelsIn", -1);
		}

		public override void OnExit()
		{
			m_Animator.Play("AnimLevelsOut", -1);
		}

		public override void Tick()
		{
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				RequestTransition<MenuManagerStates.MainState>();
			}
		}
	}
}
