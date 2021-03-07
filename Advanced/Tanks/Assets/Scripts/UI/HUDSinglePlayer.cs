using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Tanks.Rules.SinglePlayer;
using Tanks.Rules.SinglePlayer.Objectives;
using Tanks.Rules;
using System.Collections.Generic;

namespace Tanks.UI
{
	/// <summary>
	/// Controls display of single-player-only HUD elements.
	/// </summary>
	public class HUDSinglePlayer : MonoBehaviour
	{
		//Timer output text object.
		[SerializeField]
		protected Text m_Timer;

		//References to objective prompts.
		[SerializeField]
		protected ObjectiveUI m_ObjectiveUiElement, m_SecondaryUiElement;

		//Transform to serve as parent for objective objects.
		[SerializeField]
		protected Transform m_ObjectiveParent;

		//Duration over which secondary objective prompts are faded.
		[SerializeField]
		protected float m_FadeOutTime = 4f;

		//Internal variables for timer display.
		protected float m_Time = 0f;
		protected int m_LastUpdateTime = 0;

		//Internal reference to the single-player rules processor.
		protected SinglePlayerRulesProcessor m_RulesProcessor;

		protected List<GameObject> m_SecondaryObjectives = new List<GameObject>();

		//Whether we have subscribed to the necessary events yet.
		private bool m_HasSubscribed = false;

		private void Awake()
		{
			gameObject.SetActive(false);
		}

		private void Start()
		{
			Subscribe();	
		}

		//Subscribe to the events we need to make this thing work. 
		private void Subscribe()
		{
			//Ensure that we only do it once.
			if (m_HasSubscribed)
			{
				return;
			}
	
			if (InGameOptionsMenu.s_InstanceExists && HUDController.s_InstanceExists)
			{
				m_HasSubscribed = true;
				InGameOptionsMenu.s_Instance.paused += OnPause;
				InGameOptionsMenu.s_Instance.resumed += OnResume;
				HUDController.s_Instance.enabledCanvas += OnEnabled;
			}
		}

		private void Update()
		{
			//If we have a timer text object to output to, increment our mission time and assign the rounded value to it.
			if (m_Timer == null)
			{
				return;
			}
		
			m_Time += Time.deltaTime;
			int flooredTime = Mathf.FloorToInt(m_Time);
            
			if (flooredTime != m_LastUpdateTime)
			{
				m_LastUpdateTime = flooredTime;
				m_Timer.text = flooredTime.ToString();
			}
		}

		/// <summary>
		/// Displays this HUD element.
		/// </summary>
		/// <param name="rulesProcessor">The rules processor for the active mission.</param>
		public void ShowHud(RulesProcessor rulesProcessor)
		{
			//If we haven't already, subscribe to all the necessary events.
			Subscribe();
			this.m_RulesProcessor = rulesProcessor as SinglePlayerRulesProcessor;
            
			if (this.m_RulesProcessor == null)
			{
				Debug.LogError("Tried to show single player HUD in non-singleplayer game!!!");
				return;
			}

			gameObject.SetActive(true);
			m_SecondaryObjectives.Clear();
    		
			//Iterate through objectives for this mission and instantiate the prompts accordingly.
			Objective[] objectives = this.m_RulesProcessor.objectiveInstances;

			int lengthOfArray = objectives.Length;
            
			for (int i = 0; i < lengthOfArray; i++)
			{	
				Objective objective = objectives[i];
				if (objective.isPrimaryObjective)
				{
					ObjectiveUI newObjectiveUi = Instantiate<ObjectiveUI>(m_ObjectiveUiElement);
					newObjectiveUi.transform.SetParent(m_ObjectiveParent, false);
					newObjectiveUi.Setup(objective);
					newObjectiveUi.transform.SetAsFirstSibling();
				}
				else
				{
					ObjectiveUI newObjectiveUi = Instantiate<ObjectiveUI>(m_SecondaryUiElement);
					newObjectiveUi.transform.SetParent(m_ObjectiveParent, false);
					newObjectiveUi.Setup(objective);
					m_SecondaryObjectives.Add(newObjectiveUi.gameObject);
				}
			}
		}

		//Mae the secondary objective prompts visible if the game is paused.
		protected void OnPause()
		{
			EnableSecondaryObjectives(true);
		}

		//Fade the secondary objectives out on return to game.
		protected void OnResume()
		{
			FadeOutSecondaryObjectives();
		}

		protected void OnDestroy()
		{
			if (InGameOptionsMenu.s_InstanceExists)
			{
				InGameOptionsMenu.s_Instance.paused -= OnPause;
				InGameOptionsMenu.s_Instance.resumed -= OnResume;
			}

			if (HUDController.s_InstanceExists)
			{
				HUDController.s_Instance.enabledCanvas -= OnEnabled;
			}
		}

		protected void OnEnabled(bool enabled)
		{
			if (enabled)
			{
				FadeOutSecondaryObjectives();
			}
		}

		protected void EnableSecondaryObjectives(bool enabled)
		{
			int count = m_SecondaryObjectives.Count;
			for (int i = 0; i < count; i++)
			{
				GameObject secondaryObjective = m_SecondaryObjectives[i];
				secondaryObjective.SetActive(enabled);
				secondaryObjective.GetComponent<CanvasGroup>().alpha = 1f;
			}
		}

		protected void FadeOutSecondaryObjectives()
		{
			int count = m_SecondaryObjectives.Count;
			for (int i = 0; i < count; i++)
			{
				m_SecondaryObjectives[i].GetComponent<FadingGroup>().StartFade(Fade.Out, m_FadeOutTime);
			}
		}
	}
}
