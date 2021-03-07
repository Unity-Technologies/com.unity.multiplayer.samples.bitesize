using UnityEngine;
using System.Collections.Generic;
using System;
using Tanks.Utilities;

namespace Tanks.CameraControl
{
	/// <summary>
	/// Component placed on a parent of the camera that handles screen shake
	/// </summary>
	[RequireComponent(typeof(Camera))]
	public class ScreenShakeController : Singleton<ScreenShakeController>
	{
		#region Inner classes

		/// <summary>
		/// Struct to contain settings per perspective/orthographic modes
		/// </summary>
		[Serializable]
		public struct ShakeSettings
		{
			public float maxShake;
			public float maxAngle;
		}


		/// <summary>
		/// Struct to contain specific instances of shaking
		/// </summary>
		protected struct ShakeInstance
		{
			public float maxDuration, duration, magnitude;
			public Vector2 direction;
			public int shakeId;

			public float normalizedProgress
			{
				get
				{
					return Mathf.Clamp01(duration / maxDuration);
				}
			}

			public bool done
			{
				get
				{
					return maxDuration > 0 && duration >= maxDuration - Mathf.Epsilon;
				}
			}

			public void StopShake()
			{
				// Set durations to ended
				maxDuration = duration = 1;
			}
		}

		#endregion


		#region Fields

		/// <summary>
		/// Perspective camera settings
		/// </summary>
		[SerializeField]
		protected ShakeSettings m_PerspectiveSettings;
		/// <summary>
		/// Orthographic settings
		/// </summary>
		[SerializeField]
		protected ShakeSettings m_OrthographicSettings;
		/// <summary>
		/// Scaling factor for directional noise
		/// </summary>
		[SerializeField]
		protected float m_DirectionNoiseScale;
		/// <summary>
		/// Scaling factor for magnitudinal noise
		/// </summary>
		[SerializeField]
		protected float m_MagnitudeNoiseScale;

		/// <summary>
		/// Collection of current shakes
		/// </summary>
		private List<ShakeInstance> m_CurrentShakes;
		/// <summary>
		/// Reference to our camera
		/// </summary>
		private Camera m_ShakingCamera;

		/// <summary>
		/// Shake ID counter
		/// </summary>
		private int m_ShakeCounter = 0;

		#endregion


		#region Unity Methods

		/// <summary>
		/// Initialize shake collection, noise generator, and find child camera
		/// </summary>
		protected override void Awake()
		{
			base.Awake();

			m_CurrentShakes = new List<ShakeInstance>();

			m_ShakingCamera = GetComponent<Camera>();
			// Disable ourselves if we have no camera
			if (m_ShakingCamera == null)
			{
				enabled = false;
				Debug.LogWarning("No camera for ScreenShakeController.");
			}
		}


		/// <summary>
		/// Do shakes
		/// </summary>
		protected virtual void Update()
		{
			// Double check that our camera still exists
			if (m_ShakingCamera == null)
				return;

			Vector2 shakeIntensity = Vector2.zero;

			// Count backwards so we can remove shakes with simpler logic
			for (int i = m_CurrentShakes.Count - 1; i >= 0; --i)
			{
				ShakeInstance shake = m_CurrentShakes[i];

				ProcessShake(ref shake, ref shakeIntensity);

				if (shake.done)
				{
					m_CurrentShakes.RemoveAt(i);
				}
				else
				{
					// Update list
					m_CurrentShakes[i] = shake;
				}
			}

			Vector3 shake3D = new Vector3(shakeIntensity.x, shakeIntensity.y, 0);

			if (m_ShakingCamera.orthographic)
			{
				// Orthographic cameras get translated
				transform.localPosition = shake3D;
				transform.localRotation = Quaternion.identity;
			}
			else
			{
				// Perspective cameras get a shake
				Vector3 rotateAxis = Vector3.Cross(Vector3.forward, shake3D).normalized;

				transform.localPosition = Vector3.zero;
				transform.localRotation = Quaternion.AngleAxis(shake3D.magnitude, rotateAxis);
			}
		}

		#endregion


		#region Methods

		/// <summary>
		/// Perform a screen shake with a world space origin
		/// </summary>
		/// <param name="worldPosition">World position</param>
		/// <param name="magnitude">The magnitude of the shake (0-1)</param>
		/// <param name="duration">The duration in seconds</param>
		public void DoShake(Vector3 worldPosition, float magnitude, float duration)
		{
			// Calculate relative screen direction
			Vector3 viewportPos = m_ShakingCamera.WorldToViewportPoint(worldPosition);
			Vector2 relativePos = new Vector2(viewportPos.x * 2 - 1, viewportPos.y * 2 - 1);

			DoShake(relativePos.normalized, magnitude, duration);
		}


		/// <summary>
		/// Perform a screen shake with a world space origin, scaling its magnitude by distance from screen center
		/// </summary>
		/// <param name="worldPosition">World position</param>
		/// <param name="magnitude">The magnitude of the shake (0-1)</param>
		/// <param name="duration">The duration in seconds</param>
		/// <param name="minScale">Minimum magnitude scalar when more than 1 diagonal away from the center of the screen</param>
		/// <param name="maxScale">Maximum magnitude scalar when explosion is centered on the screen</param>
		public void DoShake(Vector3 worldPosition, float magnitude, float duration, 
		                    float minScale, float maxScale)
		{
			// Calculate relative screen direction
			Vector3 viewportPos = m_ShakingCamera.WorldToViewportPoint(worldPosition);
			Vector2 relativePos = new Vector2(viewportPos.x * 2 - 1, viewportPos.y * 2 - 1);

			// Scale magnitude based on distance to center of screen
			float distanceScalar = Mathf.Clamp01(relativePos.magnitude / Mathf.Sqrt(2));
			distanceScalar = (1 - distanceScalar);
			distanceScalar *= distanceScalar;
			float durationScalar = distanceScalar * 0.5f + 0.5f;
			magnitude *= Mathf.Lerp(minScale, maxScale, distanceScalar);

			DoShake(relativePos.normalized, magnitude, duration * durationScalar);
		}


		/// <summary>
		/// Perform a screen shake
		/// </summary>
		/// <param name="direction">The direction in screen space of the shake. Should be normalized</param>
		/// <param name="magnitude">The magnitude of the shake (0-1)</param>
		/// <param name="duration">The duration in seconds</param>
		public void DoShake(Vector2 direction, float magnitude, float duration)
		{
			// Add a new shake
			ShakeInstance shake = new ShakeInstance
			{
				shakeId = m_ShakeCounter++,
				maxDuration = duration,
				duration = 0,
				magnitude = magnitude,
				direction = direction
			};

			m_CurrentShakes.Add(shake);

			if (m_ShakeCounter == int.MaxValue)
				m_ShakeCounter = 0;
		}


		/// <summary>
		/// Enable a repeating screen shake
		/// </summary>
		/// <param name="direction">The direction in screen space of the shake. Should be normalized</param>
		/// <param name="magnitude">The magnitude of the shake (0-1)</param>
		/// <returns>Index of shake so it can be stopped later</returns>
		public int DoPerpetualShake(Vector2 direction, float magnitude)
		{
			int result = m_ShakeCounter;

			// Add a new shake
			ShakeInstance shake = new ShakeInstance
			{
				shakeId = m_ShakeCounter++,
				maxDuration = -1,
				duration = 0,
				magnitude = magnitude,
				direction = direction
			};

			m_CurrentShakes.Add(shake);

			if (m_ShakeCounter == int.MaxValue)
			{
				m_ShakeCounter = 0;
			}

			return result;
		}


		/// <summary>
		/// Stop a perpetual screenshake
		/// </summary>
		public bool StopShake(int shakeId)
		{
			// Find shake
			for (int i = m_CurrentShakes.Count - 1; i >= 0; --i)
			{
				ShakeInstance shake = m_CurrentShakes[i];

				if (shake.shakeId == shakeId)
				{
					shake.StopShake();
					m_CurrentShakes[i] = shake;
					return true;
				}
			}

			return false;
		}


		/// <summary>
		/// Process and accumulate each shake
		/// </summary>
		protected virtual void ProcessShake(ref ShakeInstance shake, ref Vector2 shakeVector)
		{
			if (shake.maxDuration > 0)
			{
				shake.duration = Mathf.Clamp(shake.duration + Time.deltaTime, 0, shake.maxDuration);
			}

			ShakeSettings settings = m_ShakingCamera.orthographic ? m_OrthographicSettings : m_PerspectiveSettings;
			float magnitude = CalculateShakeMagnitude(ref shake, settings);
			Vector2 additionalShake = CalculateRandomVector(ref shake, settings);

			shakeVector += additionalShake * magnitude;
		}


		private float CalculateShakeMagnitude(ref ShakeInstance shake, ShakeSettings currentSettings)
		{
			float t = shake.normalizedProgress;

			float noise = Mathf.PerlinNoise(Time.realtimeSinceStartup * m_MagnitudeNoiseScale, shake.duration);
			// Rescale noise so it shakes primarily towards direction rather than in both directions
			// This changes the noise range from [1,-1] to [1, -0.2],
			noise *= 0.6f + 0.4f; 

			return Mathf.Lerp(shake.magnitude, 0, t) * noise * currentSettings.maxShake;
		}


		private Vector2 CalculateRandomVector(ref ShakeInstance shake, ShakeSettings currentSettings)
		{
			float noise = Mathf.PerlinNoise(Time.realtimeSinceStartup * m_DirectionNoiseScale, shake.duration);
			float deviation = noise * shake.magnitude * currentSettings.maxAngle;

			return shake.direction.Rotate(deviation);
		}

		#endregion
	}
}