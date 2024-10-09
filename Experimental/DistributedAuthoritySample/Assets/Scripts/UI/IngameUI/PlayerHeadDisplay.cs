using UnityEngine;
using UnityEngine.UIElements;

public class PlayerHeadDisplay : VisualElement
{
    public PlayerHeadDisplay(VisualTreeAsset asset)
    {
        this.AddToClassList("player-top-ui");
        this.Add(asset.CloneTree());
        ShowMicIcon(false);
    }

    public void SetPlayerName(string name)
    {
        this.Q<Label>().text = name;
    }

    void ShowMicIcon(bool show)
    {
        this.Q<VisualElement>("mic-icon").style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
