#import <Foundation/Foundation.h>

typedef void (^EveryplayRequestResponseHandler)(NSURLResponse *response, NSData *responseData, NSError *error);
typedef void (^EveryplayRequestSendingProgressHandler)(unsigned long long bytesSend, unsigned long long bytesTotal);


enum EveryplayRequestMethod {
    EveryplayRequestMethodGET = 0,
    EveryplayRequestMethodPOST,
    EveryplayRequestMethodPUT,
    EveryplayRequestMethodDELETE,
    EveryplayRequestMethodHEAD
};
typedef enum EveryplayRequestMethod EveryplayRequestMethod;


@class NXOAuth2Request;
@class EveryplayAccount;

/*!
 @class EveryplayRequest

 @abstract
 The `EveryplayRequest` object is used to make authenticated requests to Everyplay
 REST API. This class provides helper methods that simplify the connection
 and response handling.

 @discussion
 An <EveryplayAccount> object is required for all authenticated uses of `EveryplayRequest`.
 Requests that do not require an authenticated user are also supported and
 do not require an <EveryplayAccount> object to be passed in.

 An instance of `EveryplayAccount` represents the arguments and setup for a connection
 to Everyplay. To access public resources using `EveryplayRequest` nil can be passed as
 <EveryplayAccount>.

 Class and instance methods prefixed with **perform* ** can be used to perform the
 request setup and initiate the connection in a single call.

 */

NS_CLASS_AVAILABLE(10_7, 4_0)
@interface EveryplayRequest : NSObject {
    @private
    NXOAuth2Request *oauthRequest;
}


#pragma mark Class Methods


/*!
 @method

 @abstract
 Starts a connection to the Everyplay API.

 @discussion
 This is used to start an API call to Everyplay.

 @param handler   The handler block to call when the request completes with a success, error, or cancel action.
 */
+ (id)       performMethod:(EveryplayRequestMethod)aMethod
                onResource:(NSURL *)resource
           usingParameters:(NSDictionary *)parameters
               withAccount:(EveryplayAccount *)account
    sendingProgressHandler:(EveryplayRequestSendingProgressHandler)progressHandler
           responseHandler:(EveryplayRequestResponseHandler)responseHandler;


/*!
 @method

 @abstract
 Starts a connection to the Everyplay API.

 @discussion
 This is used to start an API call to Everyplay.

 @param handler   The handler block to call when the request completes with a success, error, or cancel action.
 */
+ (id)       performMethod:(EveryplayRequestMethod)aMethod
                onResource:(NSURL *)resource
           usingParameters:(NSDictionary *)parameters
              withClientId:(NSString *)clientId
    sendingProgressHandler:(EveryplayRequestSendingProgressHandler)progressHandler
           responseHandler:(EveryplayRequestResponseHandler)responseHandler;


/*!
 @method

 @abstract
 Starts a connection to the Everyplay API.

 @discussion
 This is used to start an API call to Everyplay.

 @param handler   The handler block to call when the request completes with a success, error, or cancel action.
 */
+ (id)       performMethod:(EveryplayRequestMethod)aMethod
                onResource:(NSURL *)resource
           usingParameters:(NSDictionary *)parameters
    sendingProgressHandler:(EveryplayRequestSendingProgressHandler)progressHandler
           responseHandler:(EveryplayRequestResponseHandler)responseHandler;

/*!
 @method

 @abstract
 Creates a connection to the Everyplay API.

 @discussion
 This is used to start an API call to Everyplay.

 @param handler   The handler block to call when the request completes with a success, error, or cancel action.
 */
+ (id)createRequestWithMethod:(EveryplayRequestMethod)aMethod
                   onResource:(NSURL *)aResource
              usingParameters:(NSDictionary *)someParameters;


+ (id)createRequestWithMethod:(EveryplayRequestMethod)aMethod
                   onResource:(NSURL *)aResource
                  withAccount:(EveryplayAccount *)account
              usingParameters:(NSDictionary *)someParameters;


/*!
 @method

 @abstract
 Cancels a connection to the Everyplay API.

 @discussion
 This is used to cancel an API call to Everyplay.

 @param handler   The handler block to call when the request completes with a success, error, or cancel action.
 */
+ (void)cancelRequest:(id)request;

/*!
 @method

 @abstract
 Returns `EveryplayRequestMethod` matching the method string.

 @discussion
 Valid method strings are "GET", "POST", "PUT", "DELETE"

 @param method  The method string to be converted into an `EveryplayRequestMethod`
 */
+ (EveryplayRequestMethod)requestMethodWithString:(NSString *)method;

/*!
 @method

 @abstract
 Returns `NSString` matching the method string.

 @discussion
 Returned strings are "GET", "POST", "PUT", "DELETE"

 @param method  The `EveryplayRequestMethod`
 */
+ (NSString *)stringFromMethod:(EveryplayRequestMethod)method;


#pragma mark Initializer

/*!
 @method

 @abstract
 Creates a new EveryplayRequest object for manual control.

 @discussion

 @param method  the `EveryplayRequestMethod`
 */
- (id)initWithMethod:(EveryplayRequestMethod)aMethod resource:(NSURL *)aResource;

#pragma mark Accessors

@property (nonatomic, readwrite, retain) EveryplayAccount *account;
@property (nonatomic, assign) EveryplayRequestMethod requestMethod;
@property (nonatomic, readwrite, retain) NSURL *resource;
@property (nonatomic, readwrite, retain) NSDictionary *parameters;


#pragma mark Signed NSURLRequest

/*!
 @method

 @abstract
 returns a NSURLRequest that has all the authorization information from `EveryplayAccount`

 */
- (NSURLRequest *)signedURLRequest;

#pragma mark Perform Request

/*!
 @method

 @abstract
 performs the actual request

 @discussion

 @param progressHandler `EveryplayRequestSendingProgressHandler`
 @param responseHandler `EveryplayRequestResponseHandler`
 */
- (void)performRequestWithSendingProgressHandler:(EveryplayRequestSendingProgressHandler)progressHandler
                                 responseHandler:(EveryplayRequestResponseHandler)responseHandler;

#pragma mark Cancel Request

/*!
 @method

 @abstract
 Cancels the request and any underleying NSURLConnections

 */
- (void)cancel;


@end
