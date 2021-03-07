using UnityEngine;
using System.Collections;
using Tanks.Rules.SinglePlayer;

namespace Tanks.UI
{
	//Base class for start game modals
	public class StartGameModal : Modal
	{
		//Requires an offline rules processor as online offline games use start modals
		protected OfflineRulesProcessor m_RulesProcessor;

		/// <summary>
		/// Setup the specified rulesProcessor.
		/// </summary>
		/// <param name="rulesProcessor">Rules processor.</param>
		public virtual void Setup(OfflineRulesProcessor rulesProcessor)
		{
			this.m_RulesProcessor = rulesProcessor;
		}

		/// <summary>
		/// Showing the modal pauses the game
		/// </summary>
		public override void Show()
		{
			base.Show();
			Time.timeScale = 0f;
		}

		/// <summary>
		/// When the START button is clicked start the game and unpause the game
		/// </summary>
		public void OnStartClick()
		{
			m_RulesProcessor.StartGame();
			Time.timeScale = 1f;
			CloseModal();
		}
	}
}
