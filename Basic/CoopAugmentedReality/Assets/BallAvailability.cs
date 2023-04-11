using Unity.Netcode;
using UnityEngine;

public class BallAvailability : NetworkBehaviour
{
	[SerializeField] private MeshRenderer ballMesh;
	// Colors
	[SerializeField] private Color defaultBallColor;
	[SerializeField] private Color remoteUnavailableColor;

	public NetworkVariable<bool> isAvailable = new NetworkVariable<bool>(
		value:true, 
		NetworkVariableReadPermission.Everyone, 
		NetworkVariableWritePermission.Server);

	[ServerRpc(RequireOwnership = false)]
	public void SetBallUnavailableServerRpc()
	{
		isAvailable.Value = false;
		SetBallUnavailableClientRpc();
	}

	[ClientRpc]
	public void SetBallUnavailableClientRpc()
	{
		ballMesh.material.color = remoteUnavailableColor;
	}


	[ServerRpc(RequireOwnership = false)]
	public void SetBallAvailableServerRpc()
	{
		isAvailable.Value = true;
		SetBallAvailableClientRpc();
	}

	[ClientRpc]
	public void SetBallAvailableClientRpc()
	{
		ballMesh.material.color = defaultBallColor;
	}
}
