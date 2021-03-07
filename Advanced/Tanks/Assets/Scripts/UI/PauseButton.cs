using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Tanks.UI
{
	/// <summary>
	/// Pause button which turns off for standalone platforms
	/// </summary>
	public class PauseButton : MonoBehaviour
	{
		protected InGameOptionsMenu m_Menu;

		[SerializeField]
		protected Button m_Button;

		protected void Awake()
		{
			#if UNITY_STANDALONE || UNITY_STANDALONE_OSX || UNITY_EDITOR
			if (m_Button != null)
			{
				m_Button.transform.localScale = Vector3.zero;
			}
			#endif
		}

		protected void LazyLoad()
		{
			if (m_Menu == null)
			{
				m_Menu = InGameOptionsMenu.s_Instance;
			}
		}

		public void ShowMenu()
		{
			LazyLoad();
			m_Menu.OnOptionsClicked();
		}
	}
}