using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Tanks.Map
{
	/// <summary>
	/// Concrete implementation of multiplayer map list
	/// </summary>
	[CreateAssetMenu(fileName = "MapList", menuName = "Maps/Create List", order = 1)]
	public class MapList : MapListBase<MapDetails>
	{
	}
}