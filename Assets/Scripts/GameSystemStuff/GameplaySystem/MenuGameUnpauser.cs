﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuGameUnpauser : MonoBehaviour
{
	[SerializeField] private CowGameManager m_Manager;
	// Start is called before the first frame update

	private void Awake()
	{
		m_Manager.MenuLoaded();
	}

	void Start()
    {


		m_Manager.SetPausedState(false);
		
		Cursor.lockState = CursorLockMode.None;

		m_Manager.InMenuStarted();
	}


}
