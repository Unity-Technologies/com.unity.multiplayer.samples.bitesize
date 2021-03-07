using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityAnalytics = UnityEngine.Analytics;
using Tanks.Networking;

namespace Tanks.Analytics
{
	/// <summary>
	/// Analytics helper class: static public functions that wrap analytics events
	/// </summary>
	public class AnalyticsHelper
	{
		/// <summary>
		/// Player used tank in game.
		/// </summary>
		/// <param name="tankId">Tank identifier.</param>
		public static void PlayerUsedTankInGame(string tankId)
		{
			Dictionary<string, object> eventData = new Dictionary<string, object>();
			eventData.Add("tankId", tankId);
			eventData.Add("isSinglePlayer", NetworkManager.s_Instance.gameType == NetworkGameType.Singleplayer);
			LogCustomEvent("PlayerUsedTankInGame", eventData);
		}

		/// <summary>
		/// Player used decoration in game.
		/// </summary>
		/// <param name="decorationId">Decoration identifier.</param>
		public static void PlayerUsedDecorationInGame(string decorationId)
		{
			Dictionary<string, object> eventData = new Dictionary<string, object>();
			eventData.Add("decorationId", decorationId);
			eventData.Add("isSinglePlayer", NetworkManager.s_Instance.gameType == NetworkGameType.Singleplayer);
			LogCustomEvent("PlayerUsedDecorationInGame", eventData);
		}

		/// <summary>
		/// Single Player level was started
		/// </summary>
		/// <param name="mapId">Map identifier.</param>
		public static void SinglePlayerLevelStarted(string mapId)
		{
			Dictionary<string, object> eventData = new Dictionary<string, object>();
			eventData.Add("mapId", mapId);
			LogCustomEvent("SinglePlayerLevelStarted", eventData);
		}

		/// <summary>
		/// Successfully completed the single player level
		/// </summary>
		/// <param name="mapId">Map identifier.</param>
		/// <param name="stars">Stars.</param>
		public static void SinglePlayerLevelCompleted(string mapId, int stars)
		{
			Dictionary<string, object> eventData = new Dictionary<string, object>();
			eventData.Add("mapId", mapId);
			eventData.Add("stars", stars);
			LogCustomEvent("SinglePlayerLevelCompleted", eventData);
		}

		/// <summary>
		/// Failed the single player level
		/// </summary>
		/// <param name="mapId">Map identifier.</param>
		public static void SinglePlayerLevelFailed(string mapId)
		{
			Dictionary<string, object> eventData = new Dictionary<string, object>();
			eventData.Add("mapId", mapId);
			LogCustomEvent("SinglePlayerLevelFailed", eventData);
		}

		/// <summary>
		/// Quit the single player level
		/// </summary>
		/// <param name="mapId">Map identifier.</param>
		public static void SinglePlayerLevelQuit(string mapId)
		{
			Dictionary<string, object> eventData = new Dictionary<string, object>();
			eventData.Add("mapId", mapId);
			LogCustomEvent("SinglePlayerLevelQuit", eventData);
		}

		/// <summary>
		/// Suicide in a single player game
		/// </summary>
		/// <param name="mapId">Map identifier.</param>
		/// <param name="weaponId">Weapon identifier.</param>
		public static void SinglePlayerSuicide(string mapId, string weaponId)
		{
			Dictionary<string, object> eventData = new Dictionary<string, object>();
			eventData.Add("mapId", mapId);
			eventData.Add("weaponId", weaponId);
			LogCustomEvent("SinglePlayerSuicide", eventData);
		}

		/// <summary>
		/// Started a multiplayer game
		/// </summary>
		/// <param name="mapId">Map identifier.</param>
		/// <param name="modeId">Mode identifier.</param>
		/// <param name="numberOfPlayers">Number of players.</param>
		public static void MultiplayerGameStarted(string mapId, string modeId, int numberOfPlayers)
		{
			Dictionary<string, object> eventData = GetBaseGameEventData(mapId, modeId, numberOfPlayers);
			LogCustomEvent("MultiplayerGameStarted", eventData);
		}

		/// <summary>
		/// Completed a multiplayer game
		/// </summary>
		/// <param name="mapId">Map identifier.</param>
		/// <param name="modeId">Mode identifier.</param>
		/// <param name="numberOfPlayers">Number of players.</param>
		/// <param name="timeInSeconds">Time in seconds.</param>
		/// <param name="winningTankId">Winning tank identifier.</param>
		public static void MultiplayerGameCompleted(string mapId, string modeId, int numberOfPlayers, int timeInSeconds, string winningTankId)
		{
			Dictionary<string, object> eventData = GetBaseGameEventData(mapId, modeId, numberOfPlayers);
			eventData.Add("timeInSeconds", timeInSeconds);
			eventData.Add("winningTankId", winningTankId);
			LogCustomEvent("MultiplayerGameCompleted", eventData);
		}

		/// <summary>
		/// Player bailed from multiplayer game
		/// </summary>
		/// <param name="mapId">Map identifier.</param>
		/// <param name="modeId">Mode identifier.</param>
		/// <param name="numberOfPlayers">Number of players.</param>
		/// <param name="timeInSeconds">Time in seconds.</param>
		/// <param name="playerPosition">Player position.</param>
		/// <param name="playerTankId">Player tank identifier.</param>
		public static void MultiplayerGamePlayerBailed(string mapId, string modeId, int numberOfPlayers, int timeInSeconds, int playerPosition, string playerTankId)
		{
			Dictionary<string, object> eventData = GetBaseGameEventData(mapId, modeId, numberOfPlayers);
			eventData.Add("timeInSeconds", timeInSeconds);
			eventData.Add("playerPosition", playerPosition);
			eventData.Add("playerTankId", playerTankId);
			LogCustomEvent("MultiplayerGamePlayerBailed", eventData);
		}

		/// <summary>
		/// Suicide in multiplayer game
		/// </summary>
		/// <param name="mapId">Map identifier.</param>
		/// <param name="modeId">Mode identifier.</param>
		/// <param name="playerTankId">Player tank identifier.</param>
		/// <param name="weaponId">Weapon identifier.</param>
		public static void MultiplayerSuicide(string mapId, string modeId, string playerTankId, string weaponId)
		{
			Dictionary<string, object> eventData = new Dictionary<string, object>();
			eventData.Add("mapId", mapId);
			eventData.Add("modeId", modeId);
			eventData.Add("playerTankId", playerTankId);
			eventData.Add("weaponId", weaponId);
			LogCustomEvent("MultiplayerSuicide", eventData);
		}

		/// <summary>
		/// Tank is killed in multiplayer
		/// </summary>
		/// <param name="mapId">Map identifier.</param>
		/// <param name="modeId">Mode identifier.</param>
		/// <param name="killedTankId">Killed tank identifier.</param>
		/// <param name="killerTankId">Killer tank identifier.</param>
		/// <param name="weaponId">Weapon identifier.</param>
		public static void MultiplayerTankKilled(string mapId, string modeId, string killedTankId, string killerTankId, string weaponId)
		{
			Dictionary<string, object> eventData = new Dictionary<string, object>();
			eventData.Add("mapId", mapId);
			eventData.Add("modeId", modeId);
			eventData.Add("killedTankId", killedTankId);
			eventData.Add("killerTankId", killerTankId);
			eventData.Add("weaponId", weaponId);
			LogCustomEvent("MultiplayerTankKilled", eventData);
		}

		/// <summary>
		/// Wrapper function for actually logging the analytics custom event
		/// </summary>
		/// <param name="eventName">Event name.</param>
		/// <param name="eventData">Event data.</param>
		private static void LogCustomEvent(string eventName, Dictionary<string, object> eventData)
		{
			UnityAnalytics.Analytics.CustomEvent(eventName, eventData);
		}

		/// <summary>
		/// Gets the base game event data.
		/// </summary>
		/// <returns>The base game event data.</returns>
		/// <param name="mapId">Map identifier.</param>
		/// <param name="modeId">Mode identifier.</param>
		/// <param name="numberOfPlayers">Number of players.</param>
		private static Dictionary<string, object> GetBaseGameEventData(string mapId, string modeId, int numberOfPlayers)
		{
			Dictionary<string, object> eventData = new Dictionary<string, object>();
			eventData.Add("mapId", mapId);
			eventData.Add("modeId", modeId);
			eventData.Add("numberOfPlayers", numberOfPlayers);
			return eventData;
		}
	}
}