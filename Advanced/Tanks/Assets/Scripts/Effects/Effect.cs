using UnityEngine;

namespace Tanks.Effects
{
	/// <summary>
	/// Auto clean-up object for effects
	/// </summary>
	public class Effect : MonoBehaviour
	{
		[SerializeField]
		protected bool m_AutoDestroy;

		public void Awake()
		{
			float lifetime = 0;
			ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>();
			
			if (systems != null)
			{
				for (int i = 0; i < systems.Length; ++i)
				{
					ParticleSystem system = systems[i];
					system.Play();
					lifetime = Mathf.Max(system.main.duration, lifetime);
				}
			}
			
			AudioSource[] sources = GetComponentsInChildren<AudioSource>();
			if (sources != null)
			{
				for (int i = 0; i < sources.Length; ++i)
				{
					AudioSource source = sources[i];
					if (source.clip != null)
					{
						lifetime = Mathf.Max(lifetime, source.clip.length);
					}
				}
			}
			
			if (m_AutoDestroy)
			{
				Destroy(gameObject, lifetime);
			}
		}
	}

}