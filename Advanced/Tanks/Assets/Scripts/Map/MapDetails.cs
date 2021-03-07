using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using Tanks.Data;

namespace Tanks.Map
{
	/// <summary>
	/// Serializable class acting as model for Maps - used in Multiplayer and is the base class of SinglePlayer maps
	/// </summary>
	[Serializable]
	public class MapDetails
	{
		[SerializeField]
		protected string m_Name;

		public string name
		{
			get{ return m_Name; }
		}
		
		//This is marked as a serialized field for debugging purposes only
		[SerializeField]
		protected bool m_IsLocked;

		public bool isLocked
		{
			get { return m_IsLocked; }
		}

		[SerializeField] [Multiline]
		protected string m_Description;

		public string description
		{
			get { return m_Description; }
		}

		[SerializeField]
		protected string m_SceneName, m_Id;

		public string sceneName
		{
			get { return m_SceneName; }
		}

		public string id
		{
			get { return m_Id; }
		}

		[SerializeField]
		protected Sprite m_Image;

		public Sprite image
		{
			get{ return m_Image; }
		}

		[SerializeField]
		protected int m_UnlockCost;

		public int unlockCost
		{
			get{ return m_UnlockCost; }
		}

		[SerializeField]
		protected MapEffectsGroup m_EffectsGroup;

		public MapEffectsGroup effectsGroup
		{
			get{ return m_EffectsGroup; }
		}

		[SerializeField]
		protected AudioClip m_LevelMusic;

		public AudioClip levelMusic
		{
			get{ return m_LevelMusic; }
		}
	}
}