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
#if IOS
		private const string PendingOAuthCallbackKey = "PendingOAuthCallback";
		private const string GoogleSignInInProgressKey = "GoogleSignInInProgress";
		private const string GoogleSignInStartKey = "GoogleSignInStartedUtc";
		private const string GoogleSignInResumeAttemptsKey = "GoogleSignInResumeAttempts";
		private bool _resumeAttempted;
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
                PasswordEntry.TextChanged += (sender, e) => {
                    System.Diagnostics.Debug.WriteLine($"Password text changed: length {e.NewTextValue?.Length ?? 0}");
                    Console.WriteLine($"Password text changed: length {e.NewTextValue?.Length ?? 0}");
                    // Only save password if "Remember Me" is checked
                    if (RememberMeCheckBox?.IsChecked == true && !string.IsNullOrWhiteSpace(e.NewTextValue))
                    {
                        Preferences.Set("LoginPage_Password", e.NewTextValue);
                    }
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
            
#if IOS
			_resumeAttempted = false;
#endif

            // FIRST: Check for pending OAuth callback (e.g., after returning from 2FA app)
            // This is critical for Google Sign-In restoration after app termination
            // Check multiple times with delays to catch callbacks that arrive after page appears
            #if IOS
            _ = Task.Run(async () =>
            {
                try
                {
                    // Check immediately
                    await CheckForPendingCallback();
                    
                    // Check again after short delays (callbacks might arrive slightly after page appears)
                    await Task.Delay(500);
                    await CheckForPendingCallback();
                    
                    await Task.Delay(1000);
                    await CheckForPendingCallback();
                    
                    await Task.Delay(1500);
                    await CheckForPendingCallback();

					// Give the system a moment to resume WebAuthenticator before attempting recovery
					await Task.Delay(500);
					await AttemptResumeGoogleSignInAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error checking for pending OAuth callback: {ex.Message}");
                    Console.WriteLine($"Error checking for pending OAuth callback: {ex.Message}");
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
                    
                    // Verify the authentication state is still valid
                    var (isAuthenticated, user) = await _authService.CheckAuthStateAsync();
                    
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
                    System.Diagnostics.Debug.WriteLine("No previous authentication found");
                    Console.WriteLine("No previous authentication found");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking authentication state in OnAppearing: {ex.Message}");
                Console.WriteLine($"Error checking authentication state in OnAppearing: {ex.Message}");
            }
        }
        
        #if IOS
        private async Task CheckForPendingCallback()
        {
            try
            {
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
			if (_resumeAttempted)
			{
				return;
			}
			
			_resumeAttempted = true;
			
			try
			{
				var userDefaults = Foundation.NSUserDefaults.StandardUserDefaults;
				userDefaults.Synchronize();
				
				var signInInProgress = userDefaults.BoolForKey(GoogleSignInInProgressKey);
				var pendingCallback = userDefaults.StringForKey(PendingOAuthCallbackKey);
				
				if (!signInInProgress || !string.IsNullOrEmpty(pendingCallback))
				{
					System.Diagnostics.Debug.WriteLine("AttemptResumeGoogleSignInAsync: Nothing to resume (either not in progress or callback already present).");
					Console.WriteLine("AttemptResumeGoogleSignInAsync: Nothing to resume (either not in progress or callback already present).");
					return;
				}
				
				var resumeAttempts = (int)userDefaults.IntForKey(GoogleSignInResumeAttemptsKey);
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
				
				var elapsed = DateTimeOffset.UtcNow - startedAt;
				System.Diagnostics.Debug.WriteLine($"AttemptResumeGoogleSignInAsync: Sign-in in progress for {elapsed.TotalSeconds:F1} seconds, resume attempts = {resumeAttempts}");
				Console.WriteLine($"AttemptResumeGoogleSignInAsync: Sign-in in progress for {elapsed.TotalSeconds:F1} seconds, resume attempts = {resumeAttempts}");
				
				if (elapsed > TimeSpan.FromMinutes(5))
				{
					System.Diagnostics.Debug.WriteLine("AttemptResumeGoogleSignInAsync: Sign-in attempt timed out. Clearing flags.");
					Console.WriteLine("AttemptResumeGoogleSignInAsync: Sign-in attempt timed out. Clearing flags.");
					
					ResetGoogleSignInTracking();
					
					await MainThread.InvokeOnMainThreadAsync(async () =>
					{
						await DisplayAlert("Google Sign-In", "Your previous Google sign-in attempt timed out. Please try again.", "OK");
					});
					
					return;
				}
				
				if (resumeAttempts >= 3)
				{
					System.Diagnostics.Debug.WriteLine("AttemptResumeGoogleSignInAsync: Maximum automatic resume attempts reached.");
					Console.WriteLine("AttemptResumeGoogleSignInAsync: Maximum automatic resume attempts reached.");
					return;
				}
				
				userDefaults.SetInt(resumeAttempts + 1, GoogleSignInResumeAttemptsKey);
				userDefaults.Synchronize();
				
				System.Diagnostics.Debug.WriteLine("AttemptResumeGoogleSignInAsync: Relaunching Google Sign-In automatically.");
				Console.WriteLine("AttemptResumeGoogleSignInAsync: Relaunching Google Sign-In automatically.");
				
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					try
					{
						if (GoogleSignInButton != null)
						{
							GoogleSignInButton.IsEnabled = false;
							GoogleSignInButton.Text = "Signing in...";
						}
						
						OnGoogleSignInClicked(null, EventArgs.Empty);
					}
					catch (Exception ex)
					{
						System.Diagnostics.Debug.WriteLine($"AttemptResumeGoogleSignInAsync: Error relaunching Google Sign-In: {ex.Message}");
						Console.WriteLine($"AttemptResumeGoogleSignInAsync: Error relaunching Google Sign-In: {ex.Message}");
					}
				});
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"AttemptResumeGoogleSignInAsync: {ex.Message}");
				Console.WriteLine($"AttemptResumeGoogleSignInAsync: {ex.Message}");
			}
		}
		
        private async Task ProcessPendingOAuthCallback(string callbackUrl)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Processing pending OAuth callback: {callbackUrl.Substring(0, Math.Min(100, callbackUrl.Length))}...");
                Console.WriteLine($"Processing pending OAuth callback: {callbackUrl.Substring(0, Math.Min(100, callbackUrl.Length))}...");
                
                // Clear the pending callback flag immediately to prevent duplicate processing
                var userDefaults = Foundation.NSUserDefaults.StandardUserDefaults;
				userDefaults.RemoveObject(PendingOAuthCallbackKey);
				userDefaults.SetBool(false, GoogleSignInInProgressKey);
				userDefaults.RemoveObject(GoogleSignInStartKey);
				userDefaults.SetInt(0, GoogleSignInResumeAttemptsKey);
                userDefaults.Synchronize();
                
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
                            
                            // Navigate to main app
                            await NavigateToMainApp();
                            return;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Google Sign-In failed: {result.error}");
                            Console.WriteLine($"Google Sign-In failed: {result.error}");
                            await DisplayAlert("Sign-In Error", result.error ?? "Failed to sign in with Google", "OK");
                            return;
                        }
                    }
                    else if (!string.IsNullOrEmpty(code))
                    {
                        // Firebase sometimes returns an authorization code instead of id_token
                        // This needs to be exchanged for tokens
                        System.Diagnostics.Debug.WriteLine("Found authorization code in callback, but code exchange not implemented");
                        Console.WriteLine("Found authorization code in callback, but code exchange not implemented");
                        await DisplayAlert("Sign-In Error", "Authorization code received but not yet supported. Please try again.", "OK");
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
				userDefaults.Synchronize();
				
				System.Diagnostics.Debug.WriteLine("ResetGoogleSignInTracking: Cleared Google sign-in tracking flags.");
				Console.WriteLine("ResetGoogleSignInTracking: Cleared Google sign-in tracking flags.");
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