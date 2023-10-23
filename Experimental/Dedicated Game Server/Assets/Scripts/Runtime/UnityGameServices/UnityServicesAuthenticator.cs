using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    /// <summary>
    /// Sample implementation of the Unity Authentication Service for Anonymous Auth
    /// Handles Race conditions between different sources of authentication, allowing multiple samples to be dragged into a scene without errors.
    /// (In a real project, you should ensure a single-entry point for authentication.)
    /// </summary>
    internal static class UnityServiceAuthenticator
    {
        const int k_InitializationTimeout = 10000;
        static bool s_IsSigningIn;
        internal static string PlayerId => AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId
                                                                                     : string.Empty;

        /// <summary>
        /// Unity anonymous Auth grants unique ID's by editor/build and machine. This means that if you open several builds or editors on the same machine, they will all have the same ID.
        /// Using a unique profile name forces a new ID. So the strategy is to make sure that each build/editor has its own profile name to act as multiple users for a service.
        /// </summary>
        /// <param name="profileName">Unique name that generates the unique ID</param>
        /// <returns></returns>
        public static async Task<bool> TryInitServicesAsync(string environment, string profileName)
        {
            async Task WaitForInitialized()
            {
                while (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    await Task.Delay(100);
                }
            }

            if (UnityServices.State == ServicesInitializationState.Initialized)
            {
                return true;
            }

            //Another Service is mid-initialization:
            if (UnityServices.State == ServicesInitializationState.Initializing)
            {
                var task = WaitForInitialized();
                if (await Task.WhenAny(task, Task.Delay(k_InitializationTimeout)) != task)
                {
                    return false; // We timed out
                }
                return UnityServices.State == ServicesInitializationState.Initialized;
            }
            var initializationOptions = new InitializationOptions();
            initializationOptions.SetEnvironmentName(environment);

            if (!string.IsNullOrEmpty(profileName))
            {
                //ProfileNames can't contain non-alphanumeric characters
                var rgx = new Regex("[^a-zA-Z0-9 - _]");
                profileName = rgx.Replace(profileName, "");
                initializationOptions.SetProfile(profileName);
            }

            //If you are using multiple unity services, make sure to initialize it only once before using your services.
            await UnityServices.InitializeAsync(initializationOptions);
            return UnityServices.State == ServicesInitializationState.Initialized;
        }

        public static async Task<bool> TrySignInAsync(string environment, string profileName)
        {
            async Task WaitForSignedIn()
            {
                while (!AuthenticationService.Instance.IsSignedIn)
                {
                    await Task.Delay(100);
                }
            }

            if (!await TryInitServicesAsync(environment, profileName))
            {
                return false;
            }
            if (s_IsSigningIn)
            {
                var task = WaitForSignedIn();
                if (await Task.WhenAny(task, Task.Delay(k_InitializationTimeout)) != task)
                {
                    return false; // We timed out
                }
                return AuthenticationService.Instance.IsSignedIn;
            }

            s_IsSigningIn = true;
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Could not sign in: {ex.Message}");
            }
            finally
            {
                s_IsSigningIn = false;
            }
            return AuthenticationService.Instance.IsSignedIn;
        }
    }
}
