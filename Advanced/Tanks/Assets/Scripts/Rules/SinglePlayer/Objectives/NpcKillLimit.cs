using UnityEngine;
using System.Collections;
using Tanks.SinglePlayer;

namespace Tanks.Rules.SinglePlayer.Objectives
{
	/// <summary>
	/// Npc kill limit - concrete type of KillLimit where T is an Npc
	/// </summary>
	public class NpcKillLimit : KillLimit<Npc>
	{
	}
}
