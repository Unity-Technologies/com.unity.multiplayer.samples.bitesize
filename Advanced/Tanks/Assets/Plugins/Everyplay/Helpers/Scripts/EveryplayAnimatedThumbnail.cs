using UnityEngine;
using System.Collections;

public class EveryplayAnimatedThumbnail : MonoBehaviour
{
    private EveryplayThumbnailPool thumbnailPool;
    private Renderer mainRenderer;
    private Texture defaultTexture;
    private int currentIndex;
    private bool transitionInProgress;
    private float blend;

    void Awake()
    {
        mainRenderer = GetComponent<Renderer>();
    }

    void Start()
    {
        thumbnailPool = (EveryplayThumbnailPool) FindObjectOfType(typeof(EveryplayThumbnailPool));

        if (thumbnailPool)
        {
            defaultTexture = mainRenderer.material.mainTexture;
            ResetThumbnail();
        }
        else
        {
            Debug.Log("Everyplay thumbnail pool not found or no material was defined!");
        }
    }

    void OnDestroy()
    {
        StopTransitions();
    }

    void OnDisable()
    {
        StopTransitions();
    }

    void ResetThumbnail()
    {
        currentIndex = -1;

        StopTransitions();

        blend = 0.0f;
        mainRenderer.material.SetFloat("_Blend", blend);
        if (mainRenderer.material.mainTexture != defaultTexture)
        {
            mainRenderer.material.mainTextureScale = Vector2.one;
            mainRenderer.material.mainTexture = defaultTexture;
        }
    }

    private IEnumerator CrossfadeTransition()
    {
        while (blend < 1.0f && transitionInProgress)
        {
            blend += 0.1f;
            mainRenderer.material.SetFloat("_Blend", blend);
            yield return new WaitForSeconds(1 / 40.0f);
        }

        mainRenderer.material.mainTexture = mainRenderer.material.GetTexture("_MainTex2");
        mainRenderer.material.mainTextureScale = mainRenderer.material.GetTextureScale("_MainTex2");

        blend = 0.0f;
        mainRenderer.material.SetFloat("_Blend", blend);

        transitionInProgress = false;
    }

    private void StopTransitions()
    {
        transitionInProgress = false;
        StopAllCoroutines();
    }

    void Update()
    {
        if (thumbnailPool && !transitionInProgress)
        {
            if (thumbnailPool.availableThumbnailCount > 0)
            {
                // Don't animate the first frame
                if (currentIndex < 0)
                {
                    currentIndex = 0;
                    mainRenderer.material.mainTextureScale = thumbnailPool.thumbnailScale;
                    mainRenderer.material.mainTexture = thumbnailPool.thumbnailTextures[currentIndex];
                }
                // Animate
                else if (thumbnailPool.availableThumbnailCount > 1)
                {
                    if ((Time.frameCount % 50) == 0)
                    {
                        currentIndex++;

                        if (currentIndex >= thumbnailPool.availableThumbnailCount)
                        {
                            currentIndex = 0;
                        }

                        mainRenderer.material.SetTextureScale("_MainTex2", thumbnailPool.thumbnailScale);
                        mainRenderer.material.SetTexture("_MainTex2", thumbnailPool.thumbnailTextures[currentIndex]);

                        transitionInProgress = true;

                        StartCoroutine("CrossfadeTransition");
                    }
                }
            }
            else if (currentIndex >= 0)
            {
                ResetThumbnail();
            }
        }
    }
}
