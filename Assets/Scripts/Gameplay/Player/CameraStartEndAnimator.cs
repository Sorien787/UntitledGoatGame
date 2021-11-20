using UnityEngine;
using System;

[RequireComponent(typeof(ObjectPositionAnimator))]
public class CameraStartEndAnimator : MonoBehaviour
{
    private ObjectPositionAnimator m_Animator;
    [SerializeField] private Transform m_StartEndTransform;
    [SerializeField] private Transform m_ThisTransform;
    [SerializeField] private Transform m_DefaultCameraTransform;
    [SerializeField] private Transform m_CurrentTransform = null;

    [SerializeField] private float m_AnimDuration;
    [SerializeField] private AnimationCurve m_PositionAnimCurveSlow;
    [SerializeField] private AnimationCurve m_RotationAnimCurveSlow;
    [SerializeField] private AnimationCurve m_PositionAnimCurveQuick;
    [SerializeField] private AnimationCurve m_RotationAnimCurveQuick;

    // Start is called before the first frame update
    void Awake()
    {
        m_Animator = GetComponent<ObjectPositionAnimator>();
        m_CurrentTransform = GetComponent<Transform>();
    }

    public void AnimateOut() 
    {
        m_Animator.OnAnimComplete = null;
        m_CurrentTransform.SetParent(null);
       
        m_Animator.StartAnimatingPositionAndRotation(m_AnimDuration);
        m_Animator.OnAnimComplete += OnAnimOutFinished;
    }

    public void OnInstantStartLevel() 
    {
        m_CurrentTransform.SetParent(m_DefaultCameraTransform);
        m_CurrentTransform.localPosition = Vector3.zero;
        m_CurrentTransform.localRotation = Quaternion.identity;
    }

    private void OnAnimOutFinished() 
    {
        m_Animator.OnAnimComplete -= OnAnimOutFinished;
        m_CurrentTransform.SetParent(m_StartEndTransform);
    }

    public void AddOnCompleteCallback(Action OnCompleteAction) 
    {
        m_Animator.OnAnimComplete += OnCompleteAction;
    }

    public void AnimateIn(in float animDuration)
    {
        m_Animator.OnAnimComplete = null;
        m_CurrentTransform.SetParent(null);
        m_Animator.SetPositionCurves(m_PositionAnimCurveSlow, m_RotationAnimCurveSlow, m_CurrentTransform, m_DefaultCameraTransform);
        m_Animator.StartAnimatingPositionAndRotation(animDuration);
        m_Animator.OnAnimComplete += OnAnimInFinished;
    }

    public void AnimateInQuick() 
    {

    }

    private void OnAnimInFinished() 
    {
        m_Animator.OnAnimComplete -= OnAnimInFinished;
        m_CurrentTransform.SetParent(m_DefaultCameraTransform);
    }
}
