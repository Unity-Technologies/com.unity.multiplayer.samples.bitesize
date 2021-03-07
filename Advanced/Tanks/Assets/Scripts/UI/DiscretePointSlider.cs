using UnityEngine;
using System.Collections;


namespace Tanks.UI
{
	/// <summary>
	/// Allows an array of "point" objects to be turned on proportionally to a provided value.
	/// </summary>
	public class DiscretePointSlider : MonoBehaviour 
	{
		[SerializeField]
		//Array of objects to enable in proportion to value.
		protected GameObject[] m_SliderPips;
		[SerializeField]
		protected float m_MaxValue = 1f;

		private float m_Value = 0f;

		private float m_LastValue = -1;

		public void UpdateValue (float newValue) 
		{
			m_Value = newValue;

			if(m_SliderPips == null)
				return;

			if(m_SliderPips.Length == 0)
			{
				return;
			}
				
			if(m_LastValue != m_Value)
			{
				int pipValue = Mathf.CeilToInt((m_Value/m_MaxValue) * m_SliderPips.Length);

				for(int i = 0; i < m_SliderPips.Length; i++)
				{
					m_SliderPips[i].SetActive(i <= (pipValue - 1));
				}

				m_LastValue = m_Value;
			}
		}
	}
}
