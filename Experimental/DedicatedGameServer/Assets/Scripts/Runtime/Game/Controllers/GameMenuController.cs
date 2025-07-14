using System;
using Unity.DedicatedGameServerSample.Runtime.ConnectionManagement;

namespace Unity.DedicatedGameServerSample.Runtime
{
    class GameMenuController : Controller<GameApplication>
    {
        GameMenuView View => App.View.Menu;

        void Awake()
        {
            AddListener<ResumeButtonClickedEvent>(OnClientResumeButtonClicked);
            AddListener<QuitButtonClickedEvent>(OnClientQuitButtonClicked);
            AddListener<MenuToggleEvent>(OnMenuToggled);
            AddListener<EndMatchEvent>(OnClientEndMatch);
        }

        void OnDestroy()
        {
            RemoveListeners();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                // open menu when losing focus
                SetMenuActive(true);
            }
        }

        internal override void RemoveListeners()
        {
            RemoveListener<ResumeButtonClickedEvent>(OnClientResumeButtonClicked);
            RemoveListener<QuitButtonClickedEvent>(OnClientQuitButtonClicked);
            RemoveListener<MenuToggleEvent>(OnMenuToggled);
            RemoveListener<EndMatchEvent>(OnClientEndMatch);
        }

        void OnClientResumeButtonClicked(ResumeButtonClickedEvent evt)
        {
            SetMenuActive(false);
        }

        void OnClientQuitButtonClicked(QuitButtonClickedEvent evt)
        {
            if (ConnectionManager.Instance != null)
            {
                ConnectionManager.Instance.RequestShutdown();
            }
        }

        void OnMenuToggled(MenuToggleEvent evt)
        {
            SetMenuActive(!App.Model.MenuVisible);
        }

        void OnClientEndMatch(EndMatchEvent evt)
        {
            View.Hide();
            RemoveListeners();
        }

        void SetMenuActive(bool isMenuActive)
        {
            if (isMenuActive)
            {
                View.Show();
            }
            else
            {
                View.Hide();
            }
            App.Model.MenuVisible = isMenuActive;
            if (App.Model.PlayerCharacter != null)
            {
                App.Model.PlayerCharacter.SetInputsActive(!isMenuActive);
            }
        }
    }
}
