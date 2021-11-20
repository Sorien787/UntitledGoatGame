using UnityEngine;

public class CameraComponent : MonoBehaviour
{
    [SerializeField]
    private CowGameManager m_Manager;

    void Awake()
    {
        m_Manager.RegisterCamera(transform);
    }
}
