using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

namespace Tanks.UI
{
	//Helper for animation count up/down from value to target
	[RequireComponent(typeof(Text))]
	public class NumberDisplay : MonoBehaviour
	{
		[SerializeField]
		protected float m_SecondsBetweenUpdate = 0.125f;
		[SerializeField]
		protected AnimationCurve m_Curve;
		
		private int m_Start, m_Current = 0, m_Target = 0;
		private float m_Timer = 0.0f, m_DurationTimer, m_Duration = 1.0f;
		private Text m_TextBox;
		private Action m_OnComplete;

		/// <summary>
		/// Sets the target value.
		/// </summary>
		/// <param name="startAmount">Start amount.</param>
		/// <param name="target">Target.</param>
		/// <param name="duration">Duration.</param>
		/// <param name="onComplete">On complete.</param>
		public void SetTargetValue(int startAmount, int target, float duration, Action onComplete = null)
		{
			m_Current = startAmount;
			m_Start = m_Current;
			this.m_Target = target;
			this.m_Duration = duration;
			m_DurationTimer = 0.0f;
			m_TextBox.text = m_Current.ToString();
			this.m_OnComplete = onComplete;
		}

		private void Awake()
		{
			m_TextBox = GetComponent<Text>();
		}

		private void Update()
		{
			if (m_Current != m_Target && m_DurationTimer <= m_Duration)
			{
				m_Timer += Time.deltaTime;
				if (m_Timer >= m_SecondsBetweenUpdate)
				{
					m_Timer -= m_SecondsBetweenUpdate;
					m_DurationTimer += m_SecondsBetweenUpdate;
					UpdateCurrent();
				}
			}
		}

		private void UpdateCurrent()
		{
			float normalizedTime = m_DurationTimer / m_Duration;
			m_Curve.Evaluate(normalizedTime);
			if (m_Start < m_Current)
			{
				m_Current = (int)(m_Start + (m_Target - m_Start) * normalizedTime);
			}
			else
			{
				m_Current = (int)(m_Start - (m_Start - m_Target) * normalizedTime);
			}
			m_TextBox.text = m_Current.ToString();
			
			if (m_Current == m_Target && m_OnComplete != null)
			{
				m_OnComplete();
			}
		}
	}
}