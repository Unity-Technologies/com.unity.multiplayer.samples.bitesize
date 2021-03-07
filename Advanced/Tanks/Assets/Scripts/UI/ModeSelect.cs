using UnityEngine;
using Tanks.Rules;
using UnityEngine.UI;

namespace Tanks.UI
{
	/// <summary>
	/// UI for selecting different modes
	/// </summary>
	public class ModeSelect : Select
	{
		//Reference to the configuration mode list scriptable object
		[SerializeField]
		protected ModeList m_ModeList;

		//UI references
		[SerializeField]
		protected Text m_ModeName, m_Description;

		//Getter for SelectedMode
		public ModeDetails selectedMode
		{
			get
			{
				return m_ModeList[m_CurrentIndex];
			}
		}

		private void Awake()
		{
			m_ListLength = m_ModeList.Count;
			OnIndexChange();
		}
			
		//Called on selection change
		protected override void AssignByIndex()
		{
			ModeDetails details = m_ModeList[m_CurrentIndex];
			m_ModeName.text = details.modeName;
			m_Description.text = details.description;
		}
	}
}