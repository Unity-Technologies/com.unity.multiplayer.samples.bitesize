using UnityEngine;
using System.Collections;

public class EveryplayInGameFaceCam : MonoBehaviour
{
    public Material targetMaterial;
    public int textureSideWidth = 128;
    public TextureFormat textureFormat = TextureFormat.RGBA32;
    public TextureWrapMode textureWrapMode = TextureWrapMode.Repeat;
    private Texture defaultTexture;
    private Texture2D targetTexture;

    void Awake()
    {
        targetTexture = new Texture2D(textureSideWidth, textureSideWidth, textureFormat, false);
        targetTexture.wrapMode = textureWrapMode;

        if (targetMaterial && targetTexture)
        {
            defaultTexture = targetMaterial.mainTexture;

            Everyplay.FaceCamSetTargetTexture(targetTexture);

            Everyplay.FaceCamSessionStarted += OnSessionStart;
            Everyplay.FaceCamSessionStopped += OnSessionStop;
        }
    }

    void OnSessionStart()
    {
        if (targetMaterial && targetTexture)
        {
            targetMaterial.mainTexture = targetTexture;
        }
    }

    void OnSessionStop()
    {
        targetMaterial.mainTexture = defaultTexture;
    }

    void OnDestroy()
    {
        Everyplay.FaceCamSessionStarted -= OnSessionStart;
        Everyplay.FaceCamSessionStopped -= OnSessionStop;
    }
}
