using UnityEngine;
using TanksNetworkPlayer = Tanks.Networking.NetworkPlayer;

namespace Tanks.UI
{
	/// <summary>
	/// Interface to source colours for a given player or team from the server.
	/// </summary>
	public interface IColorProvider
	{
		Color ServerGetColor(TanksNetworkPlayer player);
		void Reset();
	}
}