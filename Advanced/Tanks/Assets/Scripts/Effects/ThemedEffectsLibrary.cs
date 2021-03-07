using System;
using Random = UnityEngine.Random;
using UnityEngine;
using UnityEngine.SceneManagement;
using Tanks.Map;
using Tanks.Explosions;
using Tanks.Utilities;

namespace Tanks.Effects
{
	//Struct that defines groups of special effects prefabs to be used for different biomes. Populated in editor via the ThemedEffectsLibrary monobehaviour.
	[Serializable]
	public struct EffectsGroup
	{
		//Enum to define the biome to which these effects belong.
		public MapEffectsGroup group;

		//Fields to populate the prefabs to be used for small, medium, large, tank and turret explosions for this biome, as well as the sound effect pool to draw from for each.
		public Effect smallExplosion;
		public AudioClip[] smallExplosionSounds;

		public Effect largeExplosion;
		public AudioClip[] largeExplosionSounds;

		public Effect extraLargeExplosion;
		public AudioClip[] extraLargeExplosionSounds;

		public Effect tankExplosion;
		public AudioClip[] tankExplosionSounds;

		public Effect turretExplosion;
		public AudioClip[] turretExplosionSounds;

		//Sound effect pool for the bouncy bomb, a special case.
		public AudioClip[] bouncyBombExplosionSounds;

		//Effect to spawn for the muzzle flash when the tank fires.
		public Effect firingExplosion;

		//The particle emitter to be used for the tank's "dust" movement effects for this biome.
		public GameObject tankTrackParticles;
	}

	//The themed effects library is a persistent singleton that provides the means to populate effects data in editor, and accessor functions to easily retrieve it from any class that needs it.
	//This differs from the other libraries in that it auto-selects which effects definition will be used based on the biome of the map the player is transitioning to.
	public class ThemedEffectsLibrary : PersistentSingleton<ThemedEffectsLibrary>
	{
		//An array of EffectsGroups. These determine which effects are used for each biome.
		[SerializeField]
		protected EffectsGroup[] m_EffectsGroups;

		//The currently selected effects group index.
		private int m_ActiveEffectsGroup = 0;

		//On start, we subscribe to the activeSceneChanged event, which fires every time we flip scenes in the game.
		private void Start()
		{
			SceneManager.activeSceneChanged += OnLevelChanged;
		}

		//Returns the full effects group data currently loaded for the map biome.
		public EffectsGroup GetEffectsGroupForMap()
		{
			return m_EffectsGroups[m_ActiveEffectsGroup];
		}

		//Helper function to get the tank explosion effect loaded for the map biome.
		public Effect GetTankExplosionForMap()
		{
			return m_EffectsGroups[m_ActiveEffectsGroup].tankExplosion;
		}

		//Helper function to get the turret explosion effect loaded for the map biome.
		public Effect GetTurretExplosionForMap()
		{
			return m_EffectsGroups[m_ActiveEffectsGroup].turretExplosion;
		}

		//Helper function to get the particle emitter for tank tracks defined for the map biome.
		public GameObject GetTrackParticlesForMap()
		{
			return m_EffectsGroups[m_ActiveEffectsGroup].tankTrackParticles;
		}

		//Helper function that returns a random sound effect for this biome corresponding to the explosion type required.
		public AudioClip GetRandomExplosionSound(ExplosionClass explosionType)
		{
			EffectsGroup activeGroup = m_EffectsGroups[m_ActiveEffectsGroup];

			switch (explosionType)
			{
				case ExplosionClass.Small:
					return activeGroup.smallExplosionSounds[Random.Range(0, activeGroup.smallExplosionSounds.Length)];

				case ExplosionClass.Large:
					return activeGroup.largeExplosionSounds[Random.Range(0, activeGroup.largeExplosionSounds.Length)];

				case ExplosionClass.ExtraLarge:
					return activeGroup.extraLargeExplosionSounds[Random.Range(0, activeGroup.extraLargeExplosionSounds.Length)];

				case ExplosionClass.BounceExplosion:
					return activeGroup.bouncyBombExplosionSounds[Random.Range(0, activeGroup.bouncyBombExplosionSounds.Length)];

				case ExplosionClass.TankExplosion:
					return activeGroup.tankExplosionSounds[Random.Range(0, activeGroup.tankExplosionSounds.Length)];

				case ExplosionClass.TurretExplosion:
					return activeGroup.turretExplosionSounds[Random.Range(0, activeGroup.turretExplosionSounds.Length)];

				default:
					return null;
			}
		}
			
		//Subscribed to the SceneManager.activeSceneChanged event.
		//Uses the active map's MapDetails data to determine which biome the currently-loaded map belongs to, and sets the index of the desired effects group accordingly.
		private void OnLevelChanged(Scene scene1, Scene newScene)
		{
			GameSettings settings = GameSettings.s_Instance;
			MapDetails map = settings.map;
			if (map == null)
			{
				return;
			}

			for (int i = 0; i < m_EffectsGroups.Length; i++)
			{
				EffectsGroup effectsGroup = m_EffectsGroups[i];
				if (effectsGroup.group == map.effectsGroup)
				{
					m_ActiveEffectsGroup = i;

					Debug.Log("Selecting effectsGroup " + effectsGroup.group);
				}
			}
		}

		//On destroy, we ensure that we unsubscribe from the SceneManager.
		protected override void OnDestroy()
		{
			SceneManager.activeSceneChanged -= OnLevelChanged;
			base.OnDestroy();
		}
	}
}
