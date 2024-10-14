using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    [UxmlElement]
    public partial class MenuButton : Button
    {
        public MenuButton()
        {
            AddToClassList("bt-mainMenu");
            var arrowLeft = new VisualElement();
            arrowLeft.name = "arrow";
            arrowLeft.AddToClassList("ve-buttonHover");
            Add(arrowLeft);

            var arrowRight = new VisualElement();
            arrowRight.name = "arrow";
            arrowRight.AddToClassList("ve-buttonHover");
            arrowRight.AddToClassList("mirror");

            Add(arrowRight);

        }
    }
}
