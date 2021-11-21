using UnityEngine;

public class CameraComponent : MonoBehaviour
{
    [SerializeField] private CowGameManager m_Manager;
    [SerializeField] private Transform m_CamContainer;

    void Awake()
    {
        m_Manager.RegisterCamera(transform);
        m_Manager.RegisterInitialCameraContainerTransform(m_CamContainer);
    }
}
