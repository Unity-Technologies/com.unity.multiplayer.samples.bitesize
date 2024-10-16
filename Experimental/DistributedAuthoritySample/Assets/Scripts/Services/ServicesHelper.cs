using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Multiplayer.Samples.SocialHub.UI;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = System.Random;

namespace Unity.Multiplayer.Samples.SocialHub.Services
{
    class ServicesHelper : MonoBehaviour
    {
        [SerializeField]
        bool m_AutoAuthenticateOnStart;

        [SerializeField]
        bool m_InitiateVivoxOnAuthentication;

        string m_SessionName;

        Task m_SessionTask;

        ISession m_LastSession;

        void Awake()
        {
            DontDestroyOnLoad(this);
        }

        async void Start()
        {
            if (m_AutoAuthenticateOnStart)
            {
                await UnityServices.InitializeAsync();

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    AuthenticationService.Instance.SignInFailed += SignInFailed;
                    AuthenticationService.Instance.SignedIn += SignedIn;
                    AuthenticationService.Instance.SwitchProfile(GetRandomString(5));
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
            }

            HomeScreenView.StartButtonPressed += OnStartButtonPressed;
        }

        async void OnStartButtonPressed(string sessionName)
        {
            m_SessionName = sessionName;
            await ConnectToSession();
        }

        void SignInFailed(RequestFailedException obj)
        {
            Debug.LogWarning($"{nameof(SignedIn)} obj.ErrorCode {obj.ErrorCode}");
        }

        void SignedIn()
        {
            Debug.Log(nameof(SignedIn));
            if (m_InitiateVivoxOnAuthentication)
            {
                Login();
            }

            LoadMenuScene();
        }

        void LoadMenuScene()
        {
            SceneManager.LoadScene("MainMenu");
        }

        void LoadHubScene()
        {
            SceneManager.LoadScene("HubScene_TownMarket");
        }

        async void Login()
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

        static string GetRandomString(int length)
        {
            var r = new Random();
            return new string(Enumerable.Range(0, length).Select(_ => (Char)r.Next('a', 'z')).ToArray());
        }

        async Task ConnectToSession()
        {
            if (AuthenticationService.Instance == null || !AuthenticationService.Instance.IsSignedIn)
            {
                return;
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

                if (m_LastSession == null)
                {
                    m_LastSession = await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionName, options);
                }
                else
                {
                    await MultiplayerService.Instance.JoinSessionByIdAsync(m_LastSession.Id);
                }

                LoadHubScene();

                // DA TODO: m_NetworkManager.OnClientStopped += OnNetworkManagerStopped;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        void OnGUI()
        {
            if (AuthenticationService.Instance == null || !AuthenticationService.Instance.IsSignedIn)
            {
                return;
            }

            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                GUI.enabled = m_SessionTask == null || m_SessionTask.IsCompleted;

                GUILayout.Label("Session Name", GUILayout.Width(100));

                if (GUILayout.Button("Host"))
                {
                    SceneManager.sceneLoaded += Host_SceneLoaded;
                    LoadHubScene();

                    //SceneManager.LoadScene("ObjectTesting");
                }

                if (GUILayout.Button("Client"))
                {
                    NetworkManager.Singleton.StartClient();
                }

                GUI.enabled = true;
            }
        }

        void Host_SceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            SceneManager.sceneLoaded -= Host_SceneLoaded;
            NetworkManager.Singleton.StartHost();
        }
    }
}
