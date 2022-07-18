#import "iOSPlugin.h"
#import "Social/Social.h"
#import "AVFoundation/AVFoundation.h"
#import "Photos/Photos.h"

bool _shareToTwitter(const char * message, const char * imagePath)
{
	NSString *m = [NSString stringWithUTF8String:message];
	NSString *i = [NSString stringWithUTF8String:imagePath];
	
	return [[iOSPlugin instance] shareToTwitter:m WithImage:i];
}

bool _shareToInstagram(const char * message, const char * imagePath)
{
	NSString *m = [NSString stringWithUTF8String:message];
	NSString *i = [NSString stringWithUTF8String:imagePath];
	
	return [[iOSPlugin instance] shareToInstagram:m WithImage:i];
}

void _shareToOther(const char * imagePath)
{
	NSString *i = [NSString stringWithUTF8String:imagePath];
	
	[[iOSPlugin instance] shareToOther:i];
}

bool _hasCameraPermission()
{
	return [[iOSPlugin instance] hasCameraPermission];
}

void _requestCameraPermission(const char * callbackGameObjectName, const char * callbackMethodName)
{
	NSString *objectName = [NSString stringWithUTF8String:callbackGameObjectName];
	NSString *methodName = [NSString stringWithUTF8String:callbackMethodName];
	
	[[iOSPlugin instance] requestCameraPermission:objectName MethodName:methodName];
}

bool _hasPhotosPermission()
{
	return [[iOSPlugin instance] hasPhotosPermission];
}

void _requestPhotosPermission(const char * callbackGameObjectName, const char * callbackMethodName)
{
	NSString *objectName = [NSString stringWithUTF8String:callbackGameObjectName];
	NSString *methodName = [NSString stringWithUTF8String:callbackMethodName];
	
	[[iOSPlugin instance] requestPhotosPermission:objectName MethodName:methodName];
}

void _showImagePicker(const char * callbackGameObjectName, const char * callbackMethodName)
{
	NSString *objectName = [NSString stringWithUTF8String:callbackGameObjectName];
	NSString *methodName = [NSString stringWithUTF8String:callbackMethodName];
	
	[[iOSPlugin instance] showImagePicker:objectName MethodName:methodName];
}

void _saveImageToDevice(const char * path)
{
	NSString *imagePath = [NSString stringWithUTF8String:path];
	
	[[iOSPlugin instance] saveImageToDevice:imagePath];
}

@implementation iOSPlugin

@synthesize dic;

static iOSPlugin *instance = nil;

+(iOSPlugin*)instance
{
	if (!instance)
	{
		instance = [[iOSPlugin alloc] init];
	}
	
	return instance;
}

-(id)init
{
	if (self = [super init])
	{
		nativeWindow = [UIApplication sharedApplication].keyWindow;
	}
	
	return self;
}

-(void)unitySendMessage:(NSString*)gameObjectName MethodName:(NSString*)methodName Message:(NSString*)message;
{
	const char * name	= [gameObjectName cStringUsingEncoding:NSASCIIStringEncoding];
	const char * method	= [methodName cStringUsingEncoding:NSASCIIStringEncoding];
	const char * msg	= [message cStringUsingEncoding:NSASCIIStringEncoding];
	
	UnitySendMessage(name, method, msg);
}

-(bool)shareToTwitter:(NSString*)message WithImage:(NSString*)imagePath;
{
	NSURL *appURL = [NSURL URLWithString:@"twitter://app"];
	if([[UIApplication sharedApplication] canOpenURL:appURL])
	{
		UIImage *image = [UIImage imageWithContentsOfFile:imagePath];
		
		SLComposeViewController *tweetSheet = [SLComposeViewController composeViewControllerForServiceType:SLServiceTypeTwitter];
		[tweetSheet setInitialText:message];
		[tweetSheet addImage:image];
		[nativeWindow.rootViewController presentViewController:tweetSheet animated:YES completion:nil];
	
		return true;
	}
	
	return false;
}

-(bool)shareToInstagram:(NSString*)message WithImage:(NSString*)imagePath;
{
	NSURL *appURL = [NSURL URLWithString:@"instagram://app"];
	if([[UIApplication sharedApplication] canOpenURL:appURL])
	{
		// Image
		UIImage *image = [UIImage imageWithContentsOfFile:imagePath];
		
		// Post
		[UIImageJPEGRepresentation(image, 1.0) writeToFile:[self photoFilePathInstagram] atomically:YES];
		NSURL *fileURL = [NSURL fileURLWithPath:[self photoFilePathInstagram]];
		
		self.dic = [UIDocumentInteractionController interactionControllerWithURL:fileURL];
		self.dic.UTI = @"com.instagram.exclusivegram";
		self.dic.delegate = self;
		
		if (message)
		{
			self.dic.annotation = [NSDictionary dictionaryWithObject:message forKey:@"InstagramCaption"];
		}
		
		[self.dic presentOpenInMenuFromRect:CGRectZero inView:nativeWindow.rootViewController.view animated:YES];
	
		return true;
	}
	
	return false;
}

-(void)shareToOther:(NSString *)imagePath;
{
	// Image
	UIImage *image = [UIImage imageWithContentsOfFile:imagePath];
	
	NSArray *objectsToShare = @[image];
	
	UIActivityViewController *activityVC = [[UIActivityViewController alloc] initWithActivityItems:objectsToShare applicationActivities:nil];
	
	NSArray *excludeActivities = @[UIActivityTypeAssignToContact,
								   UIActivityTypeAddToReadingList,
								   UIActivityTypePostToTwitter,
								   UIActivityTypePostToVimeo];
	
	activityVC.excludedActivityTypes = excludeActivities;
	
	if ([activityVC respondsToSelector:@selector(popoverPresentationController)] )
	{
		activityVC.popoverPresentationController.sourceView = nativeWindow.rootViewController.view;
	}
	
	[nativeWindow.rootViewController presentViewController:activityVC animated:YES completion:nil];
}

-(NSString*)photoFilePathInstagram
{
	return [NSString stringWithFormat:@"%@/%@",[NSHomeDirectory() stringByAppendingPathComponent:@"Documents"], @"tempinstgramphoto.igo"];
}

-(NSString*)photoFilePath
{
	return [NSString stringWithFormat:@"%@/%@",[NSHomeDirectory() stringByAppendingPathComponent:@"Documents"], @"temp.jpeg"];
}

-(UIDocumentInteractionController*)setupControllerWithURL:(NSURL*)fileURL usingDelegate:(id<UIDocumentInteractionControllerDelegate>)interactionDelegate
{
	UIDocumentInteractionController *interactionController = [UIDocumentInteractionController interactionControllerWithURL:fileURL];
	interactionController.delegate = interactionDelegate;
	return interactionController;
}

-(bool)hasCameraPermission
{
	// iOS < 7 always has permission
	if (![AVCaptureDevice respondsToSelector:@selector(requestAccessForMediaType: completionHandler:)])
	{
		return true;
	}

	// Get the permission status
	AVAuthorizationStatus status = [AVCaptureDevice authorizationStatusForMediaType:AVMediaTypeVideo];
	
	// Return true if its authorized
	return (status == AVAuthorizationStatusAuthorized);
}

-(void) requestCameraPermission:(NSString*)callbackGameObjectName MethodName:(NSString*)callbackMethodName;
{
	// Don't need to request for iOS < 7
	if (![AVCaptureDevice respondsToSelector:@selector(requestAccessForMediaType: completionHandler:)])
	{
		[self unitySendMessage:callbackGameObjectName MethodName:callbackMethodName Message:@"true"];
		
		return;
	}
	
	// Request authorization from the user, this will display a dialog to the user
	[AVCaptureDevice requestAccessForMediaType:AVMediaTypeVideo completionHandler:^(BOOL granted)
	{
		if(granted)
		{
			// Access granted by user
			[self unitySendMessage:callbackGameObjectName MethodName:callbackMethodName Message:@"true"];
		}
		else
		{
			// Access denied by user
			[self unitySendMessage:callbackGameObjectName MethodName:callbackMethodName Message:@"false"];
		}
	}];
}

-(bool)hasPhotosPermission
{
	// Get the permission status
	PHAuthorizationStatus status = [PHPhotoLibrary authorizationStatus];
	
	// Return true if its authorized
	return (status == PHAuthorizationStatusAuthorized);
}

-(void) requestPhotosPermission:(NSString*)callbackGameObjectName MethodName:(NSString*)callbackMethodName;
{
	// Request authorization from the user, this will display a dialog to the user
	[PHPhotoLibrary requestAuthorization:^(PHAuthorizationStatus status)
	{
		if(status == PHAuthorizationStatusAuthorized)
		{
			// Access granted by user
			[self unitySendMessage:callbackGameObjectName MethodName:callbackMethodName Message:@"true"];
		}
		else
		{
			// Access denied by user
			[self unitySendMessage:callbackGameObjectName MethodName:callbackMethodName Message:@"false"];
		}
	}];
}

 - (void)showImagePicker:(NSString*)callbackGameObjectName MethodName:(NSString*)callbackMethodName;
 {
 	self.imagePickerCallbackGameObject = callbackGameObjectName;
 	self.imagePickerCallbackMethod = callbackMethodName;
 
 	UIImagePickerController	*imagePicker = [[UIImagePickerController alloc] init];

 	imagePicker.delegate = self;
 	imagePicker.allowsEditing = NO;
 	imagePicker.sourceType = UIImagePickerControllerSourceTypePhotoLibrary;

     UIViewController *unityController = UnityGetGLViewController();
	 
     [unityController presentViewController:imagePicker animated:YES completion:NULL];
 }

- (void)imagePickerController:(UIImagePickerController *)picker didFinishPickingMediaWithInfo:(NSDictionary<NSString *, id> *)info
{
	// Get an image path that we can save the image to and pass the path back to Unity
    NSArray *paths = NSSearchPathForDirectoriesInDomains(NSDocumentDirectory, NSUserDomainMask, YES);
    NSString *documentsDirectory = [paths objectAtIndex:0];
    NSString *imagePath = [documentsDirectory stringByAppendingPathComponent:@"device_image.png"];
	
	// Get the image and save it to the path
    UIImage *image = info[UIImagePickerControllerOriginalImage];
    NSData *imageData = UIImagePNGRepresentation(image);
    [imageData writeToFile:imagePath atomically:YES];

	UIViewController *unityController = UnityGetGLViewController();

    [unityController dismissViewControllerAnimated:YES completion:NULL];

	NSString *callbackGameObject = [[iOSPlugin instance] imagePickerCallbackGameObject];
	NSString *callbackMethod = [[iOSPlugin instance] imagePickerCallbackMethod];

	[[iOSPlugin instance] unitySendMessage:callbackGameObject MethodName:callbackMethod Message:imagePath];
}

- (void)imagePickerControllerDidCancel:(UIImagePickerController *)picker
{
	NSString *callbackGameObject = [[iOSPlugin instance] imagePickerCallbackGameObject];
	NSString *callbackMethod = [[iOSPlugin instance] imagePickerCallbackMethod];
	
	UIViewController *unityController = UnityGetGLViewController();

    [unityController dismissViewControllerAnimated:YES completion:NULL];
	
	[[iOSPlugin instance] unitySendMessage:callbackGameObject MethodName:callbackMethod Message:@""];
}

- (void)saveImageToDevice:(NSString*)imagePath;
{
	UIImage *image = [UIImage imageWithContentsOfFile:imagePath];
	
	UIImageWriteToSavedPhotosAlbum(image, nil, nil, nil);
}

@end
