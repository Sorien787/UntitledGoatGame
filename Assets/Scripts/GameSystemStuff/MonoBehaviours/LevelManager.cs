using System.Collections;
using UnityEngine;
using System;
using TMPro;
using LevelManagerStates;

[RequireComponent(typeof(CustomAnimation))]
public class LevelManager : MonoBehaviour
{
	#region SerializedParams

	[Header("Animation parameters")]
	[SerializeField] private float m_fTransitionTime;
	[SerializeField] private float m_fMenuTransitionTime;
	[SerializeField] private float m_DebugCountdownTimerTime;
	[SerializeField] private float m_DefaultCountdownTimerTime;
	[Header("Level parameters")]
	[SerializeField] private int m_LevelNumber;

	[Header("Object references")]
	[SerializeField] private CowGameManager m_Manager;
	[SerializeField] private CustomAnimation m_LevelEnterAnimation;
	[SerializeField] private TextMeshProUGUI m_LevelIntroTextLeft;
	[SerializeField] private TextMeshProUGUI m_LevelIntroTextRight;
	[SerializeField] private GameObject m_ObjectiveObjectPrefab;

	[Header("Canvas references")]
	[SerializeField] private Transform m_ObjectiveCanvasTransform;
	[SerializeField] private CountdownTimerUI m_FinalCountdownTimer;
	[SerializeField] private CountdownTimerUI m_StartCountdownTimer;
	[SerializeField] private CanvasGroup m_MainCanvas;
	[SerializeField] private CanvasGroup m_StartCountdownCanvas;
	[SerializeField] private CanvasGroup m_PauseCanvas;
	[SerializeField] private CanvasGroup m_EndSuccessCanvas;
	[SerializeField] private CanvasGroup m_EndFailureCanvas;
	[SerializeField] private CanvasGroup m_StartButtonCanvas;
	[SerializeField] private CanvasGroup m_TextCanvas;

	[Header("Animator References")]
	[SerializeField] private Animator m_LevelTransitionAnimator;
	[SerializeField] private Animator m_LevelUIAnimator;
	#endregion

	// private params for internal use
	#region PrivateParams
	private LevelData m_LevelData;
	private bool m_bIsPaused = false;
	private StateMachine<LevelManager> m_LevelState;
	private CanvasGroup m_CurrentOpenCanvas;
	#endregion

	// publicly accessible properties and events
	#region Properties
	public int GetLevelNumber => m_LevelNumber;


	public Transform GetCamTransform { get; private set; }

	public Transform GetObjectiveCanvasTransform => m_ObjectiveCanvasTransform;

	public event Action OnLevelStarted;

	public event Action OnLevelPaused;

	public event Action OnLevelUnpaused;

	public event Action OnLevelFinished;

	#endregion

	// logic to control the intro animation for levels (zooming in and around the stage before starting)
	#region IntroAnimation
	private void InitializeIntroAnimation()
	{
		switch (m_Manager.GetRestartState)
		{
			case (CowGameManager.RestartState.Debug):
				GetCamTransform.SetParent(m_Manager.GetPlayerCameraContainerTransform);
				GetCamTransform.localPosition = Vector3.zero;
				GetCamTransform.localRotation = Quaternion.identity;
				m_Manager.EnterPlayerPerspective();
				OnLevelStarted?.Invoke();
				m_LevelState.RequestTransition(typeof(PlayingState));
				break;
			case (CowGameManager.RestartState.Quick):
				StartCountdownTimer();
				break;
			default:
				m_LevelEnterAnimation.AddClipStartedCallbackToClip(0, OnFirstIntroAnimationPortionShown);
				m_LevelEnterAnimation.AddClipStartedCallbackToClip(1, OnSecondIntroAnimationPortionShown);
				m_LevelEnterAnimation.AddClipStartedCallbackToClip(2, OnThirdIntroAnimationPortionShown);
				m_LevelEnterAnimation.AddClipStartedCallbackToClip(3, (CustomAnimation.AnimationClip _) => StartCountdownTimer());
				m_LevelEnterAnimation.StartAnimation();
				break;
		}
	}
	private int m_AlphaAnim = -1;
	private void ShowIntroText(CustomAnimation.AnimationClip clip)
	{
		float animInOutTime = 1.0f;
		float animInOutBuffer = 0.1f;
		float lengthCanBeShownFor = Mathf.Max(0, clip.animationTime - clip.entranceAnimationDelay - clip.exitAnimationDelay - clip.entranceAnimationTime / 2 - clip.exitAnimationTime/2 - 2 * animInOutTime - 2 * animInOutBuffer);
		LeanTween.cancel(m_AlphaAnim);
		m_AlphaAnim = LeanTween.alphaCanvas(m_TextCanvas, 1.0f, animInOutTime).setDelay(animInOutBuffer + clip.entranceAnimationDelay + clip.entranceAnimationTime / 2).setOnComplete(() => HideIntroText(animInOutTime, lengthCanBeShownFor)).uniqueId;
	}

	private void HideIntroText(float animInOutTime, float lengthCanBeShownFor)
	{
		m_AlphaAnim = LeanTween.alphaCanvas(m_TextCanvas, 0.0f, animInOutTime).setDelay(lengthCanBeShownFor).uniqueId;
	}

	private void OnFirstIntroAnimationPortionShown(CustomAnimation.AnimationClip clip)
	{
		m_LevelIntroTextLeft.text = "Level " + (m_Manager.GetCurrentLevelIndex).ToString();
		m_LevelIntroTextRight.text = m_LevelData.GetLevelName;
		ShowIntroText(clip);
	}

	private void OnSecondIntroAnimationPortionShown(CustomAnimation.AnimationClip clip)
	{
		m_LevelIntroTextLeft.text = "Time to Beat";
		m_LevelIntroTextRight.text = UnityUtils.UnityUtils.TurnTimeToString(m_LevelData.GetTargetTime);
		ShowIntroText(clip);
	}

	private void OnThirdIntroAnimationPortionShown(CustomAnimation.AnimationClip clip)
	{
		m_LevelIntroTextLeft.text = "I dont know what to put here.";
		m_LevelIntroTextRight.text = "Difficulty?";
		ShowIntroText(clip);
	}

	private void StartCountdownTimer()
	{
		m_StartCountdownTimer.StartTimerFromTime(3.9f);

		GetCamTransform.SetParent(m_Manager.GetPlayerCameraContainerTransform);
		GetCamTransform.localPosition = Vector3.zero;
		GetCamTransform.localRotation = Quaternion.identity;
		m_Manager.EnterPlayerPerspective();
		m_StartCountdownTimer.OnTimerComplete += () =>
		{
			OnLevelStarted?.Invoke();
			m_LevelState.RequestTransition(typeof(PlayingState));
			m_StartCountdownTimer.StopTimer();
		};
	}

	public void StartLevel() 
	{
		m_Manager.StartLevel();
	}

	private IEnumerator StartLevelWithoutCountdown(float time)
	{
		yield return new WaitForSeconds(time);
		m_Manager.EnterPlayerPerspective();
		OnLevelStarted?.Invoke();
		m_LevelState.RequestTransition(typeof(PlayingState));

	}
	#endregion

	// logic controlling the end countdown for a successful level completion
	#region EndCountdown
	public void StartSucceedCountdown()
	{
		int successTime = m_LevelData.GetSuccessTimerTime;
		if (successTime > Mathf.Epsilon) 
		{
			m_FinalCountdownTimer.ShowTimer();
			m_FinalCountdownTimer.StartTimerFromTime(successTime);
			m_FinalCountdownTimer.OnTimerComplete += OnLevelSucceeded;
		}
		else 
		{
			OnLevelSucceeded();
		}
	}

	public void EndSucceedCountdown()
	{
		m_FinalCountdownTimer.StopTimer();
	}
	#endregion

	// Start, Update, Awake
	#region UnityFunctions

	private void Awake()
	{
		m_LevelState = new StateMachine<LevelManager>(new StartState( m_StartButtonCanvas), this);
		m_LevelState.AddState(new PausedState( m_PauseCanvas, m_LevelUIAnimator));
		m_LevelState.AddState(new EndFailureState( m_EndFailureCanvas, m_LevelUIAnimator));
		m_LevelState.AddState(new EndSuccessState( m_EndSuccessCanvas, m_LevelUIAnimator));
		m_LevelState.AddState(new PlayingState( m_MainCanvas));
		m_LevelTransitionAnimator.Play("TransitionIn", -1);
		m_Manager.NewLevelLoaded(this);
		PauseLevel(true);
	}

	private void Start()
	{
		m_LevelState.InitializeStateMachine();
		InitializeIntroAnimation();
	}

	private void Update()
	{
		m_LevelState.Tick(Time.deltaTime);
	}

	#endregion

	// Logic to set/unset certain gameplay canvases and start scene transitions out (they automatically start in)
	#region CanvasSceneFunctions

	private IEnumerator BeginSceneTransition(Action queuedOnFinish)
	{
		m_LevelTransitionAnimator.Play("TransitionOut", -1);
		yield return new WaitForSeconds(m_fTransitionTime);
		queuedOnFinish();
	}

	public void SetCurrentCanvas(CanvasGroup canvas, Action callOnComplete, in float delay = 0.0f)
	{
		ClearCanvas();
		LeanTween.alphaCanvas(canvas, 1.0f, m_fMenuTransitionTime).setEaseInOutCubic().setOnComplete(callOnComplete).setDelay(delay);
		m_CurrentOpenCanvas = canvas;
	}

	public void SetCurrentCanvas(CanvasGroup canvas, in float delay = 0.0f)
	{
		ClearCanvas();
		LeanTween.alphaCanvas(canvas, 1.0f, m_fMenuTransitionTime).setEaseInOutCubic().setDelay(delay);
		m_CurrentOpenCanvas = canvas;
	}

	public void ClearCanvas()
	{
		if (m_CurrentOpenCanvas)
		{
			m_CurrentOpenCanvas.interactable = false;
			m_CurrentOpenCanvas.blocksRaycasts = false;
			LeanTween.alphaCanvas(m_CurrentOpenCanvas, 0.0f, m_fMenuTransitionTime).setEaseInOutCubic();
		}
	}

	#endregion

	// Functions called primarily by player UI requests, sent up to manager
	#region UILevelEvents
	public void LoadNextLevel()
	{
		StartCoroutine(BeginSceneTransition(() => m_Manager.MoveToNextLevel()));
	}

	public void RestartLevel()
	{
		StartCoroutine(BeginSceneTransition(() => m_Manager.RestartCurrentLevel()));
	}

	public void LoadMenu()
	{
		StartCoroutine(BeginSceneTransition(() => m_Manager.MoveToMenu()));
	}

	public void PlayerStartedLevel()
	{
		SetCurrentCanvas(m_StartCountdownCanvas, () => { });
		GetCamTransform.GetComponent<CameraStartEndAnimator>().AnimateIn(m_DefaultCountdownTimerTime);
		m_StartCountdownTimer.StartTimerFromTime(m_DefaultCountdownTimerTime);
		m_StartCountdownTimer.ShowTimer();
		m_StartCountdownTimer.OnTimerComplete += () =>
		{
			OnLevelStarted?.Invoke();
			m_LevelState.RequestTransition(typeof(PlayingState));
		};
	}
	#endregion

	// Functions called primarily by the manager, for defining animations before exiting levels, etc
	#region ManagerLevelEvents

	public void PauseLevel(bool shouldPause)
	{
		if (m_bIsPaused != shouldPause)
		{
			if (shouldPause)
			{
				OnLevelPaused?.Invoke();
			}
			else
			{
				OnLevelUnpaused?.Invoke();
			}
			m_Manager.SetPausedState(shouldPause);
			m_bIsPaused = shouldPause;
		}
	}

	public void InitializeLevel(LevelData levelData, Transform camTransform)
	{
		GetCamTransform = camTransform;
		m_LevelData = levelData;
		levelData.ForEachObjective((LevelObjective objective) =>
		{
			GameObject go = Instantiate(m_ObjectiveObjectPrefab, GetObjectiveCanvasTransform);
			LevelObjectiveUI objectiveUI = go.GetComponent<LevelObjectiveUI>();
			objective.Reset();
			objective.AddObjectiveListener(objectiveUI);
			objective.CheckChanged();
		});
	}


	public void OnLevelSucceeded()
	{
		OnLevelFinished?.Invoke();
		//m_CameraTransform.GetComponent<CameraStartEndAnimator>().AnimateOut();
		m_LevelState.RequestTransition(typeof(EndSuccessState));
	}

	public void OnLevelFailed()
	{
		OnLevelFinished?.Invoke();
		//m_CameraTransform.GetComponent<CameraStartEndAnimator>().AnimateOut();
		m_LevelState.RequestTransition(typeof(EndFailureState));
	}

	public void ResumeLevel()
	{
		m_LevelState.RequestTransition(typeof(PlayingState));
	}
	#endregion

	// Functions called by the state machine
	#region StateMachineEvents

	public void PopulateFailureScreen()
	{
		// Time 
		// objective statistics

		// need to populate objectives using UIObjectiveElements
	}

	public void PopulateSuccessScreen()
	{
		// Time completed
		// objective statistics 
		// Points (?) - speed, efficiency (how close to optimum the scores were)
		// star rating (?)
	}

	#endregion

}


namespace LevelManagerStates
{
	public class PlayingState : AStateBase<LevelManager>
	{
		private bool m_bHasAlreadyStarted = false;
		private readonly CanvasGroup m_CanvasGroup;
		public PlayingState( CanvasGroup pauseGroup)
		{
			m_CanvasGroup = pauseGroup;
		}
		public override void OnEnter()
		{
			if (!m_bHasAlreadyStarted) 
			{
				Host.StartLevel();
			}
			Host.PauseLevel(false);
			Host.SetCurrentCanvas(m_CanvasGroup, () =>
			{
				m_CanvasGroup.blocksRaycasts = true;
				m_CanvasGroup.interactable = true;
			});
		}

		public override void Tick()
		{
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				RequestTransition<PausedState>();
			}
		}

		public override void OnExit()
		{
			Host.PauseLevel(true);
		}
	}

	public class PausedState : AStateBase<LevelManager>
	{
		private readonly CanvasGroup m_CanvasGroup;
		private readonly Animator m_AnimationController;
		public PausedState(CanvasGroup pauseGroup, Animator animator)
		{
			m_AnimationController = animator;
			m_CanvasGroup = pauseGroup;
		}

		public override void OnEnter()
		{
			Host.SetCurrentCanvas(m_CanvasGroup, () =>
			{
				m_CanvasGroup.blocksRaycasts = true;
				m_CanvasGroup.interactable = true;
			});
			//m_AnimationController.Play("AnimIn", -1);
		}

		public override void Tick()
		{
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				Host.ResumeLevel();
			}
		}

		public override void OnExit()
		{
			//m_AnimationController.Play("AnimOut", -1);
		}
	}

	public class EndFailureState : AStateBase<LevelManager>
	{
		private readonly CanvasGroup m_CanvasGroup;
		private readonly Animator m_AnimationController;
		public EndFailureState(CanvasGroup pauseGroup, Animator animator)
		{
			m_AnimationController = animator;
			m_CanvasGroup = pauseGroup;
		}
		public override void OnEnter()
		{
			Host.SetCurrentCanvas(m_CanvasGroup, () =>
			{
				m_CanvasGroup.blocksRaycasts = true;
				m_CanvasGroup.interactable = true;
			}, delay: 1.0f);
			Host.PopulateFailureScreen();
			//m_AnimationController.Play("AnimIn", -1);
		}

		public override void OnExit()
		{
			//m_AnimationController.Play("AnimOut", -1);
		}
	}

	public class EndSuccessState : AStateBase<LevelManager>
	{
		private readonly CanvasGroup m_CanvasGroup;
		private readonly Animator m_AnimationController;

		public EndSuccessState(CanvasGroup pauseGroup, Animator animator)
		{
			m_AnimationController = animator;
			m_CanvasGroup = pauseGroup;
		}

		public override void OnEnter()
		{
			Host.SetCurrentCanvas(m_CanvasGroup, () =>
			{
				m_CanvasGroup.blocksRaycasts = true;
				m_CanvasGroup.interactable = true;
			}, delay: 1.0f);
			Host.PopulateSuccessScreen();
			//m_AnimationController.Play("AnimIn", -1);
		}

		public override void OnExit()
		{
			//m_AnimationController.Play("AnimOut", -1);
		}
	}

	public class StartState : AStateBase<LevelManager>
	{
		private readonly CanvasGroup m_CanvasGroup;
		public StartState(CanvasGroup startGroup)
		{
			m_CanvasGroup = startGroup;
		}
	}
}

