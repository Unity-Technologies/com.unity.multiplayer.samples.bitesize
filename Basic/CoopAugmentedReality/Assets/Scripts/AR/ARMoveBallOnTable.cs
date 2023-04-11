using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.Netcode;

[RequireComponent(typeof(ARPlaneManager), typeof(ARRaycastManager))]
public class ARMoveBallOnTable : MonoBehaviour
{
	[SerializeField] private Camera arCamera;
	[SerializeField] private Transform selectedBall;
	private GameObject tableGO;
	private ulong selectedBallNetworkId;
	private BallAvailability selectedBallAvailability;

	[SerializeField] private TMPro.TMP_Text userMsgTxt;
	private ARRaycastManager arRaycastMgr;

	private float ballRadiusTimesBallScale;
	private bool ballHeld;

	void Awake()
	{
		arRaycastMgr = GetComponent<ARRaycastManager>();
	}

	private void OnEnable()
	{
		userMsgTxt.text = "ENABLED!";
		EnhancedTouch.TouchSimulation.Enable();
		EnhancedTouch.EnhancedTouchSupport.Enable();
		EnhancedTouch.Touch.onFingerDown += ScreenFingerDown;
		tableGO = ARSpawnManager.Instance.TableGO;
	}
	private void OnDisable()
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

private void CastRaycastOnFingerDown(Vector2 screenTouchPos) 
	{
		Ray ray = arCamera.ScreenPointToRay(screenTouchPos);
		RaycastHit hitObj;

		if (Physics.Raycast(ray, out hitObj))
		{
			Debug.Log(hitObj.transform.tag + "is what was hit!!");

			// No ball held so we are selecting a ball.
			if (!ballHeld)
			{
				userMsgTxt.text = hitObj.transform.tag;
				if (hitObj.transform.tag == "Ball")
				{
					selectedBall = hitObj.transform;
					selectedBallAvailability = selectedBall.gameObject.GetComponent<BallAvailability>();
					// Check if the ball is available to move.
					if (selectedBallAvailability.isAvailable.Value)
					{
						selectedBallNetworkId = selectedBall.gameObject.GetComponent<NetworkObject>().NetworkObjectId;
						// Chancge Ownership call here to local client id.
						ARSpawnManager.Instance.ChangeBallOwnershipServerRpc(NetworkManager.Singleton.LocalClientId, selectedBallNetworkId);
						ballHeld = true;
						// Set ownership of ball to unavailable for other clients.
						selectedBallAvailability.SetBallUnavailableServerRpc();

						userMsgTxt.text = "Tap to place ball.";
					}
					else
					{
						userMsgTxt.text = "Ball unavailable.";
					}
				}
			}
			else //// Ball held so we are placing the ball somewhere on the table or in the cup.
			{
				if (hitObj.transform.tag == "Table" || hitObj.transform.tag == "Cup")
				{
					// Add half of the ball height to the touch position, so that the ball rests on the top of the table.
					ballRadiusTimesBallScale = (0.5f * 0.05f); 
					Vector3 newPositionAdjusted = new Vector3(hitObj.point.x, (hitObj.point.y + ballRadiusTimesBallScale) , hitObj.point.z);

					// Get hit position of screen touch, but as local position in respect to the table gameobject.
					Vector3 relativePosition = tableGO.transform.InverseTransformPoint(newPositionAdjusted);
					selectedBall.localPosition = new Vector3(relativePosition.x, relativePosition.y, relativePosition.z);

					// Set ownership of ball to available
					selectedBallAvailability.SetBallAvailableServerRpc();
					// Remove Ownership of ball
					ARSpawnManager.Instance.RemoveBallOwnershipServerRpc(NetworkManager.Singleton.LocalClientId, selectedBallNetworkId);
					// Reset variables and update text to no ball held.
					selectedBallNetworkId = 0;
					ballHeld = false;
					selectedBallAvailability = null;
					userMsgTxt.text = "No ball held";
				}
			}
		}
		return;
	}
}
