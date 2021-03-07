using UnityEngine;
using System.Collections;

namespace Tanks.UI
{
	/// <summary>
	/// Base class for select an item from the list which wraps
	/// </summary>
	public class Select : MonoBehaviour
	{
		protected int m_CurrentIndex = 0, m_ListLength;

		public int currentIndex
		{
			get
			{
				return m_CurrentIndex;
			}
		}

		//Called by button
		public void OnNextClick()
		{
			m_CurrentIndex++;
			OnIndexChange();
		}

		//Called by button
		public void OnPreviousClick()
		{
			m_CurrentIndex--;
			OnIndexChange();
		}

		//Called to force currentIndex to be within the bounds and handle updating the UI
		protected void OnIndexChange()
		{
			HandleBounds();
			AssignByIndex(); 
		}

		//Base method for updating the UI to reflect the current index
		protected virtual void AssignByIndex()
		{
		}

		//Force the currentIndex to be within the list bounds
		protected void HandleBounds()
		{
			if (m_CurrentIndex < 0)
			{
				m_CurrentIndex = m_ListLength - 1;
			}

			if (m_CurrentIndex >= m_ListLength)
			{
				m_CurrentIndex = 0;
			}
		}
	}
}
