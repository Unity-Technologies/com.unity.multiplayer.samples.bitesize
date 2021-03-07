using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Tanks.Map
{
	/// <summary>
	/// Concrete implementation of single player map list
	/// </summary>
	[CreateAssetMenu(fileName = "SinglePlayerMapList", menuName = "Maps/Create Single Player List", order = 1)]
	public class SinglePlayerMapList : MapListBase<SinglePlayerMapDetails>
	{
	}
}
