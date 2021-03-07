using UnityEngine;
using System.Collections;
using System;

namespace Tanks.Rules
{
	[Serializable]
	public class ModeDetails
	{
		[SerializeField]
		protected string m_Name;

		public string modeName
		{
			get{ return m_Name; }
		}

		[SerializeField]
		protected string m_Abbreviation;

		public string abbreviation
		{
			get{ return m_Abbreviation; }
		}

		[SerializeField]
		protected string m_Description;

		public string description
		{
			get { return m_Description; }
		}

		[SerializeField]
		protected string m_Id;

		public string id
		{
			get { return m_Id; }
		}

		[SerializeField]
		protected RulesProcessor m_RulesProcessor;

		public RulesProcessor rulesProcessor
		{
			get
			{
				return m_RulesProcessor;
			}
		}

		[SerializeField]
		protected GameObject m_HudScoreObject;

		public GameObject hudScoreObject
		{
			get
			{
				return m_HudScoreObject;
			}
		}

		private int m_Index;

		public int index
		{
			get { return m_Index; }
			set { m_Index = value; }
		}

		public ModeDetails(string name, string description)
		{
			this.m_Name = name;
			this.m_Description = description;
		}

		public ModeDetails(string name, string description, RulesProcessor rulesProcessor)
		{
			this.m_Name = name;
			this.m_Description = description;
			this.m_RulesProcessor = rulesProcessor;
		}
	}
}
