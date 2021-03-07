using UnityEngine;
using Tanks.Data;
using UnityEngine.UI;

namespace Tanks.UI
{
	//Handles selecting a decoration
	public class SkinSelect : MonoBehaviour
	{
		[SerializeField]
		protected Skin m_SkinPrefab;

		[SerializeField]
		protected RouletteModal m_RouletteModal;

		[SerializeField]
		protected Modal m_SelectionModal;

		[SerializeField]
		protected Button m_RouletteButton;

		[SerializeField]
		protected LobbyCustomization m_Customization;

		public RouletteModal rouletteModal
		{
			get
			{
				return m_RouletteModal;
			}
		}

		public Modal selectionModal
		{
			get
			{
				return m_SelectionModal;
			}
		}

		public LobbyCustomization customization
		{
			get
			{
				return m_Customization;
			}
		}

		public void Clear()
		{
			for (int i = 0; i < transform.childCount; i++)
			{
				Destroy(transform.GetChild(i).gameObject);
			}
		}

		protected virtual void OnEnable()
		{
			RegenerateItems();
		}

		protected virtual void OnDisable()
		{
			Clear();
		}
		
		//Clears current UI items and adds new UI items
		public void RegenerateItems()
		{
			m_RouletteButton.gameObject.SetActive(!PlayerDataManager.s_Instance.AreAllDecorationsUnlocked());

			Clear();

			int length = TankDecorationLibrary.s_Instance.GetNumberOfDefinitions();

			for (int i = 0; i < length; i++)
			{
				TankDecorationDefinition decoration = TankDecorationLibrary.s_Instance.GetDecorationForIndex(i);
				GameObject skinObject = Instantiate<GameObject>(m_SkinPrefab.gameObject);
				skinObject.transform.SetParent(gameObject.transform, false);
				Skin skin = skinObject.GetComponent<Skin>();
				skin.SetPreview(decoration.preview);

				if (decoration.preview == null)
				{
					skin.SetNameText(decoration.name);
				}

				//Checks if the Decoration is unlocked, either permanently or temporarily (by an add)
				bool isUnlocked = false;
				if (PlayerDataManager.s_InstanceExists)
				{
					isUnlocked |= PlayerDataManager.s_Instance.IsDecorationUnlocked(i);
				}
				if (DailyUnlockManager.s_InstanceExists)
				{
					isUnlocked |= DailyUnlockManager.s_Instance.IsItemTempUnlocked(decoration.id);
				}
				skin.SetUnlockedStatus(isUnlocked);
				skin.SetupSkinSelect(this);
				skin.SetupSkin(i, decoration);
			}
		}
	}
}
