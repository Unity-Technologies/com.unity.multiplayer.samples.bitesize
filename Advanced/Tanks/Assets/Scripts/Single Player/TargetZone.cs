using UnityEngine;
using System.Collections;
using Tanks.Rules.SinglePlayer;

namespace Tanks.SinglePlayer
{
	/// <summary>
	/// Base class target zone - handles either player or npc reaching a zone. Passes this to the rule processor
	/// </summary>
	[RequireComponent(typeof(Collider))]
	public class TargetZone : MonoBehaviour
	{
		/// <summary>
		/// Cached rule processor
		/// </summary>
		private OfflineRulesProcessor m_RuleProcessor;

		/// <summary>
		/// Passes the triggering collider to the rule processor and to the HandleTrigger method
		/// </summary>
		/// <param name="c">C.</param>
		protected virtual void OnTriggerEnter(Collider c)
		{
			LazyLoadRuleProcessor();
			if (m_RuleProcessor != null)
			{
				m_RuleProcessor.EntersZone(c.gameObject, this);
			}

			HandleTrigger(c.gameObject);
		}

		/// <summary>
		/// Set the navigator to be complte
		/// </summary>
		/// <param name="zoneObject">Zone object.</param>
		protected virtual void HandleTrigger(GameObject zoneObject)
		{
			Navigator navigator = zoneObject.GetComponent<Navigator>();
			if (navigator != null)
			{
				navigator.SetComplete();
			}
		}

		/// <summary>
		/// Lazy load the rule processor
		/// </summary>
		private void LazyLoadRuleProcessor()
		{
			if (m_RuleProcessor != null)
			{
				return;
			}       

			if (GameManager.s_Instance != null)
			{
				m_RuleProcessor = GameManager.s_Instance.rulesProcessor as OfflineRulesProcessor;
			}
		}
	}
}
