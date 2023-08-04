using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class MetagameController : Controller<MetagameApplication>
    {
        void Awake()
        {
            AddListener<ObjectClickedEvent>(OnObjectClicked);
            AddListener<PlayerSignedIn>(OnPlayerSignedIn);
            AddListener<MatchEnteredEvent>(OnMatchEntered);
        }

        void OnDestroy()
        {
            RemoveListeners();
        }

        internal override void RemoveListeners()
        {
            RemoveListener<ObjectClickedEvent>(OnObjectClicked);
            RemoveListener<PlayerSignedIn>(OnPlayerSignedIn);
            RemoveListener<MatchEnteredEvent>(OnMatchEntered);
        }

        void OnObjectClicked(ObjectClickedEvent evt)
        {
            Debug.Log("Clicked: " + evt.Object);
        }

        void OnPlayerSignedIn(PlayerSignedIn evt)
        {
            if (evt.Success)
            {
                Debug.Log($"Player signed in with id {evt.PlayerId}");
            }
            else
            {
                Debug.Log("Player did not sign in");
            }
        }

        void OnMatchEntered(MatchEnteredEvent evt)
        {
            DisableViewsAndListeners();
        }

        void DisableViewsAndListeners()
        {
            for (int i = 0; i < App.View.transform.childCount; i++)
            {
                App.View.transform.GetChild(i).gameObject.SetActive(false);
            }
            App.OnReturnToMetagameAfterMatch -= OnReturnToMetagameAfterMatch;
            App.OnReturnToMetagameAfterMatch += OnReturnToMetagameAfterMatch;
            //CustomNetworkManager.Singleton.ReturnToMetagame = App.CallOnReturnToMetagameAfterMatch;
        }

        void OnReturnToMetagameAfterMatch()
        {
            EnableViewsAndListener();
        }

        void EnableViewsAndListener()
        {
            for (int i = 0; i < App.View.transform.childCount; i++)
            {
                App.View.transform.GetChild(i).gameObject.SetActive(true);
            }
            App.View.Matchmaker.Hide();
        }
    }
}
