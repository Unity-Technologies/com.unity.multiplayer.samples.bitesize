using System.Collections;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.DedicatedGameServerSample.Runtime.ApplicationLifecycle;
using Unity.Multiplayer;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    ///<summary>
    ///Initializes all the Unity Services managers
    ///</summary>
    [MultiplayerRoleRestricted]
    internal class UnityServicesInitializer : MonoBehaviour
    {
        public const string k_ServerID = "SERVER";
        public static UnityServicesInitializer Instance { get; private set; }
        public MatchmakerTicketer Matchmaker { get; private set; }

        public const string k_Environment =
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
            StartCoroutine(InitializeOnConfigurationLoaded());
        }

        IEnumerator InitializeOnConfigurationLoaded()
        {
            yield return new WaitUntil(() => ApplicationEntryPoint.Configuration != null);
            OnConfigurationLoaded(ApplicationEntryPoint.Configuration);
        }

        async void OnConfigurationLoaded(ConfigurationManager configuration)
        {
            await Initialize(MultiplayerRolesManager.ActiveMultiplayerRoleMask == MultiplayerRoleFlags.Client);
        }

        async public Task Initialize(bool isClient)
        {
            string serviceProfileName = ProfileManager.Singleton.Profile;

            if (!isClient)
            {
                UnityServices.ExternalUserId = k_ServerID;
            }

            bool signedIn = await UnityServiceAuthenticator.TrySignInAsync(k_Environment, serviceProfileName);
            
            if (isClient)
            {
                MetagameApplication.Instance.Broadcast(new PlayerSignedIn(signedIn, UnityServiceAuthenticator.PlayerId));
                if (signedIn)
                {
                    InitializeClientOnlyServices();
                }
            }
        }

        void InitializeClientOnlyServices()
        {
            Matchmaker = gameObject.AddComponent<MatchmakerTicketer>();
        }
    }
}
