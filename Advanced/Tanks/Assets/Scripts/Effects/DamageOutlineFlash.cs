using UnityEngine;
using System.Collections;

namespace Tanks.Effects
{
	//This class allows the cel outline of in-game objects to be pulsed. Used primarily to indicate that something has taken damage.
	public class DamageOutlineFlash : MonoBehaviour 
	{
		//The colour that the outline will pulse to.
		[SerializeField]
		protected Color m_DamageColor = Color.red;

		//The maximum value that the border will be pulsed to.
		[SerializeField]
		protected float m_DamageBorderPulseAmount = 4f;

		//The rate of fade as well as the internal fade count.
		[SerializeField]
		protected float m_DamageFadeDuration = 0.15f;
		private float m_DamageFadeTime = 0f;

		//List of renderers to apply the effect to. This class assumes that it is populated with renderers with a shared material that uses the OutlineShadow shader.
		[SerializeField]
		protected Renderer[] m_BorderRenderers;

		//Internal reference to the border's original shared material.
		private Material m_BorderBaseMaterial;

		//Internal reference to the border's original thickness.
		private float m_BorderBaseThickness;

		//Start the effect by setting the damageFadeTime to the damageFadeDuration.
		public void StartDamageFlash()
		{
			m_DamageFadeTime = m_DamageFadeDuration;
		}

		private void Awake()
		{
			if (m_BorderRenderers.Length > 0)
			{
				//Get the first renderer in the list and its shared material reference. As all borders should share the same outline material, this reference is sufficient.
				m_BorderBaseMaterial = m_BorderRenderers[0].sharedMaterial;

				//Get the material's default border size.
				m_BorderBaseThickness = m_BorderBaseMaterial.GetFloat("_OutlineWidth");
			}
		}

		private void Update()
		{
			if (m_DamageFadeTime > 0f)
			{
				m_DamageFadeTime -= Time.deltaTime;

				for (int i = 0; i < m_BorderRenderers.Length; i++)
				{
					//Lerp the colour from the damageColour back to black.
					m_BorderRenderers[i].material.color = Color.Lerp(Color.black, m_DamageColor, m_DamageFadeTime / m_DamageFadeDuration);

					//Lerp the damage outline from the maximum thickness back to the base thickness. The visible border will "snap" to maximum size initially.
					m_BorderRenderers[i].material.SetFloat("_OutlineWidth", Mathf.Lerp(m_BorderBaseThickness, m_DamageBorderPulseAmount, m_DamageFadeTime / m_DamageFadeDuration));
				}
					
				if (m_DamageFadeTime <= 0f)
				{
					//When the effect is complete, we find ourselves with auto-instanced copies of the material on all our renderers because we've manipulated them directly.
					//We destroy these material instances and reset their references back to the shared material to avoid leakage.
					for (int i = 0; i < m_BorderRenderers.Length; i++)
					{
						Destroy(m_BorderRenderers[i].material);
						m_BorderRenderers[i].material = m_BorderBaseMaterial;
					}
				}
			}
		}
	}
}
