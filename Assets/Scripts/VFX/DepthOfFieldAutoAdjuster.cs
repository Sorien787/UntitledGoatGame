using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class DepthOfFieldAutoAdjuster : MonoBehaviour
{
	[SerializeField] private Transform m_CamTransform;
	[SerializeField] private PostProcessVolume m_Volume;
	[SerializeField] private LassoInputComponent m_LassoStart;
	[SerializeField] private float m_fFocusDistanceSettleTime;
	[SerializeField] private float m_fMaxFocusDistanceSettleVelocity;
	[SerializeField] private float m_fMaxFocalLength;
	[SerializeField] private LayerMask m_DepthOfFieldRaycastMask;

	[SerializeField] private SettingsManager m_SettingsManager;

	private float m_fFocusDistanceSettleVelocity;
	private DepthOfField m_DepthOfField;
	private float m_CurrentFocusDistance;
	private float m_fTargetFocusDistance;
	private Transform m_FocusedTransform;
	private StateMachine<DepthOfFieldAutoAdjuster> m_StateMachine;

	public float GetMaxFocalLength => m_fMaxFocalLength;
	public Transform GetFocusedTransform => m_FocusedTransform;
	public Transform GetCamTransform => m_CamTransform;
	public LayerMask GetDepthOfFieldRaycastMask => m_DepthOfFieldRaycastMask;

	public void SetFocusedTransform(ThrowableObjectComponent focusedTransform) 
	{ 
		m_FocusedTransform = focusedTransform.transform; 
	}

	public void UnsetFocusedTransform() 
	{ 
		m_FocusedTransform = null; 
	}

	private void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == UnityUtils.UnityUtils.GetPropertyName(() => m_SettingsManager.DepthOfField))
		{
			enabled = m_SettingsManager.DepthOfField;
		}
	}

	private void Awake()
    {
		m_StateMachine = new StateMachine<DepthOfFieldAutoAdjuster>(new AutoAdjustByRaycastState(), this);
		m_StateMachine.AddState(new AutoAdjustToTargetState());
		m_StateMachine.AddTransition(typeof(AutoAdjustToTargetState), typeof(AutoAdjustByRaycastState), () => m_FocusedTransform == null);
		m_StateMachine.AddTransition(typeof(AutoAdjustByRaycastState), typeof(AutoAdjustToTargetState), () => m_FocusedTransform != null);
		m_StateMachine.InitializeStateMachine();

		m_LassoStart.OnSetPullingObject += SetFocusedTransform;
		m_LassoStart.OnStoppedPullingObject += UnsetFocusedTransform;

		m_SettingsManager.PropertyChanged += OnPropertyChanged;
		enabled = m_SettingsManager.DepthOfField;

		PostProcessProfile volumeProfile = m_Volume?.profile;
		if (!volumeProfile) throw new System.NullReferenceException(nameof(PostProcessProfile));
		if (!volumeProfile.TryGetSettings(out m_DepthOfField)) throw new System.NullReferenceException(nameof(m_DepthOfField));
	}

	public void SetTargetFocalLength(float targetFocalLength) 
	{
		m_fTargetFocusDistance = targetFocalLength;
	}

	void Update()
    {
		m_StateMachine.Tick(Time.deltaTime);
		m_CurrentFocusDistance = Mathf.SmoothDamp(m_CurrentFocusDistance, m_fTargetFocusDistance, ref m_fFocusDistanceSettleVelocity, m_fFocusDistanceSettleTime);
		m_fFocusDistanceSettleVelocity = Mathf.Clamp(m_fFocusDistanceSettleVelocity, -m_fMaxFocusDistanceSettleVelocity, m_fMaxFocusDistanceSettleVelocity);
		m_DepthOfField.focusDistance.Override(m_CurrentFocusDistance);
    }

	private void OnDestroy()
	{
		m_SettingsManager.PropertyChanged -= OnPropertyChanged;
	}
}

class AutoAdjustToTargetState : AStateBase<DepthOfFieldAutoAdjuster> 
{
	public override void Tick()
	{
		Host.SetTargetFocalLength((Host.GetCamTransform.position - Host.GetFocusedTransform.position).magnitude);
	}
}

class AutoAdjustByRaycastState : AStateBase<DepthOfFieldAutoAdjuster> 
{
	public override void Tick()
	{
		if (Physics.Raycast(Host.GetCamTransform.position, Host.GetCamTransform.forward, out RaycastHit hit, Host.GetMaxFocalLength, Host.GetDepthOfFieldRaycastMask))
		{
			Host.SetTargetFocalLength(hit.distance);
		}
		else
		{
			Host.SetTargetFocalLength(Host.GetMaxFocalLength);
		}
	}
}