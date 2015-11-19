# Branch-Xamarin-SDK


## Introduction

The Xamarin SDK is a cross platform SDK you can use to access the Branch APIs from your Xamarin application.  The SDK is a PCL (Portable Class Library) that works with Xamarin Android, Xamarin iOS or Xamarin Forms applications.

## A Word About Async Methods

Most of the REST API calls in the SDK are submitted to a queue and executed in the background.  These requests, and their subsequent callbacks, occur on a background thread.  Due to the nature of how exceptions are handled by C# in background threads, exceptions that occur in a callback that are not caught, will be output to the console and consumed by the processing loop.

Be aware of this when executing UI functions in a callback.  Make sure that the UI functions are being executed inside a BeginInvokeOnMainThread call or it's platform equivalents.

## A Word About Building on Android

There's a problem with the Newtonsoft JSON package that we're using to do JSON processing. (It get’s pulled in as a dependency of the NuGet package.) In a release build, it has a linking problem which leads to an exception we are seeing under certain circumstances. This can be fixed by a change to the options for the Android app. It is only an Android problem.

Basically, right click on the project and select Options. Go to “Android Build” and select the “Linker” tab. Make sure the Release build configuration is selected. In the “Ignore assemblies” box, add “System.Core”. Rebuild the app. It should now run successfully.

## Installation

The Branch Xamarin SDK is now available as a [NuGet package](https://www.nuget.org/packages/Branch-Xamarin-Linking-SDK).  You will need to add the package to your Android, iOS and Forms (if applicable) projects.  

1. Right click on each project and select Add->Add NuGet Package or double click on the Packages folder to bring up the NuGet package dialog in Xamarin Studio.  
2. Find the _Branch Xamarin Linking SDK_ and select it.  This will add the required assemblies to your projects.  You need to do this for each project that will use Branch calls.  This include the Android and iOS projects even if this is a Forms based app since an initialization call needs to be added to each of the platform specific projects.  (See the next section.)

If you would rather build and reference the assemblies directly:

1. Clone this repository to your local machine  
2. Add the BranchXamarinSDK project to your solution and reference it from your Android, iOS and Forms (if applicable) project.  
3. Add the BranchXamarinSDK.Droid project to your solution and reference it from your Android project, if any.
4. Add the BranchXamarinSDK.iOS project and reference it from you iOS project, if any.

### Register your app

You can sign up for your own Branch Key at [https://dashboard.branch.io](https://dashboard.branch.io)

## Configuration (for tracking)

Ideally, you want to use our links any time you have an external link pointing to your app (share, invite, referral, etc) because:

1. Our dashboard can tell you where your installs are coming from
1. Our links are the highest possible converting channel to new downloads and users
1. You can pass that shared data across install to give new users a custom welcome or show them the content they expect to see

Our linking infrastructure will support anything you want to build. If it doesn't, we'll fix it so that it does: just reach out to alex@branch.io with requests.

## Initialize a session on Xamarin

Before starting, it's important to understand that we require a generic Xamarin initialization in addition to the Android and iOS initialization. To make matters worse, it's different depending on whether you're using Xamarin Forms or not. Please click one of the following to be linked to the appropriate init path to follow:

1. [Click here](https://github.com/BranchMetrics/Branch-Xamarin-SDK#xamarin-forms-setup) if you're using Xamarin Forms
2. [Click here](https://github.com/BranchMetrics/Branch-Xamarin-SDK#non-forms-xamarin-setup) if you're *not* using Xamarin Forms

### Xamarin Forms Setup

The SDK needs to be initialized at startup in each platform.  The code below shows how to do the platform specific initialization.  Note that this example shows a Xamarin Forms app.  The same Branch<platform>.Init calls need to be made whether Forms is used or not.


#### Android with Forms

For Android add the call to the onCreate of either your Application class or the first Activity you start. This just creates the singleton object on Android with the appropriate Branch key but does not make any server requests.  Note also the addition of OnNewIntent.  This is needed to get the latest link identifier when the app is opened from the background by following a deep link.

```csharp
public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsApplicationActivity
{
	protected override void OnCreate (Bundle savedInstanceState)
	{
		base.OnCreate (savedInstanceState);

		global::Xamarin.Forms.Forms.Init (this, savedInstanceState);

		BranchAndroid.Init (this, "your branch key here", Intent.Data);

		LoadApplication (new App ());
	}
	
	// Ensure we get the updated link identifier when the app is opened from the
	// background with a new link.
	protected override void OnNewIntent(Intent intent) {
		BranchAndroid.GetInstance().SetNewUrl(intent.Data);
	}
}
```

#### iOS with Forms

For iOS add the code to your AppDelegate. This just creates the singleton object on Android with the appropriate Branch key but does not make any server requests.  Note also the addition of the OpenUrl method.  This is needed to get the latest link identifier when the app is opened from the background by following a deep link.

```csharp
[Register ("AppDelegate")]
public class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
{
	public override bool FinishedLaunching (UIApplication uiApplication, NSDictionary launchOptions)
	{
		global::Xamarin.Forms.Forms.Init ();
		
		NSUrl url = null;
		if ((launchOptions != null) && launchOptions.ContainsKey(UIApplication.LaunchOptionsUrlKey)) {
			url = (NSUrl)launchOptions.ValueForKey (UIApplication.LaunchOptionsUrlKey);
		}

		BranchIOS.Init ("your branch key here", url);

		LoadApplication (new App ());
		return base.FinishedLaunching (uiApplication, launchOptions);
	}
	
	// Ensure we get the updated link identifier when the app is opened from the
	// background with a new link.
	public override bool OpenUrl(UIApplication application,
		NSUrl url,
		string sourceApplication,
		NSObject annotation)
	{
		Console.WriteLine ("New URL: " + url.ToString ());
		BranchIOS.getInstance ().SetNewUrl (url);
		return true;
	}
}
```

Note that in both cases the first argument is the Branch key found in your app from the Branch dashboard (see the screenshot below).  The second argument allows the Branch SDK to recognize if the application was launched from a content URI.

Here is the location of the Branch key

![branch key](docs/images/branch-key.png)

#### Generic init with Forms

The following code will make a request to the Branch servers to initialize a new session, and retrieve any referring link parameters if available. For example, If you created a custom link with your own custom dictionary data, you probably want to know when the user session init finishes, so you can check that data. Think of this callback as your "deep link router". If your app opens with some data, you want to route the user depending on the data you passed in. Otherwise, send them to a generic install flow.

This deep link routing callback is called 100% of the time on init, with your link params or an empty dictionary if none present.

```csharp
public class App : Application, IBranchSessionInterface
{
	protected override void OnResume ()
	{
		Branch branch = Branch.GetInstance ();
		branch.InitSessionAsync (this);
	}
	
	protected override async void OnSleep ()
	{
		Branch branch = Branch.GetInstance ();
		// Await here ensure the thread stays alive long enough to complete the close.
		await branch.CloseSessionAsync ();
	}
	
	#region IBranchSessionInterface implementation
	
	public void InitSessionComplete (Dictionary<string, object> data)
	{
		// Do something with the referring link data...
	}

	public void CloseSessionComplete ()
	{
		// Handle any additional cleanup after the session is closed
	}

	public void SessionRequestError (BranchError error)
	{
		// Handle the error case here
	}

	#endregion
}
```

#### Close session

Required: this call will clear the deep link parameters when the app is closed, so they can be refreshed after a new link is clicked or the app is reopened.

In a Forms App CloseSession is done in the OnSleep method of your App class. See the example above.

### Non-Forms Xamarin Setup

The following code will make a request to the Branch servers to initialize a new session, and retrieve any referring link parameters if available. For example, If you created a custom link with your own custom dictionary data, you probably want to know when the user session init finishes, so you can check that data. Think of this callback as your "deep link router". If your app opens with some data, you want to route the user depending on the data you passed in. Otherwise, send them to a generic install flow.

This deep link routing callback is called 100% of the time on init, with your link params or an empty dictionary if none present.

#### iOS without Forms

The iOS device specific code can register notification listeners to handle the init and close of sessions when the app is sent to the background or resumed.  The BranchIOS.Init call takes an optional third parameter that will enable this automatic close session behavior if the parameter is set to true.  If your iOS app is not a Forms app, use the following device specific init.

```csharp
[Register ("AppDelegate")]
public class AppDelegate : UIApplicationDelegate, IBranchSessionInterface
{
	public override bool FinishedLaunching (UIApplication uiApplication, NSDictionary launchOptions)
	{
		NSUrl url = null;
		if ((launchOptions != null) && launchOptions.ContainsKey(UIApplication.LaunchOptionsUrlKey)) {
			url = (NSUrl)launchOptions.ValueForKey (UIApplication.LaunchOptionsUrlKey);
		}

		BranchIOS.Init ("your branch key here", url, true);
		
		Branch branch = Branch.GetInstance ();
		branch.InitSessionAsync (this);

		// Do your remaining launch stuff here...
	}
	
	// Ensure we get the updated link identifier when the app is opened from the
	// background with a new link.
	public override bool OpenUrl(UIApplication application,
		NSUrl url,
		string sourceApplication,
		NSObject annotation)
	{
		BranchIOS.getInstance ().SetNewUrl (url);
		return true;
	}

	#region IBranchSessionInterface implementation
	
	public void InitSessionComplete (Dictionary<string, object> data)
	{
		// Do something with the referring link data...
	}

	public void CloseSessionComplete ()
	{
		// Handle any additional cleanup after the session is closed
	}

	public void SessionRequestError (BranchError error)
	{
		// Handle the error case here
	}

	#endregion
}
```

#### Android without Forms

For Android add the call to the onCreate of either your Application class or the first Activity you start. This just creates the singleton object on Android with the appropriate Branch key but does not make any server requests

```csharp
public class MainActivity : Activity, IBranchSessionInterface
{
	protected override void OnCreate (Bundle savedInstanceState)
	{
		base.OnCreate (savedInstanceState);

		global::Xamarin.Forms.Forms.Init (this, savedInstanceState);

		BranchAndroid.Init (this, "your branch key here", Intent.Data);

		Branch branch = Branch.GetInstance ();
		branch.InitSessionAsync (this);

		LoadApplication (new App ());
	}

	protected override void OnStop (Bundle savedInstanceState)
	{
		base.OnStop (savedInstanceState);

		Branch branch = Branch.GetInstance ();
		// Await here ensure the thread stays alive long enough to complete the close.
		await branch.CloseSessionAsync ();
	}
	
	// Ensure we get the updated link identifier when the app is opened from the
	// background with a new link.
	protected override void OnNewIntent(Intent intent) {
		BranchAndroid.GetInstance().SetNewUrl(intent.Data);
	}

	#region IBranchSessionInterface implementation
	
	public void InitSessionComplete (Dictionary<string, object> data)
	{
		// Do something with the referring link data...
	}

	public void CloseSessionComplete ()
	{
		// Handle any additional cleanup after the session is closed
	}

	public void SessionRequestError (BranchError error)
	{
		// Handle the error case here
	}

	#endregion
}
```

#### Close session

Required: this call will clear the deep link parameters when the app is closed, so they can be refreshed after a new link is clicked or the app is reopened.

For Android this should be done in OnStop. See the example above.

### Forms and non-Forms Functions

#### Retrieve session (install or open) parameters

These session parameters will be available at any point later on with this command. If no params, the dictionary will be empty. This refreshes with every new session (app installs AND app opens)

```csharp
Branch branch = Branch.GetInstance ();
Dictionary<string, object> sessionParams = branch.GetLatestReferringParams();
```

#### Retrieve install (install only) parameters

If you ever want to access the original session params (the parameters passed in for the first install event only), you can use this line. This is useful if you only want to reward users who newly installed the app from a referral link or something.

```csharp
Branch branch = Branch.GetInstance ();
Dictionary<string, object> installParams = branch.GetFirstReferringParams();
```

### Persistent identities

Often, you might have your own user IDs, or want referral and event data to persist across platforms or uninstall/reinstall. It's helpful if you know your users access your service from different devices. This where we introduce the concept of an 'identity'.

To identify a user, just call:

```csharp
Branch branch = Branch.GetInstance ();
branch.SetIdentityAsync("your user id", this);  // Where this implements IBranchIdentityInterface
```

#### Logout

If you provide a logout function in your app, be sure to clear the user when the logout completes. This will ensure that all the stored parameters get cleared and all events are properly attributed to the right identity.

**Warning** this call will clear the referral credits and attribution on the device.

```csharp
Branch.GetInstance(getApplicationContext()).LogoutAsync(this); // Where this implements IBranchIdentityInterface
```

### Register custom events

```csharp
Branch branch = Branch.GetInstance ();
await branch.UserCompletedActionAsync("your_custom_event");
```

OR if you want to store some state with the event

```csharp
Branch branch = Branch.GetInstance ();
Dictionary<string, object> data = new Dictionary<string, object>();
data.Add("sku", "123456789");
await branch.UserCompletedActionAsync("purchase_event", data);
```

Some example events you might want to track:

```csharp
"complete_purchase"
"wrote_message"
"finished_level_ten"
```

## Generate Tracked, Deep Linking URLs (pass data across install and open)

### Shortened links

There are a bunch of options for creating these links. You can tag them for analytics in the dashboard, or you can even pass data to the new installs or opens that come from the link click. How awesome is that? You need to pass a callback for when you link is prepared (which should return very quickly, ~ 50 ms to process).

For more details on how to create links, see the [Branch link creation guide](https://github.com/BranchMetrics/Branch-Integration-Guides/blob/master/url-creation-guide.md)

```csharp
// associate data with a link
// you can access this data from any instance that installs or opens the app from this link (amazing...)

var data = new Dictionary<string, object>(); 
data.Add("user", "Joe");
data.Add("profile_pic", "https://s3-us-west-1.amazonaws.com/myapp/joes_pic.jpg");
data.Add("description", "Joe likes long walks on the beach...") 

// associate a url with a set of tags, channel, feature, and stage for better analytics.
// tags: null or example set of tags could be "version1", "trial6", etc
// channel: null or examples: "facebook", "twitter", "text_message", etc
// feature: null or examples: Branch.FEATURE_TAG_SHARE, Branch.FEATURE_TAG_REFERRAL, "unlock", etc
// stage: null or examples: "past_customer", "logged_in", "level_6"

List<String> tags = new List<String>();
tags.Add("version1");
tags.Add("trial6");

// Link 'type' can be used for scenarios where you want the link to only deep link the first time. 
// Use _null_, _LINK_TYPE_UNLIMITED_USE_ or _LINK_TYPE_ONE_TIME_USE_

// Link 'alias' can be used to label the endpoint on the link. For example: http://bnc.lt/AUSTIN28. 
// Be careful about aliases: these are immutable objects permanently associated with the data and associated paramters you pass into the link. When you create one in the SDK, it's tied to that user identity as well (automatically specified by the Branch internals). If you want to retrieve the same link again, you'll need to call getShortUrl with all of the same parameters from before.

Branch branch = Branch.GetInstance ();
await branch.GetShortUrlAsync(this, data, "alias","channel","stage", tags, "feature", uriType);

// The error method of the callback will be called if the link generation fails (or if the alias specified is aleady taken.)
```

There are other methods which exclude tags and data if you don't want to pass those. Explore the autocomplete functionality.

**Note**
You can customize the Facebook OG tags of each URL if you want to dynamically share content by using the following _optional keys in the data dictionary_. Please use this [Facebook tool](https://developers.facebook.com/tools/debug/og/object) to debug your OG tags!

| Key | Value
| --- | ---
| "$og_title" | The title you'd like to appear for the link in social media
| "$og_description" | The description you'd like to appear for the link in social media
| "$og_image_url" | The URL for the image you'd like to appear for the link in social media
| "$og_video" | The URL for the video 
| "$og_url" | The URL you'd like to appear
| "$og_app_id" | Your OG app ID. Optional and rarely used.

Also, you do custom redirection by inserting the following _optional keys in the dictionary_:

| Key | Value
| --- | ---
| "$desktop_url" | Where to send the user on a desktop or laptop. By default it is the Branch-hosted text-me service
| "$android_url" | The replacement URL for the Play Store to send the user if they don't have the app. _Only necessary if you want a mobile web splash_
| "$ios_url" | The replacement URL for the App Store to send the user if they don't have the app. _Only necessary if you want a mobile web splash_
| "$ipad_url" | Same as above but for iPad Store
| "$fire_url" | Same as above but for Amazon Fire Store
| "$blackberry_url" | Same as above but for Blackberry Store
| "$windows_phone_url" | Same as above but for Windows Store

You have the ability to control the direct deep linking of each link by inserting the following _optional keys in the dictionary_:

| Key | Value
| --- | ---
| "$deeplink_path" | The value of the deep link path that you'd like us to append to your URI. For example, you could specify "$deeplink_path": "radio/station/456" and we'll open the app with the URI "yourapp://radio/station/456?link_click_id=branch-identifier". This is primarily for supporting legacy deep linking infrastructure. 
| "$always_deeplink" | true or false. (default is not to deep link first) This key can be specified to have our linking service force try to open the app, even if we're not sure the user has the app installed. If the app is not installed, we fall back to the respective app store or $platform_url key. By default, we only open the app if we've seen a user initiate a session in your app from a Branch link (has been cookied and deep linked by Branch)

## Referral system rewarding functionality

In a standard referral system, you have 2 parties: the original user and the invitee. Our system is flexible enough to handle rewards for all users. Here are a couple example scenarios:

1) Reward the original user for taking action (eg. inviting, purchasing, etc)

2) Reward the invitee for installing the app from the original user's referral link

3) Reward the original user when the invitee takes action (eg. give the original user credit when their the invitee buys something)

These reward definitions are created on the dashboard, under the 'Reward Rules' section in the 'Referrals' tab on the dashboard.

Warning: For a referral program, you should not use unique awards for custom events and redeem pre-identify call. This can allow users to cheat the system.
