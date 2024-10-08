using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    public static class WorldspaceUtils
    {
        public static Quaternion LookAtCameraY(Camera cam, Transform t)
        {
            var lookAtVector = t.position - cam.transform.position ;
            lookAtVector.y = 0;
            Debug.DrawRay(t.position, lookAtVector, Color.red);
            var rotation = Quaternion.LookRotation( lookAtVector, Vector3.up);
            return Quaternion.Euler(0f, 360f - rotation.eulerAngles.y, 0f);
        }

        public static void TranslateVEWorldspaceInPixelSpace(UIDocument doc , VisualElement element, Transform objectInWorldSpace, float yOffset = 0f)
        {
            var pixelsPerUnit = doc.panelSettings.referenceSpritePixelsPerUnit;
            var x = new Length(objectInWorldSpace.transform.position.x*pixelsPerUnit, LengthUnit.Pixel);
            var y = new Length((objectInWorldSpace.transform.position.y+ yOffset)*pixelsPerUnit*-1f, LengthUnit.Pixel);
            var z = objectInWorldSpace.transform.position.z*-1f * pixelsPerUnit;
            element.style.translate = new StyleTranslate(new Translate(x, y, z));
        }
    }
}
