using UnityEngine;
using UnityEngine.UI;
using Tanks.Map;
using Tanks.Data;

namespace Tanks.UI
{
	/// <summary>
	/// Map select UI
	/// </summary>
	public class MapSelect : Select
	{
		[SerializeField]
		protected MapList m_MapList;

		[SerializeField]
		protected Image m_MapPreview;

		[SerializeField]
		protected Button m_UnlockButton, m_CreateButton;

		[SerializeField]
		protected Image m_BgImage;

		[SerializeField]
		protected Color m_SnowBgColour, m_DesertBgColour;

		[SerializeField]
		protected Text m_Description, m_MapNamePrompt, m_MapCostPrompt;

		[SerializeField]
		protected GameObject m_CostParent;

		[SerializeField]
		protected BuyModal m_BuyModal;

		private string m_MapName, m_MapId;
		private int m_MapCost;

		public MapDetails selectedMap
		{
			get
			{
				return m_MapList[m_CurrentIndex];
			}
		}


		private void Awake()
		{
			m_ListLength = m_MapList.Count;
			OnIndexChange();
		}

		private void OnEnable()
		{
			AssignByIndex();
		}

		public void OnPreviewClick()
		{
			m_Description.enabled = !m_Description.enabled;
		}

		protected override void AssignByIndex()
		{
			MapDetails details = m_MapList[m_CurrentIndex];

			m_MapPreview.sprite = details.image;
			m_Description.text = details.description;
			m_MapName = details.name;
			m_MapId = details.id;
			m_MapCost = details.unlockCost;

			m_MapNamePrompt.text = m_MapName.ToUpperInvariant();

			// Set BG
			if (m_BgImage != null)
			{
				m_BgImage.color = details.effectsGroup == MapEffectsGroup.Snow ? m_SnowBgColour : m_DesertBgColour;
			}

			//We determine whether a level should be displayed as locked. We assume it's unlocked by default.
			bool levelLocked = false;

			//If the map details specify that this is a locked level, we do a check vs. purchased maps and temp unlocks to determine whether it should remain so.
			if (details.isLocked)
			{
				levelLocked = (!PlayerDataManager.s_Instance.IsMapUnlocked(m_MapId) && (!DailyUnlockManager.s_Instance.IsItemTempUnlocked(details.id)));
			}

			if (m_CostParent != null)
			{
				m_CostParent.gameObject.SetActive(levelLocked);
				if (m_UnlockButton != null)
				{
					m_UnlockButton.gameObject.SetActive(levelLocked);
				}
				if (m_MapCostPrompt != null)
				{
					m_MapCostPrompt.text = m_MapCost.ToString();
				}
			}
			m_CreateButton.interactable = !levelLocked;
		}

		public void OnUnlockClicked()
		{
			m_BuyModal.OpenBuyModal(m_MapName, m_MapCost, OnMapBuy, OnMapUnlock);
		}

		private void OnMapBuy()
		{
			PlayerDataManager.s_Instance.SetMapUnlocked(m_MapId);
			AssignByIndex();
		}

		private void OnMapUnlock()
		{
			DailyUnlockManager.s_Instance.SetDailyUnlock(m_MapId);
			AssignByIndex();
		}
	}
}