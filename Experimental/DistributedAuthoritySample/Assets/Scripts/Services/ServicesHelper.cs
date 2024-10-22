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

        string m_SessionName;

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

        async void OnStartButtonPressed(string sessionName)
        {
            m_SessionName = sessionName;
            var connectTask = ConnectToSession();
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

        void LeaveSession()
        {
            m_CurrentSession?.LeaveAsync();
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

        async Task ConnectToSession()
        {
            if (AuthenticationService.Instance == null)
            {
                return;
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await SignIn(m_SessionName);
            }

            if (string.IsNullOrEmpty(m_SessionName))
            {
                Debug.LogError("Session name is empty. Cannot connect.");
                return;
            }

            await ConnectThroughLiveService(m_SessionName);
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

                if (m_CurrentSession == null)
                {
                    m_CurrentSession = await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionName, options);
                }
                else
                {
                    await MultiplayerService.Instance.JoinSessionByIdAsync(m_CurrentSession.Id);
                }

                LoadHubScene();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
