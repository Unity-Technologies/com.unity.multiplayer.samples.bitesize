using System;
using Tanks.Utilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tanks.Data
{
	//This class existed to manage temporary unlocks of any given item (tank, decoration, decoration colour) until midnight if the user watched an advert.
	//With the removal of adverts from the game, it is partially deprecated.
	public class DailyUnlockManager : Singleton<DailyUnlockManager>
	{
		//Fields to store the unique ID and (if relevant) colour of the unlocked item.
		private string m_TempUnlockId;
		private int m_TempUnlockColour;

		//The date that the last unlock was performed.
		private DateTime m_UnlockDay;

		//Internal state to ensure that the internal data is initialized before querying information.
		private bool m_Initialized = false;

		//On start, retrieve the last unlock data from the player data and set to initialized.
		private void Start()
		{
			m_UnlockDay = PlayerDataManager.s_Instance.LoadUnlockTime();
			m_TempUnlockId = PlayerDataManager.s_Instance.GetLastUnlockId();
			m_TempUnlockColour = PlayerDataManager.s_Instance.GetLastUnlockColour();

			m_Initialized = true;
		}

		//Called by external scripts to set an item as unlocked. The item's unique ID and colour (if relevant) is passed in.
		public void SetDailyUnlock(string itemId, int itemColour = -1)
		{
			m_UnlockDay = DateTime.Today;

			m_TempUnlockId = itemId;
			m_TempUnlockColour = itemColour;

			PlayerDataManager.s_Instance.SaveTempUnlockData(m_TempUnlockId, m_TempUnlockColour, m_UnlockDay);
		}

		//Returns whether a free unlock has been made today (ie, if the last stored unlock date is today's date).
		public bool IsUnlockActive()
		{
			return (DateTime.Today == m_UnlockDay);
		}

		//Returns whether init is complete (ie, if we've loaded last unlock info from player data).
		public bool IsInitialized()
		{
			return m_Initialized;
		}

		//Returns whether a given item has been unlocked based on its unique ID.
		//If we don't have an unlock active today at all, it assumes false.
		public bool IsItemTempUnlocked(string itemId)
		{
			if (IsUnlockActive())
			{
				return (itemId == m_TempUnlockId);
			}
			else
			{
				return false;
			}
		}

		//Returns the colour index saved as being unlocked. Since this is always in combination with a given item, we need no further context.
		public int GetTempUnlockedColour()
		{
			if (IsUnlockActive())
			{
				return m_TempUnlockColour;
			}
			else
			{
				return -1;
			}
		}

		#if UNITY_EDITOR
		//Handy menu option to reset the unlock date for debugging.
		[MenuItem("GameDataUtilities/Reset UnlockManager")]
		static void DebugResetDailyUnlock()
		{
			s_Instance.m_UnlockDay = DateTime.Today.Subtract(TimeSpan.FromDays(1));

			PlayerDataManager.s_Instance.SaveTempUnlockData(s_Instance.m_TempUnlockId, s_Instance.m_TempUnlockColour, s_Instance.m_UnlockDay);
		}
		#endif

	}
}
