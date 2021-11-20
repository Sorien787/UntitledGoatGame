using System.Collections;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;

public class KeyboardBindingUI : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI m_BindingName;
	[SerializeField] private TextMeshProUGUI m_BindingKeyString;
	[SerializeField] private Image m_BindingBackgroundImage;

	public event Action<KeyCode> OnAttemptSetBinding;
	private KeyCode[] values;

	public void UpdateUI(ControlBinding binding, Color32 normal, Color32 duplicated)
	{
		m_BindingName.name = binding.GetBindingDisplayName;
		m_BindingKeyString.name = binding.KeyCode.ToString();
		m_BindingBackgroundImage.color = binding.IsDuplicated ? normal : duplicated;
	}

	public void OnClickToChangeKeycode()
	{
		values = (KeyCode[])Enum.GetValues(typeof(KeyCode));
		StartCoroutine(WaitingForInput());
	}

	float switchTime = 0.0f;
	bool isShowingUnderscore = false;


	private IEnumerator WaitingForInput()
	{
		isShowingUnderscore = false;
		switchTime = 0.0f;
		string oldString = m_BindingKeyString.name;
		while (true)
		{
			switchTime += Time.deltaTime;

			if (switchTime > 0.5f)
			{
				m_BindingKeyString.name = isShowingUnderscore ? "_" : " ";
				isShowingUnderscore = !isShowingUnderscore;
			}

			if (Input.GetKey(KeyCode.Escape))
			{
				m_BindingKeyString.name = oldString;
				break;
			}
			for (int i = 0; i < values.Length; i++)
			{
				if (Input.GetKey(values[i]))
				{
					OnAttemptSetBinding(values[i]);
					break;
				}
			}
			yield return null;
		}
	}
}
