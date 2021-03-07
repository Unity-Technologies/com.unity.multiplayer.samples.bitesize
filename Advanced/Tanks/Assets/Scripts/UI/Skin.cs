using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Tanks.Data;

namespace Tanks.UI
{
	//The representation of a decoration
	public class Skin : MonoBehaviour
	{
		//UI references
		[SerializeField]
		protected Button m_Preview;

		[SerializeField]
		protected Button m_Locked;

		[SerializeField]
		protected Text m_NameText;

		//Pop-up prompt
		[SerializeField]
		protected string m_SrExplanationPrompt = "Enter the Shooting Range to unlock a decoration.";

		//Cache references
		private SkinSelect m_SkinSelect;

		private int m_Index = -1;

		public UnityEvent previewButtonPressed
		{
			get
			{
				if (m_Preview == null)
				{
					return null;
				}

				return m_Preview.onClick;
			}
		}

		public UnityEvent lockButtonPressed
		{
			get
			{
				if (m_Locked == null)
				{
					return null;
				}

				return m_Locked.onClick;
			}
		}
		
		//Cache reference to SkinSelect and add EventListeners
		public void SetupSkinSelect(SkinSelect skinSelect)
		{
			this.m_SkinSelect = skinSelect;
			previewButtonPressed.AddListener(ApplySkinToPreview);
			lockButtonPressed.AddListener(SelectLockedSkin);
		}

		//Make the Skin have knowledge of its index and definition
		public void SetupSkin(int index, TankDecorationDefinition definition)
		{
			this.m_Index = index;
		}

		//Setup preview sprite
		public void SetPreview(Sprite previewSprite)
		{
			if (previewSprite == null)
			{
				m_Preview.image.color = Color.clear;
			}
			else
			{
				m_Preview.image.sprite = previewSprite;
			}
		}

		//Set the name
		public void SetNameText(string text)
		{
			m_NameText.gameObject.SetActive(true);
			m_NameText.text = text;
		}
		
		//Setup based whether the decoration is locked
		public void SetUnlockedStatus(bool isUnlocked)
		{
			m_Locked.gameObject.SetActive(!isUnlocked);
			m_Preview.interactable = isUnlocked;
		}

		//Called if a decoration is locked
		public void SelectLockedSkin()
		{
			MainMenuUI.s_Instance.ShowInfoPopup(m_SrExplanationPrompt, null);
		}

		//Place decoration on preview
		public void ApplySkinToPreview()
		{
			m_SkinSelect.customization.ChangeDecoration(m_Index);
			m_SkinSelect.customization.RefreshColourSelector();
			m_SkinSelect.selectionModal.CloseModal();
		}
	}
}
