using Foundation;
using UIKit;
using System;
using System.Collections.Generic;
using Microsoft.Maui.Authentication;
using Microsoft.Maui.Storage;
using System.Threading.Tasks;
using System.Reflection;
// Note: Google.SignIn namespace is accessed via reflection to allow compilation
// even when the Xamarin.Google.iOS.SignIn package is not available

namespace PhotoJobApp;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	private const string AppCustomScheme = "com.pinebelttrophy.photojobapp2025";
	private static readonly Lazy<HashSet<string>> GoogleOAuthSchemes = new(LoadGoogleOAuthSchemes);

	protected override MauiApp CreateMauiApp()
	{
		// Initialize persistent logging immediately
		// PersistentLogger.Log("AppDelegate", "CreateMauiApp called", "App is starting");
		System.Diagnostics.Debug.WriteLine("AppDelegate: CreateMauiApp called");
		return MauiProgram.CreateMauiApp();
	}
	
	// Handle URL callbacks when app is launched or returns from background (e.g., from 2FA app)
	// This is critical for OAuth flows that require switching to other apps
	public override bool OpenUrl(UIApplication application, NSUrl url, NSDictionary options)
	{
		// CRITICAL DEBUGGING: Log EVERYTHING about this call
		// Use PersistentLogger so logs survive app termination
		var urlDetails = $"URL: {url?.AbsoluteString ?? "NULL"}\n" +
		                $"Scheme: {url?.Scheme ?? "NULL"}\n" +
		                $"Host: {url?.Host ?? "NULL"}\n" +
		                $"Path: {url?.Path ?? "NULL"}\n" +
		                $"Query: {url?.Query ?? "NULL"}\n" +
		                $"Fragment: {url?.Fragment ?? "NULL"}\n" +
		                $"Options: {(options != null ? options.Count.ToString() : "NULL")}";
		
		// PersistentLogger.LogCritical("AppDelegate.OpenUrl", "🔵 CALLED!", urlDetails);
		
		// Also log to standard outputs (for Visual Studio while connected)
		System.Diagnostics.Debug.WriteLine($"═══════════════════════════════════════════════════════");
		System.Diagnostics.Debug.WriteLine($"🔵 AppDelegate.OpenUrl CALLED!");
		System.Diagnostics.Debug.WriteLine($"   {urlDetails.Replace("\n", "\n   ")}");
		System.Diagnostics.Debug.WriteLine($"═══════════════════════════════════════════════════════");
		Console.WriteLine($"═══════════════════════════════════════════════════════");
		Console.WriteLine($"🔵 AppDelegate.OpenUrl CALLED!");
		Console.WriteLine($"   {urlDetails.Replace("\n", "\n   ")}");
		Console.WriteLine($"═══════════════════════════════════════════════════════");
		
		if (url == null)
		{
			return base.OpenUrl(application, url, options);
		}
		
		// Handle Google Sign-In SDK URLs first (if SDK is available)
		try
		{
			// Use reflection to check if Google Sign-In SDK is available
			var gidSignInType = Type.GetType("Google.SignIn.GIDSignIn, Xamarin.Google.iOS.SignIn");
			if (gidSignInType != null)
			{
				var sharedInstanceProperty = gidSignInType.GetProperty("SharedInstance", BindingFlags.Public | BindingFlags.Static);
				if (sharedInstanceProperty != null)
				{
					var sharedInstance = sharedInstanceProperty.GetValue(null);
					var handleUrlMethod = gidSignInType.GetMethod("HandleUrl", new[] { typeof(NSUrl) });
					if (handleUrlMethod != null)
					{
						var handled = (bool)(handleUrlMethod.Invoke(sharedInstance, new object[] { url }) ?? false);
						if (handled)
						{
							System.Diagnostics.Debug.WriteLine("AppDelegate: Google Sign-In handled the URL");
							Console.WriteLine("AppDelegate: Google Sign-In handled the URL");
							return true;
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"AppDelegate: Error handling Google Sign-In URL: {ex.Message}");
			Console.WriteLine($"AppDelegate: Error handling Google Sign-In URL: {ex.Message}");
		}
		
		// Handle our custom URL schemes (OAuth callbacks)
		var scheme = url.Scheme ?? string.Empty;
		var isAppScheme = string.Equals(scheme, AppCustomScheme, StringComparison.OrdinalIgnoreCase);
		var isGoogleScheme = GoogleOAuthSchemes.Value.Contains(scheme);
		
		if (isAppScheme || isGoogleScheme)
		{
			var schemeType = isGoogleScheme ? $"Google OAuth scheme ({scheme})" : "App custom scheme";
			System.Diagnostics.Debug.WriteLine($"Handling OAuth callback URL ({schemeType}): {url.AbsoluteString}");
			Console.WriteLine($"Handling OAuth callback URL ({schemeType}): {url.AbsoluteString}");
			
			// PersistentLogger.LogCritical("AppDelegate.OpenUrl", $"Handling {schemeType} callback", url.AbsoluteString);
			
			// Always store the URL first (in case WebAuthenticator fails or app was terminated)
			// Use NSUserDefaults directly for most reliable storage (works even if Preferences isn't ready)
			// NSUserDefaults is the iOS native storage API and is always available
			try
			{
				var userDefaults = Foundation.NSUserDefaults.StandardUserDefaults;
				
				// Store the callback URL
				userDefaults.SetString(url.AbsoluteString, "PendingOAuthCallback");
				userDefaults.SetBool(true, "GoogleSignInInProgress");
				
				// CRITICAL: Force synchronization to ensure data is written to disk immediately
				// This ensures the data persists even if the app is terminated
				userDefaults.Synchronize();
				
				// Verify it was stored
				var verifyCallback = userDefaults.StringForKey("PendingOAuthCallback");
				var verifyInProgress = userDefaults.BoolForKey("GoogleSignInInProgress");
				
				var verificationStatus = $"Callback: {(string.IsNullOrEmpty(verifyCallback) ? "FAILED" : "SUCCESS")}, InProgress: {verifyInProgress}";
				
				// PersistentLogger.LogCritical("AppDelegate.OpenUrl", "✓✓✓ STORED OAuth callback in NSUserDefaults", 
				// 	$"URL: {url.AbsoluteString}\nVerification: {verificationStatus}");
				
				System.Diagnostics.Debug.WriteLine($"✓✓✓ STORED OAuth callback in NSUserDefaults (will persist even if app terminates):");
				System.Diagnostics.Debug.WriteLine($"  URL: {url.AbsoluteString}");
				System.Diagnostics.Debug.WriteLine($"  Verification - Callback: {(string.IsNullOrEmpty(verifyCallback) ? "FAILED" : "SUCCESS")}");
				System.Diagnostics.Debug.WriteLine($"  Verification - InProgress: {verifyInProgress}");
				Console.WriteLine($"✓✓✓ STORED OAuth callback in NSUserDefaults (will persist even if app terminates):");
				Console.WriteLine($"  URL: {url.AbsoluteString}");
				Console.WriteLine($"  Verification - Callback: {(string.IsNullOrEmpty(verifyCallback) ? "FAILED" : "SUCCESS")}");
				Console.WriteLine($"  Verification - InProgress: {verifyInProgress}");
				
				// Post notification immediately so App can process it if already running
				NSNotificationCenter.DefaultCenter.PostNotificationName("PendingOAuthCallbackFound", null);
				System.Diagnostics.Debug.WriteLine("Posted PendingOAuthCallbackFound notification from OpenUrl");
				Console.WriteLine("Posted PendingOAuthCallbackFound notification from OpenUrl");
				
				// Also try to store in Preferences if MAUI is ready (for cross-platform consistency)
				try
				{
					Microsoft.Maui.Storage.Preferences.Set("PendingOAuthCallback", url.AbsoluteString);
					Microsoft.Maui.Storage.Preferences.Set("GoogleSignInInProgress", true);
					System.Diagnostics.Debug.WriteLine("Also stored in MAUI Preferences");
					Console.WriteLine("Also stored in MAUI Preferences");
				}
				catch (Exception prefEx)
				{
					// Preferences might not be ready yet - that's OK, NSUserDefaults is the primary storage
					System.Diagnostics.Debug.WriteLine($"Preferences not ready (this is OK): {prefEx.Message}");
					Console.WriteLine($"Preferences not ready (this is OK): {prefEx.Message}");
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"ERROR storing OAuth callback: {ex.Message}");
				System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
				Console.WriteLine($"ERROR storing OAuth callback: {ex.Message}");
				Console.WriteLine($"Stack trace: {ex.StackTrace}");
			}
			
			// Try WebAuthenticator (works if app wasn't terminated)
			try
			{
				var handled = WebAuthenticator.Default.OpenUrl(url);
				if (handled)
				{
					System.Diagnostics.Debug.WriteLine("WebAuthenticator handled the URL callback");
					Console.WriteLine("WebAuthenticator handled the URL callback");
					return true;
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error forwarding URL to WebAuthenticator: {ex.Message}");
				Console.WriteLine($"Error forwarding URL to WebAuthenticator: {ex.Message}");
			}
			
			// If WebAuthenticator didn't handle it, the URL is already stored above
			System.Diagnostics.Debug.WriteLine("WebAuthenticator did not handle URL - URL stored for processing on app startup");
			Console.WriteLine("WebAuthenticator did not handle URL - URL stored for processing on app startup");
			
			return true;
		}
		
		return base.OpenUrl(application, url, options);
	}
	
	// Handle URL callbacks via Universal Links or when app is already running
	public override bool ContinueUserActivity(UIApplication application, NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler)
	{
		System.Diagnostics.Debug.WriteLine($"AppDelegate.ContinueUserActivity called");
		Console.WriteLine($"AppDelegate.ContinueUserActivity called");
		
		if (userActivity?.ActivityType == NSUserActivityType.BrowsingWeb)
		{
			var url = userActivity.WebPageUrl;
			if (url != null)
			{
				System.Diagnostics.Debug.WriteLine($"ContinueUserActivity URL: {url.AbsoluteString}");
				Console.WriteLine($"ContinueUserActivity URL: {url.AbsoluteString}");
				return OpenUrl(application, url, null);
			}
		}
		
		return base.ContinueUserActivity(application, userActivity, completionHandler);
	}
	
	// Handle app launch with URL (when app is launched via URL scheme)
	public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
	{
		// PersistentLogger.Log("AppDelegate", "FinishedLaunching called", $"LaunchOptions: {(launchOptions != null ? launchOptions.Count.ToString() : "null")}");
		System.Diagnostics.Debug.WriteLine("AppDelegate.FinishedLaunching called");
		Console.WriteLine("AppDelegate.FinishedLaunching called");
		
		// Log all launch options keys for debugging
		if (launchOptions != null)
		{
			System.Diagnostics.Debug.WriteLine($"LaunchOptions count: {launchOptions.Count}");
			Console.WriteLine($"LaunchOptions count: {launchOptions.Count}");
			
			foreach (var key in launchOptions.Keys)
			{
				var value = launchOptions[key];
				System.Diagnostics.Debug.WriteLine($"LaunchOption key: {key}, value: {value}");
				Console.WriteLine($"LaunchOption key: {key}, value: {value}");
			}
			
			// Check if app was launched via URL
			var urlKey = UIApplication.LaunchOptionsUrlKey;
			System.Diagnostics.Debug.WriteLine($"Checking for URL key: {urlKey}");
			Console.WriteLine($"Checking for URL key: {urlKey}");
			
			if (launchOptions.ContainsKey(urlKey))
			{
				var url = launchOptions[urlKey] as NSUrl;
				if (url != null)
				{
					System.Diagnostics.Debug.WriteLine($"✓✓✓ App launched with URL: {url.AbsoluteString}");
					Console.WriteLine($"✓✓✓ App launched with URL: {url.AbsoluteString}");
					
					// Store the URL immediately (before .NET runtime is ready)
					try
					{
						var userDefaults = Foundation.NSUserDefaults.StandardUserDefaults;
						userDefaults.SetString(url.AbsoluteString, "PendingOAuthCallback");
						userDefaults.SetBool(true, "GoogleSignInInProgress");
						userDefaults.Synchronize();
						
						// Verify storage
						var verifyCallback = userDefaults.StringForKey("PendingOAuthCallback");
						var verifyInProgress = userDefaults.BoolForKey("GoogleSignInInProgress");
						
						System.Diagnostics.Debug.WriteLine($"✓✓✓ Stored launch URL in NSUserDefaults:");
						System.Diagnostics.Debug.WriteLine($"  URL: {url.AbsoluteString}");
						System.Diagnostics.Debug.WriteLine($"  Verification - Callback: {(string.IsNullOrEmpty(verifyCallback) ? "FAILED" : "SUCCESS")}");
						System.Diagnostics.Debug.WriteLine($"  Verification - InProgress: {verifyInProgress}");
						Console.WriteLine($"✓✓✓ Stored launch URL in NSUserDefaults:");
						Console.WriteLine($"  URL: {url.AbsoluteString}");
						Console.WriteLine($"  Verification - Callback: {(string.IsNullOrEmpty(verifyCallback) ? "FAILED" : "SUCCESS")}");
						Console.WriteLine($"  Verification - InProgress: {verifyInProgress}");
						
						// Also call OpenUrl to handle it properly (after MAUI initializes)
						// This ensures WebAuthenticator gets a chance to handle it
						Task.Run(async () =>
						{
							// Wait for MAUI to initialize
							await Task.Delay(1000);
							try
							{
								System.Diagnostics.Debug.WriteLine("Calling OpenUrl from FinishedLaunching...");
								Console.WriteLine("Calling OpenUrl from FinishedLaunching...");
								OpenUrl(application, url, null);
							}
							catch (Exception ex)
							{
								System.Diagnostics.Debug.WriteLine($"Error calling OpenUrl from FinishedLaunching: {ex.Message}");
								Console.WriteLine($"Error calling OpenUrl from FinishedLaunching: {ex.Message}");
							}
						});
					}
					catch (Exception ex)
					{
						System.Diagnostics.Debug.WriteLine($"ERROR storing launch URL: {ex.Message}");
						System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
						Console.WriteLine($"ERROR storing launch URL: {ex.Message}");
						Console.WriteLine($"Stack trace: {ex.StackTrace}");
					}
				}
				else
				{
					System.Diagnostics.Debug.WriteLine("URL key found but URL is null");
					Console.WriteLine("URL key found but URL is null");
				}
			}
			else
			{
				System.Diagnostics.Debug.WriteLine("No URL key found in launchOptions");
				Console.WriteLine("No URL key found in launchOptions");
			}
		}
		else
		{
			System.Diagnostics.Debug.WriteLine("launchOptions is null");
			Console.WriteLine("launchOptions is null");
		}
		
		return base.FinishedLaunching(application, launchOptions);
	}
	
	// Called when app becomes active (after launch or returning from background)
	// This is called AFTER FinishedLaunching, so OpenUrl might be called here
	public override void OnActivated(UIApplication application)
	{
		// PersistentLogger.Log("AppDelegate", "OnActivated called", "App became active - checking for pending callbacks");
		System.Diagnostics.Debug.WriteLine("AppDelegate.OnActivated called");
		Console.WriteLine("AppDelegate.OnActivated called");
		
		// Check if there's a pending callback in UserDefaults (might have been stored by OpenUrl)
		// Use the most reliable storage method - NSUserDefaults with explicit synchronization
		try
		{
			var userDefaults = Foundation.NSUserDefaults.StandardUserDefaults;
			
			// Force synchronization to ensure we have the latest data
			userDefaults.Synchronize();
			
			var pendingCallback = userDefaults.StringForKey("PendingOAuthCallback");
			var signInInProgress = userDefaults.BoolForKey("GoogleSignInInProgress");
			
			var callbackPreview = string.IsNullOrEmpty(pendingCallback) 
				? "null/empty" 
				: pendingCallback.Substring(0, Math.Min(100, pendingCallback.Length)) + "...";
			
			// PersistentLogger.Log("AppDelegate.OnActivated", "Checking UserDefaults", 
			// 	$"PendingOAuthCallback: {callbackPreview}\nGoogleSignInInProgress: {signInInProgress}");
			
			System.Diagnostics.Debug.WriteLine($"OnActivated - Checking UserDefaults:");
			System.Diagnostics.Debug.WriteLine($"  PendingOAuthCallback: {(string.IsNullOrEmpty(pendingCallback) ? "null/empty" : pendingCallback.Substring(0, Math.Min(50, pendingCallback.Length)) + "...")}");
			System.Diagnostics.Debug.WriteLine($"  GoogleSignInInProgress: {signInInProgress}");
			Console.WriteLine($"OnActivated - Checking UserDefaults:");
			Console.WriteLine($"  PendingOAuthCallback: {(string.IsNullOrEmpty(pendingCallback) ? "null/empty" : pendingCallback.Substring(0, Math.Min(50, pendingCallback.Length)) + "...")}");
			Console.WriteLine($"  GoogleSignInInProgress: {signInInProgress}");
			
			if (!string.IsNullOrEmpty(pendingCallback) && signInInProgress)
			{
				// PersistentLogger.LogCritical("AppDelegate.OnActivated", "✓✓✓ Found pending callback!", 
				// 	$"Callback URL: {pendingCallback}");
				
				System.Diagnostics.Debug.WriteLine($"✓✓✓ Found pending callback in OnActivated: {pendingCallback.Substring(0, Math.Min(100, pendingCallback.Length))}...");
				Console.WriteLine($"✓✓✓ Found pending callback in OnActivated: {pendingCallback.Substring(0, Math.Min(100, pendingCallback.Length))}...");
				
				// Post a notification that a callback was found (App.xaml.cs can listen for this)
				// This ensures the callback is processed even if timing is off
				NSNotificationCenter.DefaultCenter.PostNotificationName("PendingOAuthCallbackFound", null);
				System.Diagnostics.Debug.WriteLine("Posted PendingOAuthCallbackFound notification");
				Console.WriteLine("Posted PendingOAuthCallbackFound notification");
				
				// Also try to sync to Preferences if MAUI is ready
				try
				{
					// Use a small delay to ensure MAUI Preferences is initialized
					Task.Run(async () =>
					{
						await Task.Delay(100);
						try
						{
							Microsoft.Maui.Storage.Preferences.Set("PendingOAuthCallback", pendingCallback);
							Microsoft.Maui.Storage.Preferences.Set("GoogleSignInInProgress", signInInProgress);
							System.Diagnostics.Debug.WriteLine("Synced callback from UserDefaults to Preferences");
							Console.WriteLine("Synced callback from UserDefaults to Preferences");
						}
						catch (Exception prefEx)
						{
							System.Diagnostics.Debug.WriteLine($"Preferences not ready yet: {prefEx.Message}");
							Console.WriteLine($"Preferences not ready yet: {prefEx.Message}");
						}
					});
				}
				catch (Exception syncEx)
				{
					System.Diagnostics.Debug.WriteLine($"Error syncing to Preferences: {syncEx.Message}");
					Console.WriteLine($"Error syncing to Preferences: {syncEx.Message}");
				}
			}
			else
			{
				System.Diagnostics.Debug.WriteLine("No pending callback found in OnActivated");
				Console.WriteLine("No pending callback found in OnActivated");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error checking UserDefaults in OnActivated: {ex.Message}");
			Console.WriteLine($"Error checking UserDefaults in OnActivated: {ex.Message}");
			System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
			Console.WriteLine($"Stack trace: {ex.StackTrace}");
		}
		
		base.OnActivated(application);
	}

	private static HashSet<string> LoadGoogleOAuthSchemes()
	{
		var schemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			// Fallback known client IDs in case GoogleService-Info.plist is outdated
			"com.googleusercontent.apps.1021759232753-mm6ns7f4r20aohg8ric4med68kqtul9e",
			"com.googleusercontent.apps.1021759232753-faphbn9aupbnev5uh958nhfhik05q8vh"
		};

		try
		{
			var plistPath = NSBundle.MainBundle.PathForResource("GoogleService-Info", "plist");
			if (!string.IsNullOrEmpty(plistPath) && NSFileManager.DefaultManager.FileExists(plistPath))
			{
				var plist = NSDictionary.FromFile(plistPath);
				if (plist != null)
				{
					if (plist.ContainsKey(new NSString("REVERSED_CLIENT_ID")))
					{
						var reversed = plist["REVERSED_CLIENT_ID"]?.ToString();
						if (!string.IsNullOrEmpty(reversed))
						{
							schemes.Add(reversed);
						}
					}

					if (plist.ContainsKey(new NSString("CLIENT_ID")))
					{
						var clientId = plist["CLIENT_ID"]?.ToString();
						var derived = DeriveReversedClientId(clientId);
						if (!string.IsNullOrEmpty(derived))
						{
							schemes.Add(derived);
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"AppDelegate: Failed to read GoogleService-Info.plist for OAuth schemes: {ex.Message}");
			Console.WriteLine($"AppDelegate: Failed to read GoogleService-Info.plist for OAuth schemes: {ex.Message}");
		}

		System.Diagnostics.Debug.WriteLine($"AppDelegate: Google OAuth schemes resolved to: {string.Join(", ", schemes)}");
		Console.WriteLine($"AppDelegate: Google OAuth schemes resolved to: {string.Join(", ", schemes)}");
		return schemes;
	}

	private static string? DeriveReversedClientId(string? clientId)
	{
		if (string.IsNullOrWhiteSpace(clientId))
		{
			return null;
		}

		const string suffix = ".apps.googleusercontent.com";
		if (clientId.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
		{
			var trimmed = clientId[..^suffix.Length];
			return $"com.googleusercontent.apps.{trimmed}";
		}

		return null;
	}
}
