using UnityEngine;
using System.Collections;
using System.IO;

namespace Tanks.CameraControl
{
	/// <summary>
	/// Camera snapshot - class used for taking snapshots in game
	/// </summary>
	public class CameraSnapshot : MonoBehaviour
	{
		int m_PicCount = 0;
	
		void Update()
		{
			if (Input.GetKeyUp(KeyCode.Space))
			{
				StartCoroutine("TakePicture");
			}
		}

		IEnumerator TakePicture()
		{
			// We should only read the screen buffer after rendering is complete
			yield return new WaitForEndOfFrame();

			// Create a texture the size of the screen, RGB24 format
			int width = Screen.width;
			int height = Screen.height;
			Texture2D tex = new Texture2D(width, height, TextureFormat.ARGB32, false);

			// Read screen contents into the texture
			tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
			tex.Apply();

			// Encode texture into PNG
			byte[] bytes = tex.EncodeToPNG();
			Object.Destroy(tex);

			File.WriteAllBytes(Application.dataPath + "/../SavedScreen" + m_PicCount.ToString() + ".png", bytes);

			Debug.Log("Saving screenie to " + Application.dataPath + "/../SavedScreen" + m_PicCount.ToString() + ".png");

			m_PicCount++;
		}
	}
}
