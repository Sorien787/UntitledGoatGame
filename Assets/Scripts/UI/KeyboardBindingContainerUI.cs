using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyboardBindingContainerUI : MonoBehaviour
{
	[SerializeField] private SettingsManager m_BindingsContainer;
	[SerializeField] private Transform m_Transform;
	[SerializeField] private GameObject m_KeyboardBindingPrefab;

	[SerializeField] private Color32 m_DuplicatedColor;
	[SerializeField] private Color32 m_NonDuplicatedColor;

	private void Awake()
	{
		m_BindingsContainer.ForEachControlBinding((ControlBinding binding) =>
		{
			GameObject go = Instantiate(m_KeyboardBindingPrefab, m_Transform);
			KeyboardBindingUI bindingUI = go.GetComponent<KeyboardBindingUI>();
			bindingUI.UpdateUI(binding, m_NonDuplicatedColor, m_DuplicatedColor);
			binding.OnControlBindingChanged += () => bindingUI.UpdateUI(binding, m_NonDuplicatedColor, m_DuplicatedColor);
			// when we want to set the binding, we should check that the new binding does not conflict with others.
			// if so, they need to be marked as duplicate.
			bindingUI.OnAttemptSetBinding += (KeyCode desiredKeyCode) => OnBindingAttemptedForBindingUI(binding, desiredKeyCode);
		});
	}

	private void OnBindingAttemptedForBindingUI(ControlBinding changedBinding, KeyCode desiredKeyCode)
	{
		bool newBindingDuplicated = false;
		List<ControlBinding> duplicatedBindingsPreviously = new List<ControlBinding>();
		// iterate through, find out if there's a duplicate. If so, then mark BOTH as red.
		m_BindingsContainer.ForEachControlBinding((ControlBinding binding) =>
		{
			// we're only concerned with bindings that arent the one we're changing
			if (binding != changedBinding)
			{
				if (binding.KeyCode == desiredKeyCode)
				{
					newBindingDuplicated = true;
					changedBinding.IsDuplicated = true;
				}
				else if (binding.KeyCode == changedBinding.KeyCode)
				{
					duplicatedBindingsPreviously.Add(binding);
				}
			}
		});
		changedBinding.IsDuplicated = newBindingDuplicated;
		// we've changed a keycode and there was only one other duplicate - 
		if (duplicatedBindingsPreviously.Count == 1)
		{
			duplicatedBindingsPreviously[0].IsDuplicated = false;
		}

		changedBinding.KeyCode = desiredKeyCode;
	}

	private void OnDestroy()
	{
		m_BindingsContainer.ForEachControlBinding((ControlBinding binding) =>
		{
			binding.ClearOnChangeCallback();
		});
	}
}
