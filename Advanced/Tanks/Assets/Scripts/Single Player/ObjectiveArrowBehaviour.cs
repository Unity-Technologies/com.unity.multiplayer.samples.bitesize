using UnityEngine;
using System.Collections;

namespace Tanks.SinglePlayer
{
	/// <summary>
	/// Objective arrow
	/// </summary>
	public class ObjectiveArrowBehaviour : MonoBehaviour
	{
		[SerializeField]
		protected float m_RotationSpeed;

		[SerializeField]
		protected float m_Amplitude;

		[SerializeField]
		protected float m_Speed;

		[SerializeField]
		protected bool m_UseHorizontalBounce;

		private float m_StartPosAxis;

		//We set our initial reference point to bounce in relation to.
		void Start()
		{
			if (!m_UseHorizontalBounce)
			{
				m_StartPosAxis = transform.position.y;
			}
		}

		//Bounce the indicator up and down, and rotate it.
		void Update()
		{
			float tempAmplitude = m_StartPosAxis + m_Amplitude * Mathf.Sin(m_Speed * Time.time);

			if (!m_UseHorizontalBounce)
			{
				transform.position = new Vector3(transform.position.x, tempAmplitude, transform.position.z);
			}

			transform.Rotate(Vector3.right * (m_RotationSpeed * Time.deltaTime));
		}
	}
}