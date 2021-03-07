using UnityEngine;
using System.Collections;

namespace Tanks.Effects
{
	/// <summary>
	/// Mine timer
	/// </summary>
	public class MineTimer : MonoBehaviour
	{
		// Use this for initialization
		void Start()
		{
			Animator animator = gameObject.GetComponent<Animator>();

			if (animator != null)
			{
				animator.Play(animator.GetCurrentAnimatorStateInfo(0).shortNameHash, 0, Random.Range(0f, 1f));
			}
		}
	}
}
