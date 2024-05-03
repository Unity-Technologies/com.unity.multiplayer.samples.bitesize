using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    /// <summary>
    /// Basic script to define a patrol made of a list of points. Handles drawing gizmos to make creation easier.
    /// </summary>
    public class Patrol : MonoBehaviour
    {
        [SerializeField]
        List<Transform> m_PatrolPoints;

        Vector3[] m_PatrolPointsPositions;
        public Vector3[] PatrolPointsPositions => m_PatrolPointsPositions;

        void OnValidate()
        {
            if (m_PatrolPoints.Count < 2)
            {
                throw new Exception("Each patrol must have at least two points in it");
            }
        }

        void Awake()
        {
            SetPatrolPointsPositions();
        }

        void SetPatrolPointsPositions()
        {
            var temp = new List<Vector3>();
            foreach (var point in m_PatrolPoints)
            {
                temp.Add(point.position);
            }

            m_PatrolPointsPositions = temp.ToArray();
        }

        void OnDrawGizmos()
        {
#if UNITY_EDITOR
            // if in editor and playmode is off, manually update point positions before drawing
            if (!EditorApplication.isPlaying)
            {
                SetPatrolPointsPositions();
            }
#endif
            if (PatrolPointsPositions.Length >= 2)
            {
                Gizmos.DrawLineStrip(PatrolPointsPositions, true);
                foreach (var point in m_PatrolPointsPositions)
                {
                    Gizmos.DrawLine(point, point + Vector3.up);
                }
            }
        }
    }
}
