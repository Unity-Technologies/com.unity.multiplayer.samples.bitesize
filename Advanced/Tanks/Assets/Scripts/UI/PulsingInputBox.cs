using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Tanks.UI
{
	//Element that pulses the text if no focus
	public class PulsingInputBox : PulsingUIGroup
	{
		[SerializeField]
		protected InputField m_Input;
		protected bool m_IsFocused = false;

		protected virtual void Update()
		{
			if (m_IsFocused)
			{
				if (!m_Input.isFocused)
				{
					m_IsFocused = false;
					StartPulse(Fade.In);
				}
			}
			else
			{
				if (m_Input.isFocused)
				{
					m_IsFocused = true;
					StopPulse();
				}
			}
		}
	}
}
