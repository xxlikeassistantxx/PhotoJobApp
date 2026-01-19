using PhotoJobApp.Services;
#if IOS
using Foundation;
using System;
using System.Collections.Generic;
using System.Linq;
#endif

namespace PhotoJobApp
{
    public partial class LoginPage : ContentPage
    {
        private readonly FirebaseAuthService _authService;
        private bool _hasCheckedAuthState = false; // Prevent multiple auth state checks
        private bool _isCheckingAuthState = false; // Prevent concurrent checks
        private DateTime _lastFocusTime = DateTime.MinValue; // Track when password field was focused
#if IOS
		private const string PendingOAuthCallbackKey = "PendingOAuthCallback";
		private const string GoogleSignInInProgressKey = "GoogleSignInInProgress";
		private const string GoogleSignInStartKey = "GoogleSignInStartedUtc";
		private const string GoogleSignInResumeAttemptsKey = "GoogleSignInResumeAttempts";
		private NSObject? _oauthCallbackObserver; // Store observer reference for cleanup
#endif

        public LoginPage(FirebaseAuthService authService)
        {
            InitializeComponent();
            _authService = authService;
            
            System.Diagnostics.Debug.WriteLine("LoginPage constructor completed");
            Console.WriteLine("LoginPage constructor completed");
            
            // Restore saved email and password
            LoadSavedCredentials();
            
            // Wire up text changed events to save credentials
            if (EmailEntry != null)
            {
                EmailEntry.TextChanged += (sender, e) => {
                    System.Diagnostics.Debug.WriteLine($"Email text changed: '{e.NewTextValue}'");
                    Console.WriteLine($"Email text changed: '{e.NewTextValue}'");
                    // Always save email (it's not sensitive)
                    if (!string.IsNullOrWhiteSpace(e.NewTextValue))
                    {
                        Preferences.Set("LoginPage_Email", e.NewTextValue);
                    }
                };
            }
            
            if (PasswordEntry != null)
            {
                PasswordEntry.Focused += (sender, e) => {
                    _lastFocusTime = DateTime.Now;
                    System.Diagnostics.Debug.WriteLine("Password field focused");
                };
                
                PasswordEntry.TextChanged += (sender, e) => {
                    // Update focus time on every keystroke to prevent checks while typing
                    _lastFocusTime = DateTime.Now;
                    
                    System.Diagnostics.Debug.WriteLine($"Password text changed: length {e.NewTextValue?.Length ?? 0}");
                    Console.WriteLine($"Password text changed: length {e.NewTextValue?.Length ?? 0}");
                    // Only save password if "Remember Me" is checked
                    if (RememberMeCheckBox?.IsChecked == true && !string.IsNullOrWhiteSpace(e.NewTextValue))
                    {
                        Preferences.Set("LoginPage_Password", e.NewTextValue);
                    }
                };
            }
            
            if (EmailEntry != null)
            {
                EmailEntry.Focused += (sender, e) => {
                    _lastFocusTime = DateTime.Now;
                };
            }
            
            // Wire up Remember Me checkbox to control password saving
            if (RememberMeCheckBox != null)
            {
                RememberMeCheckBox.CheckedChanged += (sender, e) => {
                    if (e.Value)
                    {
                        // If checked, save current password
                        if (!string.IsNullOrWhiteSpace(PasswordEntry?.Text))
                        {
                            Preferences.Set("LoginPage_Password", PasswordEntry.Text);
                            Preferences.Set("LoginPage_RememberMe", true);
                        }
                    }
                    else
                    {
                        // If unchecked, clear saved password
                        Preferences.Remove("LoginPage_Password");
                        Preferences.Set("LoginPage_RememberMe", false);
                    }
                };
            }
            
            #if IOS
            // Listen for OAuth callback notification from AppDelegate
            // This handles the case when the app restarts during OAuth (e.g., during 2FA)
            _oauthCallbackObserver = NSNotificationCenter.DefaultCenter.AddObserver(
                new NSString("PendingOAuthCallbackFound"),
                async (notification) =>
                {
                    System.Diagnostics.Debug.WriteLine("LoginPage: Received PendingOAuthCallbackFound notification");
                    Console.WriteLine("LoginPage: Received PendingOAuthCallbackFound notification");
                    
                    try
                    {
                        var userDefaults = Foundation.NSUserDefaults.StandardUserDefaults;
                        userDefaults.Synchronize();
                        var url = userDefaults.StringForKey("PendingOAuthCallback");
                        
                        if (!string.IsNullOrEmpty(url))
                        {
                            System.Diagnostics.Debug.WriteLine($"LoginPage: Found stored callback URL, processing...");
                            Console.WriteLine($"LoginPage: Found stored callback URL, processing...");
                            
                            // Clear it so we don't process it twice
                            userDefaults.RemoveObject("PendingOAuthCallback");
                            userDefaults.SetBool(false, "GoogleSignInInProgress");
                            userDefaults.Synchronize();
                            
                            // Process the callback URL
                            var result = await _authService.ParseCallbackUrlAsync(url);
                            
                            if (result.success && result.user != null)
                            {
                                System.Diagnostics.Debug.WriteLine($"LoginPage: OAuth callback processed successfully, signing in user: {result.user.Email}");
                                Console.WriteLine($"LoginPage: OAuth callback processed successfully, signing in user: {result.user.Email}");
                                
                                // Save authentication state
                                Preferences.Set("IsAuthenticated", true);
                                Preferences.Set("UserId", result.user.Id);
                                Preferences.Set("UserEmail", result.user.Email ?? "");
                                
                                // Navigate to main app
                                await NavigateToMainApp();
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"LoginPage: OAuth callback processing failed: {result.error}");
                                Console.WriteLine($"LoginPage: OAuth callback processing failed: {result.error}");
                                await MainThread.InvokeOnMainThreadAsync(async () =>
                                {
                                    await DisplayAlert("Sign In Error", result.error ?? "Failed to process OAuth callback. Please try again.", "OK");
                                });
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("LoginPage: No stored callback URL found");
                            Console.WriteLine("LoginPage: No stored callback URL found");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"LoginPage: Error processing OAuth callback notification: {ex.Message}");
                        Console.WriteLine($"LoginPage: Error processing OAuth callback notification: {ex.Message}");
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            await DisplayAlert("Error", $"Failed to process OAuth callback: {ex.Message}", "OK");
                        });
                    }
                });
            #endif
        }
        
        private void LoadSavedCredentials()
        {
            try
            {
                // Always restore email
                var savedEmail = Preferences.Get("LoginPage_Email", string.Empty);
                if (!string.IsNullOrEmpty(savedEmail) && EmailEntry != null)
                {
                    EmailEntry.Text = savedEmail;
                    System.Diagnostics.Debug.WriteLine($"Restored saved email: {savedEmail}");
                    Console.WriteLine($"Restored saved email: {savedEmail}");
                }
                
                // Restore password and Remember Me checkbox if they were saved
                var rememberMe = Preferences.Get("LoginPage_RememberMe", false);
                if (rememberMe && RememberMeCheckBox != null)
                {
                    RememberMeCheckBox.IsChecked = true;
                    
                    var savedPassword = Preferences.Get("LoginPage_Password", string.Empty);
                    if (!string.IsNullOrEmpty(savedPassword) && PasswordEntry != null)
                    {
                        PasswordEntry.Text = savedPassword;
                        System.Diagnostics.Debug.WriteLine("Restored saved password (length hidden for security)");
                        Console.WriteLine("Restored saved password (length hidden for security)");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading saved credentials: {ex.Message}");
                Console.WriteLine($"Error loading saved credentials: {ex.Message}");
            }
        }
        
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Prevent checks if user is actively typing (within last 3 seconds)
            // This prevents keyboard from being dismissed while typing
            if ((DateTime.Now - _lastFocusTime).TotalSeconds < 3)
            {
                System.Diagnostics.Debug.WriteLine("LoginPage.OnAppearing - User is actively typing, skipping checks to prevent keyboard dismissal");
                Console.WriteLine("LoginPage.OnAppearing - User is actively typing, skipping checks to prevent keyboard dismissal");
                return;
            }

            // Prevent multiple concurrent auth state checks (this was causing the blinking)
            if (_isCheckingAuthState)
            {
                System.Diagnostics.Debug.WriteLine("LoginPage.OnAppearing - Auth state check already in progress, skipping...");
                Console.WriteLine("LoginPage.OnAppearing - Auth state check already in progress, skipping...");
                return;
            }

            // Only check auth state once per page appearance (not on every focus change)
            if (_hasCheckedAuthState)
            {
                System.Diagnostics.Debug.WriteLine("LoginPage.OnAppearing - Already checked auth state, skipping...");
                Console.WriteLine("LoginPage.OnAppearing - Already checked auth state, skipping...");
                return;
            }

            _isCheckingAuthState = true;

            // FIRST: Check for pending OAuth callback (e.g. after returning from browser or 2FA app)
            // Only check once, not repeatedly
            #if IOS
            _ = Task.Run(async () =>
            {
                try
                {
                    // Add a small delay to ensure NSUserDefaults is fully synchronized
                    await Task.Delay(200);
                    
                    // Check for pending callback first (highest priority)
                    await CheckForPendingCallback();
                    
                    // Then check if we should resume OAuth flow (only if no callback found)
                    await AttemptResumeGoogleSignInAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error checking for pending OAuth callback: {ex.Message}");
                    Console.WriteLine($"Error checking for pending OAuth callback: {ex.Message}");
                }
                finally
                {
                    _isCheckingAuthState = false;
                }
            });
            #endif
            
            // SECOND: Check if user is already authenticated (from previous Google Sign-In or email/password)
            // This provides automatic restoration for Google Sign-In
            try
            {
                System.Diagnostics.Debug.WriteLine("LoginPage.OnAppearing - Checking authentication state...");
                Console.WriteLine("LoginPage.OnAppearing - Checking authentication state...");
                
                if (_authService.IsAuthenticated())
                {
                    System.Diagnostics.Debug.WriteLine("User is already authenticated, checking if session is still valid...");
                    Console.WriteLine("User is already authenticated, checking if session is still valid...");
                    
                    // Verify the authentication state is still valid with timeout to prevent hanging
                    var checkAuthTask = _authService.CheckAuthStateAsync();
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
                    var completedTask = await Task.WhenAny(checkAuthTask, timeoutTask);
                    
                    if (completedTask == checkAuthTask && !checkAuthTask.IsFaulted)
                    {
                        var (isAuthenticated, user) = await checkAuthTask;
                    
                        if (isAuthenticated && user != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"✓ Automatic restoration: User is authenticated ({user.Email}), navigating to main app...");
                            Console.WriteLine($"✓ Automatic restoration: User is authenticated ({user.Email}), navigating to main app...");
                            
                            // Ensure authentication state is saved
                            Preferences.Set("IsAuthenticated", true);
                            Preferences.Set("UserId", user.Id);
                            Preferences.Set("UserEmail", user.Email ?? "");
                            
                            // Navigate to main app automatically
                            await NavigateToMainApp();
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Previous session expired, user needs to sign in again");
                            Console.WriteLine("Previous session expired, user needs to sign in again");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("CheckAuthStateAsync timed out or failed, skipping automatic restoration");
                        Console.WriteLine("CheckAuthStateAsync timed out or failed, skipping automatic restoration");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No previous authentication found");
                    Console.WriteLine("No previous authentication found");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking authentication state in OnAppearing: {ex.Message}");
                Console.WriteLine($"Error checking authentication state in OnAppearing: {ex.Message}");
            }
            finally
            {
                _hasCheckedAuthState = true;
                _isCheckingAuthState = false;
            }
        }
        
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Reset flag when page disappears so it can check again when it reappears
            _hasCheckedAuthState = false;
            
            #if IOS
            // Remove notification observer to prevent memory leaks
            if (_oauthCallbackObserver != null)
            {
                NSNotificationCenter.DefaultCenter.RemoveObserver(_oauthCallbackObserver);
                _oauthCallbackObserver = null;
            }
            #endif
        }
        
        #if IOS
        private async Task CheckForPendingCallback()
        {
            try
            {
                // Don't check if user is actively typing (prevents keyboard dismissal)
                if ((DateTime.Now - _lastFocusTime).TotalSeconds < 3)
                {
                    System.Diagnostics.Debug.WriteLine("CheckForPendingCallback - User is actively typing, skipping to prevent keyboard dismissal");
                    return;
                }
            
                System.Diagnostics.Debug.WriteLine("LoginPage.CheckForPendingCallback - Checking for pending OAuth callback...");
                Console.WriteLine("LoginPage.CheckForPendingCallback - Checking for pending OAuth callback...");
                
                var userDefaults = Foundation.NSUserDefaults.StandardUserDefaults;
                userDefaults.Synchronize(); // Ensure we have latest data
                
				var pendingCallback = userDefaults.StringForKey(PendingOAuthCallbackKey);
				var signInInProgress = userDefaults.BoolForKey(GoogleSignInInProgressKey);
                
                if (!string.IsNullOrEmpty(pendingCallback) && signInInProgress)
                {
                    System.Diagnostics.Debug.WriteLine($"✓✓✓ Found pending OAuth callback in LoginPage!");
                    Console.WriteLine($"✓✓✓ Found pending OAuth callback in LoginPage!");
                    System.Diagnostics.Debug.WriteLine($"  Callback URL: {pendingCallback.Substring(0, Math.Min(100, pendingCallback.Length))}...");
                    Console.WriteLine($"  Callback URL: {pendingCallback.Substring(0, Math.Min(100, pendingCallback.Length))}...");
                    
                    // Process the callback on main thread
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await ProcessPendingOAuthCallback(pendingCallback);
                    });
                }
                else
                {
					System.Diagnostics.Debug.WriteLine($"No pending callback found (callback: {(!string.IsNullOrEmpty(pendingCallback) ? "exists" : "null")}, inProgress: {signInInProgress})");
					Console.WriteLine($"No pending callback found (callback: {(!string.IsNullOrEmpty(pendingCallback) ? "exists" : "null")}, inProgress: {signInInProgress})");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CheckForPendingCallback: {ex.Message}");
                Console.WriteLine($"Error in CheckForPendingCallback: {ex.Message}");
            }
        }
        
		private async Task AttemptResumeGoogleSignInAsync()
		{
			try
			{
				// Don't check if user is actively typing (prevents keyboard dismissal)
				if ((DateTime.Now - _lastFocusTime).TotalSeconds < 3)
				{
					System.Diagnostics.Debug.WriteLine("AttemptResumeGoogleSignInAsync - User is actively typing, skipping to prevent keyboard dismissal");
					return;
				}
			
				var userDefaults = Foundation.NSUserDefaults.StandardUserDefaults;
				userDefaults.Synchronize();
				
				var signInInProgress = userDefaults.BoolForKey(GoogleSignInInProgressKey);
				var pendingCallback = userDefaults.StringForKey(PendingOAuthCallbackKey);
				
				// If we have a pending callback, process it instead of resuming
				if (!string.IsNullOrEmpty(pendingCallback))
				{
					System.Diagnostics.Debug.WriteLine("AttemptResumeGoogleSignInAsync: Pending callback found, processing it.");
					Console.WriteLine("AttemptResumeGoogleSignInAsync: Pending callback found, processing it.");
					await CheckForPendingCallback();
					return;
				}
				
				if (!signInInProgress)
				{
					System.Diagnostics.Debug.WriteLine("AttemptResumeGoogleSignInAsync: Sign-in not in progress, nothing to resume.");
					Console.WriteLine("AttemptResumeGoogleSignInAsync: Sign-in not in progress, nothing to resume.");
					return;
				}
				
				var startedSeconds = userDefaults.DoubleForKey(GoogleSignInStartKey);
				DateTimeOffset startedAt;
				
				if (startedSeconds > 0)
				{
					startedAt = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(Math.Round(startedSeconds)));
				}
				else
				{
					startedAt = DateTimeOffset.UtcNow;
				}
				
				var signInElapsed = DateTimeOffset.UtcNow - startedAt;
				System.Diagnostics.Debug.WriteLine($"AttemptResumeGoogleSignInAsync: Sign-in in progress for {signInElapsed.TotalSeconds:F1}s");
				Console.WriteLine($"AttemptResumeGoogleSignInAsync: Sign-in in progress for {signInElapsed.TotalSeconds:F1}s");
				
				if (signInElapsed > TimeSpan.FromMinutes(5))
				{
					System.Diagnostics.Debug.WriteLine("AttemptResumeGoogleSignInAsync: Sign-in attempt timed out. Clearing flags.");
					Console.WriteLine("AttemptResumeGoogleSignInAsync: Sign-in attempt timed out. Clearing flags.");
					
					ResetGoogleSignInTracking();
					return;
				}
				
				// Check if we have a stored OAuth URL to resume
				// Only resume if it's very recent (< 2 minutes) to avoid using stale URLs
				var storedAuthUrl = userDefaults.StringForKey("GoogleOAuthAuthUrl");
				var storedCallbackUrl = userDefaults.StringForKey("GoogleOAuthRedirectUri");
				var storedTimestamp = userDefaults.DoubleForKey("GoogleOAuthTimestamp");
				
				if (!string.IsNullOrEmpty(storedAuthUrl) && !string.IsNullOrEmpty(storedCallbackUrl) && storedTimestamp > 0)
				{
					var storedTime = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(Math.Round(storedTimestamp)));
					var urlAge = DateTimeOffset.UtcNow - storedTime;
					
					// Only resume if very recent (< 2 minutes) - this means user just tapped button and app was terminated
					if (urlAge < TimeSpan.FromMinutes(2))
					{
						System.Diagnostics.Debug.WriteLine($"AttemptResumeGoogleSignInAsync: Found recent stored OAuth URL ({urlAge.TotalSeconds:F1}s old) - will use it when user taps Sign in with Google");
						Console.WriteLine($"AttemptResumeGoogleSignInAsync: Found recent stored OAuth URL ({urlAge.TotalSeconds:F1}s old) - will use it when user taps Sign in with Google");
						// Don't launch WebAuthenticator automatically - wait for user to tap the button
						// This prevents Code=3 errors from launching when app isn't fully in foreground
					}
					else
					{
						System.Diagnostics.Debug.WriteLine($"AttemptResumeGoogleSignInAsync: Stored OAuth URL is too old ({urlAge.TotalMinutes:F1} minutes) - clearing it");
						Console.WriteLine($"AttemptResumeGoogleSignInAsync: Stored OAuth URL is too old ({urlAge.TotalMinutes:F1} minutes) - clearing it");
						
						// Clear stale OAuth data
						userDefaults.RemoveObject("GoogleOAuthAuthUrl");
						userDefaults.RemoveObject("GoogleOAuthRedirectUri");
						userDefaults.RemoveObject("GoogleOAuthState");
						userDefaults.RemoveObject("GoogleOAuthNonce");
						userDefaults.RemoveObject("GoogleOAuthTimestamp");
						userDefaults.Synchronize();
					}
				}
				else
				{
					System.Diagnostics.Debug.WriteLine("AttemptResumeGoogleSignInAsync: No stored OAuth URL found, waiting for callback.");
					Console.WriteLine("AttemptResumeGoogleSignInAsync: No stored OAuth URL found, waiting for callback.");
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"AttemptResumeGoogleSignInAsync: {ex.Message}");
				Console.WriteLine($"AttemptResumeGoogleSignInAsync: {ex.Message}");
			}
		}
		
        private async Task ProcessPendingOAuthCallback(string callbackUrl)
        {
#if IOS
            // Declare userDefaults at method level so it's accessible in all nested #if IOS blocks
            var userDefaults = Foundation.NSUserDefaults.StandardUserDefaults;
#endif
            try
            {
                System.Diagnostics.Debug.WriteLine($"Processing pending OAuth callback: {callbackUrl.Substring(0, Math.Min(100, callbackUrl.Length))}...");
                Console.WriteLine($"Processing pending OAuth callback: {callbackUrl.Substring(0, Math.Min(100, callbackUrl.Length))}...");
                
                // Clear the pending callback flag immediately to prevent duplicate processing
#if IOS
				userDefaults.RemoveObject(PendingOAuthCallbackKey);
				userDefaults.SetBool(false, GoogleSignInInProgressKey);
				userDefaults.RemoveObject(GoogleSignInStartKey);
				userDefaults.SetInt(0, GoogleSignInResumeAttemptsKey);
                // Note: We keep GoogleOAuthState, GoogleOAuthNonce, GoogleOAuthRedirectUri, GoogleOAuthClientId
                // until sign-in completes successfully or fails - they're needed for code exchange
                userDefaults.Synchronize();
#endif
                
                // Parse the callback URL
                // Firebase redirects can come in different formats:
                // 1. com.pinebelttrophy.photojobapp2025://?id_token=...
                // 2. com.pinebelttrophy.photojobapp2025://#id_token=...
                // 3. com.pinebelttrophy.photojobapp2025://?code=...&state=...
                System.Diagnostics.Debug.WriteLine($"Parsing callback URL: {callbackUrl}");
                Console.WriteLine($"Parsing callback URL: {callbackUrl}");
                
                if (Uri.TryCreate(callbackUrl, UriKind.Absolute, out var callbackUri))
                {
                    // Extract query parameters and fragment
                    var fullUrl = callbackUri.ToString();
                    System.Diagnostics.Debug.WriteLine($"Full callback URL: {fullUrl}");
                    Console.WriteLine($"Full callback URL: {fullUrl}");
                    
                    var queryStart = fullUrl.IndexOf('?');
                    var fragmentStart = fullUrl.IndexOf('#');
                    
                    // Parse query string manually
                    var queryParams = new Dictionary<string, string>();
                    
                    // Parse query parameters (after ?)
                    if (queryStart >= 0)
                    {
                        var queryEnd = fragmentStart >= 0 ? fragmentStart : fullUrl.Length;
                        var queryString = fullUrl.Substring(queryStart + 1, queryEnd - queryStart - 1);
                        System.Diagnostics.Debug.WriteLine($"Query string: {queryString}");
                        Console.WriteLine($"Query string: {queryString}");
                        
                        foreach (var param in queryString.Split('&'))
                        {
                            var parts = param.Split('=');
                            if (parts.Length >= 2)
                            {
                                var key = Uri.UnescapeDataString(parts[0]);
                                var value = Uri.UnescapeDataString(string.Join("=", parts.Skip(1))); // Handle values with = in them
                                queryParams[key] = value;
                                System.Diagnostics.Debug.WriteLine($"  Query param: {key} = {value.Substring(0, Math.Min(50, value.Length))}...");
                            }
                        }
                    }
                    
                    // Parse fragment (tokens are often in the fragment for OAuth)
                    if (fragmentStart >= 0)
                    {
                        var fragment = fullUrl.Substring(fragmentStart + 1);
                        System.Diagnostics.Debug.WriteLine($"Fragment: {fragment.Substring(0, Math.Min(100, fragment.Length))}...");
                        Console.WriteLine($"Fragment: {fragment.Substring(0, Math.Min(100, fragment.Length))}...");
                        
                        foreach (var param in fragment.Split('&'))
                        {
                            var parts = param.Split('=');
                            if (parts.Length >= 2)
                            {
                                var key = Uri.UnescapeDataString(parts[0]);
                                var value = Uri.UnescapeDataString(string.Join("=", parts.Skip(1))); // Handle values with = in them
                                queryParams[key] = value;
                                System.Diagnostics.Debug.WriteLine($"  Fragment param: {key} = {value.Substring(0, Math.Min(50, value.Length))}...");
                            }
                        }
                    }
                    
                    // Log all parsed parameters
                    System.Diagnostics.Debug.WriteLine($"Parsed {queryParams.Count} parameters from callback URL");
                    Console.WriteLine($"Parsed {queryParams.Count} parameters from callback URL");
                    foreach (var param in queryParams.Keys)
                    {
                        System.Diagnostics.Debug.WriteLine($"  {param} = {(queryParams[param].Length > 50 ? queryParams[param].Substring(0, 50) + "..." : queryParams[param])}");
                    }
                    
                    // Verify state parameter if present (security check)
                    var callbackState = queryParams.ContainsKey("state") ? queryParams["state"] : null;
#if IOS
                    if (!string.IsNullOrEmpty(callbackState))
                    {
                        try
                        {
                            userDefaults.Synchronize();
                            var storedState = userDefaults.StringForKey("GoogleOAuthState");
                            
                            if (!string.IsNullOrEmpty(storedState) && storedState != callbackState)
                            {
                                System.Diagnostics.Debug.WriteLine($"⚠️ State mismatch! Stored: {storedState}, Callback: {callbackState}");
                                Console.WriteLine($"⚠️ State mismatch! Stored: {storedState}, Callback: {callbackState}");
                                // Clear OAuth state tracking flags on state mismatch
                                ResetGoogleSignInTracking();
                                await DisplayAlert("Sign-In Error", "OAuth state verification failed. This may indicate a security issue. Please try again.", "OK");
                                return;
                            }
                            else if (!string.IsNullOrEmpty(storedState))
                            {
                                System.Diagnostics.Debug.WriteLine($"✓ OAuth state verified successfully");
                                Console.WriteLine($"✓ OAuth state verified successfully");
                            }
                        }
                        catch (Exception stateEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Warning: Failed to verify OAuth state: {stateEx.Message}");
                            Console.WriteLine($"Warning: Failed to verify OAuth state: {stateEx.Message}");
                            // Continue anyway - state verification is a security best practice but not critical
                        }
                    }
#endif
                    
                    // Look for Firebase auth tokens
                    // Firebase can return tokens in different parameter names
                    var idToken = queryParams.ContainsKey("id_token") ? queryParams["id_token"] : 
                                 queryParams.ContainsKey("idToken") ? queryParams["idToken"] :
                                 queryParams.ContainsKey("token") ? queryParams["token"] : null;
                    
                    var accessToken = queryParams.ContainsKey("access_token") ? queryParams["access_token"] : 
                                    queryParams.ContainsKey("accessToken") ? queryParams["accessToken"] : null;
                    
                    var code = queryParams.ContainsKey("code") ? queryParams["code"] : null; // Authorization code
                    var error = queryParams.ContainsKey("error") ? queryParams["error"] : null;
                    
                    if (!string.IsNullOrEmpty(error))
                    {
                        var errorDescription = queryParams.ContainsKey("error_description") ? queryParams["error_description"] : error;
                        System.Diagnostics.Debug.WriteLine($"OAuth callback contains error: {error} - {errorDescription}");
                        Console.WriteLine($"OAuth callback contains error: {error} - {errorDescription}");
                        // Clear OAuth state tracking flags on error
                        ResetGoogleSignInTracking();
                        await DisplayAlert("Sign-In Error", $"Google Sign-In failed: {errorDescription ?? error}", "OK");
                        return;
                    }
                    
                    if (!string.IsNullOrEmpty(idToken))
                    {
                        System.Diagnostics.Debug.WriteLine("✓ Found id_token in callback, exchanging for Firebase token...");
                        Console.WriteLine("✓ Found id_token in callback, exchanging for Firebase token...");
                        
                        // Exchange Google ID token for Firebase token
                        var result = await _authService.SignInWithGoogleIdTokenAsync(idToken);
                        
                        if (result.success && result.user != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"✓✓✓ Google Sign-In successful! User: {result.user.Email}");
                            Console.WriteLine($"✓✓✓ Google Sign-In successful! User: {result.user.Email}");
                            
                            // Save authentication state
                            Preferences.Set("IsAuthenticated", true);
                            Preferences.Set("UserId", result.user.Id);
                            Preferences.Set("UserEmail", result.user.Email ?? "");
                            
                            // Clear OAuth state tracking flags
                            ResetGoogleSignInTracking();
                            
                            // Navigate to main app
                            await NavigateToMainApp();
                            return;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Google Sign-In failed: {result.error}");
                            Console.WriteLine($"Google Sign-In failed: {result.error}");
                            // Clear OAuth state tracking flags on failure
                            ResetGoogleSignInTracking();
                            await DisplayAlert("Sign-In Error", result.error ?? "Failed to sign in with Google", "OK");
                            return;
                        }
                    }
                    else if (!string.IsNullOrEmpty(code))
                    {
                        System.Diagnostics.Debug.WriteLine("Found authorization code in callback, attempting exchange for tokens...");
                        Console.WriteLine("Found authorization code in callback, attempting exchange for tokens...");

                        // Try to restore redirectUri from stored OAuth state first
                        string redirectUri = null;
#if IOS
                        try
                        {
                            userDefaults.Synchronize();
                            redirectUri = userDefaults.StringForKey("GoogleOAuthRedirectUri");
                            if (!string.IsNullOrEmpty(redirectUri))
                            {
                                System.Diagnostics.Debug.WriteLine($"✓ Restored redirectUri from stored OAuth state: {redirectUri}");
                                Console.WriteLine($"✓ Restored redirectUri from stored OAuth state: {redirectUri}");
                            }
                        }
                        catch (Exception restoreEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Warning: Failed to restore redirectUri: {restoreEx.Message}");
                            Console.WriteLine($"Warning: Failed to restore redirectUri: {restoreEx.Message}");
                        }
#endif
                        
                        // Fallback to extracting from callback URI if not restored
                        if (string.IsNullOrWhiteSpace(redirectUri))
                        {
                            redirectUri = callbackUri.GetLeftPart(UriPartial.Path);
                            if (string.IsNullOrWhiteSpace(redirectUri))
                            {
                                redirectUri = $"{callbackUri.Scheme}:/oauth2redirect";
                            }
                            System.Diagnostics.Debug.WriteLine($"Using redirectUri from callback URI: {redirectUri}");
                            Console.WriteLine($"Using redirectUri from callback URI: {redirectUri}");
                        }

                        var result = await _authService.SignInWithGoogleAuthorizationCodeAsync(code, redirectUri);
                        if (result.success && result.user != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"✓✓✓ Google Sign-In successful via authorization code! User: {result.user.Email}");
                            Console.WriteLine($"✓✓✓ Google Sign-In successful via authorization code! User: {result.user.Email}");

                            Preferences.Set("IsAuthenticated", true);
                            Preferences.Set("UserId", result.user.Id);
                            Preferences.Set("UserEmail", result.user.Email ?? "");
                            
                            // Clear OAuth state tracking flags
                            ResetGoogleSignInTracking();

                            await NavigateToMainApp();
                            return;
                        }

                        var errorMessage = result.error ?? "Unable to exchange Google authorization code.";
                        System.Diagnostics.Debug.WriteLine($"Authorization code exchange failed: {errorMessage}");
                        Console.WriteLine($"Authorization code exchange failed: {errorMessage}");
                        // Clear OAuth state tracking flags on failure
                        ResetGoogleSignInTracking();
                        await DisplayAlert("Sign-In Error", errorMessage, "OK");
                        return;
                    }
                    else if (!string.IsNullOrEmpty(accessToken))
                    {
                        System.Diagnostics.Debug.WriteLine("Found access_token in callback, but need id_token for Firebase");
                        Console.WriteLine("Found access_token in callback, but need id_token for Firebase");
                        await DisplayAlert("Sign-In Error", "Unable to complete Google Sign-In. Please try again.", "OK");
                        return;
                    }
                    else
                    {
                        // No tokens found - log all parameters for debugging
                        System.Diagnostics.Debug.WriteLine("⚠️ No tokens found in callback URL. Available parameters:");
                        Console.WriteLine("⚠️ No tokens found in callback URL. Available parameters:");
                        foreach (var param in queryParams)
                        {
                            System.Diagnostics.Debug.WriteLine($"  {param.Key} = {param.Value.Substring(0, Math.Min(100, param.Value.Length))}...");
                            Console.WriteLine($"  {param.Key} = {param.Value.Substring(0, Math.Min(100, param.Value.Length))}...");
                        }
                        // Clear OAuth state tracking flags on error
                        ResetGoogleSignInTracking();
                        await DisplayAlert("Sign-In Error", "Unable to complete Google Sign-In. No authentication token received.", "OK");
                        return;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Invalid callback URL format: {callbackUrl}");
                    Console.WriteLine($"Invalid callback URL format: {callbackUrl}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing pending OAuth callback: {ex.Message}");
                Console.WriteLine($"Error processing pending OAuth callback: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await DisplayAlert("Error", $"Failed to process sign-in callback: {ex.Message}", "OK");
            }
        }
        #endif

#if IOS
		private void ResetGoogleSignInTracking()
		{
			try
			{
				var userDefaults = Foundation.NSUserDefaults.StandardUserDefaults;
				userDefaults.SetBool(false, GoogleSignInInProgressKey);
				userDefaults.RemoveObject(PendingOAuthCallbackKey);
				userDefaults.RemoveObject(GoogleSignInStartKey);
				userDefaults.SetInt(0, GoogleSignInResumeAttemptsKey);
				// Also clear OAuth state (state, nonce, redirectUri, clientId, authUrl, timestamp) after successful sign-in
				userDefaults.RemoveObject("GoogleOAuthState");
				userDefaults.RemoveObject("GoogleOAuthNonce");
				userDefaults.RemoveObject("GoogleOAuthRedirectUri");
				userDefaults.RemoveObject("GoogleOAuthClientId");
				userDefaults.RemoveObject("GoogleOAuthAuthUrl"); // Clear stored OAuth URL
				userDefaults.RemoveObject("GoogleOAuthTimestamp"); // Clear timestamp
				userDefaults.Synchronize();
				
				System.Diagnostics.Debug.WriteLine("ResetGoogleSignInTracking: Cleared Google sign-in tracking flags and OAuth state.");
				Console.WriteLine("ResetGoogleSignInTracking: Cleared Google sign-in tracking flags and OAuth state.");
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"ResetGoogleSignInTracking error: {ex.Message}");
				Console.WriteLine($"ResetGoogleSignInTracking error: {ex.Message}");
			}
		}
#endif
        
        private async Task NavigateToMainApp()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Navigating to main app...");
                Console.WriteLine("Navigating to main app...");
                
                // Create a new AppShell and set it as the window page
                var appShell = new AppShell(_authService);
                
                // Use the recommended approach to update the window page
                if (Application.Current?.Windows != null && Application.Current.Windows.Count > 0)
                {
                    Application.Current.Windows[0].Page = appShell;
                    System.Diagnostics.Debug.WriteLine("✓ Successfully navigated to main app");
                    Console.WriteLine("✓ Successfully navigated to main app");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error navigating to main app: {ex.Message}");
                Console.WriteLine($"Error navigating to main app: {ex.Message}");
            }
        }

        private async void OnSignInClicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("OnSignInClicked called!");
            Console.WriteLine("OnSignInClicked called!");
            
            // Get values directly from the Entry controls
            var email = EmailEntry?.Text ?? "";
            var password = PasswordEntry?.Text ?? "";
            
            System.Diagnostics.Debug.WriteLine($"Email from Entry: '{email}', Password length: {password?.Length ?? 0}");
            Console.WriteLine($"Email from Entry: '{email}', Password length: {password?.Length ?? 0}");
            
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("Error", "Please enter both email and password.", "OK");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("Starting sign in process...");
                Console.WriteLine("Starting sign in process...");
                
                var result = await _authService.SignInAsync(email, password);
                System.Diagnostics.Debug.WriteLine($"Sign in result: success={result.success}, user={result.user != null}, error={result.error}");
                Console.WriteLine($"Sign in result: success={result.success}, user={result.user != null}, error={result.error}");
                
                if (result.success && result.user != null)
                {
                    System.Diagnostics.Debug.WriteLine("Authentication successful, attempting navigation...");
                    Console.WriteLine("Authentication successful, attempting navigation...");
                    
                    try
                    {
                        // Save authentication state
                        Preferences.Set("IsAuthenticated", true);
                        Preferences.Set("UserId", result.user.Id);
                        Preferences.Set("UserEmail", result.user.Email);
                        
                        // Clear password if "Remember Me" is not checked
                        if (RememberMeCheckBox?.IsChecked != true)
                        {
                            Preferences.Remove("LoginPage_Password");
                            Preferences.Set("LoginPage_RememberMe", false);
                        }
                        
                        System.Diagnostics.Debug.WriteLine("Authentication state saved to preferences");
                        Console.WriteLine("Authentication state saved to preferences");
                        
                        System.Diagnostics.Debug.WriteLine("About to create MainApplicationPage in LoginPage...");
                        Console.WriteLine("About to create MainApplicationPage in LoginPage...");
                        
                        // Create a new AppShell and set it as the window page
                        var appShell = new AppShell(_authService);
                        
                        // Use the recommended approach to update the window page
                        if (Application.Current.Windows.Count > 0)
                        {
                            Application.Current.Windows[0].Page = appShell;
                        }
                        
                        System.Diagnostics.Debug.WriteLine("AppShell created and set as MainPage");
                        Console.WriteLine("AppShell created and set as MainPage");
                        
                        // Force the window to update by setting the page
                        if (Application.Current.Windows.Count > 0)
                        {
                            var window = Application.Current.Windows[0];
                            window.Page = appShell;
                            window.Title = "PhotoJobApp - Main";
                            
                            System.Diagnostics.Debug.WriteLine("Window page updated to AppShell");
                            Console.WriteLine("Window page updated to AppShell");
                            
                            // Force a UI refresh
                            MainThread.BeginInvokeOnMainThread(() => {
                                System.Diagnostics.Debug.WriteLine("Forcing UI refresh on main thread");
                                Console.WriteLine("Forcing UI refresh on main thread");
                                
                                // Try to force the window to be visible
                                window.Width = 1200;
                                window.Height = 800;
                                
                                System.Diagnostics.Debug.WriteLine($"Window page type after update: {window.Page?.GetType().Name}");
                                Console.WriteLine($"Window page type after update: {window.Page?.GetType().Name}");
                            });
                        }
                        
                        System.Diagnostics.Debug.WriteLine("Navigation completed");
                        Console.WriteLine("Navigation completed");
                    }
                    catch (Exception navEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Navigation error: {navEx.Message}");
                        Console.WriteLine($"Navigation error: {navEx.Message}");
                        await DisplayAlert("Navigation Error", $"Failed to navigate to main page: {navEx.Message}", "OK");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Authentication failed: {result.error}");
                    Console.WriteLine($"Authentication failed: {result.error}");
                    await DisplayAlert("Error", result.error ?? "Invalid email or password.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception during sign in: {ex.Message}");
                Console.WriteLine($"Exception during sign in: {ex.Message}");
                await DisplayAlert("Error", $"Sign in failed: {ex.Message}", "OK");
            }
        }

        private async void OnSignUpClicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("OnSignUpClicked called!");
            Console.WriteLine("OnSignUpClicked called!");
            
            // Get values directly from the Entry controls
            var email = EmailEntry?.Text ?? "";
            var password = PasswordEntry?.Text ?? "";
            
            System.Diagnostics.Debug.WriteLine($"Email from Entry: '{email}', Password length: {password?.Length ?? 0}");
            Console.WriteLine($"Email from Entry: '{email}', Password length: {password?.Length ?? 0}");
            
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("Error", "Please enter both email and password.", "OK");
                return;
            }

            if (password.Length < 6)
            {
                await DisplayAlert("Error", "Password must be at least 6 characters long.", "OK");
                return;
            }

            try
            {
                var name = email.Split('@')[0]; // Use email prefix as name
                var result = await _authService.SignUpAsync(email, password, name);
                if (result.success && result.user != null)
                {
                    await DisplayAlert("Success", "Account created successfully! You can now sign in.", "OK");
                }
                else
                {
                    await DisplayAlert("Error", result.error ?? "Failed to create account.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Sign up failed: {ex.Message}", "OK");
            }
        }

        private async void OnGoogleSignInClicked(object sender, EventArgs e)
        {
            try
            {
				System.Diagnostics.Debug.WriteLine("OnGoogleSignInClicked called!");
				Console.WriteLine("OnGoogleSignInClicked called!");
				
				// Disable the button to prevent multiple clicks
				if (sender is Button button)
				{
					button.IsEnabled = false;
					button.Text = "Signing in...";
				}
				else if (GoogleSignInButton != null)
				{
					GoogleSignInButton.IsEnabled = false;
					GoogleSignInButton.Text = "Signing in...";
				}
				
#if IOS
				// Track that Google Sign-In is running so we can recover if the app is terminated during 2FA
				var userDefaults = Foundation.NSUserDefaults.StandardUserDefaults;
				userDefaults.SetBool(true, GoogleSignInInProgressKey);
				userDefaults.SetDouble(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), GoogleSignInStartKey);
				
				// Only reset resume attempts when the user manually taps the button
				if (sender is Button)
				{
					userDefaults.SetInt(0, GoogleSignInResumeAttemptsKey);
				}
				
				userDefaults.Synchronize();
				
				System.Diagnostics.Debug.WriteLine("Set GoogleSignInInProgress flag and timestamp in NSUserDefaults");
				Console.WriteLine("Set GoogleSignInInProgress flag and timestamp in NSUserDefaults");

				// Hot Restart warning removed - let user try Google Sign-In
#endif

				var result = await _authService.SignInWithGoogleAsync();
                
                if (result.success && result.user != null)
                {
                    System.Diagnostics.Debug.WriteLine("Google Sign-In successful, attempting navigation...");
                    Console.WriteLine("Google Sign-In successful, attempting navigation...");
                    
                    try
                    {
                        // Save authentication state
                        Preferences.Set("IsAuthenticated", true);
                        Preferences.Set("UserId", result.user.Id);
                        Preferences.Set("UserEmail", result.user.Email);
                        
                        System.Diagnostics.Debug.WriteLine("Authentication state saved to preferences");
                        Console.WriteLine("Authentication state saved to preferences");
                        
                        // Create a new AppShell and set it as the window page
                        var appShell = new AppShell(_authService);
                        
                        // Use the recommended approach to update the window page
                        if (Application.Current.Windows.Count > 0)
                        {
                            Application.Current.Windows[0].Page = appShell;
                        }
                        
                        System.Diagnostics.Debug.WriteLine("AppShell created and set as MainPage");
                        Console.WriteLine("AppShell created and set as MainPage");

#if IOS
                        ResetGoogleSignInTracking();
#endif
                    }
                    catch (Exception navEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Navigation error: {navEx.Message}");
                        Console.WriteLine($"Navigation error: {navEx.Message}");
                        await DisplayAlert("Navigation Error", $"Failed to navigate to main page: {navEx.Message}", "OK");
                    }
                }
                else
                {
#if IOS
					ResetGoogleSignInTracking();
#endif
                    System.Diagnostics.Debug.WriteLine($"Google Sign-In failed: {result.error}");
                    Console.WriteLine($"Google Sign-In failed: {result.error}");
                    await DisplayAlert("Error", result.error ?? "Google Sign-In failed. Please try again.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception during Google Sign-In: {ex.Message}");
                Console.WriteLine($"Exception during Google Sign-In: {ex.Message}");
#if IOS
				ResetGoogleSignInTracking();
#endif
                await DisplayAlert("Error", $"Google Sign-In failed: {ex.Message}", "OK");
            }
            finally
            {
                // Re-enable the button
                if (sender is Button button)
                {
                    button.IsEnabled = true;
                    button.Text = "Sign in with Google";
                }
				else if (GoogleSignInButton != null)
				{
					GoogleSignInButton.IsEnabled = true;
					GoogleSignInButton.Text = "Sign in with Google";
				}
            }
        }

        private async void OnShowRedirectUrlClicked(object sender, EventArgs e)
        {
            try
            {
                var info = _authService.GetGoogleRedirectDebugInfo();
                if (string.IsNullOrWhiteSpace(info))
                {
                    info = "Redirect information is not available.";
                }

                await DisplayAlert("Google Redirect Info", info, "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnShowRedirectUrlClicked error: {ex.Message}");
                Console.WriteLine($"OnShowRedirectUrlClicked error: {ex.Message}");
                await DisplayAlert("Google Redirect Info", $"Failed to retrieve redirect information: {ex.Message}", "OK");
            }
        }

        private async void OnResendVerificationClicked(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("OnResendVerificationClicked called!");
                Console.WriteLine("OnResendVerificationClicked called!");

                var idToken = Preferences.Get("FirebaseIdToken", string.Empty);

                if (string.IsNullOrEmpty(idToken))
                {
                    await DisplayAlert("Email Verification", "Please sign in before requesting a verification email.", "OK");
                    return;
                }

                VerificationStatusLayout.IsVisible = true;
                VerificationStatusLabel.Text = "Sending verification email...";

                var success = await _authService.SendEmailVerificationAsync(idToken);

                if (success)
                {
                    VerificationStatusLabel.Text = "Verification email sent! Please check your inbox.";
                    await DisplayAlert("Email Verification", "Verification email sent successfully.", "OK");
                }
                else
                {
                    VerificationStatusLabel.Text = "Failed to send verification email. Please try again.";
                    await DisplayAlert("Email Verification", "Failed to send verification email. Please try again later.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnResendVerificationClicked error: {ex.Message}");
                Console.WriteLine($"OnResendVerificationClicked error: {ex.Message}");
                await DisplayAlert("Email Verification", $"An error occurred: {ex.Message}", "OK");
            }
        }

        private async void OnForgotPasswordClicked(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("OnForgotPasswordClicked called!");
                Console.WriteLine("OnForgotPasswordClicked called!");

                var forgotPasswordPage = new ForgotPasswordPage(_authService);

                // Present as a modal page to keep the login context
                await Navigation.PushModalAsync(new NavigationPage(forgotPasswordPage));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnForgotPasswordClicked error: {ex.Message}");
                Console.WriteLine($"OnForgotPasswordClicked error: {ex.Message}");
                await DisplayAlert("Forgot Password", $"Unable to open the forgot password page: {ex.Message}", "OK");
            }
        }

        private void OnCreateAccountClicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("OnCreateAccountClicked called!");
            Console.WriteLine("OnCreateAccountClicked called!");
            // For now, just call sign up directly
            OnSignUpClicked(sender, e);
        }
    }
} 