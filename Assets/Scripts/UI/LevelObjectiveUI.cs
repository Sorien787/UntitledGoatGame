using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class LevelObjectiveUI : MonoBehaviour, IObjectiveListener
{
	[Header("Internal References")]
	[SerializeField] private RectTransform m_GoalImage;
	[SerializeField] private RectTransform m_NeutralImage;
	[SerializeField] private RectTransform m_FailImageLeft;
	[SerializeField] private RectTransform m_FailImageRight;
	[SerializeField] private Slider m_Slider;
	[SerializeField] private RectTransform m_SliderBackgroundRect;
	[SerializeField] private Image m_SliderBackgroundImage;
	[SerializeField] private CountdownTimerUI m_CountdownTimer;
	[SerializeField] private TextMeshProUGUI m_TopText;
	[SerializeField] private RectTransform m_TopTextTransform;
	[SerializeField] private CowGameManager m_Manager;
	[SerializeField] private AudioManager m_AudioManager;
	[SerializeField] private VerticalLayoutGroup m_LayoutGroup;

	[Header("Audio Settings")]
	[SerializeField] private SoundObject m_EnterGoalZoneAudioIdentifier;
	[SerializeField] private SoundObject m_ExitGoalZoneAudioIdentifier;

	[Header("Animation Settings")]
	[SerializeField] private float m_fSliderAcceleration;
	[SerializeField] private Color m_EnterGoalPulseColour;
	[SerializeField] private Color m_ExitGoalPulseColour;
	[SerializeField] private AnimationCurve m_PulseStrengthByTimer;
	[SerializeField] [Range(0.0f, 0.2f)] private float m_FailureEndBarSize;
	[SerializeField] private float m_fPulseTime = 0.7f;

	private float m_fCurrentSliderPosition;
	private float m_fCurrentSliderVelocity;
	private Color m_InitialBackgroundColor = default;
	private int m_PulseAnimationId = 0;
	private int m_PulseSizeAnimationId = 0;
	private int m_TopTextPulseAnimationId = 0;

	#region UnityFunctions

	private void Awake()
	{
		m_InitialBackgroundColor = m_SliderBackgroundImage.color;
		m_CountdownTimer.OnTimerTick += OnTimerTick;
		if (m_Manager.HasLevelStarted())
		{

		}
	}

	private string m_InitialText = "";

	private string GenerateTopText(in int val) 
	{
		string initial = m_InitialText + val.ToString();
		if (m_fDesiredCounterVal >= m_MaxValue)
			initial += " / " + (m_MaxValue-1).ToString();
		else if (m_fDesiredCounterVal <= m_MinValue)
			initial += " / " + (m_MinValue+1).ToString();
		return initial;
	}

	void Update()
	{
		m_fCurrentSliderPosition = Mathf.SmoothDamp(m_fCurrentSliderPosition, m_fDesiredCounterVal, ref m_fCurrentSliderVelocity, 1 / m_fSliderAcceleration);
		m_Slider.normalizedValue = (float)(m_fCurrentSliderPosition - m_MinValue) / (m_MaxValue - m_MinValue);
		if (LeanTween.isTweening(gameObject)) 
		{
			m_LayoutGroup.enabled = false;
			m_LayoutGroup.enabled = true;
		}
	}

	#endregion

	// Function implementations of IObjectiveListener
	#region IObjectiveListener

	public void OnCounterChanged(in int val)
	{
		m_fDesiredCounterVal = val;
		m_TopText.text = GenerateTopText(val);
		PulseTopText(1.5f);
	}

	private int m_fDesiredCounterVal = 0;

	public void OnTimerTriggered(in Action callOnComplete, in int time)
	{
		m_CountdownTimer.ShowTimer();
		m_CountdownTimer.StartTimerFromTime(time);
		m_CountdownTimer.OnTimerComplete += callOnComplete;
		PulseBackground(m_ExitGoalPulseColour, 1.0f);

		if (!m_Manager.HasLevelStarted())
		{
			m_CountdownTimer.PauseTimer();
		}
	}

	public void OnObjectiveValidated()
	{
		m_CountdownTimer.ContinueTimer();
	}

	private void OnTimerTick(float timerPercentage) 
	{
		PulseBackground(m_ExitGoalPulseColour, m_PulseStrengthByTimer.Evaluate(timerPercentage));
	}

	public void OnTimerRemoved()
	{
		m_CountdownTimer.StopTimer();
		m_SliderBackgroundImage.color = m_EnterGoalPulseColour;
		LeanTween.color(m_SliderBackgroundRect, m_InitialBackgroundColor, 0.5f).setRecursive(false).setEaseOutCubic();
	}

	public void OnObjectiveEnteredGoal()
	{
		m_AudioManager.PlayOneShot(m_EnterGoalZoneAudioIdentifier);
		PulseBackground(m_EnterGoalPulseColour, 1.0f);
	}

	public void OnObjectiveLeftGoal()
	{
		m_AudioManager.PlayOneShot(m_ExitGoalZoneAudioIdentifier);
		PulseBackground(m_ExitGoalPulseColour, 1.0f);
	}

	float m_MinValue = 0;
	float m_MaxValue = 0;

	public void InitializeData(LevelObjective objective)
	{
		float goalAnchorXMin = objective.GetStartGoalPos;
		float goalAnchorXMax = objective.GetEndGoalPos;

		m_GoalImage.anchorMin = new Vector2(goalAnchorXMin, m_GoalImage.anchorMin.y);
		m_GoalImage.anchorMax = new Vector2(goalAnchorXMax, m_GoalImage.anchorMax.y);

		m_FailImageLeft.anchorMax = new Vector2(objective.HasMinimumFailure ? m_FailureEndBarSize : 0.0f, m_FailImageLeft.anchorMax.y);
		m_FailImageRight.anchorMin = new Vector2(objective.HasMaximumFailure ? 1 - m_FailureEndBarSize : 1.0f, m_FailImageLeft.anchorMin.y);
		
		m_fDesiredCounterVal = objective.GetInternalCounterVal();
		m_MinValue = objective.GetLowestValue;
		m_MaxValue = objective.GetHighestValue;

		m_InitialText = objective.GetEntityInformation.name + (objective.GetObjectiveType == ObjectiveType.Capturing ? " Capture" : " Population");

		m_fDesiredCounterVal = 0;
		m_TopText.text = GenerateTopText(0);
	}

	public void OnObjectiveFailed()
	{

	}
	#endregion

	#region MiscellaneousHelperFunctions

	private void PulseBackground(in Color pulseColor, in float time)
	{
		LeanTween.cancel(m_PulseAnimationId);
		LeanTween.cancel(m_PulseSizeAnimationId);
		m_SliderBackgroundImage.color = Color.Lerp(m_InitialBackgroundColor, pulseColor, time);
		m_PulseAnimationId = LeanTween.color(m_SliderBackgroundRect, m_InitialBackgroundColor, m_fPulseTime).setRecursive(false).setEaseInCubic().uniqueId;
		m_SliderBackgroundImage.rectTransform.localScale = new Vector3(1.0f, 1.4f, 1.0f);
		m_PulseSizeAnimationId = LeanTween.scale(m_SliderBackgroundImage.rectTransform, Vector3.one, m_fPulseTime).setEaseOutCubic().uniqueId;
	}

	private void PulseTopText(in float size) 
	{
		LeanTween.cancel(m_TopTextPulseAnimationId);
		m_TopTextTransform.localScale = Vector3.one * size;
		m_TopTextPulseAnimationId = LeanTween.scale(m_TopTextTransform, Vector3.one, m_fPulseTime).setEaseOutCubic().uniqueId;	
	}
	#endregion
}
