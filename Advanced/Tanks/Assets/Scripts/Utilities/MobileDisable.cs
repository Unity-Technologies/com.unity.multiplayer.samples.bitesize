using UnityEngine;

namespace Tanks.Utilities
{
	/// <summary>
	/// A collection of game objects we should disable on mobile platforms
	/// </summary>
	/// <remarks>
	/// On actual non-editor deploys, this will actually destroy the component
	/// </remarks>
	public class MobileDisable : MonoBehaviour
	{
		[SerializeField]
		protected Behaviour[] m_ObjectsToDisable;

		protected virtual void Awake()
		{
#if UNITY_ANDROID || UNITY_IOS || UNITY_WSA_10_0
#if UNITY_WSA_10_0
			// Skip if we're not actually on mobile
			// We really only need this for UWP because it can theoretically be on a wide range of platforms
			if (!MobileUtilities.IsOnMobile())
			{
				return;
			}
#endif

			for (int i = 0; i < m_ObjectsToDisable.Length; ++i)
			{
				Behaviour comp = m_ObjectsToDisable[i];

				if (comp != null)
				{
#if UNITY_EDITOR
					comp.enabled = false;
#else
					Destroy(comp);
#endif
				}
			}
#endif
		}
	}
}