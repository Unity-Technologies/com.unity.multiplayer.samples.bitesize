using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class NetworkAuthenticationManager : MonoBehaviour
{
	[Header("UI Settings")]
	[SerializeField] private GameObject authenticatingText;
	[SerializeField] private GameObject startButtons;

	private async void Start()
	{
		startButtons.SetActive(false);
		try 
		{
			await UnityServices.InitializeAsync();
			await AuthenticationService.Instance.SignInAnonymouslyAsync();
			Debug.Log($"Authenticated Successfully - Player Id: {AuthenticationService.Instance.PlayerId}");
		}
		catch(Exception e)
		{
			Debug.LogError($"Anonymous authentication with Unity Services failed: {e.Message}");
			throw;
		}
		authenticatingText.SetActive(false); ;
		startButtons.SetActive(true);
	}
}
