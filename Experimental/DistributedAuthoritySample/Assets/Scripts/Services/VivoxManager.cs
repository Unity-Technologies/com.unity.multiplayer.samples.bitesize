using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEngine;

namespace Services
{
    public class VivoxManager : MonoBehaviour
    {
        async void Start()
        {
            await InitializeVivoxAsync();
        }

        async Task InitializeVivoxAsync()
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            await VivoxService.Instance.InitializeAsync();
            await LoginToVivoxAsync();
        }

        async Task LoginToVivoxAsync()
        {
            LoginOptions options = new LoginOptions();
            options.DisplayName = "Player_" + AuthenticationService.Instance.PlayerId;
            options.EnableTTS = false;
            await VivoxService.Instance.LoginAsync(options);
        }
    }
}
