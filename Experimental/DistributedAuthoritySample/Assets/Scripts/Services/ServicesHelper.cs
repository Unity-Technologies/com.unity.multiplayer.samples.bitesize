using System;
using System.Threading.Tasks;
using Unity.Multiplayer.Samples.SocialHub.GameManagement;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Samples.SocialHub.Services
{
    class ServicesHelper : MonoBehaviour
    {
        [SerializeField]
        bool m_InitiateVivoxOnAuthentication;

        static bool s_InitialLoad;

        Task m_SessionTask;

        ISession m_CurrentSession;

        void Awake()
        {
            DontDestroyOnLoad(this);
        }

        async void Start()
        {
            await UnityServices.InitializeAsync();

            if (!s_InitialLoad)
            {
                s_InitialLoad = true;
                LoadMenuScene();
            }

            GameplayEventHandler.OnStartButtonPressed += OnStartButtonPressed;
            GameplayEventHandler.OnReturnToMainMenuButtonPressed += OnReturnToMainMenuButtonPressed;
            GameplayEventHandler.OnQuitGameButtonPressed += OnQuitGameButtonPressed;
        }

        void OnDestroy()
        {
            GameplayEventHandler.OnStartButtonPressed -= OnStartButtonPressed;
            GameplayEventHandler.OnReturnToMainMenuButtonPressed -= OnReturnToMainMenuButtonPressed;
            GameplayEventHandler.OnQuitGameButtonPressed -= OnQuitGameButtonPressed;
        }

        async void OnStartButtonPressed(string playerName, string sessionName)
        {
            var connectTask = ConnectToSession(playerName, sessionName);
            await connectTask;
            GameplayEventHandler.ConnectToSessionComplete(connectTask);
        }

        void OnReturnToMainMenuButtonPressed()
        {
            LeaveSession();
            LoadMenuScene();
        }

        void OnQuitGameButtonPressed()
        {
            LeaveSession();
            Application.Quit();
        }

        async void LeaveSession()
        {
            if (m_CurrentSession != null)
            {
                try
                {
                    await m_CurrentSession.LeaveAsync();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw;
                }
            }
        }

        void SignInFailed(RequestFailedException obj)
        {
            Debug.LogWarning($"{nameof(SignedIn)} obj.ErrorCode {obj.ErrorCode}");
        }

        void SignedIn()
        {
            if (m_InitiateVivoxOnAuthentication)
            {
                LogInToVivox();
            }
        }

        void LoadMenuScene()
        {
            SceneManager.LoadScene("MainMenu");
        }

        void LoadHubScene()
        {
            SceneManager.LoadScene("HubScene_TownMarket");
        }

        async void LogInToVivox()
        {
            await VivoxService.Instance.InitializeAsync();

            var options = new LoginOptions
            {
                DisplayName = AuthenticationService.Instance.Profile,
                EnableTTS = true
            };
            VivoxService.Instance.LoggedIn += LoggedInToVivox;
            await VivoxService.Instance.LoginAsync(options);
        }

        void LoggedInToVivox()
        {
            Debug.Log(nameof(LoggedInToVivox));
        }

        async Task SignIn(string profileName)
        {
            try
            {
                AuthenticationService.Instance.SignInFailed += SignInFailed;
                AuthenticationService.Instance.SignedIn += SignedIn;
                AuthenticationService.Instance.SwitchProfile(profileName);
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        async Task ConnectToSession(string playerName, string sessionName)
        {
            if (AuthenticationService.Instance == null)
            {
                return;
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await SignIn(playerName);
            }

            if (string.IsNullOrEmpty(sessionName))
            {
                Debug.LogError("Session name is empty. Cannot connect.");
                return;
            }

            await ConnectThroughLiveService(sessionName);
        }

        async Task ConnectThroughLiveService(string sessionName)
        {
            try
            {
                var options = new SessionOptions()
                {
                    Name = sessionName,
                    MaxPlayers = 64,
                }.WithDistributedAuthorityNetwork();

                m_CurrentSession = await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionName, options);

                LoadHubScene();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
