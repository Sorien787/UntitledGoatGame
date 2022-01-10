﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityWeld.Binding.Internal;
using UnityEditor;
using UnityWeld_Editor;

[CustomEditor(typeof(AudioType))]
public class AudioTypeEditor : BaseBindingEditor
{
	private AudioType m_TargetScript;

	private void OnEnable()
	{
		m_TargetScript = (AudioType)target;
	}


	public override void OnInspectorGUI()
	{

		EditorGUI.BeginChangeCheck();
		ShowViewModelTarget(ref m_TargetScript.GetViewModel());

		var guiPreviouslyEnabled = GUI.enabled;

		// dont allow editing view model properties if we dont have a view model
		if ((m_TargetScript.GetViewModel()) == null)
		{
			GUI.enabled = false;
		}

		//BindableMember<System.Reflection.PropertyInfo>[] props = TypeResolver.FindBindableProperties(m_TargetScript.GetViewModel());

		//for (int i = 0; i < props.Length; i++)
		//{
		//	string str = props[i].ToString();
		//}

		string oldVal = m_TargetScript.ViewModelPropertyName;
		ShowViewModelPropertyMenu(
			new GUIContent(
				"View-model property",
				"Property on the view-model to bind to."
			),
			TypeResolver.FindBindableProperties(m_TargetScript.GetViewModel()),
			updatedValue => m_TargetScript.ViewModelPropertyName = updatedValue,
			m_TargetScript.ViewModelPropertyName,
			property => property.PropertyType == typeof(float)
		);
		bool wasChanged = (oldVal != m_TargetScript.ViewModelPropertyName);
		GUI.enabled = guiPreviouslyEnabled;

		wasChanged |= EditorGUI.EndChangeCheck();
		if (wasChanged)
		{
			EditorUtility.SetDirty(target);
			AssetDatabase.SaveAssets();
		}
	}
}
