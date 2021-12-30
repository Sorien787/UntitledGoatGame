using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class BloomChangeListener : MonoBehaviour
{
	[SerializeField] private SettingsManager m_SettingsManager;
	[SerializeField] private PostProcessProfile m_PostProcessing;
	private Bloom m_Bloom;

	private void Awake()
	{
		m_SettingsManager.PropertyChanged += OnPropertyChanged;
		m_Bloom = m_PostProcessing.GetSetting<Bloom>();
		m_Bloom.enabled.value = m_SettingsManager.Bloom;
	}

	private void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == UnityUtils.UnityUtils.GetPropertyName(() => m_SettingsManager.FoV))
		{
			m_Bloom.enabled.value = m_SettingsManager.Bloom;
		}
	}

	private void OnDestroy()
	{
		m_SettingsManager.PropertyChanged -= OnPropertyChanged;
	}
}
