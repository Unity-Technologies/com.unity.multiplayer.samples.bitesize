using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Tanks.Rules
{
	[Serializable]
	[CreateAssetMenu(fileName = "ModeList", menuName = "Modes/Create/Mode List", order = 1)]
	public class ModeList : ScriptableObject
	{
		[SerializeField]
		protected List<ModeDetails> m_Modes;

		public ModeDetails this [int index]
		{
			get { return m_Modes[index]; }
		}

		public int Count
		{
			get{ return m_Modes.Count; }
		}
	}
}
