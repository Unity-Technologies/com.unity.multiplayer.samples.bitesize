#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections.Generic;
using Tanks.Utilities;
using Tanks.Data;

namespace Tanks.SinglePlayer
{
	/// <summary>
	/// Crate manager - used for moving creates along a path
	/// </summary>
	public class CrateManager : Singleton<CrateManager>
	{
		[SerializeField]
		protected Crate m_CratePrefab;
		[SerializeField]
		protected Transform[] m_CratePath;
		[SerializeField]
		protected int m_NumCrates;
		[SerializeField]
		protected float m_CrateSpeed;

		public float crateSpeed
		{
			get { return m_CrateSpeed; }
			set { m_CrateSpeed = value; }
		}

		protected List<Crate> m_Crates;

		/// <summary>
		/// Clears the crates.
		/// </summary>
		public void ClearCrates()
		{
			if (m_Crates.Count > 0)
			{
				for (int i = m_Crates.Count - 1; i >= 0; --i)
				{
					Crate crate = m_Crates[i];

					if (crate != null)
					{
						Destroy(m_Crates[i].gameObject);
					}
				}
				m_Crates.Clear();
			}
		}

		/// <summary>
		/// Resets the crates.
		/// </summary>
		public void ResetCrates()
		{
			ClearCrates();

			for (int i = 0; i < m_NumCrates; ++i)
			{
				CreateCrate(i);
			}
		}

		/// <summary>
		/// Creates the crate.
		/// </summary>
		/// <param name="index">Index.</param>
		private void CreateCrate(int index)
		{
			float perCrateInterval = m_CratePath.Length / (float)m_NumCrates;
			float crateProgress = perCrateInterval * index;

			Crate newCrate = Instantiate<Crate>(m_CratePrefab);
			newCrate.transform.position = GetCurvePosition(crateProgress);
			newCrate.movementProgress = crateProgress;
			newCrate.SetupCrate(TankDecorationLibrary.s_Instance.SelectRandomLockedDecoration());

			m_Crates.Add(newCrate);
		}

		protected override void Awake()
		{
			base.Awake();
			m_Crates = new List<Crate>();

			ResetCrates();
		}

		//Handle movement on FixedUpdate
		protected virtual void FixedUpdate()
		{
			for (int i = 0; i < m_Crates.Count; ++i)
			{
				Crate crate = m_Crates[i];

				if (crate != null)
				{
					float curveSpeed = GetCurveDerivative(crate.movementProgress).magnitude;

					crate.movementProgress = (crate.movementProgress + m_CrateSpeed * Time.deltaTime / curveSpeed) % m_CratePath.Length;
					crate.MoveTo(GetCurvePosition(crate.movementProgress));
				}
			}
		}

		/// <summary>
		/// Gets the crate position on the curve
		/// </summary>
		/// <returns>The curve position.</returns>
		/// <param name="progress">Progress.</param>
		private Vector3 GetCurvePosition(float progress)
		{
			int lowIndex = Mathf.FloorToInt(progress);
			float normalizedProgress = progress - lowIndex;

			return GetCurvePosition(lowIndex, normalizedProgress);
		}

		/// <summary>
		/// Gets the crate specified at index position on curve
		/// </summary>
		/// <returns>The curve position.</returns>
		/// <param name="index">Index.</param>
		/// <param name="progress">Progress.</param>
		private Vector3 GetCurvePosition(int index, float progress)
		{
			// Get correct spline points
			Vector3 prev, current, target, next;

			GetCatmullPositions(index, out prev, out current, out target, out next);

			return MathUtilities.CatmullRom(prev, current, target, next, progress);
		}

		/// <summary>
		/// Calculates the derivative of the curve at progress value
		/// </summary>
		/// <returns>The curve derivative.</returns>
		/// <param name="progress">Progress.</param>
		private Vector3 GetCurveDerivative(float progress)
		{
			int lowIndex = Mathf.FloorToInt(progress);
			float normalizedProgress = progress - lowIndex;

			return GetCurvePosition(lowIndex, normalizedProgress);
		}

		/// <summary>
		/// Calculates the derivative of the curve at progress value based on an index
		/// </summary>
		/// <returns>The curve derivative.</returns>
		/// <param name="index">Index.</param>
		/// <param name="progress">Progress.</param>
		private Vector3 GetCurveDerivative(int index, float progress)
		{
			// Get correct spline points
			Vector3 prev, current, target, next;

			GetCatmullPositions(index, out prev, out current, out target, out next);

			return MathUtilities.CatmullRomDerivative(prev, current, target, next, progress);
		}

		/// <summary>
		/// Given a point in the curve calculate the quadratic interpolation points
		/// </summary>
		/// <param name="index">Index.</param>
		/// <param name="prev">Previous.</param>
		/// <param name="curr">Curr.</param>
		/// <param name="target">Target.</param>
		/// <param name="next">Next.</param>
		private void GetCatmullPositions(int index, out Vector3 prev, out Vector3 curr, out Vector3 target, out Vector3 next)
		{
			int numPoints = m_CratePath.Length;
			Transform currTransform = m_CratePath[index];

			prev = index > 0 ? m_CratePath[index - 1].position : m_CratePath[numPoints - 1].position;
			curr = currTransform.position;
			target = index < numPoints - 1 ? m_CratePath[index + 1].position : m_CratePath[0].position;
			if (index < numPoints - 2)
			{
				next = m_CratePath[index + 2].position;
			}
			else if (index < numPoints - 1)
			{
				next = m_CratePath[0].position;
			}
			else
			{
				next = m_CratePath[1].position;
			}
		}

		//Visualization code for viewing in editor
		#if UNITY_EDITOR
		private const int curvePoints = 16;
		private const float curveInterval = 1.0f / (float)curvePoints;

		protected virtual void OnDrawGizmos()
		{
			if (m_CratePath != null && m_CratePath.Length > 2)
			{
				for (int i = 0; i < m_CratePath.Length; ++i)
				{
					// Get correct spline points
					Vector3 prev, current, target, next;

					GetCatmullPositions(i, out prev, out current, out target, out next);

					Vector3 lastLinePoint = current;
					Gizmos.DrawWireSphere(lastLinePoint, 0.5f);

					// Draw spline
					for (float progress = curveInterval; progress <= 1 + Mathf.Epsilon; progress += curveInterval)
					{
						Vector3 linePoint = MathUtilities.CatmullRom(prev, current, target, next, progress);

						Handles.DrawDottedLine(lastLinePoint, linePoint, 5);
						lastLinePoint = linePoint;
					}
				}
			}
		}
		#endif
	}
}