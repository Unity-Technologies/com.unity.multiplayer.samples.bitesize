using UnityEngine;
using System.Collections;
using System;

namespace Tanks.UI
{
	//Base class for modal that closes itself automatically after a time has elapsed and has a callback
	public class TimedModal : Modal
	{
		protected float m_Timer;

		private static TimedModal s_instance;

		public static TimedModal s_Instance
		{
			get
			{
				return s_instance;
			}
		}

		protected Action m_Callback;
	
		protected bool m_IsTiming = true;

		//Closes self automatically
		protected void Awake()
		{
			//Singleton modal
			s_instance = this;
			CloseModal();
		}

		//Handle timer
		protected void Update()
		{
			if (m_IsTiming)
			{
				m_Timer = m_Timer - Time.unscaledDeltaTime;
				if (m_Timer <= 0f)
				{
					m_IsTiming = false;
					if (m_Callback != null)
					{
						m_Callback();
					}
				}
			}
		}

		//Setup how the modal behaves
		public void SetupTimer(float timer, Action callback)
		{
			this.m_Timer = timer;
			this.m_Callback = callback;
			m_IsTiming = true;
		}
	}
}