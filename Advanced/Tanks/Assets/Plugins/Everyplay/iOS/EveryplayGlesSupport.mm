#define EVERYPLAY_GLES_WRAPPER
#import "EveryplayGlesSupport.h"
#if __has_include(<Everyplay/Everyplay.h>)
#import <Everyplay/Everyplay.h>
#elif __has_include(<EveryplayCore/EveryplayCore.h>)
#define EVERYPLAY_COMPAT_WRAPPER 1
#import <EveryplayCore/EveryplayCore.h>
#endif

#if !defined(EVERYPLAY_CAPTURE_API_VERSION) || EVERYPLAY_CAPTURE_API_VERSION <= 1
#error "Everyplay SDK 1.7.6 or later required"
#endif
