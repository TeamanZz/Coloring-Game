
SharePlugin setup for Android:

1. Download the AndroidSharePlugin.unitypackage here: https://www.bizzybeegames.com/plugins/AndroidSharePlugin.unitypackage

3. Import the unity package into your project.

2. The plugin will be imported to Assets/Plugins/Android/SharePlugin.
   NOTE: The SharePlugin cannot be moved to another Plugins folder, it must reside in the root
   Plugins folder: Assets/Plugins/Android or the plugin will not work.

3. Open the AndroidManifest.xml located in Assets/Plugins/Android/SharePlugin and
   change the package name on line 4 and line 9 to the package name in your Player Settings.
   NOTE: If you ever change the package name in your Player Settings you must also
   change the package name in the AndroidManifest.xml or the plugin will not work.

4. Download and import the google play services resolver using the link:
   https://github.com/googlesamples/unity-jar-resolver/raw/master/play-services-resolver-1.2.124.0.unitypackage

5. If the play services resolver does not automatically run when you import it into the project
   select the menu item Assets -> Google Play Resolver -> Android Resolver -> Resolve
   
***************
Android Plugins
***************

There are two android jar plugins: UtilsPlugin and SharePlugin

UtilsPlugin:
- Has a single method used to check if the user has granted a given permission.
- It is currently being used in Assets/PictureColoring/Scripts/Shareing/NativePlugin.cs to check
  if the device has permission to save images to the device.

SharePlugin:
- Has methods used to share an image to various platforms (Instagram, Twitter, and Other)
- It is also used in Assets/Scripts/Framework/NativePlugin.cs

***********
iOS Plugins
***********

There is only one iOS plugin located in Assets/PictureColoring/Plugins/iOS. The plugin
contains the same functionallity as the android plugins. No setup is required.