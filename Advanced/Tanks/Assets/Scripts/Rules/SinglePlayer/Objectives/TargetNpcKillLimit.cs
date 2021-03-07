using UnityEngine;
using System.Collections;
using Tanks.SinglePlayer;

namespace Tanks.Rules.SinglePlayer.Objectives
{
	/// <summary>
	/// Target npc kill limit - concrete tyoe of KillLimit where T is a TargetNpc
	/// </summary>
	public class TargetNpcKillLimit : KillLimit<TargetNpc>
	{
	}
}
