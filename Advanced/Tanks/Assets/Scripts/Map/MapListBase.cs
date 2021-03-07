using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Tanks.Map
{
	/// <summary>
	/// Map list base - provides indexer implementation. It is a scriptable object
	/// </summary>
	public abstract class MapListBase<T> : ScriptableObject where T: MapDetails
	{
		[SerializeField]
		private List<T> m_Details;

		/// <summary>
		/// Gets the <see cref="Tanks.Map.MapListBase"/> at the specified index.
		/// </summary>
		/// <param name="index">Index.</param>
		public T this [int index]
		{
			get { return m_Details[index]; }
		}

		/// <summary>
		/// Number of elements in the list
		/// </summary>
		/// <value>The count.</value>
		public int Count
		{
			get{ return m_Details.Count; }
		}
	}
}