using UnityEngine;
using System.Collections;
using Tanks.Utilities;
using UnityEngine.UI;

namespace Tanks.UI
{
	/// <summary>
	/// This class controls a generic modal object used for generic status popups in the UI.
	/// </summary>
	public class AnnouncerModal : Singleton<AnnouncerModal>
	{
		[SerializeField]
		protected Text m_Body, m_Heading;

		protected override void Awake()
		{
			base.Awake();
			gameObject.SetActive(false);
		}

		public void Show(string heading, string body)
		{
			gameObject.SetActive(true);
			if (this.m_Body != null)
			{
				this.m_Body.text = body;
			}

			if (this.m_Heading != null)
			{
				this.m_Heading.text = heading;
			}	
		}

		public void Hide()
		{
			gameObject.SetActive(false);
		}
	}
}