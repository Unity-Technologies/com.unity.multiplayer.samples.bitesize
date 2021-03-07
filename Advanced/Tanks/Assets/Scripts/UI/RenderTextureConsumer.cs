using UnityEngine;
using UnityEngine.UI;

namespace Tanks.UI
{
	/// <summary>
	/// Manager object for any object that uses a render texture on a given camera, to ensure there's only ever one
	/// </summary>
	[RequireComponent(typeof(RawImage))]
	public class RenderTextureConsumer : MonoBehaviour
	{
		/// <summary>
		/// The camera that the render texture is rendered from
		/// </summary>
		[SerializeField]
		protected Camera m_OffscreenCam;

		/// <summary>
		/// Static reference to currently active RT
		/// </summary>
		private static RenderTexture s_CurrentRt;

		/// <summary>
		/// Cached image component
		/// </summary>
		private RawImage m_Image;
		/// <summary>
		/// Cached rect transform component
		/// </summary>
		private RectTransform m_Rect;
		/// <summary>
		/// Cached canvas component
		/// </summary>
		private Canvas m_Canvas;

		/// <summary>
		/// Update current RT settings
		/// </summary>
		protected virtual void OnEnable()
		{
			UpdateSize();
		}

		/// <summary>
		/// Calculate the correct RT size for this rect transform
		/// </summary>
		protected void UpdateSize()
		{
			// Lazy gather components if they haven't been
			if (m_Image == null)
			{
				m_Image = GetComponent<RawImage>();
			}
			if (m_Rect == null)
			{
				m_Rect = GetComponent<RectTransform>();
			}
			if (m_Canvas == null)
			{
				m_Canvas = GetComponentInParent<Canvas>();
			}

			// Destroy existing RT if there is one
			if (s_CurrentRt != null)
			{
				if (m_OffscreenCam != null)
				{
					m_OffscreenCam.targetTexture = null;
				}

				s_CurrentRt.Release();
				Destroy(s_CurrentRt);
				s_CurrentRt = null;
			}

			// Create the RT
			if (m_OffscreenCam != null && m_Image != null)
			{
				Vector2 rectSize = m_Rect.rect.size * m_Canvas.scaleFactor;
				s_CurrentRt = new RenderTexture((int)rectSize.x, (int)rectSize.y, 16, RenderTextureFormat.ARGB32);
				if (QualitySettings.antiAliasing > 0)
				{
					s_CurrentRt.antiAliasing = QualitySettings.antiAliasing;
				}
				m_OffscreenCam.enabled = true;
				m_OffscreenCam.targetTexture = s_CurrentRt;
				m_Image.texture = s_CurrentRt;
			}
		}

		/// <summary>
		/// Update the RT size when the rect transform lays itself out
		/// </summary>
		protected void OnRectTransformDimensionsChange()
		{
			if (enabled && gameObject.activeInHierarchy)
			{
				UpdateSize();
			}
		}

		/// <summary>
		/// Release our RT
		/// </summary>
		protected virtual void OnDisable()
		{
			if (s_CurrentRt != null)
			{
				s_CurrentRt.Release();
				Destroy(s_CurrentRt);
				s_CurrentRt = null;
			}

			if (m_OffscreenCam != null)
			{
				m_OffscreenCam.enabled = false;
			}
		}
	}
}