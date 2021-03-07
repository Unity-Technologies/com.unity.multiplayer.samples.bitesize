using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Tanks.UI
{
	/// <summary>
	/// UI Text that changes based on platform
	/// </summary>
	[RequireComponent(typeof(Text))]
	public class PlatformSpecificText : MonoBehaviour
	{
		[SerializeField]
		protected string m_Mobile, m_Standalone;

		protected void Awake()
		{
			Text text = GetComponent<Text>();

			#if UNITY_STANDALONE || UNITY_STANDALONE_OSX || UNITY_EDITOR
			text.text = m_Standalone;
			#else
			text.text = m_Mobile;
			#endif
		}
	}
}