using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Tanks.Utilities;
using System.Linq;

namespace Tanks.Map
{
	/// <summary>
	/// Spawn manager - used to get an unoccupied spawn point
	/// </summary>
	public class SpawnManager : Singleton<SpawnManager>
	{
		private List<SpawnPoint> spawnPoints = new List<SpawnPoint>();

		protected override void Awake()
		{
			base.Awake();
			LazyLoadSpawnPoints();
		}

		private void Start()
		{
			LazyLoadSpawnPoints();
		}

		/// <summary>
		/// Lazy load the spawn points - this assumes that all spawn points are children of the SpawnManager
		/// </summary>
		private void LazyLoadSpawnPoints()
		{
			if (spawnPoints != null && spawnPoints.Count > 0)
			{
				return;
			}

			SpawnPoint[] foundSpawnPoints = GetComponentsInChildren<SpawnPoint>();
			spawnPoints.AddRange(foundSpawnPoints);
		}

		/// <summary>
		/// Gets index of a random empty spawn point
		/// </summary>
		/// <returns>The random empty spawn point index.</returns>
		public int GetRandomEmptySpawnPointIndex()
		{
			LazyLoadSpawnPoints();
			//Check for empty zones
			List<SpawnPoint> emptySpawnPoints = spawnPoints.Where(sp => sp.isEmptyZone).ToList();
			
			//If no zones are empty, which is impossible if the setup is correct, then return the first spawnpoint in the list
			if (emptySpawnPoints.Count == 0)
			{
				return 0;
			}
			
			//Get random empty spawn point
			SpawnPoint emptySpawnPoint = emptySpawnPoints[Random.Range(0, emptySpawnPoints.Count)];
			
			//Mark it as dirty
			emptySpawnPoint.SetDirty();
			
			//return the index of this spawn point
			return spawnPoints.IndexOf(emptySpawnPoint);
		}

		public SpawnPoint GetSpawnPointByIndex(int i)
		{
			LazyLoadSpawnPoints();
			return spawnPoints[i];
		}

		public Transform GetSpawnPointTransformByIndex(int i)
		{
			return GetSpawnPointByIndex(i).spawnPointTransform;
		}

		/// <summary>
		/// Cleans up the spawn points.
		/// </summary>
		public void CleanupSpawnPoints()
		{
			for (int i = 0; i < spawnPoints.Count(); i++)
			{
				spawnPoints[i].Cleanup();
			}
		}
	}
}