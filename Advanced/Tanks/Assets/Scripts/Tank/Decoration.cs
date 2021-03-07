using UnityEngine;
using Tanks.TankControllers;
using Tanks.Extensions;

namespace Tanks.Data
{
	/// <summary>
	/// Decoration, such as a hat
	/// </summary>
	public class Decoration : MonoBehaviour
	{
		[SerializeField]
		protected Joint[] m_JointsToDestroy;

		[SerializeField]
		protected float m_DetachedLifetime = 10;

		[SerializeField]
		protected Renderer[] m_DecorationBaseRenderers;

		[SerializeField]
		protected string m_DeathLayer;

		protected TankManager m_TankRef;

		//Attach the decoration to a specific tank
		public void Attach(TankManager tank)
		{
			m_TankRef = tank;
		}

		//Set up the materials for the decoration
		public void SetMaterial(Material newMaterial)
		{
			for (int i = 0; i < m_DecorationBaseRenderers.Length; i++)
			{
				m_DecorationBaseRenderers[i].material = newMaterial;
			}
		}
		
		//Detach the joints
		public void Detach()
		{
			for (int i = 0; i < m_JointsToDestroy.Length; ++i)
			{
				Joint joint = m_JointsToDestroy[i];

				if (joint != null)
				{
					if (!string.IsNullOrEmpty(m_DeathLayer))
					{
						int layerId = LayerMask.NameToLayer(m_DeathLayer);
						joint.gameObject.SetLayerRecursively(layerId);
					}

					Destroy(joint);
				}
			}

			Destroy(gameObject, m_DetachedLifetime);
		}

		/// <summary>
		/// Gets the decoration bounds.
		/// </summary>
		/// <returns>The decoration bounds.</returns>
		public Bounds? GetDecorationBounds()
		{
			Bounds? bounds = null;
			foreach (Renderer rend in GetComponentsInChildren<MeshRenderer>())
			{
				if (rend.enabled && rend.gameObject.activeInHierarchy)
				{
					if (bounds.HasValue)
					{
						Bounds boundVal = bounds.Value;
						boundVal.Encapsulate(rend.bounds);
						bounds = boundVal;
					}
					else
					{
						bounds = rend.bounds;
					}
				}
			}

			// If we have a detacher, encapsulate the bounds of its detached children too
			SpringDetach detacher = GetComponent<SpringDetach>();
			if (detacher != null)
			{
				Bounds? detachedBounds = detacher.GetDetachedChildBounds();
				if (detachedBounds.HasValue)
				{
					if (bounds.HasValue)
					{
						Bounds boundVal = bounds.Value;
						boundVal.Encapsulate(detachedBounds.Value);
						bounds = boundVal;
					}
					else
					{
						bounds = detachedBounds;
					}
				}
			}

			return bounds;
		}
	}
}