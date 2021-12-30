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

	public event Action<ControlBinding, KeyCode> OnAttemptSetBinding;
	private KeyCode[] values;
	private ControlBinding m_binding;
	private Color32 m_normal;
	private Color32 m_duplicated;

	public void InitializeUI(ControlBinding binding, Color32 normal, Color32 duplicated)
	{
		m_binding = binding;
		m_normal = normal;
		m_duplicated = duplicated;
		m_BindingName.text = binding.GetBindingDisplayName;
		m_BindingKeyString.text = binding.KeyCode.ToString();
	}

	public void UpdateUI()
	{
		m_BindingBackgroundImage.color = m_binding.IsDuplicated ? m_duplicated  : m_normal;
	}

	bool m_bIsChangingKeycode = false;

	public void OnClickToChangeKeycode()
	{
		if (m_bIsChangingKeycode)
			return;
		m_bIsChangingKeycode = true;
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
		m_BindingKeyString.text = " ";
		while (true)
		{
			switchTime += Time.deltaTime;
			if (switchTime > 0.5f)
			{
				isShowingUnderscore = !isShowingUnderscore;
				m_BindingKeyString.text = isShowingUnderscore ? "____" : " ";			
				switchTime = 0.0f;
			}

			if (Input.GetKey(KeyCode.Escape))
			{
				m_BindingKeyString.text = oldString;
				m_bIsChangingKeycode = false;
				yield break;
			}
			for (int i = 0; i < values.Length; i++)
			{
				if (Input.GetKey(values[i]))
				{
					OnAttemptSetBinding(m_binding, values[i]);
					m_BindingKeyString.text = values[i].ToString();
					isShowingUnderscore = false;
					m_bIsChangingKeycode = false;
					yield break;
				}
			}
			yield return null;
		}
	}
}
