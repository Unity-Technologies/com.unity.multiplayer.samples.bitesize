using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Tanks.UI
{
	//This class is responsible for displaying and updating the score display at the top of the HUD during multiplayer matches.
	public class HUDMultiplayerScore : MonoBehaviour
	{
		//Reference to the target score text object.
		[SerializeField]
		protected Text m_TargetValue;

		//Contains references to the combined icon/score text parent objects in the score overlay. Populated in editor.
		[SerializeField]
		protected GameObject[] m_ScoreParent;

		//Contains references to individual player icons. Populated in editor.
		[SerializeField]
		protected Image[] m_TeamIcons;

		//Contains references to individual score text elements. Populated in editor.
		[SerializeField]
		protected Text[] m_TeamScores;

		//On start, we pull our target score from the Game Settings and populate the HUD element.
		protected virtual void Start()
		{
			if (m_TargetValue != null && GameSettings.s_InstanceExists)
			{
				m_TargetValue.text = GameSettings.s_Instance.scoreTarget.ToString();
			}
		}

		//Receives arrays of colours and scores from the GameManager, and uses these to tint and populate child objects.
		//Icons without a value are disabled. A layout group in the prefab ensures that everything remains centered.
		//Sorting is done by the GameManager, so colours/scores should already be in the correct order.
		public void UpdateScoreDisplay(Color[] colours, int[] scores)
		{
			for (int i = 0; i < m_ScoreParent.Length; i++)
			{
				m_ScoreParent[i].SetActive(false);
			}

			for (int i = 0; i < colours.Length; i++)
			{
				m_ScoreParent[i].SetActive(true);

				m_TeamIcons[i].color = colours[i];
				m_TeamScores[i].text = scores[i].ToString();
			}
		}
	}
}