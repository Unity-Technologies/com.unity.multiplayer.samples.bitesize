using System;
using UnityEngine;
using Tanks.Map;
using Tanks.Rules;
using Tanks.Networking;
using Tanks.Utilities;

namespace Tanks
{
	/// <summary>
	/// Persistent singleton for handling the game settings
	/// </summary>
	public class GameSettings : PersistentSingleton<GameSettings>
	{
		public event Action<MapDetails> mapChanged;
		public event Action<ModeDetails> modeChanged;

		[SerializeField]
		protected MapList m_MapList;

		[SerializeField]
		protected SinglePlayerMapList m_SinglePlayerMapList;

		[SerializeField]
		protected ModeList m_ModeList;

		public MapDetails map
		{
			get;
			private set;
		}

		public int mapIndex
		{
			get;
			private set;
		}

		public ModeDetails mode
		{
			get;
			private set;
		}

		public int modeIndex
		{
			get;
			private set;
		}

		public int scoreTarget
		{
			get;
			private set;
		}


		public bool isSinglePlayer
		{
			get { return NetworkManager.s_Instance.isSinglePlayer; }
		}

		/// <summary>
		/// Sets the index of the map.
		/// </summary>
		/// <param name="index">Index.</param>
		public void SetMapIndex(int index)
		{
			map = m_MapList[index];
			mapIndex = index;

			if (mapChanged != null)
			{
				mapChanged(map);
			}
		}

		/// <summary>
		/// Sets the index of the mode.
		/// </summary>
		/// <param name="index">Index.</param>
		public void SetModeIndex(int index)
		{
			SetMode(m_ModeList[index], index);
		}

		/// <summary>
		/// Sets up single player
		/// </summary>
		/// <param name="mapIndex">Map index.</param>
		/// <param name="modeDetails">Mode details.</param>
		public void SetupSinglePlayer(int mapIndex, ModeDetails modeDetails)
		{
			this.map = m_SinglePlayerMapList[mapIndex];
			this.mapIndex = mapIndex;
			if (mapChanged != null)
			{
				mapChanged(map);
			}

			SetMode(modeDetails, -1);
		}

		/// <summary>
		/// Sets up single player
		/// </summary>
		/// <param name="map">Map.</param>
		/// <param name="modeDetails">Mode details.</param>
		public void SetupSinglePlayer(MapDetails map, ModeDetails modeDetails)
		{
			this.map = map;
			this.mapIndex = -1;
			if (mapChanged != null)
			{
				mapChanged(map);
			}

			SetMode(modeDetails, -1);
		}

		/// <summary>
		/// Sets the mode.
		/// </summary>
		/// <param name="mode">Mode.</param>
		/// <param name="modeIndex">Mode index.</param>
		private void SetMode(ModeDetails mode, int modeIndex)
		{
			this.mode = mode;
			this.modeIndex = modeIndex;
			if (modeChanged != null)
			{
				modeChanged(mode);
			}

			mode.rulesProcessor.GetColorProvider().Reset();
			scoreTarget = mode.rulesProcessor.scoreTarget;
		}
	}
}