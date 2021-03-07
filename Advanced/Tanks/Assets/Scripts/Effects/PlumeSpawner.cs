using UnityEngine;
using System.Collections;

namespace Tanks.Effects
{
	/// <summary>
	/// Plume spawner helper script for managing particle plumes
	/// </summary>
	public class PlumeSpawner : MonoBehaviour
	{
		[SerializeField]
		protected ParticleSystem m_PlumeParticles;
		[SerializeField]
		protected float m_DistanceBetweenParticles;
		[SerializeField]
		protected float m_PosDeviation;

		private ParticleSystem m_CachedParticleSystem;
		private ParticleSystem.Particle[] m_ParticleSpawners;
		private Vector3[] m_PrevPosition;
		private ParticleSystem.EmitParams m_EmitParams;

		private void Start()
		{
			m_EmitParams = new ParticleSystem.EmitParams();
		}

		/// <summary>
		/// Lazy initialization function
		/// </summary>
		private void Init()
		{
			if (m_CachedParticleSystem == null)
			{
				m_CachedParticleSystem = GetComponent<ParticleSystem>();
			}

			if (m_ParticleSpawners == null || m_ParticleSpawners.Length < m_CachedParticleSystem.main.maxParticles)
			{
				m_ParticleSpawners = new ParticleSystem.Particle[m_CachedParticleSystem.main.maxParticles];
			}

			if (m_PrevPosition == null || m_PrevPosition.Length != m_CachedParticleSystem.main.maxParticles)
			{
				m_PrevPosition = new Vector3[m_CachedParticleSystem.main.maxParticles];
			}
		}

		private void LateUpdate()
		{
			Init();

			// GetParticles is allocation free because we reuse the m_Particles buffer between updates
			int numParticlesAlive = m_CachedParticleSystem.GetParticles(m_ParticleSpawners);

			// Change only the particles that are alive
			for (int i = 0; i < numParticlesAlive; i++)
			{
				ParticleSystem.Particle p = m_ParticleSpawners[i];
				if (Vector3.Distance(p.position, m_PrevPosition[i]) > m_DistanceBetweenParticles)
				{
					m_EmitParams.startSize = p.GetCurrentSize(m_CachedParticleSystem);
					m_EmitParams.position = p.position + Random.insideUnitSphere * m_PosDeviation;
					m_PlumeParticles.Emit(m_EmitParams, 1);
				}
				m_PrevPosition[i] = p.position;
			}

			// Apply the particle changes to the particle system
			m_CachedParticleSystem.SetParticles(m_ParticleSpawners, numParticlesAlive);
		}
	}
}