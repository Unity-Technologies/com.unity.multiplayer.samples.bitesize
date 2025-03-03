using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Multiplayer;
using Unity.Services.Core.Environments;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    ///<summary>
    ///Initializes all the Unity Services managers
    ///</summary>
    [MultiplayerRoleRestricted]
    class UnityServicesInitializer : MonoBehaviour
    {
        const string k_ServerID = "SERVER";
        public static UnityServicesInitializer Instance { get; private set; }

        [SerializeField]
        MatchmakerHandler m_MatchmakerHandler;
        public MatchmakerHandler Matchmaker => m_MatchmakerHandler;

        const string k_Environment =
#if LIVE
                                        "production";
#elif STAGE
                                        "staging";
#else
                                        "dev";
#endif
        public void Awake()
        {
            if (Instance && Instance != this)
            {
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            OnConfigurationLoaded();
        }

        async void OnConfigurationLoaded()
        {
            await Initialize(MultiplayerRolesManager.ActiveMultiplayerRoleMask == MultiplayerRoleFlags.Client);
        }

        async Task Initialize(bool isClient)
        {
            string serviceProfileName = ProfileManager.Singleton.Profile;
            if (!isClient)
            {
                //servers should always have a single ID so their data isn't mixed with Users'.
                UnityServices.ExternalUserId = k_ServerID;
            }

            await UnityServices.InitializeAsync(new InitializationOptions().SetEnvironmentName(k_Environment));

            if (isClient)
            {
                var signedIn = await UnityServiceAuthenticator.TrySignInAsync(k_Environment, serviceProfileName);
                //wait for the MetagameApplication to be instantiated, to avoid race conditions
                StartCoroutine(CoroutinesHelper.WaitAndDo(new WaitUntil(() => MetagameApplication.Instance), () =>
                {
                    //at this point, it's safe to tell the Application that the player signed in
                    MetagameApplication.Instance.Broadcast(new PlayerSignedIn(signedIn, UnityServiceAuthenticator.PlayerId));
                    if (!signedIn)
                    {
                        Debug.LogError("User could not sign in. Please check that your device is connected to the internet, and that the project is linked to an existing Project in the Unity Cloud.");
                    }
                }));
            }
        }
    }
}
