using System.Collections;
using UnityEngine;
using System;


public class ObjectPositionAnimator : MonoBehaviour
{
    private Transform m_Transform;
    private void Awake()
    {
        m_Transform = GetComponent<Transform>();
    }

    public Action OnAnimComplete;

    private TransformAnimation positions = new TransformAnimation();

    public void SetPositionCurves(in AnimationCurve lincurve, in AnimationCurve rotcurve, in Transform lowVals, in Transform highVals)
    {
        positions.animationCurve = lincurve;
        positions.rotAnimationCurve = rotcurve;
        positions.lowVal = lowVals;
        positions.highVal = highVals;
    }

    public void StartAnimatingPositionAndRotation(in float duration) 
    {
        timePassed = 0.0f;
        totalTime = duration;
        StartCoroutine(AnimatePosition());
    }

    // Update is called once per frame
    private float timePassed;

    private float totalTime;

    private IEnumerator AnimatePosition() 
    {
        while (timePassed < totalTime)
        {
            float time = timePassed / totalTime;
			positions.Evaluate(time/4.0f, out Vector3 position, out Quaternion rotation);
			m_Transform.position = position;
            m_Transform.rotation = rotation;
            timePassed += Time.deltaTime;
            yield return null;
        }
        OnAnimComplete();
    }
}

public struct TransformAnimation 
{
    public AnimationCurve animationCurve;
    public AnimationCurve rotAnimationCurve;
    public Transform lowVal;
    public Transform highVal;
    public void Evaluate(in float time, out Vector3 pos, out Quaternion quat) 
    {
        pos = Vector3.Lerp(lowVal.position, highVal.position, animationCurve.Evaluate(time));
        quat = Quaternion.Lerp(lowVal.rotation, highVal.rotation, rotAnimationCurve.Evaluate(time));
    }
}