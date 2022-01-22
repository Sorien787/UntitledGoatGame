using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class EntityTypeComponent : MonoBehaviour
{
    [SerializeField] private EntityInformation m_EntityInformation;
    [SerializeField] private CowGameManager m_Manager;
    [SerializeField] private Transform m_TrackingTransform;
    [SerializeField] private float m_TrackableRadius = 0f;

    private bool m_bIsDead = false;
    public void MarkAsDead() 
    {
        m_bIsDead = true;
    }

    public bool IsDead => m_bIsDead;

    private UnityUtils.ListenerSet<IEntityTrackingListener> m_Listeners = new UnityUtils.ListenerSet<IEntityTrackingListener>();

    public EntityInformation GetEntityInformation => m_EntityInformation;

	private void Awake()
	{
        m_Manager.OnEntitySpawned(this);	
	}

    public Transform GetTrackingTransform => m_TrackingTransform;

    public float GetTrackableRadius => m_TrackableRadius;

	public void AddListener(in IEntityTrackingListener listener) 
    {
        m_Listeners.Add(listener);
    }

    public void RemoveListener(in IEntityTrackingListener listener)
    {
        m_Listeners.Remove(listener);
    }

    public void RemoveFromTrackable() 
    {
        m_Manager.OnEntityStopTracking(this);
        HashSet<IEntityTrackingListener> tempListeners = new HashSet<IEntityTrackingListener>(m_Listeners);
        foreach (var listener in tempListeners)
        {
            listener.OnTargetInvalidated();
        }
        m_Listeners.Clear();
    }

	public void OnRemovedFromGame() 
    {
        m_Manager.OnEntityKilled(this);
        HashSet<IEntityTrackingListener> tempListeners = new HashSet<IEntityTrackingListener>(m_Listeners);
        foreach(var listener in tempListeners) 
        {
            listener.OnTargetInvalidated();
        }
        m_Listeners.Clear();
    }

    public void AddToTrackable() 
    {
       m_Manager.OnEntityStartTracking(this);
    }

	public void OnDrawGizmosSelected()
	{
        Gizmos.DrawWireSphere(m_TrackingTransform.position, m_TrackableRadius);
	}
}

public interface IEntityTrackingListener 
{
    void OnTargetInvalidated();
}