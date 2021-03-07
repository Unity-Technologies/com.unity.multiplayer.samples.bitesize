using UnityEngine;
using UnityEngine.UI;


namespace Tanks.UI
{
	//Helper class for animating EveryPlay thumbnails
	public class ThumbnailAnimator : MonoBehaviour
	{
		protected EveryplayThumbnailPool m_ThumbnailPool;
		[SerializeField]
		protected Image m_Sprite1;
		[SerializeField]
		protected Image m_Sprite2;
		[SerializeField]
		protected float m_FadeSpeed;
		[SerializeField]
		protected float m_HoldTime;

		private float m_FadeCounter = 0;
		private float m_HoldCounter = 0;

		private int m_Index;

		protected virtual void OnEnable()
		{
			// Reacquire persistent component
			m_ThumbnailPool = EveryplayThumbnailPool.instance;

			m_FadeCounter = 1;
			m_HoldCounter = m_HoldTime;
			m_Index = 0;

			m_Sprite1.sprite = CreateSpriteForThumbnailTexture(m_ThumbnailPool.thumbnailTextures[0]);
			m_Sprite2.enabled = false;

			UpdateSprites();
		}

		//Create Sprite from Texture2D
		private Sprite CreateSpriteForThumbnailTexture(Texture2D tex)
		{
			return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
		}

		//Swaps between the two thumbnail sprite
		protected virtual void Update()
		{
			if (m_HoldCounter > 0)
			{
				m_HoldCounter -= Time.deltaTime;
			}
			else if (m_FadeCounter > 0)
			{
				m_Sprite2.enabled = true;
				m_Sprite2.color = new Color(1, 1, 1, 1 - m_FadeCounter);
				m_FadeCounter -= Time.deltaTime * m_FadeSpeed;
			}
			else
			{
				// Swap sprites
				Destroy(m_Sprite1.sprite);
				m_Sprite1.sprite = m_Sprite2.sprite;
				m_Sprite2.enabled = false;

				m_FadeCounter = 1;
				m_HoldCounter = m_HoldTime;
				m_Index = (m_Index + 1) % m_ThumbnailPool.availableThumbnailCount;
				UpdateSprites();
			}
		}

		//Updates visuals
		private void UpdateSprites()
		{
			int nextIndex = (m_Index + 1) % m_ThumbnailPool.availableThumbnailCount;
			m_Sprite2.sprite = CreateSpriteForThumbnailTexture(m_ThumbnailPool.thumbnailTextures[nextIndex]);
		}
	}
}