using UnityEngine;
using System.Collections;

public class EveryplayFaceCamSettings : MonoBehaviour
{
    public bool previewVisible = true;
    public int iPhonePreviewSideWidth = 64;
    public int iPhonePreviewPositionX = 16;
    public int iPhonePreviewPositionY = 16;
    public int iPhonePreviewBorderWidth = 2;
    public int iPadPreviewSideWidth = 96;
    public int iPadPreviewPositionX = 24;
    public int iPadPreviewPositionY = 24;
    public int iPadPreviewBorderWidth = 2;
    public Color previewBorderColor = Color.white;
    public Everyplay.FaceCamPreviewOrigin previewOrigin = Everyplay.FaceCamPreviewOrigin.BottomRight;
    public bool previewScaleRetina = true;
    public bool audioOnly = false;

    void Start()
    {
        if (Everyplay.GetUserInterfaceIdiom() == (int) Everyplay.UserInterfaceIdiom.iPad)
        {
            Everyplay.FaceCamSetPreviewSideWidth(iPadPreviewSideWidth);
            Everyplay.FaceCamSetPreviewBorderWidth(iPadPreviewBorderWidth);
            Everyplay.FaceCamSetPreviewPositionX(iPadPreviewPositionX);
            Everyplay.FaceCamSetPreviewPositionY(iPadPreviewPositionY);
        }
        else
        {
            Everyplay.FaceCamSetPreviewSideWidth(iPhonePreviewSideWidth);
            Everyplay.FaceCamSetPreviewBorderWidth(iPhonePreviewBorderWidth);
            Everyplay.FaceCamSetPreviewPositionX(iPhonePreviewPositionX);
            Everyplay.FaceCamSetPreviewPositionY(iPhonePreviewPositionY);
        }

        Everyplay.FaceCamSetPreviewBorderColor(previewBorderColor.r, previewBorderColor.g, previewBorderColor.b, previewBorderColor.a);
        Everyplay.FaceCamSetPreviewOrigin(previewOrigin);
        Everyplay.FaceCamSetPreviewScaleRetina(previewScaleRetina);
        Everyplay.FaceCamSetPreviewVisible(previewVisible);

        Everyplay.FaceCamSetAudioOnly(audioOnly);
    }
}
