using System;
using UnityEngine;
using UnityEngine.UI;
using Tanks.Rules.SinglePlayer;
using Tanks.Data;
using Tanks.Audio;

namespace Tanks.UI
{
	/// <summary>
	/// Modal that pops-up after the decoration shooting range mini game
	/// </summary>
	public class ShootingRangeEndGameModal : OfflineEndGameModal
	{
		//UI references
		[SerializeField]
		protected Text m_Label;
		[SerializeField]
		protected Image m_PreviewImage;
		[SerializeField]
		protected Image m_ColourSwatch;
		[SerializeField]
		protected Text m_CostMessageLabel;
		[SerializeField]
		protected Animator m_Animator;
		[SerializeField]
		protected ParticleSystem[] m_ConfettiParticleSystems;

		//Cost to try again
		[SerializeField]
		protected int m_RerollCost = 50;

		public override void Show()
		{
			base.Show();
			
			MusicManager.s_Instance.StopMusic();

			//play victory fanfare
			UIAudioManager.s_Instance.PlayVictorySound();
			
			//safely casts the rulesProcessor to ShootingRangeRulesProcessor
			ShootingRangeRulesProcessor rules = m_RulesProcessor as ShootingRangeRulesProcessor;

			if (rules != null)
			{
				if (m_RetryButton != null)
				{
					//Update reroll cost
					m_CostMessageLabel.text = m_RerollCost.ToString();

					// Enable if a reroll is applicable
					m_RetryButton.interactable = !rules.hasReset && PlayerDataManager.s_Instance.CanPlayerAffordPurchase(m_RerollCost);
				}
				
				//Get the prize from rules processor
				TankDecorationDefinition prize = rules.prize;
				int prizeColour = rules.prizeColourId;

				// Populate decoration you've earned
				m_ColourSwatch.color = prize.availableMaterials[prizeColour].color;
				m_Label.text = prize.name;
				m_PreviewImage.sprite = prize.preview;
				m_PreviewImage.transform.localScale = Vector3.zero;
				
				//Play reward effects
				m_Animator.Play("Reward");
				int count = m_ConfettiParticleSystems.Length;
				for (int i = 0; i < count; i++)
				{
					m_ConfettiParticleSystems[i].Play();	
				}
			}
		}
		
		//reset acts like a re-roll
		public override void OnResetClick()
		{
			PlayerDataManager.s_Instance.RemoveCurrency(m_RerollCost);
			base.OnResetClick();
		}
	}
}