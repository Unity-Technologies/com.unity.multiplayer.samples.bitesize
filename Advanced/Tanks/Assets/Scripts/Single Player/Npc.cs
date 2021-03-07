using UnityEngine;
using Tanks.Rules.SinglePlayer;
using Tanks;
using Tanks.Effects;
using Tanks.Data;
using UnityEngine.UI;
using Tanks.Explosions;
using Tanks.TankControllers;

namespace Tanks.SinglePlayer
{
	/// <summary>
	/// Npc base class that registers death with single player rules processor
	/// </summary>
	public class Npc : MonoBehaviour, IDamageObject
	{
		[SerializeField]
		protected float m_MaximumHealth = 50;
		private float m_CurrentHealth;
		private bool m_IsDead = false;

		[SerializeField]
		protected Slider m_HealthSlider;

		[SerializeField]
		protected Transform m_HealthSliderCanvas;

		[SerializeField]
		protected GameObject[] m_Meshes;

		[SerializeField]
		protected ExplosionSettings m_ExplosionDefinition;

		public bool isAlive { get { return !m_IsDead; } }

		protected Collider m_MyCollider;

		private OfflineRulesProcessor m_RuleProcessor;

		private DamageOutlineFlash m_DamageFlash;

		void Awake()
		{
			m_DamageFlash = GetComponent<DamageOutlineFlash>();
		}

		void Update()
		{
			if (m_HealthSliderCanvas != null)
			{
				Vector3 screenPoint = Camera.main.WorldToScreenPoint(m_HealthSliderCanvas.transform.position);
				screenPoint.z = 0f;

				m_HealthSliderCanvas.transform.LookAt(Camera.main.ScreenToWorldPoint(screenPoint));
			}
		}

		// Use this for initialization
		void Start()
		{
			m_CurrentHealth = m_MaximumHealth;
			m_MyCollider = GetComponent<Collider>();
			LazyLoadRuleProcessor();
		}

		private void SetMeshesActive(bool isActive)
		{
			int length = m_Meshes.Length;
			for (int i = 0; i < length; i++)
			{
				m_Meshes[i].SetActive(isActive);
			}
		}

		private void LazyLoadRuleProcessor()
		{
			if (m_RuleProcessor != null ||
			    GameManager.s_Instance == null)
			{
				return;
			}

			m_RuleProcessor = GameManager.s_Instance.rulesProcessor as OfflineRulesProcessor;
		}

		protected virtual void OnDied()
		{
			LazyLoadRuleProcessor();
			if (m_RuleProcessor != null)
			{
				m_RuleProcessor.DestroyNpc(this);
			}

			if (m_ExplosionDefinition != null)
			{
				ExplosionManager.s_Instance.SpawnExplosion(transform.position, Vector3.up, null, 9999, m_ExplosionDefinition, false);
			}

			Destroy(gameObject);
		}

		public Vector3 GetPosition()
		{
			return transform.position;
		}

		public void Damage(float damage)
		{
			m_CurrentHealth -= damage;

			if (m_HealthSlider != null)
			{
				m_HealthSlider.value = m_CurrentHealth / m_MaximumHealth;
			}

			if (m_DamageFlash != null)
			{
				//if(lastDamagedBy == GameManager.s_Instance.GetLocalPlayerID())
				{
					m_DamageFlash.StartDamageFlash();
				}
			}

			if (!m_IsDead && (m_CurrentHealth <= 0f))
			{
				OnDied();
			}
		}

		public void SetDamagedBy(int playerNumber, string explosionId)
		{
			// Doesn't need to do anything here
		}
	}
}