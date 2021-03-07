using UnityEngine;

namespace Tanks.IAP
{
	/// <summary>
	/// A class to represent a bit of IAP content that you will be awarded after buying an iap item
	/// </summary>
	public abstract class IAPContent : ScriptableObject
	{
		public abstract void Award();
	}
}