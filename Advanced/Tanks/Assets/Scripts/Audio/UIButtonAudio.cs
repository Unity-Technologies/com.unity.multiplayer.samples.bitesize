using UnityEngine;
using System.Collections;

namespace Tanks.Audio
{
	// This class is intended to be attached to all in-game buttons. It will call the UIAudioManager to play either the default button sound, or an override sound specified in inspector.
	public class UIButtonAudio : MonoBehaviour 
	{
		[SerializeField]
		protected AudioClip m_OverrideClip;

		//Assign this to the OnClick event of the button it's attached to.
		public void OnClick()
		{
			if(UIAudioManager.s_InstanceExists)
			{
				UIAudioManager.s_Instance.PlayButtonEffect(m_OverrideClip);
			}
		}
	}
}
