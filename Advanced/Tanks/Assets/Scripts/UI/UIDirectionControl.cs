using UnityEngine;

namespace Tanks.UI
{
	// This class is used to make sure world space UI
	// elements such as the health bar face the correct direction.
	public class UIDirectionControl : MonoBehaviour
	{
		// Use relative position should be used for this gameobject?
		[SerializeField]
		protected bool m_UseRelativePosition = true;
		// Use relative rotation should be used for this gameobject?
		[SerializeField]
		protected bool m_UseRelativeRotation = true;
		
		
		// The local position at the start of the scene.
		private Vector3 m_RelativePosition;
		// The local rotatation at the start of the scene.
		private Quaternion m_RelativeRotation;

		
		private void Start()
		{
			m_RelativePosition = transform.localPosition;
			m_RelativeRotation = transform.localRotation;
		}

		private void Update()
		{
			if (m_UseRelativeRotation)
				transform.rotation = m_RelativeRotation;
			
			if (m_UseRelativePosition)
				transform.position = transform.parent.position + m_RelativePosition;
		}
	}
}