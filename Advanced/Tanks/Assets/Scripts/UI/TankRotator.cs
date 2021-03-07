using UnityEngine;
using Tanks.Data;
using Tanks.TankControllers;
using Tanks.Utilities;
using System.Collections;

namespace Tanks.UI
{
	/// <summary>
	/// Singleton that rotates the tank preview
	/// </summary>
	public class TankRotator : Singleton<TankRotator>
	{
		[SerializeField]
		protected float m_XRotationFactor = 0.5f, m_XAnimatingRotationSpeed = 20f;
		[SerializeField]
		protected Camera m_TankDrawingCamera;
		[SerializeField]
		protected float m_CameraSizePadding = 0.2f;
		[SerializeField]
		protected float m_CamMovementSmoothing = 2;
		[SerializeField]
		protected float m_CamSizeSmoothing = 1;
		[SerializeField]
		protected float m_TankOffsetFactor = 0.3f;

		private bool m_IsAutomaticallySpinning = true;

		private float m_StartingYRotation = 0f, m_YRotationDirection = 1;
		private Vector3 m_CameraOffset;

		private Vector3 m_CurrentCamTarget;
		private Vector3 m_CamTargetVel;
		private float m_CamSizeVel;

		private TankDisplay m_CurrentTankDisplay;

		//rotation convenience function
		private void SetYRotation(float y)
		{
			transform.rotation = Quaternion.Euler(new Vector3(transform.rotation.eulerAngles.x, y, transform.rotation.eulerAngles.z));
		}

		//Setup the rotator
		protected override void Awake()
		{
			base.Awake();
			m_StartingYRotation = transform.rotation.eulerAngles.y;
			if (m_TankDrawingCamera != null)
			{
				m_CurrentCamTarget = transform.position;
				m_CameraOffset = m_TankDrawingCamera.transform.forward *
				Vector3.Dot(m_TankDrawingCamera.transform.position - m_CurrentCamTarget, m_TankDrawingCamera.transform.forward);
			}
		}

		//As this class is recreated each time we enter the menu from another scene, we can rely on Start to refresh our selections.
		private IEnumerator Start()
		{
			//Alas, we must wait for the Daily unlock manager to be initialized before we can poll it for temp unlock data.
			while (!DailyUnlockManager.s_Instance.IsInitialized())
				yield return null;			
			
			//Check if the last selected tank is still valid for selection. If not, reset to default.
			int testIndex = PlayerDataManager.s_Instance.selectedTank;

			TankTypeDefinition tankData = TankLibrary.s_Instance.GetTankDataForIndex(testIndex);

			if (!PlayerDataManager.s_Instance.IsTankUnlocked(testIndex) && !DailyUnlockManager.s_Instance.IsItemTempUnlocked(tankData.id))
			{
				testIndex = 0;
				PlayerDataManager.s_Instance.selectedTank = 0;
			}
			
			//Loads correct tank
			LoadModelForTankIndex(testIndex);
		}

		private void Update()
		{
			//Auto-resize camera based on bounds
			ResizeCamera();

			//Handles spinning the tank
			if (m_IsAutomaticallySpinning)
			{
				float newRotation = transform.rotation.eulerAngles.y - m_XAnimatingRotationSpeed * Time.deltaTime * m_YRotationDirection;
				SetYRotation(newRotation);
			}
		}
		
		//Handles the start of the drag - ensures that the tank doesn't automatically spin
		public void BeginDrag()
		{
			m_IsAutomaticallySpinning = false;
			m_StartingYRotation = transform.rotation.eulerAngles.y;
		}
		
		//calculates the drag rotation - during the dragging
		public void DragRotation(float startMouseX, float currentMouseX)
		{
			float yRotation = m_StartingYRotation + m_XRotationFactor * (startMouseX - currentMouseX);
			SetYRotation(yRotation);
		}

		//at the end of a drag enable auto spinning and calculate the spin direction
		public void EndDrag(float startMouseX, float currentMouseX)
		{
			m_IsAutomaticallySpinning = true;
			m_YRotationDirection = Mathf.Sign(currentMouseX - startMouseX);
		}

		//Loads the correct tank model
		public void LoadModelForTankIndex(int definitionIndex)
		{
			TankTypeDefinition tankData = TankLibrary.s_Instance.GetTankDataForIndex(definitionIndex);
			ChangeTankModel(tankData.displayPrefab);
		}

		public void LoadDecorationForIndex(int definitionIndex, int materialIndex)
		{
			//While attempting to load the decoration, we determine whether it's still a valid unlock. 
			//This is due to our ad-to-temp-unlock system - any previously-selected items may have lapsed since we last updated.

			int requestedMaterial = -1;

			//Is the requested colour still valid for selection? If so, choose that one.
			if (PlayerDataManager.s_Instance.IsColourUnlockedForDecoration(definitionIndex, materialIndex) || (DailyUnlockManager.s_Instance.GetTempUnlockedColour() == materialIndex))
			{
				requestedMaterial = materialIndex;
			}
			//Otherwise we iterate through unlocked colours for this decoration to find the first valid one.
			else
			{
				int materialQuantity = TankDecorationLibrary.s_Instance.GetMaterialQuantityForIndex(definitionIndex);

				for (int i = 0; i < materialQuantity; i++)
				{
					if (PlayerDataManager.s_Instance.IsColourUnlockedForDecoration(definitionIndex, i) || (DailyUnlockManager.s_Instance.GetTempUnlockedColour() == i))
					{
						PlayerDataManager.s_Instance.SetSelectedMaterialForDecoration(definitionIndex, i);
						requestedMaterial = i;
						break;
					}
				}

				//if we've got this far with no positive material, it means that this decoration has no valid unlocked colours and therefore isn't selectable at all. Reset both decoration and colour to the default.
				if (requestedMaterial < 0)
				{
					PlayerDataManager.s_Instance.selectedDecoration = 0;
					PlayerDataManager.s_Instance.SetSelectedMaterialForDecoration(definitionIndex, 0);

					definitionIndex = 0;
					requestedMaterial = -1;
				}
			}

			m_CurrentTankDisplay.SetTankDecoration(definitionIndex, requestedMaterial);
		}
	
		//Handles changing the current tank
		public void ChangeTankModel(GameObject newModel)
		{
			if (transform.childCount > 0)
			{
				Transform tankChild = transform.GetChild(0);

				if (tankChild != null)
				{
					Destroy(tankChild.gameObject);
				}
			}
		
			GameObject newTankMesh = (GameObject)Instantiate(newModel, transform.position, transform.rotation);
			newTankMesh.transform.localScale = transform.localScale;
			newTankMesh.transform.SetParent(transform, true);

			m_CurrentTankDisplay = newTankMesh.GetComponent<TankDisplay>();
			m_CurrentTankDisplay.HideShadow();

			PlayerDataManager dataManager = PlayerDataManager.s_Instance;
			if (dataManager != null)
			{
				int decorationIndex = dataManager.selectedDecoration;
				LoadDecorationForIndex(decorationIndex, dataManager.GetSelectedMaterialForDecoration(decorationIndex));
			}
		}

		//Resizes camera based on bounds of tank and decoration
		private void ResizeCamera()
		{
			if (m_TankDrawingCamera != null && m_CurrentTankDisplay != null)
			{
				float currentY = transform.rotation.eulerAngles.y;
				SetYRotation(180);

				Bounds tankBounds = m_CurrentTankDisplay.GetTankBounds();

				float invAspect = 1.0f / m_TankDrawingCamera.aspect;

				// Calculate diagonal of footprint for horizontal size
				// Camera is not rolled and has no yaw, so this is sufficient
				float horizontalSize = new Vector2(tankBounds.size.x, tankBounds.size.z).magnitude;

				// Because the camera has some pitch, the world vertical size isn't the same as the camera
				// vertical size, so we project into camera space and measure the 2D height
				float verticalSize = tankBounds.size.y;

				Vector3 screenVec = m_TankDrawingCamera.transform.TransformVector(new Vector3(0, verticalSize, 0));
				screenVec.z = 0;
				float transformedHeight = screenVec.magnitude;

				// Desired size is the maximum of aspect correct horizontal or vertical size
				float desiredSize = Mathf.Max(horizontalSize * invAspect, transformedHeight) * 0.5f;

				// Approach correct ortho size
				m_TankDrawingCamera.orthographicSize = Mathf.SmoothDamp(m_TankDrawingCamera.orthographicSize, desiredSize + m_CameraSizePadding, ref m_CamSizeVel, m_CamSizeSmoothing);

				// Cam target prefers to balance tank towards its base
				Vector3 desiredTarget = (transform.position * m_TankOffsetFactor) + (tankBounds.center * (1 - m_TankOffsetFactor));

				// World x is always 0 too, even if some decorations unbalance it
				desiredTarget.x = 0;

				m_CurrentCamTarget = Vector3.SmoothDamp(m_CurrentCamTarget, desiredTarget, ref m_CamTargetVel, m_CamMovementSmoothing);
				m_TankDrawingCamera.transform.position = m_CurrentCamTarget + m_CameraOffset;
				SetYRotation(currentY);
			}
		}

		//Debug gizmos
		private void OnDrawGizmosSelected()
		{
			if (m_CurrentTankDisplay != null)
			{
				float currentY = transform.rotation.eulerAngles.y;
				SetYRotation(180);
				Bounds tankBounds = m_CurrentTankDisplay.GetTankBounds();

				Gizmos.DrawWireCube(tankBounds.center, tankBounds.size);
				SetYRotation(currentY);
			}
		}
	}
}
