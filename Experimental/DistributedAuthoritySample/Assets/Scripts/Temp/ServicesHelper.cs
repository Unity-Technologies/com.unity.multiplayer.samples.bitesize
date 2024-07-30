using System;
using System.Linq;
using System.Threading.Tasks;
using Services;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = System.Random;

public class ServicesHelper : MonoBehaviour
{
    [SerializeField]
    bool m_AutoAuthenticateOnStart;

    [SerializeField]
    bool m_InitiateVivoxOnAuthentication;

    string m_SessionName;

    Task m_SessionTask;

    ISession m_LastSession;

    public string PlayerProfileName { get; private set; }

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
                PlayerProfileName = GetRandomString(5);
                AuthenticationService.Instance.SignInFailed += SignInFailed;
                AuthenticationService.Instance.SignedIn += SignedIn;
                AuthenticationService.Instance.SwitchProfile(PlayerProfileName);
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }
    }

    void SignInFailed(RequestFailedException obj)
    {
        Debug.LogWarning($"{nameof(SignedIn)} obj.ErrorCode {obj.ErrorCode}");
    }

    async void SignedIn()
    {
        Debug.Log(nameof(SignedIn));
        if (m_InitiateVivoxOnAuthentication)
        {
            await LoginToVivox();
        }

        LoadMenuScene();
    }

    void LoadMenuScene()
    {
        SceneManager.LoadScene("MainMenu");
    }

    void LoadHubScene()
    {
        SceneManager.LoadScene("HubScene");
    }

    async Task LoginToVivox()
    {
        // Wait until VivoxManager.Instance is not null
        while (VivoxManager.Instance == null)
        {
            await Task.Yield();
        }

        await VivoxManager.Instance.InitializeVivoxAsync(PlayerProfileName);
    }

    static string GetRandomString(int length)
    {
        var r = new Random();
        return new string(Enumerable.Range(0, length).Select(_ => (Char)r.Next('a', 'z')).ToArray());
    }

    async Task ConnectThroughLiveService(string sessionName)
    {
        try
        {
            var options = new CreateSessionOptions(100)
            {
                Name = sessionName,
                MaxPlayers = 100,
            };

            if (m_LastSession == null)
            {
                m_LastSession = await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionName, options.WithDistributedConnection());
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
            m_SessionName = GUILayout.TextField(m_SessionName);
            if (GUILayout.Button("Connect"))
            {
                m_SessionTask = ConnectThroughLiveService(m_SessionName);
            }

            GUI.enabled = true;
        }
    }
}
