using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Tanks.UI
{
    [RequireComponent(typeof(Text))]
	/// <summary>
	/// Controls the display of one-line game status notications (kills, suicides, etc.)
	/// </summary>
    public class InGameNotification : MonoBehaviour
    {
        //Time before message is destroyed.
		[SerializeField]
		private float m_Lifetime = 3f;

		//The text to be displayed.
		private Text m_NotificationText;

        private void Awake()
        {
            m_NotificationText = GetComponent<Text>();

			//Queue this message's death. It has been ordained.
            Destroy(gameObject, m_Lifetime);
        }

		/// <summary>
		/// Sets the notification text to display.
		/// </summary>
		/// <param name="message">Message to display.</param>
        public void SetNotificationText(string message)
        {
            m_NotificationText.text = message;
        }
    }
}