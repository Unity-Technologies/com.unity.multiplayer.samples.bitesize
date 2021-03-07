using UnityEngine;
using Tanks.TankControllers;
using Tanks.Utilities;

namespace Tanks.UI
{
	public class InstructionSwap : MonoBehaviour 
	{
		//Parent node for mobile instruction text.
		[SerializeField]
		protected GameObject m_MobileInstructions;

		//Parent node for mouse/keyboard instruction text.
		[SerializeField]
		protected GameObject m_DesktopInstructions;

		protected void Start () 
		{
			//Set default method based on current platform.
			if(MobileUtilities.IsOnMobile())
			{
				ChangeInput(true);
			}
			else
			{
				ChangeInput(false);
			}

			//Subscribe to the input module to change active text on the fly if new input is received.
			TankInputModule.s_InputMethodChanged += ChangeInput;
		}

		//Toggle instruction sets based on whether we're using touch controls or not.
		private void ChangeInput(bool isTouch)
		{
			m_MobileInstructions.SetActive(isTouch);
			m_DesktopInstructions.SetActive(!isTouch);
		}

		protected virtual void OnDestroy()
		{
			TankInputModule.s_InputMethodChanged -= ChangeInput;
		}

	}
}
