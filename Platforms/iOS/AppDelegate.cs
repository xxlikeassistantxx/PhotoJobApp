using Foundation;
using UIKit;
using System;
using System.Collections.Generic;
using Microsoft.Maui.Authentication;
using Microsoft.Maui.ApplicationModel; // Required for Platform.OpenUrl
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
	
	/// <summary>
	/// Handles URL callbacks when app is launched or returns from background (e.g., from 2FA app).
	/// This is critical for OAuth flows that require switching to other apps.
	/// 
	/// Implementation follows the recommended MAUI pattern:
	/// 1. Try Platform.OpenUrl FIRST (MAUI's recommended way - routes to WebAuthenticator automatically)
	/// 2. If Platform.OpenUrl handles it, return true immediately
	/// 3. Otherwise, store callback URL in NSUserDefaults (for persistence if app was terminated)
	/// 4. Call base.OpenUrl as fallback
	/// 
	/// This simplified approach ensures Platform.OpenUrl gets the first chance to handle OAuth callbacks,
	/// which is essential for WebAuthenticator to receive the login token.
	/// </summary>
	[Export("application:openURL:options:")]
	public override bool OpenUrl(UIApplication application, NSUrl url, NSDictionary options)
	{
		// Log that OpenUrl was called (critical for debugging)
		System.Diagnostics.Debug.WriteLine($"═══════════════════════════════════════════════════════");
		System.Diagnostics.Debug.WriteLine($"🔵 AppDelegate.OpenUrl CALLED!");
		System.Diagnostics.Debug.WriteLine($"   URL: {url?.AbsoluteString ?? "NULL"}");
		System.Diagnostics.Debug.WriteLine($"   Scheme: {url?.Scheme ?? "NULL"}");
		System.Diagnostics.Debug.WriteLine($"═══════════════════════════════════════════════════════");
		Console.WriteLine($"═══════════════════════════════════════════════════════");
		Console.WriteLine($"🔵 AppDelegate.OpenUrl CALLED!");
		Console.WriteLine($"   URL: {url?.AbsoluteString ?? "NULL"}");
		Console.WriteLine($"   Scheme: {url?.Scheme ?? "NULL"}");
		Console.WriteLine($"═══════════════════════════════════════════════════════");
		
		if (url == null)
		{
			return base.OpenUrl(application, url, options);
		}
		
		// STEP 1: Try WebAuthenticator.Default.OpenUrl FIRST (most direct approach)
		// This is the most reliable way to handle OAuth callbacks for WebAuthenticator
		var scheme = url.Scheme ?? string.Empty;
		var isAppScheme = string.Equals(scheme, AppCustomScheme, StringComparison.OrdinalIgnoreCase);
		var isGoogleScheme = GoogleOAuthSchemes.Value.Contains(scheme);
		
		// Log scheme matching details for debugging
		System.Diagnostics.Debug.WriteLine($"Scheme matching:");
		System.Diagnostics.Debug.WriteLine($"  URL Scheme: '{scheme}'");
		System.Diagnostics.Debug.WriteLine($"  App Custom Scheme: '{AppCustomScheme}'");
		System.Diagnostics.Debug.WriteLine($"  Is App Scheme: {isAppScheme}");
		System.Diagnostics.Debug.WriteLine($"  Google OAuth Schemes: {string.Join(", ", GoogleOAuthSchemes.Value)}");
		System.Diagnostics.Debug.WriteLine($"  Is Google Scheme: {isGoogleScheme}");
		Console.WriteLine($"Scheme matching:");
		Console.WriteLine($"  URL Scheme: '{scheme}'");
		Console.WriteLine($"  App Custom Scheme: '{AppCustomScheme}'");
		Console.WriteLine($"  Is App Scheme: {isAppScheme}");
		Console.WriteLine($"  Google OAuth Schemes: {string.Join(", ", GoogleOAuthSchemes.Value)}");
		Console.WriteLine($"  Is Google Scheme: {isGoogleScheme}");
		
		if (isAppScheme || isGoogleScheme)
		{
			var schemeType = isGoogleScheme ? $"Google OAuth scheme ({scheme})" : "App custom scheme";
			System.Diagnostics.Debug.WriteLine($"Detected {schemeType} - attempting to handle OAuth callback");
			Console.WriteLine($"Detected {schemeType} - attempting to handle OAuth callback");
			
			// Try WebAuthenticator.Default.OpenUrl first (most direct for OAuth)
			try
			{
				var handled = WebAuthenticator.Default.OpenUrl(url);
				if (handled)
				{
					System.Diagnostics.Debug.WriteLine("✓ WebAuthenticator.Default.OpenUrl handled the URL callback - OAuth flow should complete");
					Console.WriteLine("✓ WebAuthenticator.Default.OpenUrl handled the URL callback - OAuth flow should complete");
					return true;
				}
				else
				{
					System.Diagnostics.Debug.WriteLine("⚠ WebAuthenticator.Default.OpenUrl returned false - trying Platform.OpenUrl");
					Console.WriteLine("⚠ WebAuthenticator.Default.OpenUrl returned false - trying Platform.OpenUrl");
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error in WebAuthenticator.Default.OpenUrl: {ex.Message}");
				Console.WriteLine($"Error in WebAuthenticator.Default.OpenUrl: {ex.Message}");
			}
			
			// STEP 2: Try Platform.OpenUrl as fallback (MAUI's general URL handler)
			try
			{
				var handled = Platform.OpenUrl(application, url, options);
				if (handled)
				{
					System.Diagnostics.Debug.WriteLine("✓ Platform.OpenUrl handled the URL callback - OAuth flow should complete");
					Console.WriteLine("✓ Platform.OpenUrl handled the URL callback - OAuth flow should complete");
					return true;
				}
				else
				{
					System.Diagnostics.Debug.WriteLine("⚠ Platform.OpenUrl returned false - storing for persistence");
					Console.WriteLine("⚠ Platform.OpenUrl returned false - storing for persistence");
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error in Platform.OpenUrl: {ex.Message}");
				Console.WriteLine($"Error in Platform.OpenUrl: {ex.Message}");
			}
		
			// STEP 3: If neither handled it, store the URL for persistence
			// This ensures the callback is processed even if the app was terminated
			System.Diagnostics.Debug.WriteLine($"Neither WebAuthenticator nor Platform.OpenUrl handled {schemeType} - storing for persistence");
			Console.WriteLine($"Neither WebAuthenticator nor Platform.OpenUrl handled {schemeType} - storing for persistence");
			
			// Store in NSUserDefaults for persistence (works even if app was terminated)
			try
			{
				var userDefaults = Foundation.NSUserDefaults.StandardUserDefaults;
				userDefaults.SetString(url.AbsoluteString, "PendingOAuthCallback");
				userDefaults.SetBool(true, "GoogleSignInInProgress");
				userDefaults.Synchronize();
				
				System.Diagnostics.Debug.WriteLine($"✓ Stored OAuth callback in NSUserDefaults: {url.AbsoluteString.Substring(0, Math.Min(100, url.AbsoluteString.Length))}...");
				Console.WriteLine($"✓ Stored OAuth callback in NSUserDefaults: {url.AbsoluteString.Substring(0, Math.Min(100, url.AbsoluteString.Length))}...");
				
				// Post notification so app can process it
				NSNotificationCenter.DefaultCenter.PostNotificationName("PendingOAuthCallbackFound", null);
				
				// Return true to indicate we handled it (even if WebAuthenticator didn't process it yet)
				return true;
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error storing OAuth callback: {ex.Message}");
				Console.WriteLine($"Error storing OAuth callback: {ex.Message}");
			}
		}
		
		// STEP 3: Call base implementation as fallback
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
			}
			else if (signInInProgress && string.IsNullOrEmpty(pendingCallback))
			{
				// Sign-in is in progress but no callback was received
				// This means the app was terminated during OAuth flow and callback was lost
				// Check if we have a stored OAuth URL to resume
				var storedAuthUrl = userDefaults.StringForKey("GoogleOAuthAuthUrl");
				var storedTimestamp = userDefaults.DoubleForKey("GoogleOAuthTimestamp");
				
				if (!string.IsNullOrEmpty(storedAuthUrl) && storedTimestamp > 0)
				{
					var storedTime = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(Math.Round(storedTimestamp)));
					var elapsed = DateTimeOffset.UtcNow - storedTime;
					
					if (elapsed < TimeSpan.FromMinutes(5))
					{
						System.Diagnostics.Debug.WriteLine($"⚠ App was terminated during OAuth flow. Stored OAuth URL is {elapsed.TotalSeconds:F1}s old - can be resumed.");
						Console.WriteLine($"⚠ App was terminated during OAuth flow. Stored OAuth URL is {elapsed.TotalSeconds:F1}s old - can be resumed.");
						System.Diagnostics.Debug.WriteLine("Posting notification to resume OAuth flow...");
						Console.WriteLine("Posting notification to resume OAuth flow...");
						
						// Post notification to trigger OAuth resumption
						NSNotificationCenter.DefaultCenter.PostNotificationName("ResumeOAuthFlow", null);
					}
					else
					{
						System.Diagnostics.Debug.WriteLine($"⚠ App was terminated during OAuth flow. Stored OAuth URL is too old ({elapsed.TotalMinutes:F1} minutes) - clearing stale data.");
						Console.WriteLine($"⚠ App was terminated during OAuth flow. Stored OAuth URL is too old ({elapsed.TotalMinutes:F1} minutes) - clearing stale data.");
						
						// Clear stale OAuth data
						userDefaults.SetBool(false, "GoogleSignInInProgress");
						userDefaults.RemoveObject("GoogleOAuthAuthUrl");
						userDefaults.RemoveObject("GoogleOAuthRedirectUri");
						userDefaults.RemoveObject("GoogleOAuthState");
						userDefaults.RemoveObject("GoogleOAuthNonce");
						userDefaults.RemoveObject("GoogleOAuthTimestamp");
						userDefaults.RemoveObject("GoogleSignInStartedUtc");
						userDefaults.Synchronize();
					}
				}
				else
				{
					System.Diagnostics.Debug.WriteLine("⚠ App was terminated during OAuth flow but no stored OAuth URL found. User will need to try again.");
					Console.WriteLine("⚠ App was terminated during OAuth flow but no stored OAuth URL found. User will need to try again.");
				}
				
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
			// This should match the REVERSED_CLIENT_ID in GoogleService-Info.plist
			"com.googleusercontent.apps.1021759232753-hhfhegcuq82cc9er9slf1r3iuqkkpbsh"
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
