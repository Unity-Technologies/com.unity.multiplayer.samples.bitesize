using UnityEngine;
using Tanks.Data;

namespace Tanks.UI
{
	//Handles colour select for skin
	public class SkinColourSelector : MonoBehaviour
	{
		[SerializeField]
		protected GameObject m_ColourSelectButton;

		[SerializeField]
		protected LobbyCustomization m_CustomizationScreen;

		[SerializeField]
		protected RouletteModal m_RouletteModal;

		[SerializeField]
		protected Transform m_ContentChild;

		//Check available colours
		protected virtual void OnEnable()
		{
			RefreshAvailableColours();
		}

		protected virtual void OnDisable()
		{
			Clear();
		}

		public void Clear()
		{
			for (int i = 0; i < m_ContentChild.childCount; i++)
			{
				Destroy(m_ContentChild.GetChild(i).gameObject);
			}
		}

		//Creates the colour buttons for the available options - clears current UI elements
		public void RefreshAvailableColours()
		{
			Clear();
				
			int currentDecoration = m_CustomizationScreen.GetCurrentPreviewDecoration();
			int materialCount = TankDecorationLibrary.s_Instance.GetMaterialQuantityForIndex(currentDecoration);

			if (materialCount > 0)
			{
				SkinColour[] colourButtons = new SkinColour[materialCount];

				for (int i = 0; i < materialCount; i++)
				{
					GameObject button = (GameObject)Instantiate(m_ColourSelectButton, Vector3.zero, Quaternion.identity);
					button.transform.SetParent(m_ContentChild, false);
					colourButtons[i] = button.GetComponent<SkinColour>();
				}
					
				for (int i = 0; i < colourButtons.Length; i++)
				{
					SkinColour skinColour = colourButtons[i];

					skinColour.gameObject.SetActive(true);
					skinColour.SetupColourSelect(this, i);
					skinColour.SetupSkinColour(TankDecorationLibrary.s_Instance.GetMaterialForDecoration(currentDecoration, i));

					bool colourTempUnlocked = DailyUnlockManager.s_Instance.IsItemTempUnlocked(TankDecorationLibrary.s_Instance.GetDecorationForIndex(currentDecoration).id) && (DailyUnlockManager.s_Instance.GetTempUnlockedColour() == i);

					skinColour.SetUnlockedStatus(PlayerDataManager.s_Instance.IsColourUnlockedForDecoration(currentDecoration, i) || colourTempUnlocked);
				}
			}
			else
			{
				gameObject.SetActive(false);
			}
		}
		
		//Handles colour change
		public void ChangeColourIndex(int newIndex)
		{
			m_CustomizationScreen.ChangeCurrentDecorationColour(newIndex);
		}

		//Opens roulette modal
		public void OpenRoulette(int skinColourIndex)
		{
			m_RouletteModal.Show(m_CustomizationScreen.GetCurrentPreviewDecoration(), skinColourIndex);
		}
	}
}
