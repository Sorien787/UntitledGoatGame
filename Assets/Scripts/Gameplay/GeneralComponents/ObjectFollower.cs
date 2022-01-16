using UnityEngine;

public class ObjectFollower : MonoBehaviour, IPauseListener
{

    [SerializeField] private float m_fRotationFollowTime = 0.1f;
    [SerializeField] private float m_fPositionFollowTime = 0.1f;
    [SerializeField] private CowGameManager m_GameManager;

    private Transform m_TransformToFollow;
    private Transform m_CurrentTransform;
    private Quaternion m_RotationFollow;
    private Vector3 m_PositionFollow;

	public void Pause()
	{
        enabled = false;
	}

	public void Unpause()
	{
        enabled = true;
	}

	void Start()
    {
        m_CurrentTransform = transform;
        m_TransformToFollow = m_CurrentTransform.parent;
        m_CurrentTransform.parent = null;
        m_CurrentTransform.position = m_TransformToFollow.position;
        m_CurrentTransform.rotation = m_TransformToFollow.rotation;
        m_GameManager.AddToPauseUnpause(this);
    }

    // Update is called once per frame
    void Update()
    {
        m_CurrentTransform.position = Vector3.SmoothDamp(m_CurrentTransform.position, m_TransformToFollow.position, ref m_PositionFollow, m_fPositionFollowTime);
        m_CurrentTransform.rotation = UnityUtils.UnityUtils.SmoothDampQuat(m_CurrentTransform.rotation, m_TransformToFollow.rotation, ref m_RotationFollow, m_fRotationFollowTime);
    }
}
