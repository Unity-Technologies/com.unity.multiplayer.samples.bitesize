using UnityEngine;
using System.Collections;
using System;

#if UNITY_ADS
using UnityEngine.Advertisements;
#endif

using Tanks.Utilities;

namespace Tanks.Advertising
{
	//NOTE: For any of this functionality to work, Unity Adverts must be enabled in your Services for this project, and the required libraries must be downloaded.
	//Advert functionality has been deprecated from this game, but this class is included for educational purposes.

	//This class allows adverts to be played via Unity Ads, and custom events to be fired based on the outcome of the ad playing.
	public class TanksAdvertController : PersistentSingleton<TanksAdvertController>
	{
		//Fields to allow the Ad ID for this project to be specified in-editor.
		[SerializeField] protected string m_AndroidAdId;
		[SerializeField] protected string m_IosAdId;

		//Fields to specify the zone IDs for debug mode and production. This allows custom settings for debug videos (such as being able to skip them).
		[SerializeField] protected string m_DefaultZoneId = "rewardedVideo";
		[SerializeField] protected string m_DevZoneId = "video";

		//The active video zone.
		private string m_videoZone;

		#if UNITY_ADS
		//Events to be fired based on advert success.

		private event Action OnNextAdvertFinished;
		private event Action OnNextAdvertFailed;
		private event Action OnNextAdvertSkipped;
		#endif

		protected virtual void Start()
		{
			#if UNITY_ADS

			//Init Ads with the ID relevant to the platform
			#if UNITY_ANDROID
					Advertisement.Initialize(m_AndroidAdId, true);
			#elif UNITY_IOS
					Advertisement.Initialize(m_IosAdId, true);
			#endif

			//We set our zone ID based on whether this is a development build or not. At this stage this gives us the ability to skip "unskippable" ads when testing.
			#if DEVELOPMENT_BUILD
				m_videoZone = m_DevZoneId;
			#else
					m_videoZone = m_DefaultZoneId;
			#endif

			#endif
		}


		//Starts an advert, firing one of the provided delegates based on what happens.
		public void StartAdvert(Action onAdComplete, Action onAdError, Action onAdSkipped = null)
		{
			#if UNITY_ADS

			//We assume immediate ad completion in editor.
			#if UNITY_EDITOR
				onAdComplete();

			#endif

			//Ads isn't supported in non-mobile versions yet, so shield it.
			#if !UNITY_STANDALONE && !UNITY_STANDALONE_OSX

				if(Advertisement.isSupported)
				{
					//If we can run ads, store the delegate references for later

					OnNextAdvertFinished = onAdComplete;
					OnNextAdvertFailed = onAdError;
					OnNextAdvertSkipped = onAdSkipped;

					//Create our options object and assign the callback method
					ShowOptions adOptions = new ShowOptions();
					adOptions.resultCallback = HandleAdCallbacks;

					//Final check for readiness, and then fire. If not ready, fire error delegate.
					if(Advertisement.IsReady())
					{
						Debug.Log("[ADVERTS] Starting Ad for zone "+m_videoZone);
						Advertisement.Show(m_videoZone, adOptions);
					}
					else
					{
						Debug.Log("[ADVERTS] Advert is not ready. Running onAdError and aborting StartAdvert.");
						onAdError();
					}
				}
				else
				{
					Debug.Log("[ADVERTS] Adverts are not supported on this platform. Aborting StartAdvert");
				}
			#endif

			#endif
		}
			
		#if UNITY_ADS
		public bool IsAdvertReady()
		{
			

#if UNITY_STANDALONE || UNITY_STANDALONE_OSX
			return true;
			

#else
			return Advertisement.IsReady();
			#endif
		}

		public bool IsAdvertShowing()
		{
			

#if UNITY_STANDALONE || UNITY_STANDALONE_OSX
			return true;
			

#else
			return Advertisement.isShowing;
			#endif
		}
		#endif

		public bool AreAdsSupported()
		{
			#if UNITY_ADS

			#if UNITY_EDITOR
				return true;
			#elif UNITY_STANDALONE || UNITY_STANDALONE_OSX
				return false;
			#else
				return Advertisement.isSupported;
			#endif

			#else

			//Disable ads.
			return false;

			#endif
		}

		#if UNITY_ADS
	   
		
	
#if !(UNITY_STANDALONE || UNITY_STANDALONE_OSX)
		
		//This method is subscribed to the advert results callback, and handles the results reported by the ad.
		protected void HandleAdCallbacks(ShowResult advertResult)
		{
			if (advertResult == ShowResult.Finished)
			{
				Debug.Log("[ADVERTS] Ad finished. Firing OnNextAdvertCompleted.");
				OnNextAdvertFinished();
			}
			else if (advertResult == ShowResult.Skipped)
			{
					
		
#if DEVELOPMENT_BUILD
				//If this is a dev build, we want ads to be skippable while still firing Finished logic.

				Debug.Log("[ADVERTS] Skipped in dev build. Firing OnNextAdvertCompleted.");
				OnNextAdvertFinished();
		
#else
				if(OnNextAdvertSkipped != null)
				{
					Debug.Log("[ADVERTS] Ad skipped and delegate provided. Firing OnNextAdvertSkipped.");
					OnNextAdvertSkipped();
				}

		#endif
			}
			else if (advertResult == ShowResult.Failed)
			{
				Debug.Log("[ADVERTS] Ad failed to display. Firing OnNextAdvertFailed.");
				OnNextAdvertFailed();
			}
			
			//reset our delegates for the next time an ad is run
			OnNextAdvertFinished = null;
			OnNextAdvertSkipped = null;
			OnNextAdvertFailed = null;

		}
	#endif
		
#endif
	}
}