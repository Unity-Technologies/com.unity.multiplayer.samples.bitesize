using UnityEngine;

namespace Tanks.UI
{
	//Juicy bob for UI element - used by Tank sprite in the loading modal
	public class UiShake : MonoBehaviour
	{
		[SerializeField]
		protected Transform m_ShakeParent;

		//Shake properties
		[SerializeField]
		protected Vector3 m_ShakeDirections;
		[SerializeField]
		protected float m_ShakeMagnitude, m_ShakeScale;
		[SerializeField]
		protected float m_BobFrequency, m_BobNoiseScale;

		//Update calls shake helper function
		protected virtual void Update()
		{
			DoShake();
		}

		//Shake helper function
		private void DoShake()
		{
			//Calculates bob based on Perlin Noise
			float xNoise = (Mathf.PerlinNoise((Time.realtimeSinceStartup + 0) * m_ShakeScale, Time.smoothDeltaTime) * 2 - 1) * m_ShakeMagnitude;
			float zNoise = (Mathf.PerlinNoise((Time.realtimeSinceStartup + 100) * m_ShakeScale, Time.smoothDeltaTime) * 2 - 1) * m_ShakeMagnitude;

			float yNoise = Mathf.Abs(Mathf.Sin(Time.realtimeSinceStartup * Mathf.PI * m_BobFrequency)) * m_ShakeMagnitude;
			yNoise *= Mathf.PerlinNoise((Time.realtimeSinceStartup + 50) * m_BobNoiseScale, Time.smoothDeltaTime);

			Vector3 offset = Vector3.Scale(m_ShakeDirections, new Vector3(xNoise, yNoise, zNoise));
			m_ShakeParent.transform.localPosition = offset;
		}
	}
}