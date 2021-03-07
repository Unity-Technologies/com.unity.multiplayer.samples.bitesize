using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Tanks.Utilities;
using System;

namespace Tanks.UI
{
	/// <summary>
	/// Controls the countdown prompt at the end of a multiplayer game, updating a Text prompts and firing an optional delegate when done.
	/// </summary>
	public class EndGameCountDown : MonoBehaviour
	{
		private float m_Time = 100f;
		private int m_LastDisplayedTime = 0;
		private bool m_CountingDown = false;
        
		private Text m_CountDown;

		//Delegate to fire when countdown is complete.
		private Action m_CountDownFinished;

		private void Awake()
		{
			gameObject.SetActive(false);
			m_CountDown = GetComponent<Text>();
		}

		private void Update()
		{
			if (m_CountingDown)
			{
				m_Time -= Time.deltaTime;
				int roundedTime = Mathf.CeilToInt(m_Time);
				if (roundedTime != m_LastDisplayedTime)
				{
					m_LastDisplayedTime = roundedTime;
					m_CountDown.text = roundedTime.ToString();
				}

				if (m_Time <= 0)
				{
					m_CountingDown = false;	
					FireEvent();
				}
			}
		}

		/// <summary>
		/// Fires delegate event.
		/// </summary>
		private void FireEvent()
		{
			if (m_CountDownFinished != null)
			{
				m_CountDownFinished.Invoke();
			}
		}

		/// <summary>
		/// Starts the countdown.
		/// </summary>
		/// <param name="time">Time to count down from.</param>
		/// <param name="countDownFinished">Delegate to fire when the countdown is complete.</param>
		public void StartCountDown(float time, Action countDownFinished = null)
		{
			this.m_Time = time;
			this.m_CountDownFinished = countDownFinished;
			gameObject.SetActive(true);
			m_CountingDown = true;
		}
	}
}