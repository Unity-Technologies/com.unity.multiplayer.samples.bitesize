using UnityEngine;
using UnityEditor;

namespace Tanks.Editor
{
	public static class ScreenSizeGetter
	{
		[MenuItem("CONTEXT/RectTransform/Print Screen Size")]
		private static void GetScreenSize()
		{
			GameObject ob = Selection.activeGameObject;

			if (ob != null)
			{
				RectTransform rect = ob.GetComponent<RectTransform>();
				Canvas canvas = ob.GetComponentInParent<Canvas>();
			
				Vector2 rectSize = rect.rect.size * canvas.scaleFactor;
				Debug.Log(rectSize);
			}
		}
	}
}