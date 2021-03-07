using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using Tanks.Utilities;

namespace Tanks.Data
{

	//This struct defines all differentiating statistics and references for individual tank types.
	//These are populated in-editor via the Tank Library monobehaviour.
	[Serializable]
	public struct TankTypeDefinition
	{
		//Unique ID to reference tank internally
		public string id;

		//The displayed name of the tank
		public string name;

		//A short blurb describing the tank
		public string description;

		//Unique stats to customize tank handling and fire rate
		public float speed;
		public float turnRate;
		public float hitPoints;
		public float fireRate;

		//The display prefab to be instantiated to represent this tank in the menu and in-game
		public GameObject displayPrefab;

		//How much this tank costs to unlock in the game's internal currency.
		public int cost;

		//How many stars are displayed per stat for this tank in the customization screen.
		[HeaderAttribute("Selection Star Rating")]
		public int armourRating;
		public int speedRating;
		public int refireRating;
	}

	//The tank library is a persistent singleton that provides the means to populate tank data in-editor, and accessor functions to easily retrieve it from any class that needs it.
	public class TankLibrary:PersistentSingleton<TankLibrary>
	{
		//An array of TankTypeDefinitions. These determine which tanks are available in the game and their properties.
		[SerializeField]
		private TankTypeDefinition[] tankDefinitions;

		protected override void Awake()
		{
			base.Awake();

			if (tankDefinitions.Length == 0)
			{
				Debug.Log("<color=red>WARNING: No tanks have been defined in the Tank Library!</color>");
			}
		}

		//Returns the TankTypeDefinition for a given array index. Provides a helpful error if an invalid index is used.
		public TankTypeDefinition GetTankDataForIndex(int index)
		{
			if ((index < 0) || ((index + 1) > tankDefinitions.Length))
			{
				Debug.Log("<color=red>WARNING: Requested tank data index exceeds definition array bounds.</color>");
			}

			return tankDefinitions[index];
		}

		//Returns an array containing data for tanks that have been unlocked.
		public List<TankTypeDefinition> GetUnlockedTanks()
		{
			List<TankTypeDefinition> unlockedTanks = new List<TankTypeDefinition>();
			int length = tankDefinitions.Length;
			for (int i = 0; i < length; i++)
			{
				if (PlayerDataManager.s_Instance.IsTankUnlocked(i))
				{
					unlockedTanks.Add(tankDefinitions[i]);
				}
			}
			return unlockedTanks;
		}

		//Returns the quantity of tanks that have been unlocked.
		public int GetNumberOfUnlockedTanks()
		{
			return GetUnlockedTanks().Count;
		}

		//Returns how many tank definitions we have in our library array.
		public int GetNumberOfDefinitions()
		{
			return tankDefinitions.Length;
		}
	}
}
