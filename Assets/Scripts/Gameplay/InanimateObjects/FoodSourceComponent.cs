using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[RequireComponent(typeof(LocalDebugData))]
[RequireComponent(typeof(HealthComponent))]
public class FoodSourceComponent : MonoBehaviour, IPauseListener
{
    [SerializeField] private AnimationCurve m_RegenerationRateByCurrentHealth = default;

    [SerializeField] private float m_RegenerationRateScalar = 1.0f;

    [SerializeField] private float m_fHealthThresholdForEaten = default;

    [SerializeField] private float m_fHealthThresholdForReadyForEating = default;

    [SerializeField] private float m_fFoodSizeChangeTime = default;

    [SerializeField] private CowGameManager m_Manager = default;

    [SerializeField] private EntityTypeComponent m_EntityInformation = default;

    [SerializeField] private DebugTextComponent m_DebugText;

    private float m_fCurrentFoodSize = 1.0f;
    private HealthComponent m_HealthComponent = default;
    private float m_fFoodSizeChangeVelocity = 0.0f;
    private enum FoodStatus 
    {
        ReadyToEat,
        Growing
    }

    private FoodStatus m_CurrentFoodStatus = FoodStatus.Growing;

    // Update is called once per frame
    private void Awake()
    {
        m_HealthComponent = GetComponent<HealthComponent>();
        m_Manager.AddToPauseUnpause(this);
    }   
    public void Pause()
    {
        enabled = false;
    }

    public void Unpause()
    {
        enabled = true;
    }

    private void OnDestroy()
    {
        m_Manager.RemoveFromPauseUnpause(this);
    }

    void Update()
    {
        if (!m_HealthComponent)
            m_HealthComponent = GetComponent<HealthComponent>();
        float regenerationRatePerSecond = m_RegenerationRateByCurrentHealth.Evaluate(m_HealthComponent.GetCurrentHealthPercentage) * m_RegenerationRateScalar;
        m_HealthComponent.ReplenishHealth(regenerationRatePerSecond * Time.deltaTime);
        m_fCurrentFoodSize = Mathf.SmoothDamp(m_fCurrentFoodSize, m_HealthComponent.GetCurrentHealthPercentage, ref m_fFoodSizeChangeVelocity, m_fFoodSizeChangeTime);
        m_Listeners.ForEachListener((IFoodSourceSizeListener listener) => listener.OnSetFoodSize(m_fCurrentFoodSize));

        if (m_HealthComponent.GetCurrentHealthPercentage < m_fHealthThresholdForEaten && m_CurrentFoodStatus == FoodStatus.ReadyToEat)
        {
            m_CurrentFoodStatus = FoodStatus.Growing;
            m_EntityInformation.RemoveFromTrackable();
        }
        else if (m_HealthComponent.GetCurrentHealthPercentage > m_fHealthThresholdForReadyForEating && m_CurrentFoodStatus == FoodStatus.Growing)
        {
            m_CurrentFoodStatus = FoodStatus.ReadyToEat;
            m_EntityInformation.AddToTrackable();
        }
#if UNITY_EDITOR
        if (m_DebugText) 
        {
            m_DebugText.AddLine(string.Format("Food Name: {0}", gameObject.name));
            m_DebugText.AddLine(string.Format("Current Food Health: {0}", m_HealthComponent.GetCurrentHealthPercentage));
            m_DebugText.AddLine(string.Format("Food Status: {0}", m_CurrentFoodStatus.Equals(FoodStatus.Growing) ? "Growing" : "Ready To Eat"));
            m_DebugText.AddLine(string.Format("Current Food Growth Rate per Second: {0}", regenerationRatePerSecond));
        }
#endif
	}

    UnityUtils.ListenerSet<IFoodSourceSizeListener> m_Listeners = new UnityUtils.ListenerSet<IFoodSourceSizeListener>();

    public void AddListener(in IFoodSourceSizeListener listener) { m_Listeners.Add(listener); }
}

public interface IFoodSourceSizeListener 
{
    void OnSetFoodSize(float foodSize);
}
