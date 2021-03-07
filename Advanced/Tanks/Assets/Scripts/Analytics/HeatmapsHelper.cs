using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//Heatmaps not supported by UWP
#if !UNITY_WSA_10_0
using UnityAnalyticsHeatmap;
#endif

namespace Tanks.Analytics
{
	/// <summary>
	/// Heatmaps helper class: static public functions that wrap heatmap events
	/// </summary>
	public class HeatmapsHelper
	{
		/// <summary>
		/// Death in single player
		/// </summary>
		/// <param name="mapId">Map identifier.</param>
		/// <param name="position">Position.</param>
		public static void SinglePlayerDeath(string mapId, Vector3 position)
		{
			Dictionary<string, object> parameters = new Dictionary<string, object>();
			parameters.Add("mapId", mapId);
			LogHeatmapEvent("SinglePlayerDeath", position, parameters);
		}

		/// <summary>
		/// Death in multiplayer
		/// </summary>
		/// <param name="mapId">Map identifier.</param>
		/// <param name="modeId">Mode identifier.</param>
		/// <param name="killedTankId">Killed tank identifier.</param>
		/// <param name="killerTankId">Killer tank identifier.</param>
		/// <param name="weaponId">Weapon identifier.</param>
		/// <param name="position">Position.</param>
		public static void MultiplayerDeath(string mapId, string modeId, string killedTankId, string killerTankId, string weaponId, Vector3 position)
		{
			Dictionary<string, object> parameters = new Dictionary<string, object>();
			parameters.Add("mapId", mapId);
			parameters.Add("modeId", modeId);
			parameters.Add("killedTankId", killedTankId);
			parameters.Add("killerTankId", killerTankId);
			parameters.Add("weaponId", weaponId);
			LogHeatmapEvent("MultiplayerDeath", position, parameters);
		}

		/// <summary>
		/// Kill in multiplayer
		/// </summary>
		/// <param name="mapId">Map identifier.</param>
		/// <param name="modeId">Mode identifier.</param>
		/// <param name="killedTankId">Killed tank identifier.</param>
		/// <param name="killerTankId">Killer tank identifier.</param>
		/// <param name="weaponId">Weapon identifier.</param>
		/// <param name="position">Position.</param>
		public static void MultiplayerKill(string mapId, string modeId, string killedTankId, string killerTankId, string weaponId, Vector3 position)
		{
			Dictionary<string, object> parameters = new Dictionary<string, object>();
			parameters.Add("mapId", mapId);
			parameters.Add("modeId", modeId);
			parameters.Add("killedTankId", killedTankId);
			parameters.Add("killerTankId", killerTankId);
			parameters.Add("weaponId", weaponId);
			LogHeatmapEvent("MultiplayerKill", position, parameters);
		}

		/// <summary>
		/// Suicide in multiplayer
		/// </summary>
		/// <param name="mapId">Map identifier.</param>
		/// <param name="modeId">Mode identifier.</param>
		/// <param name="playerTankId">Player tank identifier.</param>
		/// <param name="weaponId">Weapon identifier.</param>
		/// <param name="position">Position.</param>
		public static void MultiplayerSuicide(string mapId, string modeId, string playerTankId, string weaponId, Vector3 position)
		{
			Dictionary<string, object> parameters = new Dictionary<string, object>();
			parameters.Add("mapId", mapId);
			parameters.Add("modeId", modeId);
			parameters.Add("playerTankId", playerTankId);
			parameters.Add("weaponId", weaponId);
			LogHeatmapEvent("MultiplayerSuicide", position, parameters);
		}

		/// <summary>
		/// Wrapper function for logging a heatmap event
		/// </summary>
		/// <param name="eventName">Event name.</param>
		/// <param name="position">Position.</param>
		/// <param name="parameters">Parameters.</param>
		private static void LogHeatmapEvent(string eventName, Vector3 position, Dictionary<string, object> parameters)
		{
			//Heatmaps not supported by UWP
			#if !UNITY_WSA_10_0
			HeatmapEvent.Send(eventName, position, Time.timeSinceLevelLoad, parameters);
			#endif
		}
	}
}
