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
            OnConfigurationLoaded();
        }

        async void OnConfigurationLoaded()
        {
            await Initialize(MultiplayerRolesManager.ActiveMultiplayerRoleMask == MultiplayerRoleFlags.Client);
        }

        async public Task Initialize(bool isClient)
        {
            string serviceProfileName = ProfileManager.Singleton.Profile;
            if (!isClient)
            {
                //servers should always have a single ID so their data isn't mixed with Users'.
                UnityServices.ExternalUserId = k_ServerID;
            }

            bool signedIn = await UnityServiceAuthenticator.TrySignInAsync(k_Environment, serviceProfileName);
            if (isClient)
            {
                //wait for the MetagameApplication to be instantiated, to avoid race conditions
                StartCoroutine(CoroutinesHelper.WaitAndDo(new WaitUntil(() => MetagameApplication.Instance), () =>
                {
                    //at this point, it's safe to tell the Application that the player signed in
                    MetagameApplication.Instance.Broadcast(new PlayerSignedIn(signedIn, UnityServiceAuthenticator.PlayerId));
                    if (signedIn)
                    {
                        InitializeClientOnlyServices();
                    }
                    else
                    {
                        Debug.LogError("User could not sign in. Please check that your device is connected to the internet, and that the project is linked to an existing Project in the Unity Cloud.");
                    }
                }));
            }
        }

        void InitializeClientOnlyServices()
        {
            Matchmaker = gameObject.AddComponent<MatchmakerTicketer>();
        }
    }
}
