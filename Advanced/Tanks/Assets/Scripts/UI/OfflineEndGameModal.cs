using UnityEngine;
using UnityEngine.UI;
using Tanks.Rules.SinglePlayer;

namespace Tanks.UI
{
	/// <summary>
	/// Offline end game modal - base class
	/// </summary>
	public class OfflineEndGameModal : EndGameModal
	{
		[SerializeField]
		protected Button m_RetryButton;

		//Show modal
		public override void Show()
		{
			base.Show();

			if (m_RetryButton != null)
			{
				m_RetryButton.interactable = false;
			}
		}
		
		//reset game
		public virtual void OnResetClick()
		{
			OfflineRulesProcessor offlineRules = m_RulesProcessor as OfflineRulesProcessor;

			if (offlineRules != null)
			{
				offlineRules.ResetGame();
			}
			CloseModal();
		}
	}
}