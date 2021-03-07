using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using Tanks.Utilities;

namespace Tanks.Data
{
    
	//This struct is used to define special multiplayer ammunition in the game. This ammo is collected through pickups.
	//These are populated in-editor via the SpecialProjectileLibrary monobehaviour.
	[Serializable]
	public struct ProjectileDefinition
	{
		//Unique ID to reference projectile internally
		public string id;

		//Display name for projectile
		public string name;

		//The icon used to represent the projectile on the HUD
		public Sprite weaponIcon;

		//The projectile prefab to be fired
		public Rigidbody projectilePrefab;

		//The colour of the HUD overlay when this projectile is active
		public Color weaponColor;

		//Sound pool to play from when this projectile is fired
		public AudioClip[] fireSound;
	}

	//The special projectile library is a persistent singleton that provides the means to populate special powerup ammo in editor, and accessor functions to easily retrieve it from any class that needs it.
	public class SpecialProjectileLibrary:PersistentSingleton<SpecialProjectileLibrary>
	{
		//An array of ProjectileDefinitions. These determine which special ammo is available in the game and its properties.
		[SerializeField]
		private ProjectileDefinition[] shellDefinitions;

		protected override void Awake()
		{
			base.Awake();

			if (shellDefinitions.Length == 0)
			{
				Debug.Log("<color=red>WARNING: No special projectiles have been defined in the Special Projectile Library!</color>");
			}
		}

		//Returns projectile data for a given array index. Displays a helpful error if requested index exceeds bounds.
		public ProjectileDefinition GetProjectileDataForIndex(int index)
		{
			if ((index < 0) || ((index + 1) > shellDefinitions.Length))
			{
				Debug.Log("<color=red>WARNING: Requested projectile data index exceeds definition array bounds.</color>");
			}

			return shellDefinitions[index];
		}

		//Helper function to return a random firing sound for the given projectile index.
		public AudioClip GetFireSoundForIndex(int index)
		{
			int soundRange = shellDefinitions[index].fireSound.Length;

			return shellDefinitions[index].fireSound[Random.Range(0, soundRange)];
		}

		//Returns the number of special ammo types defined in this library.
		public int GetNumberOfDefinitions()
		{
			return shellDefinitions.Length;
		}
	}
}
