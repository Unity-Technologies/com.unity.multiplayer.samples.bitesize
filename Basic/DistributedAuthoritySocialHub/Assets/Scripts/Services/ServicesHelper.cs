using System;
using System.Threading.Tasks;
using Services;
using Unity.Multiplayer.Samples.SocialHub.GameManagement;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using Unity.Services.Vivox;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Services
{
    class ServicesHelper : MonoBehaviour
    {
        [SerializeField]
        bool m_InitiateVivoxOnAuthentication;

        static bool s_InitialLoad;

        Task m_SessionTask;

        ISession m_CurrentSession;
        bool m_IsLeavingSession;

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
                GameplayEventHandler.LoadMainMenuScene();
            }

            NetworkManager.Singleton.OnClientStopped += OnClientStopped;

            GameplayEventHandler.OnStartButtonPressed += OnStartButtonPressed;
            GameplayEventHandler.OnReturnToMainMenuButtonPressed += OnReturnToMainMenuButtonPressed;
            GameplayEventHandler.OnQuitGameButtonPressed += OnQuitGameButtonPressed;
        }

        void OnClientStopped(bool obj)
        {
            LeaveSession();
        }

        void OnDestroy()
        {
            if (NetworkManager.Singleton)
            {
                NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
            }

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
        }

        void OnQuitGameButtonPressed()
        {
            LeaveSession();
            Application.Quit();
        }

        async void LeaveSession()
        {
            if (m_CurrentSession != null && !m_IsLeavingSession)
            {
                try
                {
                    m_IsLeavingSession = true;
                    await m_CurrentSession.LeaveAsync();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw;
                }
                finally
                {
                    m_IsLeavingSession = false;
                    ExitedSession();
                }
            }
        }

        void SignInFailed(RequestFailedException obj)
        {
            Debug.LogWarning($"{nameof(SignedIn)} obj.ErrorCode {obj.ErrorCode}");
        }

        async void SignedIn()
        {

        }


        static string GetRandomString(int length)
        {
            var r = new System.Random();
            var result = new char[length];

            for (int i = 0; i < length; i++)
            {
                result[i] = (char)r.Next('a', 'z' + 1);
            }

            return new string(result);
        }

        async Task SignIn()
        {
            try
            {
                AuthenticationService.Instance.SignInFailed += SignInFailed;
                AuthenticationService.Instance.SignedIn += SignedIn;
                AuthenticationService.Instance.SwitchProfile(GetRandomString(5));
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
                await SignIn();
            }

            await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);

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
                m_CurrentSession.RemovedFromSession += RemovedFromSession;
                m_CurrentSession.StateChanged += CurrentSessionOnStateChanged;

                if (m_InitiateVivoxOnAuthentication)
                {
                    await VivoxService.Instance.InitializeAsync();
                    VivoxManager.Instance.JoinChannel(m_CurrentSession.Id);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        void RemovedFromSession()
        {
            ExitedSession();
        }

        void CurrentSessionOnStateChanged(SessionState sessionState)
        {
            if (sessionState != SessionState.Connected)
            {
                ExitedSession();
            }
        }

        void ExitedSession()
        {
            if (m_CurrentSession != null)
            {
                m_CurrentSession.RemovedFromSession -= RemovedFromSession;
                m_CurrentSession.StateChanged -= CurrentSessionOnStateChanged;
                m_CurrentSession = null;
                GameplayEventHandler.ExitedSession();
            }
        }
    }
}
