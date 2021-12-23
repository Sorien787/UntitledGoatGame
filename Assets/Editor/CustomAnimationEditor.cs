using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(CustomAnimation))]
public class CustomAnimationEditor : Editor
{
    CustomAnimation animation;
    float m_testAnimationPosition = 0.0f;
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.LabelField("Animation References", EditorStyles.boldLabel);
        ref List<CustomAnimation.AnimationClip> animationClips = ref animation.GetGameraAnimationClip;
        string[] choices = new string[animationClips.Count];
        for (int i = 0; i < animationClips.Count; i++) 
        {
            choices[i] = animationClips[i].name;
        }
        int chosenIndex = animation.m_AnimEditorClipNumReference;
        chosenIndex = Mathf.Clamp(chosenIndex, 0, animationClips.Count);
        chosenIndex = EditorGUILayout.Popup(chosenIndex, choices);
        using (new EditorGUILayout.HorizontalScope()) 
        {
            using (new EditorGUI.DisabledScope(chosenIndex == animationClips.Count - 1 && animationClips.Count > 0))
            {
                if (GUILayout.Button("Next Clip"))
                {
                    chosenIndex++;
                }
            }
            using (new EditorGUI.DisabledScope(chosenIndex == 0))
            {
                if (GUILayout.Button("Previous Clip"))
                {
                    chosenIndex--;
                }
            }
            using (new EditorGUI.DisabledScope(animationClips.Count == 0))
            {
                if (GUILayout.Button("Delete Clip"))
                {
                    animationClips.RemoveAt(chosenIndex);
                }
            }

            if (GUILayout.Button("Add Clip"))
            {
                if (animationClips.Count == 0 || chosenIndex == animationClips.Count) 
                {
                    animationClips.Add(new CustomAnimation.AnimationClip());
                }
				else 
                {
                    animationClips.Insert(chosenIndex+1, new CustomAnimation.AnimationClip());
                }
            }
        }

        animation.m_AnimEditorClipNumReference = chosenIndex;

        EditorGUILayout.Space();

        if (animationClips.Count > 0)
        {
            CustomAnimation.AnimationClip clip = animationClips[chosenIndex];

            clip.name = EditorGUILayout.TextField("Clip Name", clip.name);
            clip.animationTime = EditorGUILayout.FloatField("Animation Time", clip.animationTime);
            clip.movementCurve = EditorGUILayout.CurveField("Animation Curve" ,clip.movementCurve, GUILayout.MinHeight(40.0f), GUILayout.Height(40.0f));
            clip.hasEntranceAnimation = EditorGUILayout.Toggle("Has Entrance Animation", clip.hasEntranceAnimation);
            if (clip.hasEntranceAnimation)
            {
                clip.entranceAnimationName = EditorGUILayout.TextField("Entrance Animation Name", clip.entranceAnimationName);
                clip.entranceAnimationTime = EditorGUILayout.FloatField("Entrance animation time", clip.entranceAnimationTime);
                clip.entranceAnimationDelay = EditorGUILayout.FloatField("Entrance animation Delay", clip.entranceAnimationDelay);
            }
            clip.hasExitAnimation = EditorGUILayout.Toggle("Has Exit Animation", clip.hasExitAnimation);

            if (clip.hasExitAnimation)
            {
                clip.exitAnimationName = EditorGUILayout.TextField("Exit Animation Name", clip.exitAnimationName);
                clip.exitAnimationTime = EditorGUILayout.FloatField("Exit animation Time", clip.exitAnimationTime);
                clip.exitAnimationDelay = EditorGUILayout.FloatField("Exit animation Delay", clip.exitAnimationDelay);
            }
            clip.hasMovementAnimation = EditorGUILayout.Toggle("Has Movement Animation", clip.hasMovementAnimation);
            if (clip.hasMovementAnimation)
            {
                using (new EditorGUI.DisabledGroupScope(animation.GetAnimatingObject == null))
                {
                    if (GUILayout.Button("SetInitialPosition"))
                    {
                        clip.startPos = animation.GetAnimatingObject.position;
                        clip.startAng = animation.GetAnimatingObject.rotation;
                    }
                    if (GUILayout.Button("SetFinalPosition"))
                    {
                        clip.endPos = animation.GetAnimatingObject.position;
                        clip.endAng = animation.GetAnimatingObject.rotation;
                    }
                }
            }

            using (var check = new EditorGUI.ChangeCheckScope()) 
            {
                m_testAnimationPosition = EditorGUILayout.Slider("Animation position", m_testAnimationPosition, 0.0f, clip.animationTime);
                if (check.changed) 
                {
                    animation.ManualSetAnim(clip, m_testAnimationPosition);
                }
            }
        }
    }

    void OnEnable()
    {
        animation = (CustomAnimation) target;
    }
}
