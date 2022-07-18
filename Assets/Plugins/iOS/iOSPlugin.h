#import <Foundation/Foundation.h>

bool _shareToTwitter(const char *, const char *);
bool _shareToInstagram(const char *, const char *);
void _shareToOther(const char *);
bool _hasCameraPermission();
void _requestCameraPermission(const char *, const char *);
bool _hasPhotosPermission();
void _requestPhotosPermission(const char *, const char *);
void _showImagePicker(const char *, const char *);
void _saveImageToDevice(const char *);

@interface iOSPlugin : NSObject <UIImagePickerControllerDelegate, UINavigationControllerDelegate, UIDocumentInteractionControllerDelegate>
{
	UIWindow *nativeWindow;
}

@property (nonatomic, retain) UIDocumentInteractionController *dic;

@property (nonatomic) NSString *imagePickerCallbackGameObject;
@property (nonatomic) NSString *imagePickerCallbackMethod;

+(iOSPlugin*)instance;

-(bool)shareToTwitter:(NSString*)message WithImage:(NSString*)imagePath;
-(bool)shareToInstagram:(NSString*)message WithImage:(NSString*)imagePath;
-(void)shareToOther:(NSString*)imagePath;
-(bool)hasCameraPermission;
-(void)requestCameraPermission:(NSString*)gameObjectName MethodName:(NSString*)methodName;
-(bool)hasPhotosPermission;
-(void)requestPhotosPermission:(NSString*)gameObjectName MethodName:(NSString*)methodName;
-(void)unitySendMessage:(NSString*)gameObjectName MethodName:(NSString*)methodName Message:(NSString*)message;
-(void)showImagePicker:(NSString*)gameObjectName MethodName:(NSString*)methodName;
-(void)saveImageToDevice:(NSString*)imagePath;

@end
