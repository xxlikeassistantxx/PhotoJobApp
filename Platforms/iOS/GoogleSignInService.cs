using System;
using System.Reflection;
using System.Threading.Tasks;
using PhotoJobApp.Services;

#if __IOS__ && USE_NATIVE_GOOGLE_SIGNIN
using Foundation;
using UIKit;
// Note: Google.SignIn types are accessed via reflection to allow compilation
// even when the Xamarin.Google.iOS.SignIn package is not available

namespace PhotoJobApp.Platforms.iOS
{
    /// <summary>
    /// iOS-specific implementation of Google Sign-In using native Google Sign-In SDK.
    /// This handles OAuth flows reliably, including when the app is terminated during 2FA.
    /// </summary>
    public class GoogleSignInService : IGoogleSignInService
    {
        private TaskCompletionSource<string?>? _signInTaskCompletionSource;
        private UIViewController? _presentingViewController;

        public async Task<string?> SignInAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("GoogleSignInService: Starting native Google Sign-In...");
                Console.WriteLine("GoogleSignInService: Starting native Google Sign-In...");

                // Get the current view controller to present the sign-in UI
                _presentingViewController = GetCurrentViewController();
                
                if (_presentingViewController == null)
                {
                    System.Diagnostics.Debug.WriteLine("GoogleSignInService: ERROR - No view controller available to present sign-in");
                    Console.WriteLine("GoogleSignInService: ERROR - No view controller available to present sign-in");
                    return null;
                }

                // Get Google Client ID from configuration
                var clientId = GetGoogleClientId();
                if (string.IsNullOrEmpty(clientId))
                {
                    System.Diagnostics.Debug.WriteLine("GoogleSignInService: ERROR - Google Client ID not configured");
                    Console.WriteLine("GoogleSignInService: ERROR - Google Client ID not configured");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"GoogleSignInService: Configuring Google Sign-In with Client ID: {clientId.Substring(0, Math.Min(30, clientId.Length))}...");
                Console.WriteLine($"GoogleSignInService: Configuring Google Sign-In with Client ID: {clientId.Substring(0, Math.Min(30, clientId.Length))}...");

                // Use reflection to access Google Sign-In SDK types
                try
                {
                    System.Diagnostics.Debug.WriteLine("GoogleSignInService: Checking if Google Sign-In SDK types are available...");
                    Console.WriteLine("GoogleSignInService: Checking if Google Sign-In SDK types are available...");
                    
                    // Get GIDConfiguration type
                    var gidConfigurationType = Type.GetType("Google.SignIn.GIDConfiguration, Xamarin.Google.iOS.SignIn");
                    if (gidConfigurationType == null)
                    {
                        System.Diagnostics.Debug.WriteLine("GoogleSignInService: GIDConfiguration type not found");
                        Console.WriteLine("GoogleSignInService: GIDConfiguration type not found");
                        return null;
                    }

                    // Get GIDSignIn type
                    var gidSignInType = Type.GetType("Google.SignIn.GIDSignIn, Xamarin.Google.iOS.SignIn");
                    if (gidSignInType == null)
                    {
                        System.Diagnostics.Debug.WriteLine("GoogleSignInService: GIDSignIn type not found");
                        Console.WriteLine("GoogleSignInService: GIDSignIn type not found");
                        return null;
                }

                    // Create GIDConfiguration instance
                    var configuration = Activator.CreateInstance(gidConfigurationType, new object[] { clientId });
                    if (configuration == null)
                {
                        System.Diagnostics.Debug.WriteLine("GoogleSignInService: Failed to create GIDConfiguration");
                        Console.WriteLine("GoogleSignInService: Failed to create GIDConfiguration");
                        return null;
                    }

                    // Get SharedInstance property
                    var sharedInstanceProperty = gidSignInType.GetProperty("SharedInstance", BindingFlags.Public | BindingFlags.Static);
                    if (sharedInstanceProperty == null)
                    {
                        System.Diagnostics.Debug.WriteLine("GoogleSignInService: SharedInstance property not found");
                        Console.WriteLine("GoogleSignInService: SharedInstance property not found");
                    return null;
                }

                    var sharedInstance = sharedInstanceProperty.GetValue(null);
                    if (sharedInstance == null)
                {
                        System.Diagnostics.Debug.WriteLine("GoogleSignInService: SharedInstance is null");
                        Console.WriteLine("GoogleSignInService: SharedInstance is null");
                    return null;
                }

                    // Set Configuration property
                    var configurationProperty = gidSignInType.GetProperty("Configuration");
                    if (configurationProperty == null)
                    {
                        System.Diagnostics.Debug.WriteLine("GoogleSignInService: Configuration property not found");
                        Console.WriteLine("GoogleSignInService: Configuration property not found");
                    return null;
                }

                    configurationProperty.SetValue(sharedInstance, configuration);
                    System.Diagnostics.Debug.WriteLine("GoogleSignInService: GIDSignIn configured successfully");
                    Console.WriteLine("GoogleSignInService: GIDSignIn configured successfully");

                // Create task completion source for async/await pattern
                _signInTaskCompletionSource = new TaskCompletionSource<string?>();

                    // Get SignIn method
                    var signInMethod = gidSignInType.GetMethod("SignIn", new[] { typeof(UIViewController), typeof(Action<NSObject, NSError>) });
                    if (signInMethod == null)
                    {
                        System.Diagnostics.Debug.WriteLine("GoogleSignInService: SignIn method not found");
                        Console.WriteLine("GoogleSignInService: SignIn method not found");
                        return null;
                    }

                    // Create callback action
                    Action<NSObject, NSError> callback = (result, error) =>
                {
                    try
                    {
                        if (error != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"GoogleSignInService: Sign-in error: {error.LocalizedDescription}");
                            Console.WriteLine($"GoogleSignInService: Sign-in error: {error.LocalizedDescription}");
                            _signInTaskCompletionSource?.SetResult(null);
                            return;
                        }

                            if (result == null)
                            {
                                System.Diagnostics.Debug.WriteLine("GoogleSignInService: Sign-in result is null");
                                Console.WriteLine("GoogleSignInService: Sign-in result is null");
                                _signInTaskCompletionSource?.SetResult(null);
                                return;
                            }

                            // Get User property from result
                            var resultType = result.GetType();
                            var userProperty = resultType.GetProperty("User");
                            if (userProperty == null)
                        {
                                System.Diagnostics.Debug.WriteLine("GoogleSignInService: User property not found on result");
                                Console.WriteLine("GoogleSignInService: User property not found on result");
                                _signInTaskCompletionSource?.SetResult(null);
                                return;
                            }

                            var user = userProperty.GetValue(result);
                            if (user == null)
                            {
                                System.Diagnostics.Debug.WriteLine("GoogleSignInService: User is null");
                                Console.WriteLine("GoogleSignInService: User is null");
                                _signInTaskCompletionSource?.SetResult(null);
                                return;
                            }

                            // Get IdToken property from user
                            var userType = user.GetType();
                            var idTokenProperty = userType.GetProperty("IdToken");
                            if (idTokenProperty == null)
                            {
                                System.Diagnostics.Debug.WriteLine("GoogleSignInService: IdToken property not found on user");
                                Console.WriteLine("GoogleSignInService: IdToken property not found on user");
                                _signInTaskCompletionSource?.SetResult(null);
                                return;
                            }

                            var idTokenObj = idTokenProperty.GetValue(user);
                            if (idTokenObj == null)
                            {
                                System.Diagnostics.Debug.WriteLine("GoogleSignInService: IdToken is null");
                                Console.WriteLine("GoogleSignInService: IdToken is null");
                                _signInTaskCompletionSource?.SetResult(null);
                                return;
                            }

                            // Get TokenString property from idToken
                            var idTokenType = idTokenObj.GetType();
                            var tokenStringProperty = idTokenType.GetProperty("TokenString");
                            if (tokenStringProperty == null)
                            {
                                System.Diagnostics.Debug.WriteLine("GoogleSignInService: TokenString property not found on IdToken");
                                Console.WriteLine("GoogleSignInService: TokenString property not found on IdToken");
                            _signInTaskCompletionSource?.SetResult(null);
                            return;
                        }

                            var idToken = tokenStringProperty.GetValue(idTokenObj) as string;
                        
                        if (string.IsNullOrEmpty(idToken))
                        {
                            System.Diagnostics.Debug.WriteLine("GoogleSignInService: ID token is null or empty");
                            Console.WriteLine("GoogleSignInService: ID token is null or empty");
                            _signInTaskCompletionSource?.SetResult(null);
                            return;
                        }

                        System.Diagnostics.Debug.WriteLine("GoogleSignInService: Sign-in successful, ID token received");
                        Console.WriteLine("GoogleSignInService: Sign-in successful, ID token received");
                        _signInTaskCompletionSource?.SetResult(idToken);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"GoogleSignInService: Exception in sign-in callback: {ex.Message}");
                        Console.WriteLine($"GoogleSignInService: Exception in sign-in callback: {ex.Message}");
                        _signInTaskCompletionSource?.SetResult(null);
                    }
                    };

                    // Invoke SignIn method
                    signInMethod.Invoke(sharedInstance, new object[] { _presentingViewController, callback });

                // Wait for sign-in to complete
                var idToken = await _signInTaskCompletionSource.Task;
                return idToken;
                }
                catch (Exception configEx)
                {
                    // Check exception type for more specific error messages
                    if (configEx is TypeLoadException)
                    {
                        System.Diagnostics.Debug.WriteLine($"GoogleSignInService: TypeLoadException - SDK types not available: {configEx.Message}");
                        Console.WriteLine($"GoogleSignInService: TypeLoadException - SDK types not available: {configEx.Message}");
                    }
                    else if (configEx is DllNotFoundException)
                    {
                        System.Diagnostics.Debug.WriteLine($"GoogleSignInService: DllNotFoundException - Native framework not linked: {configEx.Message}");
                        Console.WriteLine($"GoogleSignInService: DllNotFoundException - Native framework not linked: {configEx.Message}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"GoogleSignInService: Exception configuring SDK: {configEx.GetType().Name}: {configEx.Message}");
                        Console.WriteLine($"GoogleSignInService: Exception configuring SDK: {configEx.GetType().Name}: {configEx.Message}");
                    }
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {configEx.StackTrace}");
                    Console.WriteLine($"Stack trace: {configEx.StackTrace}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GoogleSignInService: Error during sign-in: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.WriteLine($"GoogleSignInService: Error during sign-in: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                _signInTaskCompletionSource?.SetResult(null);
                return null;
            }
        }

        private string? GetGoogleClientId()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("GoogleSignInService: Attempting to read GoogleService-Info.plist...");
                Console.WriteLine("GoogleSignInService: Attempting to read GoogleService-Info.plist...");
                
                // Try to get from GoogleService-Info.plist first
                var googleServiceInfoPath = NSBundle.MainBundle.PathForResource("GoogleService-Info", "plist");
                System.Diagnostics.Debug.WriteLine($"GoogleSignInService: GoogleService-Info.plist path: {(string.IsNullOrEmpty(googleServiceInfoPath) ? "NOT FOUND" : googleServiceInfoPath)}");
                Console.WriteLine($"GoogleSignInService: GoogleService-Info.plist path: {(string.IsNullOrEmpty(googleServiceInfoPath) ? "NOT FOUND" : googleServiceInfoPath)}");
                
                if (!string.IsNullOrEmpty(googleServiceInfoPath))
                {
                    var plist = NSDictionary.FromFile(googleServiceInfoPath);
                    if (plist != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"GoogleSignInService: Successfully loaded plist with {plist.Count} keys");
                        Console.WriteLine($"GoogleSignInService: Successfully loaded plist with {plist.Count} keys");
                        
                        // Log all keys for debugging
                        foreach (var key in plist.Keys)
                        {
                            System.Diagnostics.Debug.WriteLine($"GoogleSignInService: Plist key: {key}");
                            Console.WriteLine($"GoogleSignInService: Plist key: {key}");
                        }
                        
                        var clientId = plist["CLIENT_ID"]?.ToString();
                        if (!string.IsNullOrEmpty(clientId))
                        {
                            System.Diagnostics.Debug.WriteLine($"GoogleSignInService: Found CLIENT_ID in GoogleService-Info.plist: {clientId.Substring(0, Math.Min(30, clientId.Length))}...");
                            Console.WriteLine($"GoogleSignInService: Found CLIENT_ID in GoogleService-Info.plist: {clientId.Substring(0, Math.Min(30, clientId.Length))}...");
                            return clientId;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("GoogleSignInService: CLIENT_ID key found but value is null or empty");
                            Console.WriteLine("GoogleSignInService: CLIENT_ID key found but value is null or empty");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("GoogleSignInService: Failed to load plist file");
                        Console.WriteLine("GoogleSignInService: Failed to load plist file");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("GoogleSignInService: GoogleService-Info.plist not found in bundle");
                    Console.WriteLine("GoogleSignInService: GoogleService-Info.plist not found in bundle");
                    
                    // Try alternative path
                    var altPath = NSBundle.MainBundle.PathForResource("GoogleService-Info", "plist", "Platforms/iOS");
                    System.Diagnostics.Debug.WriteLine($"GoogleSignInService: Alternative path check: {(string.IsNullOrEmpty(altPath) ? "NOT FOUND" : altPath)}");
                    Console.WriteLine($"GoogleSignInService: Alternative path check: {(string.IsNullOrEmpty(altPath) ? "NOT FOUND" : altPath)}");
                }

                // Fallback: Use the CLIENT_ID from GoogleService-Info.plist directly (hardcoded as fallback)
                // This is the iOS Client ID: 1021759232753-faphbn9aupbnev5uh958nhfhik05q8vh.apps.googleusercontent.com
                var fallbackClientId = "1021759232753-faphbn9aupbnev5uh958nhfhik05q8vh.apps.googleusercontent.com";
                System.Diagnostics.Debug.WriteLine($"GoogleSignInService: Using fallback CLIENT_ID: {fallbackClientId.Substring(0, Math.Min(30, fallbackClientId.Length))}...");
                Console.WriteLine($"GoogleSignInService: Using fallback CLIENT_ID: {fallbackClientId.Substring(0, Math.Min(30, fallbackClientId.Length))}...");
                return fallbackClientId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GoogleSignInService: Error getting Client ID: {ex.Message}");
                Console.WriteLine($"GoogleSignInService: Error getting Client ID: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Return fallback even on error
                return "1021759232753-faphbn9aupbnev5uh958nhfhik05q8vh.apps.googleusercontent.com";
            }
        }

        private UIViewController? GetCurrentViewController()
        {
            try
            {
                var window = UIApplication.SharedApplication.KeyWindow;
                if (window == null)
                {
                    // Try to get any window
                    var windows = UIApplication.SharedApplication.Windows;
                    if (windows != null && windows.Length > 0)
                    {
                        window = windows[0];
                    }
                }

                var rootViewController = window?.RootViewController;
                if (rootViewController == null)
                {
                    System.Diagnostics.Debug.WriteLine("GoogleSignInService: RootViewController is null");
                    Console.WriteLine("GoogleSignInService: RootViewController is null");
                    return null;
                }

                while (rootViewController.PresentedViewController != null)
                {
                    rootViewController = rootViewController.PresentedViewController;
                }

                return rootViewController;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GoogleSignInService: Error getting current ViewController: {ex.Message}");
                Console.WriteLine($"GoogleSignInService: Error getting current ViewController: {ex.Message}");
                return null;
            }
        }
    }
}
#else
namespace PhotoJobApp.Platforms.iOS
{
    /// <summary>
    /// Fallback implementation when the native Google Sign-In SDK is not available.
    /// Returns null so the calling code can fall back to WebAuthenticator.
    /// </summary>
    public class GoogleSignInService : IGoogleSignInService
    {
        public Task<string?> SignInAsync()
        {
            System.Diagnostics.Debug.WriteLine("GoogleSignInService: Native Google Sign-In SDK not available; returning null.");
            Console.WriteLine("GoogleSignInService: Native Google Sign-In SDK not available; returning null.");
            return Task.FromResult<string?>(null);
        }
    }
}
#endif

