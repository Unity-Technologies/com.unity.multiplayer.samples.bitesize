#if !(UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7)
#define UNITY_5_OR_LATER
#endif

#if !UNITY_EDITOR

#if (UNITY_IPHONE && EVERYPLAY_IPHONE)
#define EVERYPLAY_IPHONE_ENABLED
#elif (UNITY_TVOS && EVERYPLAY_TVOS)
#define EVERYPLAY_TVOS_ENABLED
#elif (UNITY_ANDROID && EVERYPLAY_ANDROID)
#define EVERYPLAY_ANDROID_ENABLED
#elif (UNITY_5_OR_LATER && UNITY_STANDALONE_OSX && EVERYPLAY_STANDALONE)
#define EVERYPLAY_OSX_ENABLED
#endif

#else

#if UNITY_5_OR_LATER && UNITY_EDITOR_OSX
#define EVERYPLAY_OSX_ENABLED
#endif

#endif

#if EVERYPLAY_IPHONE_ENABLED || EVERYPLAY_ANDROID_ENABLED
#define EVERYPLAY_BINDINGS_ENABLED
#elif EVERYPLAY_TVOS_ENABLED || EVERYPLAY_OSX_ENABLED
#define EVERYPLAY_CORE_BINDINGS_ENABLED
#endif

#if EVERYPLAY_TVOS_ENABLED
#define EVERYPLAY_NO_FACECAM_SUPPORT
#endif

#if !EVERYPLAY_NO_FACECAM_SUPPORT && (EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED)
#define EVERYPLAY_FACECAM_BINDINGS_ENABLED
#endif

#if EVERYPLAY_OSX_ENABLED
#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
#define EVERYPLAY_RESET_BINDINGS_ENABLED
#endif
#endif

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.Collections;
using EveryplayMiniJSON;

public class Everyplay : MonoBehaviour
{
	// Enumerations

	public enum FaceCamPreviewOrigin
	{
		TopLeft = 0,
		TopRight,
		BottomLeft,
		BottomRight
	}

	public enum FaceCamRecordingMode
	{
		RecordAudio = 0,
		RecordVideo,
		PassThrough
	}

	public enum UserInterfaceIdiom
	{
		Phone = 0,
		Tablet,
		TV,
		iPhone = Phone,
		iPad = Tablet
	}

	// Delegates and events

	public delegate void WasClosedDelegate();

	public static event WasClosedDelegate WasClosed;

	public delegate void ReadyForRecordingDelegate(bool enabled);

	public static event ReadyForRecordingDelegate ReadyForRecording;

	public delegate void RecordingStartedDelegate();

	public static event RecordingStartedDelegate RecordingStarted;

	public delegate void RecordingStoppedDelegate();

	public static event RecordingStoppedDelegate RecordingStopped;

	public delegate void FaceCamSessionStartedDelegate();

	public static event FaceCamSessionStartedDelegate FaceCamSessionStarted;

	public delegate void FaceCamRecordingPermissionDelegate(bool granted);

	public static event FaceCamRecordingPermissionDelegate FaceCamRecordingPermission;

	public delegate void FaceCamSessionStoppedDelegate();

	public static event FaceCamSessionStoppedDelegate FaceCamSessionStopped;

	public delegate void ThumbnailReadyAtTextureIdDelegate(int textureId,bool portrait);

	public static event ThumbnailReadyAtTextureIdDelegate ThumbnailReadyAtTextureId;

	public delegate void ThumbnailTextureReadyDelegate(Texture2D texture,bool portrait);

	public static event ThumbnailTextureReadyDelegate ThumbnailTextureReady;

	public delegate void UploadDidStartDelegate(int videoId);

	public static event UploadDidStartDelegate UploadDidStart;

	public delegate void UploadDidProgressDelegate(int videoId,float progress);

	public static event UploadDidProgressDelegate UploadDidProgress;

	public delegate void UploadDidCompleteDelegate(int videoId);

	public static event UploadDidCompleteDelegate UploadDidComplete;

	public delegate void RequestReadyDelegate(string response);

	public delegate void RequestFailedDelegate(string error);

	// Private member variables

	private static string clientId;
	private static bool appIsClosing = false;
	private static bool hasMethods = true;
	private static bool seenInitialization = false;
	private static bool readyForRecording = false;

	#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
	private const string nativeMethodSource = "EveryplayPlugin";
	#else
    private const string nativeMethodSource = "__Internal";
#endif

	private static Everyplay everyplayInstance = null;

	private static Everyplay EveryplayInstance
	{
		get
		{
			if (everyplayInstance == null && !appIsClosing)
			{
				EveryplaySettings settings = (EveryplaySettings)Resources.Load("EveryplaySettings");

				if (settings != null)
				{
					if (settings.IsEnabled)
					{
						GameObject everyplayGameObject = new GameObject("Everyplay");

						if (everyplayGameObject != null)
						{
							everyplayGameObject.name = everyplayGameObject.name + everyplayGameObject.GetInstanceID();

							everyplayInstance = everyplayGameObject.AddComponent<Everyplay>();

							if (everyplayInstance != null)
							{
								clientId = settings.clientId;
								hasMethods = true;

								// Initialize the native
								#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
								try
								{
									InitEveryplay(settings.clientId, settings.clientSecret, settings.redirectURI, everyplayGameObject.name);
								}
								catch (DllNotFoundException)
								{
									hasMethods = false;
									everyplayInstance.OnApplicationQuit();
									return null;
								}
								catch (EntryPointNotFoundException)
								{
									hasMethods = false;
									everyplayInstance.OnApplicationQuit();
									return null;
								}
								#endif

								if (seenInitialization == false)
								{
#if EVERYPLAY_OSX_ENABLED
#if UNITY_5_OR_LATER
									AudioConfiguration config = AudioSettings.GetConfiguration();
									AudioSettings.Reset(config);
#endif
#endif
								}

								seenInitialization = true;

								// Add test buttons if requested
								if (settings.testButtonsEnabled)
								{
									AddTestButtons(everyplayGameObject);
								}

								DontDestroyOnLoad(everyplayGameObject);
							}
						}
					}
				}
			}

			return everyplayInstance;
		}
	}

	// Public static methods

	public static void Initialize()
	{
		// If everyplayInstance is not yet initialized, calling EveryplayInstance property getter will trigger the initialization
		if (EveryplayInstance == null)
		{
			Debug.Log("Unable to initialize Everyplay. Everyplay might be disabled for this platform or the app is closing.");
		}
	}

	public static void Show()
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_BINDINGS_ENABLED
            EveryplayShow();
			#endif
		}
	}

	public static void ShowWithPath(string path)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_BINDINGS_ENABLED
            EveryplayShowWithPath(path);
			#endif
		}
	}

	public static void PlayVideoWithURL(string url)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_BINDINGS_ENABLED
            EveryplayPlayVideoWithURL(url);
			#endif
		}
	}

	public static void PlayVideoWithDictionary(Dictionary<string, object> dict)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_BINDINGS_ENABLED
            EveryplayPlayVideoWithDictionary(Json.Serialize(dict));
			#endif
		}
	}

	public static void MakeRequest(string method, string url, Dictionary<string, object> data, Everyplay.RequestReadyDelegate readyDelegate, Everyplay.RequestFailedDelegate failedDelegate)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			EveryplayInstance.AsyncMakeRequest(method, url, data, readyDelegate, failedDelegate);
		}
	}

	public static string AccessToken()
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_BINDINGS_ENABLED
            return EveryplayAccountAccessToken();
			#endif
		}
		return null;
	}

	public static void ShowSharingModal()
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_BINDINGS_ENABLED
            EveryplayShowSharingModal();
			#endif
		}
	}

	public static void PlayLastRecording()
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_BINDINGS_ENABLED
            EveryplayPlayLastRecording();
			#endif
		}
	}

	public static void StartRecording()
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
			EveryplayStartRecording();
			#endif
		}
	}

	public static void StopRecording()
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
			EveryplayStopRecording();
			#endif
		}
	}

	public static void PauseRecording()
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
			EveryplayPauseRecording();
			#endif
		}
	}

	public static void ResumeRecording()
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
			EveryplayResumeRecording();
			#endif
		}
	}

	public static bool IsRecording()
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
			return EveryplayIsRecording();
			#endif
		}
		return false;
	}

	public static bool IsRecordingSupported()
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
			return EveryplayIsRecordingSupported();
			#endif
		}
		return false;
	}

	public static bool IsPaused()
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
			return EveryplayIsPaused();
			#endif
		}
		return false;
	}

	public static bool SnapshotRenderbuffer()
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
			return EveryplaySnapshotRenderbuffer();
			#endif
		}
		return false;
	}

	public static bool IsSupported()
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
			return EveryplayIsSupported();
			#endif
		}
		return false;
	}

	public static bool IsSingleCoreDevice()
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
			return EveryplayIsSingleCoreDevice();
			#endif
		}
		return false;
	}

	public static int GetUserInterfaceIdiom()
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
			return EveryplayGetUserInterfaceIdiom();
			#endif
		}
		return 0;
	}

	public static void SetMetadata(string key, object val)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
			if (key != null && val != null)
			{
				Dictionary<string, object> dict = new Dictionary<string, object>();
				dict.Add(key, val);
				EveryplaySetMetadata(Json.Serialize(dict));
			}
			#endif
		}
	}

	public static void SetMetadata(Dictionary<string, object> dict)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
			if (dict != null)
			{
				if (dict.Count > 0)
				{
					EveryplaySetMetadata(Json.Serialize(dict));
				}
			}
			#endif
		}
	}

	public static void SetTargetFPS(int fps)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
			EveryplaySetTargetFPS(fps);
			#endif
		}
	}

	public static void SetMotionFactor(int factor)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
			EveryplaySetMotionFactor(factor);
			#endif
		}
	}

	public static void SetAudioResamplerQuality(int quality)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if UNITY_ANDROID && !UNITY_EDITOR
			#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
            EveryplaySetAudioResamplerQuality(quality);
			#endif
			#endif
		}
	}

	public static void SetMaxRecordingMinutesLength(int minutes)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
			EveryplaySetMaxRecordingMinutesLength(minutes);
			#endif
		}
	}

	public static void SetMaxRecordingSecondsLength(int seconds)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
			EveryplaySetMaxRecordingSecondsLength(seconds);
			#endif
		}
	}

	public static void SetLowMemoryDevice(bool state)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
			EveryplaySetLowMemoryDevice(state);
			#endif
		}
	}

	public static void SetDisableSingleCoreDevices(bool state)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
			EveryplaySetDisableSingleCoreDevices(state);
			#endif
		}
	}

	public static bool FaceCamIsVideoRecordingSupported()
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_FACECAM_BINDINGS_ENABLED
			return EveryplayFaceCamIsVideoRecordingSupported();
			#endif
		}
		return false;
	}

	public static bool FaceCamIsAudioRecordingSupported()
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_FACECAM_BINDINGS_ENABLED
			return EveryplayFaceCamIsAudioRecordingSupported();
			#endif
		}
		return false;
	}

	public static bool FaceCamIsHeadphonesPluggedIn()
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_FACECAM_BINDINGS_ENABLED
			return EveryplayFaceCamIsHeadphonesPluggedIn();
			#endif
		}
		return false;
	}

	public static bool FaceCamIsSessionRunning()
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_FACECAM_BINDINGS_ENABLED
			return EveryplayFaceCamIsSessionRunning();
			#endif
		}
		return false;
	}

	public static bool FaceCamIsRecordingPermissionGranted()
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_FACECAM_BINDINGS_ENABLED
			return EveryplayFaceCamIsRecordingPermissionGranted();
			#endif
		}
		return false;
	}

	public static float FaceCamAudioPeakLevel()
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_FACECAM_BINDINGS_ENABLED
			return EveryplayFaceCamAudioPeakLevel();
			#endif
		}
		return 0.0f;
	}

	public static float FaceCamAudioPowerLevel()
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_FACECAM_BINDINGS_ENABLED
			return EveryplayFaceCamAudioPowerLevel();
			#endif
		}
		return 0.0f;
	}

	public static void FaceCamSetMonitorAudioLevels(bool enabled)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_FACECAM_BINDINGS_ENABLED
			EveryplayFaceCamSetMonitorAudioLevels(enabled);
			#endif
		}
	}

	public static void FaceCamSetRecordingMode(Everyplay.FaceCamRecordingMode mode)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_FACECAM_BINDINGS_ENABLED
			EveryplayFaceCamSetRecordingMode((int)mode);
			#endif
		}
	}

	public static void FaceCamSetAudioOnly(bool audioOnly)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_FACECAM_BINDINGS_ENABLED
			EveryplayFaceCamSetAudioOnly(audioOnly);
			#endif
		}
	}

	public static void FaceCamSetPreviewVisible(bool visible)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_FACECAM_BINDINGS_ENABLED
			EveryplayFaceCamSetPreviewVisible(visible);
			#endif
		}
	}

	public static void FaceCamSetPreviewScaleRetina(bool autoScale)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_FACECAM_BINDINGS_ENABLED
			EveryplayFaceCamSetPreviewScaleRetina(autoScale);
			#endif
		}
	}

	public static void FaceCamSetPreviewSideWidth(int width)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_FACECAM_BINDINGS_ENABLED
			EveryplayFaceCamSetPreviewSideWidth(width);
			#endif
		}
	}

	public static void FaceCamSetPreviewBorderWidth(int width)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_FACECAM_BINDINGS_ENABLED
			EveryplayFaceCamSetPreviewBorderWidth(width);
			#endif
		}
	}

	public static void FaceCamSetPreviewPositionX(int x)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_FACECAM_BINDINGS_ENABLED
			EveryplayFaceCamSetPreviewPositionX(x);
			#endif
		}
	}

	public static void FaceCamSetPreviewPositionY(int y)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_FACECAM_BINDINGS_ENABLED
			EveryplayFaceCamSetPreviewPositionY(y);
			#endif
		}
	}

	public static void FaceCamSetPreviewBorderColor(float r, float g, float b, float a)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_FACECAM_BINDINGS_ENABLED
			EveryplayFaceCamSetPreviewBorderColor(r, g, b, a);
			#endif
		}
	}

	public static void FaceCamSetPreviewOrigin(Everyplay.FaceCamPreviewOrigin origin)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_FACECAM_BINDINGS_ENABLED
			EveryplayFaceCamSetPreviewOrigin((int)origin);
			#endif
		}
	}

	public static void FaceCamSetTargetTexture(Texture2D texture)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
#if !UNITY_3_5
			#if EVERYPLAY_FACECAM_BINDINGS_ENABLED
			#if EVERYPLAY_IPHONE_ENABLED || EVERYPLAY_OSX_ENABLED
			if (texture != null)
			{
				EveryplayFaceCamSetTargetTexture(texture.GetNativeTexturePtr());
				EveryplayFaceCamSetTargetTextureWidth(texture.width);
				EveryplayFaceCamSetTargetTextureHeight(texture.height);
			}
			else
			{
				EveryplayFaceCamSetTargetTexture(System.IntPtr.Zero);
			}
			#elif EVERYPLAY_ANDROID_ENABLED
            if (texture != null)
            {
                int textureId = texture.GetNativeTexturePtr().ToInt32();
                EveryplayFaceCamSetTargetTextureId(textureId);
                EveryplayFaceCamSetTargetTextureWidth(texture.width);
                EveryplayFaceCamSetTargetTextureHeight(texture.height);
            }
            else
            {
                EveryplayFaceCamSetTargetTextureId(0);
            }
			#endif
			#endif
#endif
		}
	}

	[Obsolete("Use FaceCamSetTargetTexture(Texture2D texture) instead.")]
	public static void FaceCamSetTargetTextureId(int textureId)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_FACECAM_BINDINGS_ENABLED
			EveryplayFaceCamSetTargetTextureId(textureId);
			#endif
		}
	}

	[Obsolete("Defining texture width is no longer required when FaceCamSetTargetTexture(Texture2D texture) is used.")]
	public static void FaceCamSetTargetTextureWidth(int textureWidth)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_FACECAM_BINDINGS_ENABLED
			EveryplayFaceCamSetTargetTextureWidth(textureWidth);
			#endif
		}
	}

	[Obsolete("Defining texture height is no longer required when FaceCamSetTargetTexture(Texture2D texture) is used.")]
	public static void FaceCamSetTargetTextureHeight(int textureHeight)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_FACECAM_BINDINGS_ENABLED
			EveryplayFaceCamSetTargetTextureHeight(textureHeight);
			#endif
		}
	}

	public static void FaceCamStartSession()
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_FACECAM_BINDINGS_ENABLED
			EveryplayFaceCamStartSession();
			#endif
		}
	}

	public static void FaceCamRequestRecordingPermission()
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_FACECAM_BINDINGS_ENABLED
			EveryplayFaceCamRequestRecordingPermission();
			#endif
		}
	}

	public static void FaceCamStopSession()
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_FACECAM_BINDINGS_ENABLED
			EveryplayFaceCamStopSession();
			#endif
		}
	}

	private static Texture2D currentThumbnailTargetTexture = null;

	public static void SetThumbnailTargetTexture(Texture2D texture)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			currentThumbnailTargetTexture = texture;
#if !UNITY_3_5
			#if EVERYPLAY_IPHONE_ENABLED || EVERYPLAY_OSX_ENABLED
			if (texture != null)
			{
				EveryplaySetThumbnailTargetTexture(currentThumbnailTargetTexture.GetNativeTexturePtr());
				EveryplaySetThumbnailTargetTextureWidth(currentThumbnailTargetTexture.width);
				EveryplaySetThumbnailTargetTextureHeight(currentThumbnailTargetTexture.height);
			}
			else
			{
				EveryplaySetThumbnailTargetTexture(System.IntPtr.Zero);
			}
			#elif EVERYPLAY_ANDROID_ENABLED
            if (texture != null)
            {
                int textureId = currentThumbnailTargetTexture.GetNativeTexturePtr().ToInt32();
                EveryplaySetThumbnailTargetTextureId(textureId);
                EveryplaySetThumbnailTargetTextureWidth(currentThumbnailTargetTexture.width);
                EveryplaySetThumbnailTargetTextureHeight(currentThumbnailTargetTexture.height);
            }
            else
            {
                EveryplaySetThumbnailTargetTextureId(0);
            }
			#endif
#endif
		}
	}

	[Obsolete("Use SetThumbnailTargetTexture(Texture2D texture) instead.")]
	public static void SetThumbnailTargetTextureId(int textureId)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
			EveryplaySetThumbnailTargetTextureId(textureId);
			#endif
		}
	}

	[Obsolete("Defining texture width is no longer required when SetThumbnailTargetTexture(Texture2D texture) is used.")]
	public static void SetThumbnailTargetTextureWidth(int textureWidth)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
			EveryplaySetThumbnailTargetTextureWidth(textureWidth);
			#endif
		}
	}

	[Obsolete("Defining texture height is no longer required when SetThumbnailTargetTexture(Texture2D texture) is used.")]
	public static void SetThumbnailTargetTextureHeight(int textureHeight)
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
			EveryplaySetThumbnailTargetTextureHeight(textureHeight);
			#endif
		}
	}

	public static void TakeThumbnail()
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
			EveryplayTakeThumbnail();
			#endif
		}
	}

	public static bool IsReadyForRecording()
	{
		if (EveryplayInstance != null && hasMethods == true)
		{
			return readyForRecording;
		}
		return false;
	}

	// Private static methods

	private static void RemoveAllEventHandlers()
	{
		WasClosed = null;
		ReadyForRecording = null;
		RecordingStarted = null;
		RecordingStopped = null;
		FaceCamSessionStarted = null;
		FaceCamRecordingPermission = null;
		FaceCamSessionStopped = null;
#pragma warning disable 612, 618
		ThumbnailReadyAtTextureId = null;
#pragma warning restore 612, 618
		ThumbnailTextureReady = null;
		UploadDidStart = null;
		UploadDidProgress = null;
		UploadDidComplete = null;
	}

	private static void Reset()
	{
#if EVERYPLAY_RESET_BINDINGS_ENABLED
		try
		{
			if (seenInitialization)
			{
				ResetEveryplay();
			}
		}
		catch (DllNotFoundException)
		{
		}
		catch (EntryPointNotFoundException)
		{
		}
#endif
	}

	private static void AddTestButtons(GameObject gameObject)
	{
		Texture2D textureAtlas = (Texture2D)Resources.Load("everyplay-test-buttons", typeof(Texture2D));
		if (textureAtlas != null)
		{
			EveryplayRecButtons recButtons = gameObject.AddComponent<EveryplayRecButtons>();
			if (recButtons != null)
			{
				recButtons.atlasTexture = textureAtlas;
			}
		}
	}

	// Private instance methods

	private void AsyncMakeRequest(string method, string url, Dictionary<string, object> data, Everyplay.RequestReadyDelegate readyDelegate, Everyplay.RequestFailedDelegate failedDelegate)
	{
		StartCoroutine(MakeRequestEnumerator(method, url, data, readyDelegate, failedDelegate));
	}

	private IEnumerator MakeRequestEnumerator(string method, string url, Dictionary<string, object> data, Everyplay.RequestReadyDelegate readyDelegate, Everyplay.RequestFailedDelegate failedDelegate)
	{
		if (data == null)
		{
			data = new Dictionary<string, object>();
		}

		if (url.IndexOf("http") != 0)
		{
			if (url.IndexOf("/") != 0)
			{
				url = "/" + url;
			}

			url = "https://api.everyplay.com" + url;
		}

		method = method.ToLower();

#if UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3
        Hashtable headers = new Hashtable();
#else
		Dictionary<string, string> headers = new Dictionary<string, string>();
#endif

		string accessToken = AccessToken();
		if (accessToken != null)
		{
			headers["Authorization"] = "Bearer " + accessToken;
		}
		else
		{
			if (url.IndexOf("client_id") == -1)
			{
				if (url.IndexOf("?") == -1)
				{
					url += "?";
				}
				else
				{
					url += "&";
				}
				url += "client_id=" + clientId;
			}
		}

		data.Add("_method", method);

		string dataString = Json.Serialize(data);
		byte[] dataArray = System.Text.Encoding.UTF8.GetBytes(dataString);

		headers["Accept"] = "application/json";
		headers["Content-Type"] = "application/json";
		headers["Data-Type"] = "json";
		headers["Content-Length"] = dataArray.Length.ToString();

		WWW www = new WWW(url, dataArray, headers);

		yield return www;

		if (!string.IsNullOrEmpty(www.error) && failedDelegate != null)
		{
			failedDelegate("Everyplay error: " + www.error);
		}
		else if (string.IsNullOrEmpty(www.error) && readyDelegate != null)
		{
			readyDelegate(www.text);
		}
	}

	#if UNITY_EDITOR && !(UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1)
	[UnityEditor.Callbacks.DidReloadScripts]
	private static void OnScriptsReloaded()
	{
		if (everyplayInstance != null)
		{
			everyplayInstance.OnApplicationQuit();
		}
		else
		{
			Reset();
		}
	}

	#endif

	// Monobehaviour methods

	void OnApplicationQuit()
	{
		Reset();

		if (currentThumbnailTargetTexture != null)
		{
			SetThumbnailTargetTexture(null);
			currentThumbnailTargetTexture = null;
		}
		RemoveAllEventHandlers();
		appIsClosing = true;
		everyplayInstance = null;
	}

	// Private instance methods called by native

	private void EveryplayHidden(string msg)
	{
		if (WasClosed != null)
		{
			WasClosed();
		}
	}

	private void EveryplayReadyForRecording(string jsonMsg)
	{
		Dictionary<string, object> dict = EveryplayDictionaryExtensions.JsonToDictionary(jsonMsg);
		bool enabled;

		if (EveryplayDictionaryExtensions.TryGetValue(dict, "enabled", out enabled))
		{
			readyForRecording = enabled;

			if (ReadyForRecording != null)
			{
				ReadyForRecording(enabled);
			}
		}
	}

	private void EveryplayRecordingStarted(string msg)
	{
		if (RecordingStarted != null)
		{
			RecordingStarted();
		}
	}

	private void EveryplayRecordingStopped(string msg)
	{
		if (RecordingStopped != null)
		{
			RecordingStopped();
		}
	}

	private void EveryplayFaceCamSessionStarted(string msg)
	{
		if (FaceCamSessionStarted != null)
		{
			FaceCamSessionStarted();
		}
	}

	private void EveryplayFaceCamRecordingPermission(string jsonMsg)
	{
		if (FaceCamRecordingPermission != null)
		{
			Dictionary<string, object> dict = EveryplayDictionaryExtensions.JsonToDictionary(jsonMsg);
			bool granted;

			if (EveryplayDictionaryExtensions.TryGetValue(dict, "granted", out granted))
			{
				FaceCamRecordingPermission(granted);
			}
		}
	}

	private void EveryplayFaceCamSessionStopped(string msg)
	{
		if (FaceCamSessionStopped != null)
		{
			FaceCamSessionStopped();
		}
	}

	private void EveryplayThumbnailReadyAtTextureId(string jsonMsg)
	{
#pragma warning disable 612, 618
		if (ThumbnailReadyAtTextureId != null || ThumbnailTextureReady != null)
		{
			Dictionary<string, object> dict = EveryplayDictionaryExtensions.JsonToDictionary(jsonMsg);
			int textureId;
			bool portrait;

			if (EveryplayDictionaryExtensions.TryGetValue(dict, "textureId", out textureId) && EveryplayDictionaryExtensions.TryGetValue(dict, "portrait", out portrait))
			{
				if (ThumbnailReadyAtTextureId != null)
				{
					ThumbnailReadyAtTextureId(textureId, portrait);
				}
#if !UNITY_3_5
				if (ThumbnailTextureReady != null && currentThumbnailTargetTexture != null)
				{
					if (currentThumbnailTargetTexture.GetNativeTextureID() == textureId)
					{
						ThumbnailTextureReady(currentThumbnailTargetTexture, portrait);
					}
				}
#endif
			}
		}
#pragma warning restore 612, 618
	}

	private void EveryplayThumbnailTextureReady(string jsonMsg)
	{
#if !UNITY_3_5
		if (ThumbnailTextureReady != null)
		{
			Dictionary<string, object> dict = EveryplayDictionaryExtensions.JsonToDictionary(jsonMsg);
			long texturePtr;
			bool portrait;

			if (currentThumbnailTargetTexture != null && EveryplayDictionaryExtensions.TryGetValue(dict, "texturePtr", out texturePtr) && EveryplayDictionaryExtensions.TryGetValue(dict, "portrait", out portrait))
			{
				long currentPtr = (long)currentThumbnailTargetTexture.GetNativeTexturePtr();
				if (currentPtr == texturePtr)
				{
					ThumbnailTextureReady(currentThumbnailTargetTexture, portrait);
				}
			}
		}
#endif
	}

	private void EveryplayUploadDidStart(string jsonMsg)
	{
		if (UploadDidStart != null)
		{
			Dictionary<string, object> dict = EveryplayDictionaryExtensions.JsonToDictionary(jsonMsg);
			int videoId;

			if (EveryplayDictionaryExtensions.TryGetValue(dict, "videoId", out videoId))
			{
				UploadDidStart(videoId);
			}
		}
	}

	private void EveryplayUploadDidProgress(string jsonMsg)
	{
		if (UploadDidProgress != null)
		{
			Dictionary<string, object> dict = EveryplayDictionaryExtensions.JsonToDictionary(jsonMsg);
			int videoId;
			double progress;

			if (EveryplayDictionaryExtensions.TryGetValue(dict, "videoId", out videoId) && EveryplayDictionaryExtensions.TryGetValue(dict, "progress", out progress))
			{
				UploadDidProgress(videoId, (float)progress);
			}
		}
	}

	private void EveryplayUploadDidComplete(string jsonMsg)
	{
		if (UploadDidComplete != null)
		{
			Dictionary<string, object> dict = EveryplayDictionaryExtensions.JsonToDictionary(jsonMsg);
			int videoId;

			if (EveryplayDictionaryExtensions.TryGetValue(dict, "videoId", out videoId))
			{
				UploadDidComplete(videoId);
			}
		}
	}

	// Native calls

	#if EVERYPLAY_IPHONE_ENABLED || EVERYPLAY_TVOS_ENABLED || EVERYPLAY_OSX_ENABLED

	#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
	[DllImport(nativeMethodSource)]
	private static extern void InitEveryplay(string clientId, string clientSecret, string redirectURI, string gameObjectName);
	#endif

	#if EVERYPLAY_BINDINGS_ENABLED
    [DllImport(nativeMethodSource)]
    private static extern void EveryplayShow();

    [DllImport(nativeMethodSource)]
    private static extern void EveryplayShowWithPath(string path);

    [DllImport(nativeMethodSource)]
    private static extern void EveryplayPlayVideoWithURL(string url);

    [DllImport(nativeMethodSource)]
    private static extern void EveryplayPlayVideoWithDictionary(string dic);

    [DllImport(nativeMethodSource)]
    private static extern string EveryplayAccountAccessToken();

    [DllImport(nativeMethodSource)]
    private static extern void EveryplayShowSharingModal();

    [DllImport(nativeMethodSource)]
    private static extern void EveryplayPlayLastRecording();
    #endif

	#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
	[DllImport(nativeMethodSource)]
	private static extern void EveryplayStartRecording();

	[DllImport(nativeMethodSource)]
	private static extern void EveryplayStopRecording();

	[DllImport(nativeMethodSource)]
	private static extern void EveryplayPauseRecording();

	[DllImport(nativeMethodSource)]
	private static extern void EveryplayResumeRecording();

	[DllImport(nativeMethodSource)]
	private static extern bool EveryplayIsRecording();

	[DllImport(nativeMethodSource)]
	private static extern bool EveryplayIsRecordingSupported();

	[DllImport(nativeMethodSource)]
	private static extern bool EveryplayIsPaused();

	[DllImport(nativeMethodSource)]
	private static extern bool EveryplaySnapshotRenderbuffer();

	[DllImport(nativeMethodSource)]
	private static extern void EveryplaySetMetadata(string json);

	[DllImport(nativeMethodSource)]
	private static extern void EveryplaySetTargetFPS(int fps);

	[DllImport(nativeMethodSource)]
	private static extern void EveryplaySetMotionFactor(int factor);

	[DllImport(nativeMethodSource)]
	private static extern void EveryplaySetMaxRecordingMinutesLength(int minutes);

	[DllImport(nativeMethodSource)]
	private static extern void EveryplaySetMaxRecordingSecondsLength(int seconds);

	[DllImport(nativeMethodSource)]
	private static extern void EveryplaySetLowMemoryDevice(bool state);

	[DllImport(nativeMethodSource)]
	private static extern void EveryplaySetDisableSingleCoreDevices(bool state);

	[DllImport(nativeMethodSource)]
	private static extern bool EveryplayIsSupported();

	[DllImport(nativeMethodSource)]
	private static extern bool EveryplayIsSingleCoreDevice();

	[DllImport(nativeMethodSource)]
	private static extern int EveryplayGetUserInterfaceIdiom();
	#endif

	#if EVERYPLAY_FACECAM_BINDINGS_ENABLED
	[DllImport(nativeMethodSource)]
	private static extern bool EveryplayFaceCamIsVideoRecordingSupported();

	[DllImport(nativeMethodSource)]
	private static extern bool EveryplayFaceCamIsAudioRecordingSupported();

	[DllImport(nativeMethodSource)]
	private static extern bool EveryplayFaceCamIsHeadphonesPluggedIn();

	[DllImport(nativeMethodSource)]
	private static extern bool EveryplayFaceCamIsSessionRunning();

	[DllImport(nativeMethodSource)]
	private static extern bool EveryplayFaceCamIsRecordingPermissionGranted();

	[DllImport(nativeMethodSource)]
	private static extern float EveryplayFaceCamAudioPeakLevel();

	[DllImport(nativeMethodSource)]
	private static extern float EveryplayFaceCamAudioPowerLevel();

	[DllImport(nativeMethodSource)]
	private static extern void EveryplayFaceCamSetMonitorAudioLevels(bool enabled);

	[DllImport(nativeMethodSource)]
	private static extern void EveryplayFaceCamSetRecordingMode(int mode);

	[DllImport(nativeMethodSource)]
	private static extern void EveryplayFaceCamSetAudioOnly(bool audioOnly);

	[DllImport(nativeMethodSource)]
	private static extern void EveryplayFaceCamSetPreviewVisible(bool visible);

	[DllImport(nativeMethodSource)]
	private static extern void EveryplayFaceCamSetPreviewScaleRetina(bool autoScale);

	[DllImport(nativeMethodSource)]
	private static extern void EveryplayFaceCamSetPreviewSideWidth(int width);

	[DllImport(nativeMethodSource)]
	private static extern void EveryplayFaceCamSetPreviewBorderWidth(int width);

	[DllImport(nativeMethodSource)]
	private static extern void EveryplayFaceCamSetPreviewPositionX(int x);

	[DllImport(nativeMethodSource)]
	private static extern void EveryplayFaceCamSetPreviewPositionY(int y);

	[DllImport(nativeMethodSource)]
	private static extern void EveryplayFaceCamSetPreviewBorderColor(float r, float g, float b, float a);

	[DllImport(nativeMethodSource)]
	private static extern void EveryplayFaceCamSetPreviewOrigin(int origin);

	[DllImport(nativeMethodSource)]
	private static extern void EveryplayFaceCamSetTargetTexture(System.IntPtr texturePtr);

	[DllImport(nativeMethodSource)]
	private static extern void EveryplayFaceCamSetTargetTextureId(int textureId);

	[DllImport(nativeMethodSource)]
	private static extern void EveryplayFaceCamSetTargetTextureWidth(int textureWidth);

	[DllImport(nativeMethodSource)]
	private static extern void EveryplayFaceCamSetTargetTextureHeight(int textureHeight);

	[DllImport(nativeMethodSource)]
	private static extern void EveryplayFaceCamStartSession();

	[DllImport(nativeMethodSource)]
	private static extern void EveryplayFaceCamRequestRecordingPermission();

	[DllImport(nativeMethodSource)]
	private static extern void EveryplayFaceCamStopSession();
	#endif

	#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
	[DllImport(nativeMethodSource)]
	private static extern void EveryplaySetThumbnailTargetTexture(System.IntPtr texturePtr);

	[DllImport(nativeMethodSource)]
	private static extern void EveryplaySetThumbnailTargetTextureId(int textureId);

	[DllImport(nativeMethodSource)]
	private static extern void EveryplaySetThumbnailTargetTextureWidth(int textureWidth);

	[DllImport(nativeMethodSource)]
	private static extern void EveryplaySetThumbnailTargetTextureHeight(int textureHeight);

	[DllImport(nativeMethodSource)]
	private static extern void EveryplayTakeThumbnail();
	#endif

	#if EVERYPLAY_RESET_BINDINGS_ENABLED
	[DllImport(nativeMethodSource)]
	private static extern void ResetEveryplay();
	#endif

	#elif EVERYPLAY_ANDROID_ENABLED
	
    private static AndroidJavaObject everyplayUnity;

    





#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
    private static void InitEveryplay(string clientId, string clientSecret, string redirectURI, string gameObjectName)
    {
        AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity = jc.GetStatic<AndroidJavaObject>("currentActivity");
        everyplayUnity = new AndroidJavaObject("com.everyplay.Everyplay.unity.EveryplayUnity3DWrapper");
        everyplayUnity.Call("initEveryplay", activity, clientId, clientSecret, redirectURI, gameObjectName);
    }

    #endif
	
    





#if EVERYPLAY_BINDINGS_ENABLED
    private static void EveryplayShow()
    {
        everyplayUnity.Call<bool>("showEveryplay");
    }

    private static void EveryplayShowWithPath(string path)
    {
        everyplayUnity.Call<bool>("showEveryplay", path);
    }

    private static void EveryplayPlayVideoWithURL(string url)
    {
        everyplayUnity.Call("playVideoWithURL", url);
    }

    private static void EveryplayPlayVideoWithDictionary(string dic)
    {
        everyplayUnity.Call("playVideoWithDictionary", dic);
    }

    private static string EveryplayAccountAccessToken()
    {
        return everyplayUnity.Call<string>("getAccessToken");
    }

    private static void EveryplayShowSharingModal()
    {
        everyplayUnity.Call("showSharingModal");
    }

    private static void EveryplayPlayLastRecording()
    {
        everyplayUnity.Call("playLastRecording");
    }

    #endif
	
    





#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
    private static void EveryplayStartRecording()
    {
        everyplayUnity.Call("startRecording");
    }

    private static void EveryplayStopRecording()
    {
        everyplayUnity.Call("stopRecording");
    }

    private static void EveryplayPauseRecording()
    {
        everyplayUnity.Call("pauseRecording");
    }

    private static void EveryplayResumeRecording()
    {
        everyplayUnity.Call("resumeRecording");
    }

    private static bool EveryplayIsRecording()
    {
        return everyplayUnity.Call<bool>("isRecording");
    }

    private static bool EveryplayIsRecordingSupported()
    {
        return everyplayUnity.Call<bool>("isRecordingSupported");
    }

    private static bool EveryplayIsPaused()
    {
        return everyplayUnity.Call<bool>("isPaused");
    }

    private static bool EveryplaySnapshotRenderbuffer()
    {
        return everyplayUnity.Call<bool>("snapshotRenderbuffer");
    }

    private static void EveryplaySetMetadata(string json)
    {
        everyplayUnity.Call("setMetadata", json);
    }

    private static void EveryplaySetTargetFPS(int fps)
    {
        everyplayUnity.Call("setTargetFPS", fps);
    }

    private static void EveryplaySetMotionFactor(int factor)
    {
        everyplayUnity.Call("setMotionFactor", factor);
    }

    private static void EveryplaySetAudioResamplerQuality(int quality)
    {
        everyplayUnity.Call("setAudioResamplerQuality", quality);
    }

    private static void EveryplaySetMaxRecordingMinutesLength(int minutes)
    {
        everyplayUnity.Call("setMaxRecordingMinutesLength", minutes);
    }

    private static void EveryplaySetMaxRecordingSecondsLength(int seconds)
    {
        everyplayUnity.Call("setMaxRecordingSecondsLength", seconds);
    }

    private static void EveryplaySetLowMemoryDevice(bool state)
    {
        everyplayUnity.Call("setLowMemoryDevice", state ? 1 : 0);
    }

    private static void EveryplaySetDisableSingleCoreDevices(bool state)
    {
        everyplayUnity.Call("setDisableSingleCoreDevices", state ? 1 : 0);
    }

    private static bool EveryplayIsSupported()
    {
        return everyplayUnity.Call<bool>("isSupported");
    }

    private static bool EveryplayIsSingleCoreDevice()
    {
        return everyplayUnity.Call<bool>("isSingleCoreDevice");
    }

    private static int EveryplayGetUserInterfaceIdiom()
    {
        return everyplayUnity.Call<int>("getUserInterfaceIdiom");
    }

    #endif
	
    





#if EVERYPLAY_FACECAM_BINDINGS_ENABLED
    private static bool EveryplayFaceCamIsVideoRecordingSupported()
    {
        return everyplayUnity.Call<bool>("faceCamIsVideoRecordingSupported");
    }

    private static bool EveryplayFaceCamIsAudioRecordingSupported()
    {
        return everyplayUnity.Call<bool>("faceCamIsAudioRecordingSupported");
    }

    private static bool EveryplayFaceCamIsHeadphonesPluggedIn()
    {
        return everyplayUnity.Call<bool>("faceCamIsHeadphonesPluggedIn");
    }

    private static bool EveryplayFaceCamIsSessionRunning()
    {
        return everyplayUnity.Call<bool>("faceCamIsSessionRunning");
    }

    private static bool EveryplayFaceCamIsRecordingPermissionGranted()
    {
        return everyplayUnity.Call<bool>("faceCamIsRecordingPermissionGranted");
    }

    private static float EveryplayFaceCamAudioPeakLevel()
    {
        return everyplayUnity.Call<float>("faceCamAudioPeakLevel");
    }

    private static float EveryplayFaceCamAudioPowerLevel()
    {
        return everyplayUnity.Call<float>("faceCamAudioPowerLevel");
    }

    private static void EveryplayFaceCamSetMonitorAudioLevels(bool enabled)
    {
        everyplayUnity.Call("faceCamSetSetMonitorAudioLevels", enabled);
    }

    private static void EveryplayFaceCamSetRecordingMode(int mode)
    {
        everyplayUnity.Call("faceCamSetRecordingMode", mode);
    }

    private static void EveryplayFaceCamSetAudioOnly(bool audioOnly)
    {
        everyplayUnity.Call("faceCamSetAudioOnly", audioOnly);
    }

    private static void EveryplayFaceCamSetPreviewVisible(bool visible)
    {
        everyplayUnity.Call("faceCamSetPreviewVisible", visible);
    }

    private static void EveryplayFaceCamSetPreviewScaleRetina(bool autoScale)
    {
        Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name + " not available on Android");
    }

    private static void EveryplayFaceCamSetPreviewSideWidth(int width)
    {
        everyplayUnity.Call("faceCamSetPreviewSideWidth", width);
    }

    private static void EveryplayFaceCamSetPreviewBorderWidth(int width)
    {
        everyplayUnity.Call("faceCamSetPreviewBorderWidth", width);
    }

    private static void EveryplayFaceCamSetPreviewPositionX(int x)
    {
        everyplayUnity.Call("faceCamSetPreviewPositionX", x);
    }

    private static void EveryplayFaceCamSetPreviewPositionY(int y)
    {
        everyplayUnity.Call("faceCamSetPreviewPositionY", y);
    }

    private static void EveryplayFaceCamSetPreviewBorderColor(float r, float g, float b, float a)
    {
        everyplayUnity.Call("faceCamSetPreviewBorderColor", r, g, b, a);
    }

    private static void EveryplayFaceCamSetPreviewOrigin(int origin)
    {
        everyplayUnity.Call("faceCamSetPreviewOrigin", origin);
    }

    private static void EveryplayFaceCamSetTargetTextureId(int textureId)
    {
        everyplayUnity.Call("faceCamSetTargetTextureId", textureId);
    }

    private static void EveryplayFaceCamSetTargetTextureWidth(int textureWidth)
    {
        everyplayUnity.Call("faceCamSetTargetTextureWidth", textureWidth);
    }

    private static void EveryplayFaceCamSetTargetTextureHeight(int textureHeight)
    {
        everyplayUnity.Call("faceCamSetTargetTextureHeight", textureHeight);
    }

    private static void EveryplayFaceCamStartSession()
    {
        everyplayUnity.Call("faceCamStartSession");
    }

    private static void EveryplayFaceCamRequestRecordingPermission()
    {
        everyplayUnity.Call("faceCamRequestRecordingPermission");
    }

    private static void EveryplayFaceCamStopSession()
    {
        everyplayUnity.Call("faceCamStopSession");
    }

    #endif
	
    





#if EVERYPLAY_BINDINGS_ENABLED || EVERYPLAY_CORE_BINDINGS_ENABLED
    private static void EveryplaySetThumbnailTargetTextureId(int textureId)
    {
        everyplayUnity.Call("setThumbnailTargetTextureId", textureId);
    }

    private static void EveryplaySetThumbnailTargetTextureWidth(int textureWidth)
    {
        everyplayUnity.Call("setThumbnailTargetTextureWidth", textureWidth);
    }

    private static void EveryplaySetThumbnailTargetTextureHeight(int textureHeight)
    {
        everyplayUnity.Call("setThumbnailTargetTextureHeight", textureHeight);
    }

    private static void EveryplayTakeThumbnail()
    {
        everyplayUnity.Call("takeThumbnail");
    }

    #endif
	
    #endif
}

#if UNITY_EDITOR
[InitializeOnLoad]
public class EveryplayEditor
{
	static EveryplayEditor()
	{
		EditorApplication.playmodeStateChanged = OnUnityPlayModeChanged;
	}

	private static void OnUnityPlayModeChanged()
	{
		if (EditorApplication.isPaused == true)
		{
			Everyplay.PauseRecording();
		}
		else if (EditorApplication.isPlaying == true)
		{
			Everyplay.ResumeRecording();
		}
	}
}
#endif

#if UNITY_EDITOR || UNITY_STANDALONE_OSX
public class EveryplaySendMessageDispatcher
{
	public static void Dispatch(string name, string method, string message)
	{
		GameObject obj = GameObject.Find(name);
		if (obj != null)
		{
			obj.SendMessage(method, message);
		}
	}
}
#endif
