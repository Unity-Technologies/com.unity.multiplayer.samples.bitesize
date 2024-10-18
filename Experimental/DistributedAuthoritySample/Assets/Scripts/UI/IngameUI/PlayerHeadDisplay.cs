using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    class PlayerHeadDisplay : VisualElement
    {
        /// <summary>
        /// Display that is shown above a players head
        /// </summary>
        /// <param name="asset">Uxml to be used</param>
        internal PlayerHeadDisplay(VisualTreeAsset asset)
        {
            AddToClassList("player-top-ui");
            Add(asset.CloneTree());
            ShowMicIcon(false);
        }

        internal void SetPlayerName(string playerName)
        {
            this.Q<Label>().text = playerName;
        }

        void ShowMicIcon(bool show)
        {
            this.Q<VisualElement>("mic-icon").style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
