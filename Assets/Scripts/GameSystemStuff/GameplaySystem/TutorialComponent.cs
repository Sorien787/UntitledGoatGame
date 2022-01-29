using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TutorialComponent : MonoBehaviour, ILevelListener, IEntityListener
{
    [SerializeField] TutorialSystem m_TutorialSystem;
    [SerializeField] CowGameManager m_Manager;
    [SerializeField] private LassoInputComponent m_LassoInput; // on the rest of stuff
    [SerializeField] private CowGameManager m_CowManager; // OnObjectiveEntered
    [SerializeField] private LevelManager m_LevelManager; // OnFullnessShown, OnHealthShown

    [SerializeField] private float m_TimeToShow;
    [SerializeField] private float m_FadeTime;

    [SerializeField] private CanvasGroup m_CanvasGroup;
    [SerializeField] private UnityEngine.UI.Text m_Text;


    private void ShowTextCoroutine(in string text) 
    {
        LeanTween.alphaCanvas(m_CanvasGroup, 1.0f, m_FadeTime).setOnComplete(OnTextMax);
        m_Text.text = text;
    }

    private void OnTextMax() 
    {
        LeanTween.delayedCall(m_TimeToShow, BeginTextTweenOut);
    }

    private void BeginTextTweenOut() 
    {
        LeanTween.alphaCanvas(m_CanvasGroup, 1.0f, m_FadeTime).setOnComplete(OnFinishShowingText);
    }

    private void OnFinishShowingText() 
    {
        enabled = true;
    }

	private void Update()
	{
        if (m_TutorialSystem.HasTutorialStageQueued())
            ShowTextCoroutine(m_TutorialSystem.GetTutorialStage());
        enabled = false;
    }



	private void OnDestroy()
	{
        m_Manager.RemoveFromLevelStarted(this);
	}



	public enum ETutorialStage 
    {
        NothingShown,
        OnShowSpinLasso,
        OnShowThrowLasso,
        OnShowCaptureObject,
        OnShowPullObject,
        OnShowSpinAnimal,
        OnShowHealthPopups,OnShowAnimalConsumption,
        OnShowCreatureBred,
        OnShowWinCondition,
        Count
    }
    private void Awake()
    {
        enabled = false;
        m_LassoInput.OnStartSwingingObject += (ThrowableObjectComponent_) => OnLassoSpun();
        m_LassoInput.OnThrowObject += (float _) => OnThrownLasso();
        m_LassoInput.OnSetPullingObject += (ThrowableObjectComponent _) => OnCapturedObject();

        m_LassoInput.OnPullObject += OnPulledObject;
        m_LassoInput.OnStartSwingingObject += (ThrowableObjectComponent_) => OnObjectSpun();

        m_LevelManager.OnPressedShowFullnessOrHealth += OnPressedFullnessOrHealth;
        m_LevelManager.OnShowTableOfHunger += OnPressedTab;


        m_Manager.OnSuccessCounterStarted += OnWinConditionChanged;
        m_Manager.AddToLevelStarted(this);
    }
    private void OnLassoSpun() 
    { 
        m_TutorialSystem.AddCompletedStage((int)ETutorialStage.OnShowSpinLasso);
    }

    private void OnThrownLasso() 
    {
        m_TutorialSystem.AddCompletedStage((int)ETutorialStage.OnShowThrowLasso);
    }

    private void OnCapturedObject() 
    {
        m_TutorialSystem.AddCompletedStage((int)ETutorialStage.OnShowCaptureObject);
    }

    private void OnPulledObject() 
    {
        m_TutorialSystem.AddCompletedStage((int)ETutorialStage.OnShowPullObject);
    }

    private void OnObjectSpun() 
    {
        m_TutorialSystem.AddCompletedStage((int)ETutorialStage.OnShowSpinAnimal);
    }

    private void OnPressedFullnessOrHealth() 
    {
        m_TutorialSystem.AddCompletedStage((int)ETutorialStage.OnShowHealthPopups);
    }

    private void OnPressedTab() 
    {
        m_TutorialSystem.AddCompletedStage((int)ETutorialStage.OnShowAnimalConsumption);
    }

    private void OnCreatureBred() 
    {
        m_TutorialSystem.AddCompletedStage((int)ETutorialStage.OnShowCreatureBred);
    }

    private void OnWinConditionChanged() 
    {
        m_TutorialSystem.AddCompletedStage((int)ETutorialStage.OnShowWinCondition);
    }

	public void LevelStarted() 
    {
        m_Manager.AddEntityAddedListener(this);
        enabled = true;
    }

	public void LevelFinished() { }

	public void PlayerPerspectiveBegin() { }

	public void OnExitLevel(float transitionTime) { }

	public void OnEntityAdded(EntityToken token)
	{
        OnCreatureBred();
    }

	public void OnEntityRemoved(EntityToken token) {}


}
