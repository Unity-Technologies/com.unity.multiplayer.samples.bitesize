#import <Foundation/Foundation.h>

#pragma mark Handlers
typedef void (^EveryplayAccountUserLoadingHandler)(NSError *error, NSDictionary *data);
typedef void (^EveryplayAccountConnectionsLoadingHandler)(NSError *error, NSArray *connections);
#pragma mark Notifications

extern NSString *const EveryplayAccountDidFailToGetAccessToken;

@class NXOAuth2Account;

NS_CLASS_AVAILABLE(10_7, 4_0)
@interface EveryplayAccount : NSObject {
    @private
    NXOAuth2Account *oauthAccount;
    NSArray *connections;
}

@property (nonatomic, readonly) NSString *identifier;

#pragma mark Accessors
- (NSDictionary *)user;
- (NSString *)accessToken;

#pragma mark Methods
- (void)loadUserWithCompletionHandler:(EveryplayAccountUserLoadingHandler)aCompletionHandler;
- (void)loadUserConnectionsWithCompletionHandler:(EveryplayAccountConnectionsLoadingHandler)aCompletionHandler;

@end
