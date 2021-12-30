using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class ContrastChangeListener : MonoBehaviour
{
	[SerializeField] private SettingsManager m_SettingsManager;
	[SerializeField] private PostProcessProfile m_PostProcessing;
	private ColorGrading m_ColorGrading;

	private void Awake()
	{
		m_SettingsManager.PropertyChanged += OnPropertyChanged;
		m_ColorGrading = m_PostProcessing.GetSetting<ColorGrading>();
		m_ColorGrading.contrast.value = m_SettingsManager.Contrast;
	}

	private void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == UnityUtils.UnityUtils.GetPropertyName(() => m_SettingsManager.Contrast))
		{
			m_ColorGrading.contrast.value = m_SettingsManager.Contrast;
		}
	}

	private void OnDestroy()
	{
		m_SettingsManager.PropertyChanged -= OnPropertyChanged;
	}
}
