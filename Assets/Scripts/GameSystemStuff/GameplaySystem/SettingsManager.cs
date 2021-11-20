using System.Collections.Generic;
using UnityEngine;
using System;
using UnityUtils;
using UnityWeld.Binding;
using System.ComponentModel;

[Binding]
[CreateAssetMenu(menuName ="SettingsManager")]
public class SettingsManager : ScriptableObject, INotifyPropertyChanged
{

	public List<ControlBinding> m_KeyBindings = new List<ControlBinding>();

	public void ForEachControlBinding(in Action<ControlBinding> act)
	{
		for (int i = 0; i < m_KeyBindings.Count; i++)
		{
			act.Invoke(m_KeyBindings[i]);
		}
	}

	#region BindingProperties
	[Header("=== Music ===")]
	[SerializeField] [Range(0.0f, 1.0f)] private float m_SFXVol = 1.0f;
	[Binding]
	public float SFXVol
	{
		get => m_SFXVol;
		set {
			m_SFXVol = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SFXVol"));
		} 
	}

	[SerializeField] [Range(0.0f, 1.0f)] private float m_AmbientVol = 1.0f;
	[Binding]
	public float AmbientVol
	{
		get { return m_AmbientVol; }
		set {
			m_AmbientVol = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AmbientVol"));
		}
	}

	[SerializeField] [Range(0.0f, 1.0f)] private float m_MusicVol = 1.0f;
	[Binding]
	public float MusicVol
	{
		get => m_MusicVol;
		set {
			m_MusicVol = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MusicVol"));
		}
	}

	[SerializeField] [Range(0.0f, 1.0f)] private float m_UISFXVol = 1.0f;
	[Binding]
	public float UISFXVol
	{
		get => m_UISFXVol;
		set {
			m_UISFXVol = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("UISFXVol"));
		}
	}

	[SerializeField] private bool m_bIsMuted = false;
	[Binding]
	public bool IsMuted
	{
		get => m_bIsMuted;
		set { m_bIsMuted = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsMuted")); }
	}

	[Header("=== Mouse Settings ===")]
	[SerializeField] [Range(0.1f, 2.0f)] private float m_MouseSensitivityX = 1.0f;
	[Binding]
	public float MouseSensitivityX
	{
		get => m_MouseSensitivityX;
		set { m_MouseSensitivityX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MouseSensitivityX")); }
	}

	[SerializeField] [Range(0.1f, 2.0f)] private float m_MouseSensitivityY = 1.0f;
	[Binding]
	public float MouseSensitivityY
	{
		get => m_MouseSensitivityY;
		set { m_MouseSensitivityY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MouseSensitivityY")); }
	}

	[SerializeField] private bool m_InvertY = false;
	[Binding]
	public bool InvertY
	{
		get => m_InvertY;
		set { m_InvertY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("InvertY")); }
	}

	[Header("=== Screen Settings ===")]
	[SerializeField] private FullScreenMode m_DisplayMode = FullScreenMode.FullScreenWindow;
	[Binding]
	public FullScreenMode DisplayMode
	{
		get => m_DisplayMode;
		set { m_DisplayMode = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DisplayMode")); }
	}

	[Header("=== Visual Settings ===")]
	[SerializeField] [Range(10.0f, 100.0f)] private float m_FoV = 60.0f;
	[Binding]
	public float FoV
	{
		get => m_FoV;
		set { m_FoV = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FoV")); }
	}

	[SerializeField] private bool m_Bloom = true;
	[Binding]
	public bool Bloom
	{
		get => m_Bloom;
		set { m_Bloom = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Bloom")); }
	}

	[SerializeField] private bool m_MotionBlur = true;
	[Binding]
	public bool MotionBlur
	{
		get => m_MotionBlur;
		set { m_MotionBlur = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MotionBlur")); }
	}

	[SerializeField] [Range(0.5f, 1.5f)] private float m_Brightness = 1.0f;
	[Binding]
	public float Brightness
	{
		get => m_Brightness;
		set { m_Brightness = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Brightness")); }
	}

	[SerializeField] [Range(0.5f, 1.5f)] private float m_Contrast = 1.0f;
	[Binding]
	public float Contrast
	{
		get => m_Contrast;
		set { m_Contrast = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Contrast")); }
	}

	[SerializeField] private bool m_DepthOfField = true;
	[Binding]
	public bool DepthOfField
	{
		get => m_DepthOfField;
		set { m_DepthOfField = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DepthOfField")); }
	}
	#endregion


	public event PropertyChangedEventHandler PropertyChanged;

}
