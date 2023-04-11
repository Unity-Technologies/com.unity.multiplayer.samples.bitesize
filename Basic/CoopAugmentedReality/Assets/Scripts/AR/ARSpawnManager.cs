using Unity.Netcode;
using UnityEngine;

public class ARSpawnManager : NetworkBehaviour
{
	public static ARSpawnManager Instance { get; private set; }

	[SerializeField] private GameObject[] ballPrefabs;
	[SerializeField] private GameObject[] ballMoveAvailability;
	[SerializeField] private Transform[] spawnPositions;
	[SerializeField] public GameObject TableGO;

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
		}
		else
		{
			Debug.Log($"********************* SPAWNER AWAKE ***");
			Instance = this;
		}
	}

	void Start()
	{
		Debug.Log($"********************* SPAWNER START ***");
	}

	public void SpawnBall()
	{
		Debug.Log($"********************* CALL BALL SPAWN ***");
		SpawnBallForPlayerServerRpc(NetworkManager.Singleton.LocalClientId);
		
	}

	[ServerRpc(RequireOwnership = false)]
	//[ServerRpc]
	public void SpawnBallForPlayerServerRpc(ulong senderObject)
	{
		Debug.Log($"********************* SPAWNING *************************************** FOR {senderObject}");

		GameObject ballPrefab = ballPrefabs[senderObject];

		Debug.Log($"Got Ball");
		Transform localSpawnPosition = spawnPositions[senderObject];
		Debug.Log($"Got Position");

		Debug.Log($" Position {localSpawnPosition.position} and Rotation {localSpawnPosition.rotation}");
		NetworkObject ballObject = GameObject.Instantiate(ballPrefab, localSpawnPosition.position, localSpawnPosition.rotation).GetComponent<NetworkObject>();

		Debug.Log($" Position {senderObject}");


		ballObject.SpawnWithOwnership(senderObject);
		ballObject.TrySetParent(TableGO.transform, false);
		// Resize ball to negate scale reduction from table.
		ballObject.transform.localScale = new Vector3(ballObject.transform.localScale.x / TableGO.transform.localScale.x,
			ballObject.transform.localScale.y / TableGO.transform.localScale.y,
			ballObject.transform.localScale.z / TableGO.transform.localScale.z);

		// Position ball in local space under table..
		ballObject.transform.localPosition = localSpawnPosition.localPosition;
		ballObject.transform.localRotation = localSpawnPosition.localRotation;
	}


	[ServerRpc(RequireOwnership = false)]
	public void ChangeBallOwnershipServerRpc(ulong senderObject, ulong targetNetworkObject)
	{
		NetworkManager.Singleton.SpawnManager.SpawnedObjects[targetNetworkObject].ChangeOwnership(senderObject);
	}

	[ServerRpc(RequireOwnership = false)]
	public void RemoveBallOwnershipServerRpc(ulong senderObject, ulong targetNetworkObject)
	{
		NetworkManager.Singleton.SpawnManager.SpawnedObjects[targetNetworkObject].RemoveOwnership();
	}

	public static Vector3 GetRotationToCameraEuler(NetworkObject tableObject)
	{
		// Position
		Vector3 directionToFace = (Camera.main.transform.position - tableObject.transform.position);
		// Rotation
		Vector3 rotationToFace = Quaternion.LookRotation(directionToFace).eulerAngles;
		// Scale the rotation on the up, to only use the y rotation value, ignore x and z.
		return Vector3.Scale(rotationToFace, tableObject.transform.up.normalized);
	}
}
