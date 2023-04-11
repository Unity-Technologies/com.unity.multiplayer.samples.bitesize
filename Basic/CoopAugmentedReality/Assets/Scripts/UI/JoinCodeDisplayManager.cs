using TMPro;
using Unity.Netcode;
using UnityEngine;

public class JoinCodeDisplayManager : NetworkBehaviour
{
	[SerializeField] private TMP_Text joinCodeDisplay;
	
	void Start()
    {
		joinCodeDisplay.text +=  (IsHost) ? HostManager.Instance.JoinCode : ClientManager.Instance.JoinCode;
	}
}
