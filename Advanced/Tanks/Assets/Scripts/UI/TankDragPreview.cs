using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using Tanks.Data;
using Tanks.TankControllers;

namespace Tanks.UI
{
	//Dragging box on Tank preview for rotating the tank - implements required Unity UI drag interfaces
	public class TankDragPreview : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		private Vector2 m_StartingPosition;

		private TankRotator m_TankRotator;

		//Passes drag information to an object that does the actual physical rotation
		public TankRotator tankRotator
		{
			get
			{
				if (m_TankRotator == null)
				{
					m_TankRotator = TankRotator.s_Instance;
				}
                
				return m_TankRotator;
			}
		}

		#region Drag implementations

		public void OnBeginDrag(PointerEventData eventData)
		{
			m_StartingPosition = eventData.position;
			tankRotator.BeginDrag();
		}

		public void OnDrag(PointerEventData eventData)
		{
			tankRotator.DragRotation(m_StartingPosition.x, eventData.position.x);
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			tankRotator.EndDrag(m_StartingPosition.x, eventData.position.x);
		}

		#endregion
	}
}