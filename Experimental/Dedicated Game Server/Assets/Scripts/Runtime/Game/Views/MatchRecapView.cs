using Unity.Netcode;
using UnityEngine.UIElements;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class MatchRecapView : View<GameApplication>
    {
        Button m_ContinueButton;
        Label m_ResultLabel;
        VisualElement root;

        void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            root = uiDocument.rootVisualElement;

            m_ContinueButton = root.Q<Button>("continueButton");
            m_ContinueButton.RegisterCallback<ClickEvent>(OnClickContinue);

            m_ResultLabel = root.Query<Label>("resultLabel");
        }

        void OnDisable()
        {
            m_ContinueButton.UnregisterCallback<ClickEvent>(OnClickContinue);
        }

        internal void OnClientMatchResultComputed(MatchResultComputedEvent evt)
        {
            gameObject.SetActive(true);
            if (evt.WinnerClientId == ulong.MaxValue)
            {
                m_ResultLabel.text = "it's a draw!";
                return;
            }

            bool localPlayerWon = NetworkManager.Singleton.LocalClientId == evt.WinnerClientId;
            m_ResultLabel.text = localPlayerWon ? "You win!" : "You lose";
        }

        void OnClickContinue(ClickEvent evt)
        {
            gameObject.SetActive(false);
            Broadcast(new MatchEndAcknowledgedEvent());
        }
    }
}
