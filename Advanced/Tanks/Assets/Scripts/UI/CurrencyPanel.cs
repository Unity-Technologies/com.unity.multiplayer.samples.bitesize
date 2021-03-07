using UnityEngine;
using Tanks.Data;
using UnityEngine.UI;

namespace Tanks.UI
{
	/// <summary>
	/// Controls the display and population of the Currency panel.
	/// </summary>
	public class CurrencyPanel : MonoBehaviour
	{
		[SerializeField]
		//Internal reference to text object displaying currency.
		protected Text m_CurrencyText;

		protected void Start()
		{
			RefreshCurrency();
		}

		protected void OnEnable()
		{
			RefreshCurrency();

			// Register change event
			PlayerDataManager playerData = PlayerDataManager.s_Instance;
			if (playerData != null)
			{
				playerData.onCurrencyChanged += OnCurrencyChanged;
			}
		}

		protected void OnDisable()
		{
			RefreshCurrency();

			// Deregister change event
			PlayerDataManager playerData = PlayerDataManager.s_Instance;
			if (playerData != null)
			{
				playerData.onCurrencyChanged -= OnCurrencyChanged;
			}
		}

		private void OnCurrencyChanged(int newCurrency)
		{
			RefreshCurrency();
		}

		public void RefreshCurrency()
		{
			PlayerDataManager playerData = PlayerDataManager.s_Instance;
			if (playerData != null)
			{
				m_CurrencyText.text = playerData.currency.ToString();
			}
		}
	}
}