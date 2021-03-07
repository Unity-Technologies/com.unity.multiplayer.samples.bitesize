using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Tanks.Rules.SinglePlayer
{
	/// <summary>
	/// Waypoint - used by navigator to evaluate next target location
	/// </summary>
	[RequireComponent(typeof(Collider))]
	public class Waypoint : MonoBehaviour
	{
		[SerializeField]
		protected GameObject m_NextWaypoint;

		//static reference to all of the waypoints
		private static List<Waypoint> s_waypoints = new List<Waypoint>();

		public static List<Waypoint> s_Waypoints
		{
			get { return s_waypoints; }
		}

		//Add to static list
		private void Awake()
		{
			s_waypoints.Add(this);
		}
		
		//if the navigator enters then assign next waypoint or set complete
		private void OnTriggerEnter(Collider c)
		{
			Navigator navigator = c.GetComponent<Navigator>();
			if (navigator != null)
			{
				if (m_NextWaypoint == null)
				{
					navigator.SetComplete();
				}
				else
				{
					navigator.SetTarget(m_NextWaypoint.transform);
				}
			}
		}

		/// <summary>
		/// Visualise the navigation process
		/// </summary>
		private void OnDrawGizmos()
		{
			if (m_NextWaypoint != null)
			{
				Gizmos.color = Color.blue;
				Gizmos.DrawLine(transform.position, m_NextWaypoint.transform.position);
			}
		}

		//Remove self from static list
		private void OnDestroy()
		{
			s_waypoints.Remove(this);
		}
	}
}
