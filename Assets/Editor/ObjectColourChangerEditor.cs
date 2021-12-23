using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ObjectColorChanger))]
public class ObjectColourChangerEditor : Editor
{
    ObjectColorChanger colorChanger;
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        colorChanger.RandomizeOnStart = GUILayout.Toggle(colorChanger.RandomizeOnStart, "Randomize On Start");
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.LabelField("Animation References", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        ref List<ObjectColorChangeMaterialSetting> materialColourSettings = ref colorChanger.GetMaterialColourSettings();
        string[] choices = new string[materialColourSettings.Count];
        for (int i = 0; i < materialColourSettings.Count; i++)
        {
            choices[i] = "Material " + i.ToString();
        }
        int chosenIndex = colorChanger.m_MaterialColourSettingReference;
        chosenIndex = Mathf.Clamp(chosenIndex, 0, Mathf.Max(0, materialColourSettings.Count-1));
        chosenIndex = EditorGUILayout.Popup(chosenIndex, choices);
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(chosenIndex == materialColourSettings.Count - 1 && materialColourSettings.Count > 0))
            {
                if (GUILayout.Button("Next Setting"))
                {
                    chosenIndex++;
                }
            }
            using (new EditorGUI.DisabledScope(chosenIndex == 0))
            {
                if (GUILayout.Button("Previous Setting"))
                {
                    chosenIndex--;
                }
            }
            using (new EditorGUI.DisabledScope(materialColourSettings.Count == 0))
            {
                if (GUILayout.Button("Delete Setting"))
                {
                    materialColourSettings.RemoveAt(chosenIndex);
                    chosenIndex--;
                }
            }

            if (GUILayout.Button("Add Setting"))
            {
                ObjectColorChangeMaterialSetting setting = new ObjectColorChangeMaterialSetting
                {
                    m_MaterialColourId = colorChanger.GetDefaultShaderId,
                    m_ColourGradient = new Gradient()
				};
  
				if (materialColourSettings.Count == 0 || chosenIndex == materialColourSettings.Count)
                {
                    materialColourSettings.Add(setting);
                }
                else
                {
                    materialColourSettings.Insert(chosenIndex + 1, setting);
                }
            }
        }

        colorChanger.m_MaterialColourSettingReference = chosenIndex;

        EditorGUILayout.Space();

        if (materialColourSettings.Count > 0)
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.LabelField("Material Setting Information", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);


            ObjectColorChangeMaterialSetting clip = materialColourSettings[chosenIndex];


            clip.m_ColourGradient = EditorGUILayout.GradientField(clip.m_ColourGradient);
            clip.m_MaterialIndex = EditorGUILayout.IntField("Material Num", clip.m_MaterialIndex);
            clip.m_MaterialColourId = EditorGUILayout.TextField(clip.m_MaterialColourId);
            using (var h = new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Use child objs");
                clip.m_changeChildObjects = EditorGUILayout.Toggle(clip.m_changeChildObjects);
            }


            if (!colorChanger.RandomizeOnStart)
            {
                if (GUILayout.Button("Reroll Colour"))
                {
                    clip.RollColour();
                    colorChanger.SetColours();
                }
            }
        }


    }

    void OnEnable()
    {
        colorChanger = (ObjectColorChanger) target;
    }
}
