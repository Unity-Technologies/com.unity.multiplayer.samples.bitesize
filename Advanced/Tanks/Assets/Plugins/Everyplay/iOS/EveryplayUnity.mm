#import "EveryplayUnity.h"

#define EVERYPLAY_GLES_WRAPPER
#import "EveryplayGlesSupport.h"

#if !EVERYPLAY_CORE_BUILD || (EVERYPLAY_CORE_BUILD && !EVERYPLAY_NO_FACECAM_SUPPORT)
#define EVERYPLAY_FACECAM_BINDINGS_ENABLED 1
#endif

#if UNITY_VERSION >= 463
#define EVERYPLAY_IS_METAL (UnitySelectedRenderingAPI() == apiMetal)
#else
#define EVERYPLAY_IS_METAL false
#endif

void UnitySendMessage(const char *obj, const char *method, const char *msg);

extern "C" {
static char *EveryplayCopyString(const char *string) {
    if (string != NULL) {
        char *res = strdup(string);
        return res;
    }

    return NULL;
}

static NSString *EveryplayCreateNSString(const char *string) {
    return string ? [NSString stringWithUTF8String:string] : [NSString stringWithUTF8String:""];
}

static NSURL *EveryplayCreateNSURL(const char *string) {
    return [NSURL URLWithString:EveryplayCreateNSString(string)];
}
}

static EveryplayUnity *everyplayUnity = [EveryplayUnity sharedInstance];
static const char *everyplayGameObjectName = NULL;

@implementation EveryplayUnity

+ (void)initialize {
    if (everyplayUnity == nil) {
        everyplayUnity = [[EveryplayUnity alloc] init];
    }
}

+ (EveryplayUnity *)sharedInstance {
    return everyplayUnity;
}

+ (NSString *)jsonFromDictionary:(NSDictionary *)dictionary {
    NSError *error = nil;
    NSData *jsonData = [NSJSONSerialization dataWithJSONObject:dictionary options:0 error:&error];
    if (error == nil) {
        return [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
    } else {
        EveryplayLog(@"Failed generating JSON: %@", error);
    }
    return nil;
}

+ (NSDictionary *)dictionaryFromJson:(NSString *)json {
    if (json != nil) {
        NSError *error = nil;
        NSData *jsonData = [json dataUsingEncoding:NSUTF8StringEncoding];

        id jsonParsedObj = [NSJSONSerialization JSONObjectWithData:jsonData options:0 error:&error];

        if (error == nil) {
            if ([jsonParsedObj isKindOfClass:[NSDictionary class]]) {
                return (NSDictionary *) jsonParsedObj;
            }
        } else {
            EveryplayLog(@"Failed parsing JSON: %@", error);
        }
    }

    return nil;
}

- (id)init {
    if (everyplayUnity != nil) {
        return everyplayUnity;
    }

    self = [super init];

    if (self) {
        everyplayUnity = self;
        displayLinkPaused = NO;

#if UNITY_VERSION >= 430
        UnityRegisterAppDelegateListener(self);
#endif
        [Everyplay initWithDelegate:self];
    }
    return self;
}

#if !EVERYPLAY_CORE_BUILD
- (void)setClientId:(NSString *)clientId andClientSecret:(NSString *)clientSecret andRedirectURI:(NSString *)redirectURI {
    [Everyplay setClientId:clientId clientSecret:clientSecret redirectURI:redirectURI];
    EveryplayLog(@"Everyplay init from Unity with client ID: %@ and client secret: %@ and redirect URI: %@", clientId, clientSecret, redirectURI);
}

#endif

- (void)everyplayShown {
    ELOG;

#if TARGET_OS_IPHONE && TARGET_OS_IOS
    currentOrientation = UnityGetGLViewController().interfaceOrientation;
#endif
    UnityPause(true);
#if UNITY_VERSION < 450
    CADisplayLink *displayLink = (CADisplayLink *) _displayLink;
#else
    CADisplayLink *displayLink = (CADisplayLink *) GetAppController().unityDisplayLink;
#endif
    if (displayLink != nil) {
        if ([displayLink isPaused] == NO) {
            displayLinkPaused = YES;
            [displayLink setPaused:YES];
            EveryplayLog(@"Everyplay paused _displayLink");
        }
    }
}

- (void)everyplayHidden {
    ELOG;
#if UNITY_VERSION < 450
    CADisplayLink *displayLink = (CADisplayLink *) _displayLink;
#else
    CADisplayLink *displayLink = (CADisplayLink *) GetAppController().unityDisplayLink;
#endif
    if (displayLink != nil && displayLinkPaused) {
        displayLinkPaused = NO;
        [displayLink setPaused:NO];
        EveryplayLog(@"Everyplay unpaused _displaylink");
    }
    UnityPause(false);

#if TARGET_OS_IPHONE && TARGET_OS_IOS
    /* Force orientation check, orientation could have changed while Unity was paused */
    UIInterfaceOrientation newOrientation = UnityGetGLViewController().interfaceOrientation;
    if (currentOrientation != newOrientation) {
#if UNITY_VERSION <= 450
        ScreenOrientation orientation = ConvertToUnityScreenOrientation(newOrientation, 0);
        UnitySetScreenOrientation(orientation);
#endif
#if UNITY_VERSION >= 400
        UnityGLInvalidateState();
#endif
    }
#endif

    if (everyplayGameObjectName != NULL) {
        UnitySendMessage(everyplayGameObjectName, "EveryplayHidden", "");
    }
}

- (void)everyplayReadyForRecording:(NSNumber *)enabled {
    ELOG;
    if (everyplayGameObjectName != NULL) {
        NSString *jsonMsg = [EveryplayUnity jsonFromDictionary:@{@"enabled": enabled}];
        UnitySendMessage(everyplayGameObjectName, "EveryplayReadyForRecording", [jsonMsg UTF8String]);
    }
}

- (void)everyplayRecordingStarted {
    ELOG;
    if (everyplayGameObjectName != NULL) {
        UnitySendMessage(everyplayGameObjectName, "EveryplayRecordingStarted", "");
    }
}

- (void)everyplayRecordingStopped {
    ELOG;
    if (everyplayGameObjectName != NULL) {
        UnitySendMessage(everyplayGameObjectName, "EveryplayRecordingStopped", "");
    }
}

#if EVERYPLAY_FACECAM_BINDINGS_ENABLED
- (void)everyplayFaceCamSessionStarted {
    ELOG;
    if (everyplayGameObjectName != NULL) {
        UnitySendMessage(everyplayGameObjectName, "EveryplayFaceCamSessionStarted", "");
    }
}

- (void)everyplayFaceCamRecordingPermission:(NSNumber *)granted {
    ELOG;
    if (everyplayGameObjectName != NULL) {
        NSString *jsonMsg = [EveryplayUnity jsonFromDictionary:@{@"granted": granted}];
        UnitySendMessage(everyplayGameObjectName, "EveryplayFaceCamRecordingPermission", [jsonMsg UTF8String]);
    }
}

- (void)everyplayFaceCamSessionStopped {
    ELOG;
    if (everyplayGameObjectName != NULL) {
        UnitySendMessage(everyplayGameObjectName, "EveryplayFaceCamSessionStopped", "");
    }
}

#endif

- (void)everyplayThumbnailReadyAtTextureId:(NSNumber *)textureId portraitMode:(NSNumber *)portrait {
    ELOG;
    if (everyplayGameObjectName != NULL) {
        NSString *jsonMsg = [EveryplayUnity jsonFromDictionary:@{@"textureId": textureId, @"portrait": portrait}];
        UnitySendMessage(everyplayGameObjectName, "EveryplayThumbnailReadyAtTextureId", [jsonMsg UTF8String]);
    }
}

- (void)everyplayMetalThumbnailReadyAtTexture:(id)texture portraitMode:(NSNumber *)portrait {
    ELOG;
    if (everyplayGameObjectName != NULL && texture != nil) {
        uintptr_t texturePtr = (uintptr_t) (__bridge void *) texture;
        NSString *jsonMsg = [EveryplayUnity jsonFromDictionary:@{@"texturePtr": [NSNumber numberWithLong:texturePtr], @"portrait": portrait}];
        UnitySendMessage(everyplayGameObjectName, "EveryplayThumbnailTextureReady", [jsonMsg UTF8String]);
    }
}

#if !EVERYPLAY_CORE_BUILD
- (void)everyplayUploadDidStart:(NSNumber *)videoId {
    ELOG;
    if (everyplayGameObjectName != NULL) {
        NSString *jsonMsg = [EveryplayUnity jsonFromDictionary:@{@"videoId": videoId}];
        UnitySendMessage(everyplayGameObjectName, "EveryplayUploadDidStart", [jsonMsg UTF8String]);
    }
}

- (void)everyplayUploadDidProgress:(NSNumber *)videoId progress:(NSNumber *)progress {
    ELOG;
    if (everyplayGameObjectName != NULL) {
        NSString *jsonMsg = [EveryplayUnity jsonFromDictionary:@{@"videoId": videoId, @"progress": progress}];
        UnitySendMessage(everyplayGameObjectName, "EveryplayUploadDidProgress", [jsonMsg UTF8String]);
    }
}

- (void)everyplayUploadDidComplete:(NSNumber *)videoId {
    ELOG;
    if (everyplayGameObjectName != NULL) {
        NSString *jsonMsg = [EveryplayUnity jsonFromDictionary:@{@"videoId": videoId}];
        UnitySendMessage(everyplayGameObjectName, "EveryplayUploadDidComplete", [jsonMsg UTF8String]);
    }
}

#if UNITY_VERSION >= 430
- (void)onOpenURL:(NSNotification *)notification {
    NSLog(@"onOpenURL notification received");
    [Everyplay handleOpenURL:[notification.userInfo objectForKey:@"url"] sourceApplication:[notification.userInfo objectForKey:@"sourceApplication"] annotation:nil];
}

#endif
#endif

@end

extern "C" {
void InitEveryplay(const char *clientId, const char *clientSecret, const char *redirectURI, const char *gameObjectName) {
    if (everyplayUnity != nil) {
#if !EVERYPLAY_CORE_BUILD
        [everyplayUnity setClientId:EveryplayCreateNSString(clientId) andClientSecret:EveryplayCreateNSString(clientSecret) andRedirectURI:EveryplayCreateNSString(redirectURI)];
#endif
    }

    if (gameObjectName != NULL) {
        if (everyplayGameObjectName != NULL) {
            free((char *) everyplayGameObjectName);
            everyplayGameObjectName = NULL;
        }

        everyplayGameObjectName = strdup(gameObjectName);
    }
}

#if !EVERYPLAY_CORE_BUILD
void EveryplayShow() {
    [[Everyplay sharedInstance] showEveryplay];
}

void EveryplayShowWithPath(const char *path) {
    NSString *pathString = EveryplayCreateNSString(path);
    [[Everyplay sharedInstance] showEveryplayWithPath:pathString];
}

void EveryplayPlayVideoWithURL(const char *url) {
    NSURL *urlUrl = EveryplayCreateNSURL(url);
    [[Everyplay sharedInstance] playVideoWithURL:urlUrl];
}

void EveryplayPlayVideoWithDictionary(const char *dic) {
    if (dic != NULL) {
        NSString *strValue = EveryplayCreateNSString(dic);
        NSDictionary *dictionary = [EveryplayUnity dictionaryFromJson:strValue];

        if (dictionary != nil) {
            [[Everyplay sharedInstance] playVideoWithDictionary:dictionary];
        }
    }
}

char *EveryplayAccountAccessToken() {
    return EveryplayCopyString([[[Everyplay account] accessToken] UTF8String]);
}

void EveryplayShowSharingModal() {
    [[Everyplay sharedInstance] showEveryplaySharingModal];
}

void EveryplayPlayLastRecording() {
    [[Everyplay sharedInstance] playLastRecording];
}

#endif

void EveryplayStartRecording() {
    [[[Everyplay sharedInstance] capture] startRecording];
}

void EveryplayStopRecording() {
    [[[Everyplay sharedInstance] capture] stopRecording];
}

void EveryplayPauseRecording() {
    [[[Everyplay sharedInstance] capture] pauseRecording];
}

void EveryplayResumeRecording() {
    [[[Everyplay sharedInstance] capture] resumeRecording];
}

bool EveryplayIsRecording() {
    return [[[Everyplay sharedInstance] capture] isRecording];
}

bool EveryplayIsRecordingSupported() {
    return [[[Everyplay sharedInstance] capture] isRecordingSupported];
}

bool EveryplayIsPaused() {
    return [[[Everyplay sharedInstance] capture] isPaused];
}

bool EveryplaySnapshotRenderbuffer() {
    BOOL ret = [[[Everyplay sharedInstance] capture] snapshotRenderbuffer];
    if (ret == true && EVERYPLAY_IS_METAL == true) {
#if UNITY_VERSION >= 400
        UnityGLInvalidateState();
#endif
    }
    return ret;
}

void EveryplaySetMetadata(const char *val) {
    if (val != NULL) {
        NSString *strValue = EveryplayCreateNSString(val);
        NSDictionary *dictionary = [EveryplayUnity dictionaryFromJson:strValue];

        if (dictionary != nil) {
            [[Everyplay sharedInstance] mergeSessionDeveloperData:dictionary];
        }
    }
}

void EveryplaySetTargetFPS(int fps) {
    [[Everyplay sharedInstance] capture].targetFPS = fps;
}

void EveryplaySetMotionFactor(int factor) {
    [[Everyplay sharedInstance] capture].motionFactor = factor;
}

void EveryplaySetMaxRecordingMinutesLength(int minutes) {
    [[Everyplay sharedInstance] capture].maxRecordingMinutesLength = minutes;
}

void EveryplaySetMaxRecordingSecondsLength(int seconds) {
    [[Everyplay sharedInstance] capture].maxRecordingSecondsLength = seconds;
}

void EveryplaySetLowMemoryDevice(bool state) {
    [[Everyplay sharedInstance] capture].lowMemoryDevice = state;
}

void EveryplaySetDisableSingleCoreDevices(bool state) {
    [[Everyplay sharedInstance] capture].disableSingleCoreDevices = state;
}

bool EveryplayIsSupported() {
    return [Everyplay isSupported];
}

bool EveryplayIsSingleCoreDevice() {
    return [[[Everyplay sharedInstance] capture] isSingleCoreDevice];
}

int EveryplayGetUserInterfaceIdiom() {
    return (int) [[UIDevice currentDevice] userInterfaceIdiom];
}

#if EVERYPLAY_FACECAM_BINDINGS_ENABLED
bool EveryplayFaceCamIsVideoRecordingSupported() {
    return [[[Everyplay sharedInstance] faceCam] isVideoRecordingSupported];
}

bool EveryplayFaceCamIsAudioRecordingSupported() {
    return [[[Everyplay sharedInstance] faceCam] isAudioRecordingSupported];
}

bool EveryplayFaceCamIsHeadphonesPluggedIn() {
    return [[[Everyplay sharedInstance] faceCam] isHeadphonesPluggedIn];
}

bool EveryplayFaceCamIsSessionRunning() {
    return [[[Everyplay sharedInstance] faceCam] isSessionRunning];
}

bool EveryplayFaceCamIsRecordingPermissionGranted() {
    return [[[Everyplay sharedInstance] faceCam] isRecordingPermissionGranted];
}

float EveryplayFaceCamAudioPeakLevel() {
    return [[[Everyplay sharedInstance] faceCam] audioPeakLevel];
}

float EveryplayFaceCamAudioPowerLevel() {
    return [[[Everyplay sharedInstance] faceCam] audioPowerLevel];
}

void EveryplayFaceCamSetMonitorAudioLevels(bool enabled) {
    [[[Everyplay sharedInstance] faceCam] setMonitorAudioLevels:enabled];
}

void EveryplayFaceCamSetRecordingMode(int mode) {
    [[[Everyplay sharedInstance] faceCam] setRecordingMode:static_cast<EveryplayFaceCamRecordingMode>(mode)];
}

void EveryplayFaceCamSetAudioOnly(bool audioOnly) {
    [[[Everyplay sharedInstance] faceCam] setAudioOnly:audioOnly];
}

void EveryplayFaceCamSetPreviewVisible(bool visible) {
    [[[Everyplay sharedInstance] faceCam] setPreviewVisible:visible];
}

void EveryplayFaceCamSetPreviewScaleRetina(bool autoScale) {
    [[[Everyplay sharedInstance] faceCam] setPreviewScaleRetina:autoScale];
}

void EveryplayFaceCamSetPreviewSideWidth(int width) {
    [[[Everyplay sharedInstance] faceCam] setPreviewSideWidth:width];
}

void EveryplayFaceCamSetPreviewBorderWidth(int width) {
    [[[Everyplay sharedInstance] faceCam] setPreviewBorderWidth:width];
}

void EveryplayFaceCamSetPreviewPositionX(int x) {
    [[[Everyplay sharedInstance] faceCam] setPreviewPositionX:x];
}

void EveryplayFaceCamSetPreviewPositionY(int y) {
    [[[Everyplay sharedInstance] faceCam] setPreviewPositionY:y];
}

void EveryplayFaceCamSetPreviewOrigin(int origin) {
    [[[Everyplay sharedInstance] faceCam] setPreviewOrigin:static_cast<EveryplayFaceCamPreviewOrigin>(origin)];
}

void EveryplayFaceCamSetPreviewBorderColor(float r, float g, float b, float a) {
    EveryplayFaceCamColor color = { .r = r, .g = g, .b = b, .a = a };
    [[[Everyplay sharedInstance] faceCam] setPreviewBorderColor:color];
}

void EveryplayFaceCamSetTargetTexture(void *texturePtr) {
    if (texturePtr != NULL) {
        if (EVERYPLAY_IS_METAL == true) {
            if ([[[Everyplay sharedInstance] faceCam] respondsToSelector:@selector(setTargetTextureMetal:)]) {
                [[[Everyplay sharedInstance] faceCam] performSelector:@selector(setTargetTextureMetal:) withObject:(__bridge id) texturePtr];
            }
        } else {
            [[[Everyplay sharedInstance] faceCam] setTargetTextureId:static_cast<int>(reinterpret_cast<uintptr_t>(texturePtr))];
        }
    } else {
        if (EVERYPLAY_IS_METAL == true) {
            if ([[[Everyplay sharedInstance] faceCam] respondsToSelector:@selector(setTargetTextureMetal:)]) {
                [[[Everyplay sharedInstance] faceCam] performSelector:@selector(setTargetTextureMetal:) withObject:nil];
            }
        } else {
            [[[Everyplay sharedInstance] faceCam] setTargetTextureId:0];
        }
    }
}

void EveryplayFaceCamSetTargetTextureId(int textureId) {
    [[[Everyplay sharedInstance] faceCam] setTargetTextureId:textureId];
}

void EveryplayFaceCamSetTargetTextureWidth(int textureWidth) {
    [[[Everyplay sharedInstance] faceCam] setTargetTextureWidth:textureWidth];
}

void EveryplayFaceCamSetTargetTextureHeight(int textureHeight) {
    [[[Everyplay sharedInstance] faceCam] setTargetTextureHeight:textureHeight];
}

void EveryplayFaceCamStartSession() {
    [[[Everyplay sharedInstance] faceCam] startSession];
}

void EveryplayFaceCamRequestRecordingPermission() {
    [[[Everyplay sharedInstance] faceCam] requestRecordingPermission];
}

void EveryplayFaceCamStopSession() {
    [[[Everyplay sharedInstance] faceCam] stopSession];
}

#endif

void EveryplaySetThumbnailTargetTexture(void *texturePtr) {
    if (texturePtr != NULL) {
        if (EVERYPLAY_IS_METAL == true) {
            if ([[[Everyplay sharedInstance] capture] respondsToSelector:@selector(setThumbnailTargetTextureMetal:)]) {
                [[[Everyplay sharedInstance] capture] performSelector:@selector(setThumbnailTargetTextureMetal:) withObject:(__bridge id) texturePtr];
            }
        } else {
            [[[Everyplay sharedInstance] capture] setThumbnailTargetTextureId:static_cast<int>(reinterpret_cast<uintptr_t>(texturePtr))];
        }
    } else {
        if (EVERYPLAY_IS_METAL == true) {
            if ([[[Everyplay sharedInstance] capture] respondsToSelector:@selector(setThumbnailTargetTextureMetal:)]) {
                [[[Everyplay sharedInstance] capture] performSelector:@selector(setThumbnailTargetTextureMetal:) withObject:nil];
            }
        } else {
            [[[Everyplay sharedInstance] capture] setThumbnailTargetTextureId:0];
        }
    }
}

void EveryplaySetThumbnailTargetTextureId(int textureId) {
    [[[Everyplay sharedInstance] capture] setThumbnailTargetTextureId:textureId];
}

void EveryplaySetThumbnailTargetTextureWidth(int textureWidth) {
    [[[Everyplay sharedInstance] capture] setThumbnailTargetTextureWidth:textureWidth];
}

void EveryplaySetThumbnailTargetTextureHeight(int textureHeight) {
    [[[Everyplay sharedInstance] capture] setThumbnailTargetTextureHeight:textureHeight];
}

void EveryplayTakeThumbnail() {
    [[[Everyplay sharedInstance] capture] takeThumbnail];
}

void EveryplayUnityRenderEvent(int eventID) {
    const int EPSR = 0x45505352;

    // EveryplayLog("UnityRenderEvent: %x", eventID);
    if (eventID == EPSR) {
        EveryplaySnapshotRenderbuffer();
    }
}

typedef void (*UnityRenderingEvent)(int eventId);

UnityRenderingEvent EveryplayGetUnityRenderEventPtr() {
    return EveryplayUnityRenderEvent;
}
}
