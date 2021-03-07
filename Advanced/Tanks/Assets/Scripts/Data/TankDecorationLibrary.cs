using UnityEngine;
using Tanks.Extensions;
using System.Collections.Generic;
using System;
using Tanks.Utilities;

namespace Tanks.Data
{
	//Struct that defines individual decorations. Populated in editor via the TankDecorationLibrary monobehaviour.
	[Serializable]
	public struct TankDecorationDefinition
	{
		//Unique ID for internal bookkeeping and save data.
		public string id;

		//Displayed name for this decoration.
		public string name;

		//Prefab to be instantiated for this decoration.
		public Decoration decorationPrefab;

		//Sprite used to represent this decoration in buttons, etc.
		public Sprite preview;

		//A list of materials that can be selected to colour/pattern this decoration.
		public Material[] availableMaterials;

		//Weighting to determine how rare this decoration is in the Shooting Range.
		public int selectionWeighting;

		//Texture to be displayed on a crate containing this decoration in the Shooting Range.
		public Texture2D crateDecal;
	}

	//The tank decoration library is a persistent singleton that provides the means to populate decoration data in editor, and accessor functions to easily retrieve it from any class that needs it.
	public class TankDecorationLibrary : PersistentSingleton<TankDecorationLibrary>
	{
		//An array of TankDecorationDefinitions. These determine which decorations are available in the game and their properties.
		[SerializeField]
		private TankDecorationDefinition[] m_TankDecorations;

		// Reusable temp list for selection
		List<TankDecorationDefinition> m_TempList = new List<TankDecorationDefinition>();

		protected override void Awake()
		{
			base.Awake();

			if (m_TankDecorations.Length == 0)
			{
				Debug.Log("<color=red>WARNING: No tank decorations have been defined in the Decorations Library!</color>");
			}
		}

		//Returns decoration data for a given array index. Displays a helpful error if requested index exceeds bounds.
		public TankDecorationDefinition GetDecorationForIndex(int index)
		{
			if (index >= m_TankDecorations.Length)
			{
				Debug.Log("<color=red>WARNING: Requested index exceeds Decorations Library bounds!</color>");
			}

			return m_TankDecorations[index];
		}

		//Returns a random decoration that has not yet been unlocked by the player, using a weighting system to make certain decorations rarer.
		//Decorations are only awarded one colour/material at a time, so weighting is adjusted based on how many materials are available to unlock for each decoration base.
		public TankDecorationDefinition SelectRandomLockedDecoration()
		{
			if (m_TempList == null)
			{
				m_TempList = new List<TankDecorationDefinition>();
			}
			else
			{
				m_TempList.Clear();
			}

			int weightTotal = 0;
			int librarySize = GetNumberOfDefinitions();

			PlayerDataManager playerdata = PlayerDataManager.s_Instance;

			for (int i = 0; i < librarySize; i++)
			{
				if (playerdata.DecorationHasLockedColours(i))
				{
					TankDecorationDefinition nextItem = TankDecorationLibrary.s_Instance.GetDecorationForIndex(i);

					m_TempList.Add(nextItem);
					weightTotal += (nextItem.selectionWeighting * nextItem.availableMaterials.Length);
				}
			}
			return m_TempList.WeightedSelection(weightTotal, d => (d.selectionWeighting * d.availableMaterials.Length));
		}

		//Returns the array index for a given TankDecorationDefinition.
		public int GetIndexForDecoration(TankDecorationDefinition item)
		{
			for (int i = 0; i < m_TankDecorations.Length; i++)
			{
				if (m_TankDecorations[i].id == item.id)
				{
					return i;
				}
			}

			return -1;
		}

		//Returns the number of decorations defined in this library.
		public int GetNumberOfDefinitions()
		{
			return m_TankDecorations.Length;
		}

		//Returns the number of materials defined in the decoration definition at the given index.
		public int GetMaterialQuantityForIndex(int index)
		{
			return m_TankDecorations[index].availableMaterials.Length;
		}

		//Returns a material reference for the material index of the decoration at the given index.
		public Material GetMaterialForDecoration(int decorationIndex, int materialIndex)
		{
			return m_TankDecorations[decorationIndex].availableMaterials[materialIndex];
		}
	}
}
