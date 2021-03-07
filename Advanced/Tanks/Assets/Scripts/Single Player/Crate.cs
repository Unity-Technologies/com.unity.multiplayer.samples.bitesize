using System.Collections.Generic;
using UnityEngine;
using Tanks.Data;
using Tanks.Explosions;

namespace Tanks.SinglePlayer
{
	/// <summary>
	/// Crate in decoration game
	/// </summary>
	[RequireComponent(typeof(Rigidbody))]
	public class Crate : Npc
	{
		[SerializeField]
		protected Renderer m_StickerRenderer;
		[SerializeField]
		protected Renderer m_PaintRenderer;
		[SerializeField]
		protected float m_SmoothMovementTime;
		[SerializeField]
		protected float m_TorqueStrength;

		private Rigidbody m_CachedRigidbody;
		private Vector3 m_Vel;

		public TankDecorationDefinition cratePrize
		{
			get;
			protected set;
		}

		public int decorationMaterialIndex
		{
			get;
			protected set;
		}

		public float movementProgress
		{
			get;
			set;
		}

		/// <summary>
		/// Initializes crates
		/// </summary>
		/// <param name="contents">Contents.</param>
		public void SetupCrate(TankDecorationDefinition contents)
		{
			cratePrize = contents;

			m_StickerRenderer.material.mainTexture = cratePrize.crateDecal;

			m_CachedRigidbody = GetComponent<Rigidbody>();

			// Work out material ID
			int prizeIndex = TankDecorationLibrary.s_Instance.GetIndexForDecoration(cratePrize);
			List<int> colourIndexList = new List<int>();

			PlayerDataManager playerdata = PlayerDataManager.s_Instance;
			for (int i = 0; i < contents.availableMaterials.Length; i++)
			{
				if (!playerdata.IsColourUnlockedForDecoration(prizeIndex, i))
				{
					colourIndexList.Add(i);
				}
			}
	
			decorationMaterialIndex = colourIndexList[Random.Range(0, colourIndexList.Count)];
			m_PaintRenderer.material.color = cratePrize.availableMaterials[decorationMaterialIndex].color;
		}

		/// <summary>
		/// When the game object is destroyed then destroy its stickerRenderer material
		/// </summary>
		protected virtual void OnDestroy()
		{
			// Destroy material instance
			Destroy(m_StickerRenderer.material);
		}

		/// <summary>
		/// Movement code
		/// </summary>
		/// <param name="target">Target.</param>
		public void MoveTo(Vector3 target)
		{
			Vector3 moveVector = Vector3.Normalize(transform.position - target);
			m_CachedRigidbody.MovePosition(Vector3.SmoothDamp(transform.position, target, ref m_Vel, m_SmoothMovementTime));
			m_CachedRigidbody.AddTorque(Vector3.Cross(moveVector, Vector3.right) * m_TorqueStrength);
		}

		/// <summary>
		/// When the create is destroyed
		/// </summary>
		protected override void OnDied()
		{
			// Spawn decoration debris
			Decoration spawnedPrefab = Instantiate<Decoration>(cratePrize.decorationPrefab);
			spawnedPrefab.Detach();
			spawnedPrefab.transform.position = transform.position + Vector3.up;
			spawnedPrefab.SetMaterial(cratePrize.availableMaterials[decorationMaterialIndex]);

			if (ExplosionManager.s_InstanceExists)
			{
				ExplosionManager.s_Instance.SpawnExplosion(transform.position, Vector3.up, null, -1, m_ExplosionDefinition, false);
			}

			base.OnDied();
		}
	}
}