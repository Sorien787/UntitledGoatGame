using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CowGameManager))]
public class ManagerEditor : Editor
{
	CowGameManager gameManager;
	Editor terrainGeneratorEditor;

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();
		if (GUILayout.Button("Reset Level Data"))
		{
			gameManager.ResetAllLevels();
		}
		//DrawSettingsEditor(gameManager, ref terrainGeneratorEditor);
	}

	private void OnEnable()
	{
		gameManager = (CowGameManager)target;
	}

	void DrawSettingsEditor(Object settings, ref Editor editor)
	{
		if (settings != null)
		{
			//_choiceIndex = EditorGUILayout.Popup("Label", _choiceIndex, _choices);
			//terrain.selectedTerrainData = terrain.terrainData[_choiceIndex];
			bool foldout = EditorGUILayout.InspectorTitlebar(true, settings);
			using (var check = new EditorGUI.ChangeCheckScope())
			{
				if (foldout)
				{
					CreateCachedEditor(settings, null, ref editor);
					editor.OnInspectorGUI();
				}
			}
		}
	}
}
