using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    public static class UIUtils
    {
        internal const string s_ActiveUSSClass = "show";
        internal const string s_InactiveUSSClass = "hide";


        /// <summary>
        /// Returns a rotation that looks at the target in worldspace. The rotation is only applied to the Y axis.
        /// </summary>
        /// <param name="target">Target Object most of the time Camera</param>
        /// <param name="origin">The object that should get rotated</param>
        /// <returns>Rotation that looks at the target in worldspace </returns>
        static Quaternion LookAtYAxis(Transform target, Transform origin)
        {
            var lookAtVector = origin.position - target.position;
            lookAtVector.y = 0;
            var rotation = Quaternion.LookRotation(lookAtVector, Vector3.up);
            return rotation;
        }

        /// <summary>
        /// Translates a UIDocument (GameObject with UI Document) in Worldspace and rotates it to the camera.
        /// </summary>
        /// <param name="uiDocument">The UI Document that should get transfomred</param>
        /// <param name="worldspaceTransform">The transform in Worldspace (a GameObject in the Scene) the VisualElement should be placed to.</param>
        /// <param name="camera">The camera the UI Document should get rotatet to</param>
        /// <param name="yOffset">Offset in Y from the position provided in worldspaceTransform</param>
        public static void TransformUIDocumentWorldspace(UIDocument uiDocument, Camera camera, Transform worldspaceTransform, float yOffset = 0f)
        {
            var position = worldspaceTransform.position;
            uiDocument.gameObject.transform.position = new Vector3(position.x, position.y + yOffset, position.z);
            uiDocument.gameObject.transform.rotation = LookAtYAxis(camera.transform, worldspaceTransform);
        }

        /// <summary>
        /// Positions a VisualElement in Screenspace to match a worldspace transform.
        /// </summary>
        /// <param name="camera">The current camera</param>
        /// <param name="worldspaceTransform">Transform of the worldspace object</param>
        /// <param name="visualElement">VisualElement that should get translated in Screenspace</param>
        /// <param name="yOffset">Offset in Y from the position provided in worldspaceTransform</param>
        public static void TranslateVEWorldToScreenspace(this VisualElement visualElement, Camera camera, Transform worldspaceTransform, float yOffset = 0f)
        {
            var positionInWorldSpace = new Vector3(worldspaceTransform.position.x, worldspaceTransform.position.y + yOffset, worldspaceTransform.position.z);
            Vector2 screenSpacePosition = camera.WorldToScreenPoint(positionInWorldSpace);
            if (visualElement.panel == null)
            {
                //Todo: Happens on style change can be removed when finished.
                return;
            }

            Vector2 panelSpacePosition = RuntimePanelUtils.ScreenToPanel(visualElement.panel, new Vector2(screenSpacePosition.x, Screen.height - screenSpacePosition.y));
            visualElement.style.left = panelSpacePosition.x;
            visualElement.style.top = panelSpacePosition.y;
        }

        // Just a helper extension method to get the first child of a VisualElement without using LINQ.
        public static VisualElement GetFirstChild(this VisualElement element)
        {
            foreach (var item in element.Children())
            {
                return item;
            }
            return null;
        }
    }
}
