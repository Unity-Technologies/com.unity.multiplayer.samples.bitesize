using UnityEngine;

namespace Tanks.Extensions
{
	public static class GameObjectExtensions
	{
		/// <summary>
		/// Sets the layer for this game object and all its children
		/// </summary>
		public static void SetLayerRecursively(this GameObject gameObject, int layer)
		{
			gameObject.layer = layer;
			SetLayerForChildren(gameObject.transform, layer);
		}

		private static void SetLayerForChildren(Transform transform, int layer)
		{
			int numChildren = transform.childCount;
			if (numChildren > 0)
			{
				for (int i = 0; i < numChildren; ++i)
				{
					Transform child = transform.GetChild(i);
					child.gameObject.layer = layer;
					SetLayerForChildren(child, layer);
				}
			}
		}
	}
}