using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Netcode;

[RequireComponent(typeof(ARPlaneManager), typeof(ARRaycastManager))]
public class ARPPlaceTableOnPlane : MonoBehaviour
{
	[SerializeField] private GameObject envPrefab;
	[SerializeField] private GameObject tablePrefab;
	[SerializeField] private GameObject redCupPrefab;
	

	private ARPlaneManager arPlaneMgr;
	private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();
	private ARRaycastManager arRaycastMgr;
	private ARMoveBallOnTable arBallMovement;

	void Awake()
    {
		arRaycastMgr = GetComponent<ARRaycastManager>();
		arPlaneMgr = GetComponent<ARPlaneManager>();
		arBallMovement = GetComponent<ARMoveBallOnTable>();
	}

	private void OnEnable()
	{
		EnhancedTouch.TouchSimulation.Enable();
		EnhancedTouch.EnhancedTouchSupport.Enable();
		EnhancedTouch.Touch.onFingerDown += ScreenFingerDown;
	}
	private void OnDisable()
	{
		DisableTouchSim();
	}

	private void DisableTouchSim()
	{
		EnhancedTouch.TouchSimulation.Disable();
		EnhancedTouch.EnhancedTouchSupport.Disable();
		EnhancedTouch.Touch.onFingerDown -= ScreenFingerDown;
	}

	private void ScreenFingerDown(EnhancedTouch.Finger finger)
	{
		if (finger.index != 0)
		{
			return;
		}
		CastRaycastOnFingerDown(finger.currentTouch.screenPosition);
	}

	[ContextMenu("placeTable")]
	public void TestTableEnable()
	{
		tablePrefab.transform.position = Vector3.one;
		tablePrefab.transform.rotation = Quaternion.identity;

		Vector3 rotationToFaceEuler = GetRotationToCameraEuler(tablePrefab);
		tablePrefab.transform.rotation *= Quaternion.Euler(rotationToFaceEuler);
		redCupPrefab.SetActive(true);

		TogglePlaneDetection(false);
		DisableTouchSim();

		Debug.Log($"b. AR Spawn Manager called for BALL!!");
		// Set ball spawn manager to spawn ball for this player on the table.
		ARSpawnManager.Instance.SpawnBall();
		// Hand over to the ball mover.
		arBallMovement.enabled = true;
	}

	private void CastRaycastOnFingerDown(Vector2 screenTouchPos) 
	{ 
		if (arRaycastMgr.Raycast(screenTouchPos, raycastHits, TrackableType.PlaneWithinPolygon))
		{
			ARRaycastHit arHit = raycastHits[0];
			Pose hitPose = arHit.pose;
		
			if (arPlaneMgr.GetPlane(arHit.trackableId).alignment == PlaneAlignment.HorizontalUp)
			{
				ulong clientId = NetworkManager.Singleton.LocalClientId;

				tablePrefab.transform.position = hitPose.position;
				tablePrefab.transform.rotation = hitPose.rotation;

				Vector3 rotationToFaceEuler = GetRotationToCameraEuler(tablePrefab);
				tablePrefab.transform.rotation *= Quaternion.Euler(rotationToFaceEuler);
				redCupPrefab.SetActive(true);

				TogglePlaneDetection(false);
				DisableTouchSim();

				Debug.Log($"b. AR Spawn Manager called for BALL!!");
				// Set ball spawn manager to spawn ball for this player on the table.
				ARSpawnManager.Instance.SpawnBall();
				// Hand over to the ball mover.
				arBallMovement.enabled = true;
			}
		}
	}

	public void TogglePlaneDetection(bool turnOnDetection)
	{
		arPlaneMgr.enabled = turnOnDetection;
		SetAllPlanesActive(turnOnDetection);
	}

	/// <summary>
	/// Iterates over all the existing planes and activates
	/// or deactivates their <c>GameObject</c>s'.
	/// </summary>
	/// <param name="value">Each planes' GameObject is SetActive with this value.</param>
	void SetAllPlanesActive(bool value)
	{
		foreach (var plane in arPlaneMgr.trackables)
		{
			plane.gameObject.SetActive(value);
		}
	}

	public static Vector3 GetRotationToCameraEuler(GameObject tableObject)
	{
		// Position
		Vector3 directionToFace = (Camera.main.transform.position - tableObject.transform.position);
		// Rotation
		Vector3 rotationToFace = Quaternion.LookRotation(directionToFace).eulerAngles;
		// Scale the rotation on the up, to only use the y rotation value, ignore x and z.
		return Vector3.Scale(rotationToFace, tableObject.transform.up.normalized);
	}
}
