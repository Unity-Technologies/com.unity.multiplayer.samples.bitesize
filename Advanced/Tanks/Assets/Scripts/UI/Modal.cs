using UnityEngine;

namespace Tanks.UI
{
	//Base class for all modals
	public class Modal : MonoBehaviour
	{
		[SerializeField]
		protected CanvasGroup m_CanvasGroup;

		public virtual void CloseModal()
		{
			gameObject.SetActive(false);
		}

		public virtual void Show()
		{
			gameObject.SetActive(true);
			EnableInteractivity();
		}

		protected virtual void EnableInteractivity()
		{
			if (m_CanvasGroup != null)
			{
				m_CanvasGroup.interactable = true;
			}
		}

		protected virtual void DisableInteractivity()
		{
			if (m_CanvasGroup != null)
			{
				m_CanvasGroup.interactable = false;
			}
		}
	}
}