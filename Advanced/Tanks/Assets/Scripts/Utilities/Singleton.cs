using UnityEngine;
using System.Collections;
using System;

namespace Tanks.Utilities
{
	/// <summary>
	/// Singleton class
	/// </summary>
	/// <typeparam name="T">Type of the singleton</typeparam>
	public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
	{
		private static T s_instance;

		/// <summary>
		/// The static reference to the instance
		/// </summary>
		public static T s_Instance
		{
			get
			{
				return s_instance;
			}
			protected set
			{
				s_instance = value;
			}
		}

		/// <summary>
		/// Gets whether an instance of this singleton exists
		/// </summary>
		public static bool s_InstanceExists { get { return s_instance != null; } }

		public static event Action InstanceSet;

		/// <summary>
		/// Awake method to associate singleton with instance
		/// </summary>
		protected virtual void Awake()
		{
			if (s_instance != null)
			{
				Destroy(gameObject);
			}
			else
			{
				s_instance = (T)this;
				if (InstanceSet != null)
				{
					InstanceSet();
				}
			}
		}

		/// <summary>
		/// OnDestroy method to clear singleton association
		/// </summary>
		protected virtual void OnDestroy()
		{
			if (s_instance == this)
			{
				s_instance = null;
			}
		}
	}
}
