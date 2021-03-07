using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Tanks.Data;

namespace Tanks.UI
{
	/// <summary>
	/// Skin colour.
	/// </summary>
	public class SkinColour : MonoBehaviour
	{
		//UI references
		[SerializeField]
		protected Button m_Preview;
		[SerializeField]
		protected Button m_Locked;
		[SerializeField]
		protected Image m_ColourSwatch;
		
		//The index in the colour list
		private int m_Index = -1;

		//The colour selector
		private SkinColourSelector m_ColourSelector;

		protected UnityEvent lockedButtonPressed
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

		//Cache the selector and add listeners
		public void SetupColourSelect(SkinColourSelector coloursSelect, int colourIndex)
		{
			m_ColourSelector = coloursSelect;
			m_Index = colourIndex;
			lockedButtonPressed.AddListener(GotoRoulette);
			previewButtonPressed.AddListener(ApplySkinToPreview);
		}

		//Set the colour
		public void SetupSkinColour(Material colourMaterial)
		{
			m_ColourSwatch.color = colourMaterial.color;
		}
		
		//Check if the colour is unlocked
		public void SetUnlockedStatus(bool isUnlocked)
		{
			m_Locked.gameObject.SetActive(!isUnlocked);
			m_Preview.interactable = isUnlocked;
		}

		//Opens the roulette
		public void GotoRoulette()
		{
			m_ColourSelector.OpenRoulette(m_Index);
		}
		
		//Sends the colour to preview
		public void ApplySkinToPreview()
		{
			m_ColourSelector.ChangeColourIndex(m_Index);
		}
	}
}
