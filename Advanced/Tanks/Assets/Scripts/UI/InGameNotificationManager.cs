using UnityEngine;
using System.Collections;
using Tanks.Utilities;

namespace Tanks.UI
{
	/// <summary>
	/// Controls the instantiation and positioning of InGameNotifications.
	/// </summary>
	public class InGameNotificationManager : Singleton<InGameNotificationManager>
	{
		//Prefab to instantiate for notification.
		[SerializeField]
		private InGameNotification notificationPrefab;

		/// <summary>
		/// Create a notification message.
		/// </summary>
		/// <param name="message">Message text.</param>
		public void Notify(string message)
		{
			InGameNotification notification = (InGameNotification)Instantiate(notificationPrefab, transform, false);
			notification.SetNotificationText(message);
		}
	}
}