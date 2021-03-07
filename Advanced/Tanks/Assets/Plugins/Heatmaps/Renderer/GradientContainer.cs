using System;
using UnityEngine;

public class GradientContainer : MonoBehaviour
{

    public Gradient ColorGradient;

    static Color s_HighDensityColor = new Color(1f, 0, 0, 1f);
    static Color s_MediumDensityColor = new Color(1f, 1f, 0, 1f);
    static Color s_LowDensityColor = new Color(0, 1f, 1f, 1f);


    public GradientContainer()
    {
        ColorGradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[3];
        colorKeys[0].color = s_LowDensityColor;
        colorKeys[0].time = 0f;
        colorKeys[1].color = s_MediumDensityColor;
        colorKeys[1].time = 0.5f;
        colorKeys[2].color = s_HighDensityColor;
        colorKeys[2].time = 1f;

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[3];
        alphaKeys[0].alpha = .2f;
        alphaKeys[0].time = 0f;
        alphaKeys[1].alpha = .5f;
        alphaKeys[1].time = .5f;
        alphaKeys[2].alpha = 1f;
        alphaKeys[2].time = 1f;

        ColorGradient.SetKeys(colorKeys, alphaKeys);
    }
}

