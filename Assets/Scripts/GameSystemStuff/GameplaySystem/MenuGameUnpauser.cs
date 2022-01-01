using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuGameUnpauser : MonoBehaviour
{
	[SerializeField] private CowGameManager m_Manager;
    // Start is called before the first frame update
    void Start()
    {
		m_Manager.SetPausedState(false);
		
		Cursor.lockState = CursorLockMode.Confined;

		m_Manager.InMenuStarted();
	}


}
