using System.Collections;
using System.Threading.Tasks;
using Unity.Services.Core;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    ///<summary>
    ///Initializes all the Unity Services managers
    ///</summary>
    public class UnityServicesInitializer : MonoBehaviour
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
            StartCoroutine(InitializeOnConfigurationLoaded());
        }

        IEnumerator InitializeOnConfigurationLoaded()
        {
            yield return new WaitUntil(() => CustomNetworkManager.Configuration != null);
            OnConfigurationLoaded(CustomNetworkManager.Configuration);
        }

        async void OnConfigurationLoaded(ConfigurationManager configuration)
        {
            await Initialize(configuration.GetBool(ConfigurationManager.k_ModeServer) ? k_ServerID
                                                                                      : string.Empty);
        }

        async public Task Initialize(string externalPlayerID)
        {
            string serviceProfileName = "MainProfile";
#if UNITY_EDITOR && HAS_PARRELSYNC
            if (ParrelSync.ClonesManager.IsClone())
            {
                serviceProfileName = "CloneProfile";
            }
#endif
            if (!string.IsNullOrEmpty(externalPlayerID))
            {
                UnityServices.ExternalUserId = externalPlayerID;
            }

            bool signedIn = await UnityServiceAuthenticator.TrySignInAsync(k_Environment, serviceProfileName);
            MetagameApplication.Instance.Broadcast(new PlayerSignedIn(signedIn, UnityServiceAuthenticator.PlayerId));
            if (!signedIn)
            {
                return;
            }
            if (externalPlayerID != k_ServerID)
            {
                InitializeClientOnlyServices();
            }
        }

        void InitializeClientOnlyServices()
        {
            Matchmaker = gameObject.AddComponent<MatchmakerTicketer>();
        }
    }
}
