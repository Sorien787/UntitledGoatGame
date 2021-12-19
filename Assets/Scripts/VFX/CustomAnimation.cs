using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class CustomAnimation : MonoBehaviour
{
	[SerializeField] private Transform m_AnimatingObject;

	[SerializeField] private Animator m_AnimationController;

	[SerializeField] private List<AnimationClip> m_AnimationsList = new List<AnimationClip>();

	public event Action OnAnimationsComplete;

	public event Action<int, AnimationClip, AnimationClip> OnAnimationComplete;
	[SerializeField]
	[HideInInspector]
	public int m_AnimEditorClipNumReference;
	public ref List<AnimationClip> GetGameraAnimationClip => ref m_AnimationsList;

	private int m_CurrentClipNum = 0;
	private float m_CurrentAnimTime = 0.0f;
	
	public void StartAnimation() 
	{
		StartCoroutine(AnimateRoutine());
	}

	public void ManualSetAnim(in AnimationClip clip, in float currentTime) 
	{
		SetAnimViaClipTime(clip, currentTime);
	}

	public void AddOnTransitionOutCallbackToClip(in int clipNum, in Action<AnimationClip> callback) 
	{
		m_AnimationsList[clipNum].onEnterExitAnimation += callback;
	}

	public void AddOnTransitionInCompleteCallbackToClip(in int clipNum, in Action<AnimationClip> callback) 
	{
		m_AnimationsList[clipNum].onLeaveEntranceAnimation += callback;
	}

	public void AddClipStartedCallbackToClip(in int clipNum, in Action<AnimationClip> callback) 
	{
		m_AnimationsList[clipNum].onClipStarted += callback;
	}

	private void SetAnimViaClipTime(in AnimationClip currentClip, in float currentTime) 
	{
	
		if (currentClip.hasEntranceAnimation)
		{
			if (currentClip.entranceAnimationTime > currentTime - currentClip.entranceAnimationDelay) 
			{
				float currentEntranceAnimTimeNormalized = Mathf.Clamp01((currentTime - currentClip.entranceAnimationDelay) / currentClip.entranceAnimationTime);
				m_AnimationController.Play(currentClip.entranceAnimationName, 0, currentEntranceAnimTimeNormalized);
			}
			else if (!m_bHasCalledCompletedEntranceAnim) 
			{
				m_bHasCalledCompletedEntranceAnim = true;
				currentClip.onLeaveEntranceAnimation?.Invoke(currentClip);
			}
		}

		if (currentClip.hasExitAnimation)
		{
			if (currentClip.animationTime - currentTime < currentClip.exitAnimationTime + currentClip.exitAnimationDelay) 
			{
				float currentExitAnimTimeNormalized = Mathf.Clamp01((currentClip.exitAnimationTime - (currentClip.animationTime - currentTime - currentClip.exitAnimationDelay)) / currentClip.exitAnimationTime);
				m_AnimationController.Play(currentClip.exitAnimationName, 0, currentExitAnimTimeNormalized);
			}
			if (!m_bHasCalledStartedExitAnimation) 
			{
				m_bHasCalledStartedExitAnimation = true;
				currentClip.onEnterExitAnimation?.Invoke(currentClip);
			}
		}


		if (currentClip.hasMovementAnimation)
		{
			float timeToPositionalTime = currentClip.movementCurve.Evaluate(currentTime / currentClip.animationTime);
			m_AnimatingObject.position = currentClip.GetCurrentPosition(timeToPositionalTime);
			m_AnimatingObject.rotation = currentClip.GetCurrentRotation(timeToPositionalTime);
		}

	}
	bool m_bHasCalledCompletedEntranceAnim = false;
	bool m_bHasCalledStartedExitAnimation = false;
	bool m_bHasPlayedIntroAnim = false;
	private IEnumerator AnimateRoutine() 
	{
		m_CurrentClipNum = 0;
		m_CurrentAnimTime = 0.0f;

		while (m_CurrentClipNum < m_AnimationsList.Count)
		{
			AnimationClip currentClip = m_AnimationsList[m_CurrentClipNum];
			if (!m_bHasPlayedIntroAnim)
			{
				currentClip.onClipStarted?.Invoke(currentClip);
				m_bHasPlayedIntroAnim = true;
			}

			SetAnimViaClipTime(currentClip, m_CurrentAnimTime);

			if (m_CurrentAnimTime > currentClip.animationTime) 
			{
				m_CurrentAnimTime -= currentClip.animationTime;
				m_bHasCalledCompletedEntranceAnim = false;
				m_bHasCalledStartedExitAnimation = false;
				m_bHasPlayedIntroAnim = false;
				m_CurrentClipNum++;
			}
			m_CurrentAnimTime += Time.deltaTime;
			yield return null;
		}
		OnAnimationsComplete?.Invoke();
	}

	public ref Transform GetAnimatingObject => ref m_AnimatingObject;

	[Serializable]
	public class AnimationClip
	{
		public string name = "New Clip";
		public float animationTime = 0f;
		public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
		public Vector3 startPos = Vector3.zero;
		public Quaternion startAng = Quaternion.identity;
		public Vector3 endPos = Vector3.zero;
		public Quaternion endAng = Quaternion.identity;
		public bool hasMovementAnimation = true;
		public bool hasEntranceAnimation = false;
		public bool hasExitAnimation = false;
		public Action<AnimationClip> onEnterExitAnimation = null;
		public float entranceAnimationTime = 0.0f;
		public float entranceAnimationDelay = 0.0f;
		public string entranceAnimationName = "";
		public Action<AnimationClip> onLeaveEntranceAnimation;
		public float exitAnimationDelay = 0.0f;
		public float exitAnimationTime = 0.0f;
		public string exitAnimationName = "";
		public Action<AnimationClip> onClipStarted;

		public Vector3 GetCurrentPosition(float currentAnimTime) 
		{
			return Vector3.Lerp(startPos, endPos, currentAnimTime / animationTime);
		}
		public Quaternion GetCurrentRotation(float currentAnimTime) 
		{
			return Quaternion.Lerp(startAng, endAng, currentAnimTime / animationTime);
		}
	}
}
