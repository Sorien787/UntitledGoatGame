using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "AudioType")]
public class AudioType : ScriptableObject
{
	/// <summary>
	/// Name of the property in the view model to bind.
	/// </summary>
	public string ViewModelPropertyName
	{
		get { return viewModelPropertyName; }
		set { viewModelPropertyName = value; }
	}

	[SerializeField] private string viewModelPropertyName;

	[SerializeField] private Object m_ViewModel;

	/// <summary>
	/// Get the specified view model
	/// </summary>
	public SettingsManager GetViewModelAsSettingsManager()
	{
		return (SettingsManager)m_ViewModel;
	}

	private string GetViewModelReducedPropertyName() 
	{
		int index = viewModelPropertyName.LastIndexOf(".");
		return viewModelPropertyName.Substring(index + 1);
	}

	public ref Object GetViewModel()
	{
		return ref m_ViewModel;
	}

	public bool WasValidPropertyChanged(string propertyName)
	{
		return GetViewModelReducedPropertyName() == propertyName;
	}

	public float GetVolumeValModifier()
	{
		System.Reflection.PropertyInfo info = m_ViewModel.GetType().GetProperty(GetViewModelReducedPropertyName());
		return (float) info.GetValue(m_ViewModel); 
	}

}
