using UnityEngine;
using Tanks.Data;
using UnityEngine.UI;

namespace Tanks.UI
{
	//Name panel used in customization screen
	public class NamePanel : MonoBehaviour
	{
		[SerializeField]
		protected Text m_NameLabel;

		protected void Start()
		{
			UpdateName();
		}

		protected void OnEnable()
		{
			UpdateName();
		}

		public void UpdateName()
		{
			PlayerDataManager playerData = PlayerDataManager.s_Instance;
			if (playerData != null)
			{
				m_NameLabel.text = playerData.playerName;
			}
		}
	}
}