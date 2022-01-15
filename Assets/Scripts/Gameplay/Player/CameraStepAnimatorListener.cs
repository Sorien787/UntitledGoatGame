using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraStepAnimatorListener : MonoBehaviour
{
    [SerializeField] private PlayerCameraComponent m_PlayerCamera;
    public void OnStep() 
    {
        m_PlayerCamera.OnStep();
    }
}
