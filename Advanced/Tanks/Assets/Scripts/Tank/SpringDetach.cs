using UnityEngine;

namespace Tanks
{
	/// <summary>
	/// Spring detach decorations
	/// </summary>
	public class SpringDetach : MonoBehaviour
	{
		[SerializeField]
		protected Transform[] m_DetachChildren;

		protected virtual void Start()
		{
			// Detach child from us
			for (int i = 0; i < m_DetachChildren.Length; ++i)
			{
				Transform detachChild = m_DetachChildren[i];
				if (detachChild != null)
				{
					detachChild.parent = null;
				}
			}
		}


		protected virtual void OnDestroy()
		{
			// Also destroy child
			for (int i = 0; i < m_DetachChildren.Length; ++i)
			{
				Transform detachChild = m_DetachChildren[i];
				if (detachChild != null)
				{
					Destroy(detachChild.gameObject);
				}
			}
		}

		/// <summary>
		/// Gets the detached child bounds.
		/// </summary>
		/// <returns>The detached child bounds.</returns>
		public Bounds? GetDetachedChildBounds()
		{
			Bounds? bounds = null;

			for (int i = 0; i < m_DetachChildren.Length; ++i)
			{
				Transform detached = m_DetachChildren[i];
				if (detached != null)
				{
					foreach (Renderer rend in detached.GetComponentsInChildren<MeshRenderer>())
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
				}
			}

			return bounds;
		}
	}
}