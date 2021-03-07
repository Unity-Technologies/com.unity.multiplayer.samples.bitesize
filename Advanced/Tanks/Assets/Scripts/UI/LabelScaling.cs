using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Tanks.UI
{
	/// <summary>
	/// A simple script that turns a text component off and then on again to ensure text is scaled correctly.
	/// </summary>
	[RequireComponent(typeof(Text))]
	public class LabelScaling : MonoBehaviour
	{
		protected Text m_Label;

		private bool m_IsWaitingToBeReEnabled = false;

		protected void OnRectTransformDimensionsChange()
		{
			UpdateLabel();
		}

		protected void UpdateLabel()
		{
			if (m_Label == null)
			{
				m_Label = GetComponent<Text>();
			}			

			Disable();
			m_IsWaitingToBeReEnabled = true;
		}

		protected void Enable()
		{
			m_Label.enabled = true;
		}

		protected void Disable()
		{
			m_Label.enabled = false;
		}

		private void Start()
		{
			if (HUDController.s_InstanceExists)
			{
				HUDController.s_Instance.enabledCanvas += HudEnabled;
			}
		}

		private void OnDestroy()
		{
			if (HUDController.s_InstanceExists)
			{
				HUDController.s_Instance.enabledCanvas -= HudEnabled;
			}
		}

		private void LateUpdate()
		{
			if (m_IsWaitingToBeReEnabled)
			{
				Enable();
				m_IsWaitingToBeReEnabled = false;
			}
		}

		private void HudEnabled(bool enabled)
		{
			if (enabled)
			{
				UpdateLabel();
			}
		}
	}
}