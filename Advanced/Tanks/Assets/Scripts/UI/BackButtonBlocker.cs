namespace Tanks.UI
{
	/// <summary>
	/// Implementation of Back Button that does nothing - used to block back button functionality on uncloseable modals
	/// </summary>
	public class BackButtonBlocker : BackButton
	{
		protected override void OnBackPressed()
		{
			// Do nothing!
		}
	}
}