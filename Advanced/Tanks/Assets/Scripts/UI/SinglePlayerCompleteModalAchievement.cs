using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Tanks.Data;
using Tanks.Audio;

namespace  Tanks.UI
{
	/// <summary>
	/// Single player complete modal achievement. Representation of the objective.
	/// </summary>
	public class SinglePlayerCompleteModalAchievement : MonoBehaviour
	{
		[SerializeField]
		/// <summary>
		/// The textbox used to display the objective's description.
		/// </summary>
		protected Text m_Textbox;
		[SerializeField]
		/// <summary>
		/// The particle systems used for achievement effects.
		/// </summary>
		protected ParticleSystem m_CurrencySystem, m_StarsSystem;

		[SerializeField]
		/// <summary>
		/// The textbox used to display the objective's reward amount.
		/// </summary>
		protected Text m_CurrencyTextbox;
		[SerializeField]
		/// <summary>
		/// The animator.
		/// </summary>
		protected Animator m_Animator;

		[SerializeField]
		/// <summary>
		/// The gameObject that is enabled when the object is failed.
		/// </summary>
		protected GameObject m_RedX;

		/// <summary>
		/// The currency reward of the objective.
		/// </summary>
		private int m_CurrencyReward = 0;

		public Animator animator
		{
			get { return m_Animator; }
		}

		public Text textbox
		{
			get { return m_Textbox; }
		}

		/// <summary>
		/// A local reference of the player currency, used for the count up lerp.
		/// </summary>
		private NumberDisplay m_PlayerCurrency;

		private int m_CurrentCurrency;

		/// <summary>
		/// Sets up the currency reward of the objective.
		/// </summary>
		/// <param name="currencyReward">Currency reward.</param>
		/// <param name="playerCurrency">Player currency.</param>
		public void SetUpCurrencyReward(int currencyReward, NumberDisplay playerCurrency, int currentCurrency)
		{
			this.m_CurrencyReward = currencyReward;
			SetCurrencyRewardText(currencyReward);
			this.m_PlayerCurrency = playerCurrency;
			PlayerDataManager.s_Instance.AddCurrency(currencyReward);
			this.m_CurrentCurrency = currentCurrency;
		}

		/// <summary>
		/// Sets the reward currency value for an unachieved achievement.
		/// </summary>
		/// <param name="currencyReward">Currency reward.</param>
		public void SetCurrencyRewardText(int currencyReward)
		{
			m_CurrencyTextbox.text = currencyReward.ToString();
		}

		/// <summary>
		/// Animation event fired at the end of the achieved animation.
		/// </summary>
		public void AchievementAnimComplete()
		{
			GetComponentInParent<SinglePlayerCompleteModal>().DoAchievement();
			UIAudioManager.s_Instance.PlayCoinSound();
		}

		/// <summary>
		/// Starts the currency lerp.
		/// </summary>
		public void StartCurrencyLerp()
		{
			m_PlayerCurrency.SetTargetValue(m_CurrentCurrency, m_CurrentCurrency + m_CurrencyReward, 0.75f);
			m_CurrencyTextbox.GetComponent<NumberDisplay>().SetTargetValue(m_CurrencyReward, 0, 0.75f);
			m_CurrencySystem.Play();
		}

		/// <summary>
		/// Plays the stars particle system.
		/// </summary>
		public void PlayStarsParticleSystem()
		{
			m_StarsSystem.Play();
		}

		/// <summary>
		/// Sets up the achievement in the case that it is already achieved.
		/// </summary>
		public void AlreadyAchieved()
		{
			HideCoins();
		}

		/// <summary>
		/// Sets the state of the achievement to failed.
		/// </summary>
		public void SetToFailedState()
		{
			HideCoins();
			m_RedX.SetActive(true);
		}

		/// <summary>
		/// Hides the currency display object.
		/// </summary>
		private void HideCoins()
		{
			m_CurrencySystem.transform.parent.gameObject.SetActive(false);
		}
	}
}
