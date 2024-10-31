using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    [UxmlElement]
    public partial class MenuButton : Button
    {
        public MenuButton()
        {
            AddToClassList("menu-button");
            var arrowLeft = new VisualElement();
            arrowLeft.name = "focus-indicator-left";
            arrowLeft.AddToClassList("menu-button__focus-indicator");
            Add(arrowLeft);

            var arrowRight = new VisualElement();
            arrowRight.name = "focus-indicator-right";
            arrowRight.AddToClassList("menu-button__focus-indicator");
            arrowRight.AddToClassList("menu-button__focus-indicator--right");
            Add(arrowRight);
        }
    }
}
