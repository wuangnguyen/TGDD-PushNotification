# Cordova Push Notifications Plugin for Android, iOS, WP

## DESCRIPTION

This plugin is for use with [Cordova](http://incubator.apache.org/cordova/), and allows your application to receive push notifications on Android, iOS and  Windows Phone devices.
* The Android implementation uses [Google's GCM (Google Cloud Messaging) service](http://developer.android.com/guide/google/gcm/index.html).
* The iOS version is based on [Apple APNS Notifications](http://developer.apple.com/library/mac/#documentation/NetworkingInternet/Conceptual/RemoteNotificationsPG/ApplePushService/ApplePushService.html).
* The WP8 implementation is based on [MPNS](http://msdn.microsoft.com/en-us/library/windowsphone/develop/ff402558(v=vs.105).aspx).

**Important** - Push notifications are intended for real devices. They are not tested for WP8 Emulator. The registration process will fail on the iOS simulator. Notifications can be made to work on the Android Emulator, however doing so requires installation of some helper libraries, as outlined [here,](http://www.androidhive.info/2012/10/android-push-notifications-using-google-cloud-messaging-gcm-php-and-mysql/) under the section titled "Installing helper libraries and setting up the Emulator".

### Contents

- [LICENSE](#license)
- [Automatic Installation](#automatic_installation)
- [Plugin API](#plugin_api)
- [Reference](#Reference)



##<a name="license"></a> LICENSE

	The MIT License

	Copyright (c) 2012 Adobe Systems, inc.
	portions Copyright (c) 2012 Olivier Louvignes

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.




##<a name="automatic_installation"></a>Automatic Installation

**Note:** For each service supported - ADM, APNS, GCM or MPNS - you may need to download the SDK and other support files. See the [Manual Installation](#manual_installation) instructions below for more details about each platform.

### Cordova

The plugin can be installed via the Cordova command line interface:

1) Navigate to the root folder for your phonegap project. 
2) Run the command.

```sh
cordova plugin add https://github.com/quang-hcmus-91/TGDD-PushNotification.git
```

### Phonegap

The plugin can be installed using the Phonegap command line interface:

1) Navigate to the root folder for your phonegap project. 
2) Run the command.

```sh
phonegap local plugin add https://github.com/quang-hcmus-91/TGDD-PushNotification.git
```

##<a name="plugin_api"></a> Plugin API

In the plugin `examples` folder you will find a sample implementation showing how to interact with the PushPlugin. Modify it to suit your needs.

#### pushNotification
The plugin instance variable.

```js
var pushNotification;

document.addEventListener("deviceready", function(){
    pushNotification = window.plugins.pushNotification;
    ...
});
```

#### register
To be called as soon as the device becomes ready.

```js
if ( device.platform == 'android' || device.platform == 'Android'){
    pushNotification.register(
    successHandler,
    errorHandler,
    {
        "senderID":"replace_with_sender_id",
        "ecb":"onNotification"
    });
} else if (device.platform == "Win32NT") {
    pushNotification.register(
    wp8SuccessHandlerHandler,
    errorHandler,
    {
	   "channelName": "channelName",
       "uccb": "channelHandler",
       "errcb": "jsonErrorHandler"
    });
} else {
    pushNotification.register(
    tokenHandler,
    errorHandler,
    {
        "badge":"true",
        "sound":"true",
        "alert":"true",
        "ecb":"onNotificationAPN"
    });
}
```

On success, you will get a call to tokenHandler (iOS), onNotification (Android), onNotificationWP8 (WP8) allowing you to obtain the device token or registration ID, or push channel name and Uri respectively. Those values will typically get posted to your intermediary push server so it knows who it can send notifications to.

***Note***

- **Android**: If you have not already done so, you'll need to set up a Google API project, to generate your senderID. [Follow these steps](http://developer.android.com/guide/google/gcm/gs.html) to do so. This is described more fully in the **Testing** section below. In this example, be sure and substitute your own senderID. Get your senderID by signing into to your [google dashboard](https://code.google.com/apis/console/). The senderID is found at *Overview->Dashboard->Project Number*.

#### successHandler
Called when a plugin method returns without error

```js
// result contains any message sent from the plugin call
function successHandler (result) {
	alert('result = ' + result);
}
```

#### errorHandler
Called when the plugin returns an error

```js
// result contains any error description text returned from the plugin call
function errorHandler (error) {
	alert('error = ' + error);
}
```

#### ecb (Android and iOS)
Event callback that gets called when your device receives a notification

```js
// iOS
function onNotificationAPN (event) {
	if ( event.alert )
	{
		navigator.notification.alert(event.alert);
	}

	if ( event.sound )
	{
		var snd = new Media(event.sound);
		snd.play();
	}

	if ( event.badge )
	{
		pushNotification.setApplicationIconBadgeNumber(successHandler, errorHandler, event.badge);
	}
}
```

```js
// Android
function onNotification(e) {
	switch( e.event )
	{
	case 'registered':
		if ( e.regid.length > 0 )
		{
			// Your GCM push server needs to know the regID before it can push to this device
			// here is where you might want to send it the regID for later use.
			console.log("regID = " + e.regid);
		}
	break;

	case 'message':
		// if this flag is set, this notification happened while we were in the foreground.
		// you might want to play a sound to get the user's attention, throw up a dialog, etc.
		if ( e.foreground )
		{
			//get data from server here
		}
		else
		{  // otherwise we were launched because the user touched a notification in the notification tray.
			if ( e.coldstart )
			{
				//ColdStart Notification
			}
			else
			{
				//Background Notification
			}
		}
		alert(e.payload.message);
		alert(e.payload.msgcnt );
	break;

	case 'error':
		alert( e.msg );
	break;

	default:
		alert("Unknown Event");
	break;
  }
}
```

#### senderID (Android only)
This is the Google project ID you need to obtain by [registering your application](http://developer.android.com/guide/google/gcm/gs.html) for GCM


#### tokenHandler (iOS only)
Called when the device has registered with a unique device token.

```js
function tokenHandler (result) {
    // Your iOS push server needs to know the token before it can push to this device
    // here is where you might want to send it the token for later use.
    alert('device token = ' + result);
}
```

#### setApplicationIconBadgeNumber (iOS only)
Set the badge count visible when the app is not running

```js
pushNotification.setApplicationIconBadgeNumber(successCallback, errorCallback, badgeCount);
```

The `badgeCount` is an integer indicating what number should show up in the badge. Passing 0 will clear the badge.
#### setBadge for Android
```js
tgdd.notification.bage.setBadge("badge", successCallback, errorCallback)
```
The `badge` is an string indicating what number should show up in the badge. Passing 0 will clear the badge.

#### unregister (Android and iOS)
You will typically call this when your app is exiting, to cleanup any used resources. Its not strictly necessary to call it, and indeed it may be desireable to NOT call it if you are debugging your intermediarry push server. When you call unregister(), the current token for a particular device will get invalidated, and the next call to register() will return a new token. If you do NOT call unregister(), the last token will remain in effect until it is invalidated for some reason at the GCM/ADM side. Since such invalidations are beyond your control, its recommended that, in a production environment, that you have a matching unregister() call, for every call to register(), and that your server updates the devices' records each time.

```js
pushNotification.unregister(successHandler, errorHandler, options);
```


### WP8

#### register (WP8 Only)

```js

if(device.platform == "Win32NT"){
    pushNotification.register(
        wp8SuccessHandlerHandler,
        errorHandler,
        {
            "channelName": channelName,
            "uccb": "channelHandler",
            "errcb": "jsonErrorHandler"
        });
}

```

#### channelHandler (WP8 only)
Called after a push notification channel is opened and push notification URI is returned. [The application is now set to receive notifications.](http://msdn.microsoft.com/en-us/library/windowsphone/develop/hh202940(v=vs.105).aspx)

#### uccb (WP8 only)
Event callback that gets called when the channel you have opened gets its Uri updated. This function is needed in case the MPNS updates the opened channel Uri. This function will take care of showing updated Uri.


#### errcb (WP8 only)
Event callback that gets called when server error occurs when receiving notification from the MPNS server (for example invalid format of the notification).

```js
function jsonErrorHandler(error) {
		aler(error.code + " " + error.message);
	}
```

To control the launch page when the user taps on your toast notification when the app is not running, add the following code to your Mainpage.xaml.cs
```cs
protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
{
    base.OnNavigatedTo(e);
    try
    {
        if (this.NavigationContext.QueryString["NavigatedFrom"] == "toast") // this is set on the server
        {
            this.PGView.StartPageUri = new Uri("//www/index.html#notification-page", UriKind.Relative);
        }
    }
    catch (KeyNotFoundException)
    {
    }
}
```
To test the tile notification, you will need to add tile images like the [MSDN Tile Sample](http://msdn.microsoft.com/en-us/library/windowsphone/develop/hh202970(v=vs.105).aspx#BKMK_CreatingaPushClienttoReceiveTileNotifications)

#### unregister (WP8 Only)

When using the plugin for wp8 you will need to unregister the push channel you have register in case you would want to open another one. You need to know the name of the channel you have opened in order to close it. Please keep in mind that one application can have only one opened channel at time and in order to open another you will have to close any already opened channel.

```cs
function unregister() {
	var channelName = $("#channel-btn").val();
	pushNotification.unregister(
		successHandler, errorHandler,
			{
				"channelName": channelName
			});
}
```

##<a name="reference"><a/> Reference

[https://github.com/phonegap-build/PushPlugin](https://github.com/phonegap-build/PushPlugin)

[https://github.com/leolin310148/ShortcutBadger](https://github.com/leolin310148/ShortcutBadger)