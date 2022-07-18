using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace BizzyBeeGames.PictureColoring
{
	public static class NativePlugin
	{
		#if UNITY_IOS
		[DllImport("__Internal")]
		static extern bool _shareToTwitter(string message, string imagePath);

		[DllImport("__Internal")]
		static extern bool _shareToInstagram(string message, string imagePath);

		[DllImport("__Internal")]
		static extern void _shareToOther(string imagePath);

		[DllImport("__Internal")]
		static extern bool _hasCameraPermission();

		[DllImport("__Internal")]
		static extern void _requestCameraPermission(string callbackGameObjectName, string callbackMethodName);

		[DllImport("__Internal")]
		static extern bool _hasPhotosPermission();

		[DllImport("__Internal")]
		static extern void _requestPhotosPermission(string callbackGameObjectName, string callbackMethodName);

		[DllImport("__Internal")]
		static extern void _showImagePicker(string callbackGameObjectName, string callbackMethodName);

		[DllImport("__Internal")]
		static extern void _saveImageToDevice(string imagePath);
		#endif

		public static bool Exists()
		{
			#if UNITY_EDITOR
			return false;
			#elif UNITY_ANDROID
			try
			{
				return new AndroidJavaClass("com.nfagan.share.Share") != null && new AndroidJavaClass("com.nfagan.utils.Utils") != null;
			}
			catch (System.Exception)
			{
				Debug.LogError("[NativePlugin] SharePlugin has not been imported.");
				return false;
			}
			#elif UNITY_IOS
			return true;
			#endif
		}

		/// <summary>
		/// Tries to open the Twitter app to share the image. Returns false if Twitter is not installed on the device.
		/// </summary>
		public static bool TryShareToTwitter(string imagePath)
		{
			#if UNITY_EDITOR
			return false;
			#elif UNITY_ANDROID
			AndroidJavaClass plugin = new AndroidJavaClass ("com.nfagan.share.Share");
		
			if (plugin != null)
			{
				return plugin.CallStatic<bool>("shareToTwitter", "", imagePath);
			}

			return false;
			#elif UNITY_IOS
			return _shareToTwitter("", imagePath);
			#endif
		}

		/// <summary>
		/// Tries to open the Instagram app to share the image. Returns false if Instagram is not installed on the device.
		/// </summary>
		public static bool TryShareToInstagram(string imagePath)
		{
			#if UNITY_EDITOR
			return false;
			#elif UNITY_ANDROID
			AndroidJavaClass plugin = new AndroidJavaClass ("com.nfagan.share.Share");
		
			if (plugin != null)
			{
				return plugin.CallStatic<bool>("shareToInstagram", "", imagePath);
			}

			return false;
			#elif UNITY_IOS
			return _shareToInstagram("", imagePath);
			#endif
		}

		/// <summary>
		/// Opens a list of applications that can handle the image for the user to select
		/// </summary>
		public static void ShareToOther(string imagePath)
		{
			#if !UNITY_EDITOR && UNITY_ANDROID
			AndroidJavaClass plugin = new AndroidJavaClass ("com.nfagan.share.Share");
		
			if (plugin != null)
			{
				plugin.CallStatic("shareToOther", imagePath);
			}
			#elif !UNITY_EDITOR && UNITY_IOS
			_shareToOther(imagePath);
			#endif
		}

		/// <summary>
		/// Checks if the application has permission to use the camera (ei WebCamTexture)
		/// </summary>
		public static bool HasCameraPermission()
		{
			#if UNITY_EDITOR
			return true;
			#elif UNITY_ANDROID
			AndroidJavaClass plugin = new AndroidJavaClass ("com.nfagan.utils.Utils");

			if (plugin != null)
			{
				return plugin.CallStatic<bool>("hasPermission", "android.permission.CAMERA");
			}

			return false;
			#elif UNITY_IOS
			return _hasCameraPermission();
			#endif
		}

		/// <summary>
		/// Checks if the application has permission to use the camera (ei WebCamTexture)
		/// </summary>
		public static bool HasReadExternalStoragePermission()
		{
			#if UNITY_EDITOR || UNITY_IOS
			return true;
			#elif UNITY_ANDROID
			AndroidJavaClass plugin = new AndroidJavaClass ("com.nfagan.utils.Utils");

			if (plugin != null)
			{
				return plugin.CallStatic<bool>("hasPermission", "android.permission.READ_EXTERNAL_STORAGE");
			}

			return false;
			#endif
		}

		/// <summary>
		/// Checks if the application has permission to use the camera (ei WebCamTexture)
		/// </summary>
		public static void RequestCameraPermission(string callbackGameObjectName, string callbackMethodName)
		{
			#if UNITY_IOS && !UNITY_EDITOR
			_requestCameraPermission(callbackGameObjectName, callbackMethodName);
			#endif
		}

			/// <summary>
		/// Checks if the application has permission to use the camera (ei WebCamTexture)
		/// </summary>
		public static bool HasPhotosPermission()
		{
			#if UNITY_EDITOR
			return true;
			#elif UNITY_ANDROID
			AndroidJavaClass plugin = new AndroidJavaClass ("com.nfagan.utils.Utils");

			if (plugin != null)
			{
				return plugin.CallStatic<bool>("hasPermission", "android.permission.WRITE_EXTERNAL_STORAGE");
			}

			return false;
			#elif UNITY_IOS
			return _hasPhotosPermission();
			#endif
		}

		/// <summary>
		/// Checks if the application has permission to use the camera (ei WebCamTexture)
		/// </summary>
		public static void RequestPhotosPermission(string callbackGameObjectName, string callbackMethodName)
		{
			#if UNITY_IOS && !UNITY_EDITOR
			_requestPhotosPermission(callbackGameObjectName, callbackMethodName);
			#endif
		}

		/// <summary>
		/// Shows the devices image picker so the user can select an image
		/// </summary>
		public static void ShowImagePicker(string callbackGameObjectName, string callbackMethodName, string androidDeviceImagePath)
		{
			#if UNITY_IOS && !UNITY_EDITOR
			_showImagePicker(callbackGameObjectName, callbackMethodName);
			#elif UNITY_ANDROID && !UNITY_EDITOR
			AndroidJavaClass plugin = new AndroidJavaClass ("com.nfagan.imagepicker.ImagePicker");

			if (plugin != null)
			{
				plugin.CallStatic("showImagePicker", callbackGameObjectName, callbackMethodName, androidDeviceImagePath);
			}
			#endif
		}

		/// <summary>
		/// Saves the given image to the devices photo gallary/library
		/// </summary>
		public static void SaveImageToPhotos(string imagePath, string imageName, string imageDescription)
		{
			#if UNITY_IOS && !UNITY_EDITOR
			_saveImageToDevice(imagePath);
			#elif UNITY_ANDROID && !UNITY_EDITOR
			AndroidJavaClass plugin = new AndroidJavaClass ("com.nfagan.utils.Utils");

			if (plugin != null)
			{
				plugin.CallStatic("saveImageToGallery", imagePath, imageName, imageDescription);
			}
			#endif
		}
	}
}
