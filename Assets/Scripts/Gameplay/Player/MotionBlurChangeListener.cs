using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class MotionBlurChangeListener : MonoBehaviour
{
	[SerializeField] private SettingsManager m_SettingsManager;
	[SerializeField] private PostProcessProfile m_PostProcessing;
	private MotionBlur m_MotionBlur;

	private void Awake()
	{
		m_SettingsManager.PropertyChanged += OnPropertyChanged;
		m_MotionBlur = m_PostProcessing.GetSetting<MotionBlur>();
		m_MotionBlur.enabled.value = m_SettingsManager.MotionBlur;
	}

	private void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == UnityUtils.UnityUtils.GetPropertyName(() => m_SettingsManager.MotionBlur))
		{
			m_MotionBlur.enabled.value = m_SettingsManager.MotionBlur;
		}
	}

	private void OnDestroy()
	{
		m_SettingsManager.PropertyChanged -= OnPropertyChanged;
	}
}
