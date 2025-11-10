using PhotoJobApp.Services;
#if IOS
using Foundation;
using Microsoft.Maui.Authentication;
#endif

namespace PhotoJobApp;

public partial class App : Application
{
	private FirebaseAuthService? _authService;

	public App()
	{
		InitializeComponent();
		System.Diagnostics.Debug.WriteLine("App constructor called");
		Console.WriteLine("App constructor called");
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		System.Diagnostics.Debug.WriteLine("CreateWindow called");
		Console.WriteLine("CreateWindow called");
		
		try
		{
			// Get FirebaseAuthService from DI container (includes GoogleSignInService on iOS)
			_authService = Handler?.MauiContext?.Services.GetService<FirebaseAuthService>();
			if (_authService == null)
			{
				// Fallback: create without DI (for compatibility)
				System.Diagnostics.Debug.WriteLine("FirebaseAuthService not found in DI, creating fallback instance");
				Console.WriteLine("FirebaseAuthService not found in DI, creating fallback instance");
			_authService = new FirebaseAuthService();
			}
			else
			{
				System.Diagnostics.Debug.WriteLine("FirebaseAuthService retrieved from DI container");
				Console.WriteLine("FirebaseAuthService retrieved from DI container");
			}
			
			var isAuthenticated = false;

			if (_authService != null)
			{
				try
				{
					isAuthenticated = _authService.IsAuthenticated();

					if (!isAuthenticated)
					{
						var storedUser = _authService.GetCurrentUserAsync().ConfigureAwait(false).GetAwaiter().GetResult();
						if (storedUser != null)
						{
							isAuthenticated = true;
							Preferences.Set("IsAuthenticated", true);
						}
					}
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"Error evaluating authentication state: {ex.Message}");
					Console.WriteLine($"Error evaluating authentication state: {ex.Message}");
					isAuthenticated = Preferences.Get("IsAuthenticated", false);
				}
			}
			else
			{
				isAuthenticated = Preferences.Get("IsAuthenticated", false);
			}

			System.Diagnostics.Debug.WriteLine($"IsAuthenticated: {isAuthenticated}");
			Console.WriteLine($"IsAuthenticated: {isAuthenticated}");
			
			Window window;
			
			if (isAuthenticated)
			{
				System.Diagnostics.Debug.WriteLine("Creating AppShell for authenticated user");
				Console.WriteLine("Creating AppShell for authenticated user");
				try
				{
					var appShell = new AppShell(_authService);
					window = new Window(appShell);
					System.Diagnostics.Debug.WriteLine("AppShell window created successfully");
					Console.WriteLine("AppShell window created successfully");
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"Error creating AppShell: {ex.Message}");
					Console.WriteLine($"Error creating AppShell: {ex.Message}");
					System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
					Console.WriteLine($"Stack trace: {ex.StackTrace}");
					// Fallback to login page
					var loginPage = new LoginPage(_authService);
					window = new Window(loginPage);
				}
			}
			else
			{
				System.Diagnostics.Debug.WriteLine("Creating LoginPage");
				Console.WriteLine("Creating LoginPage");
				try
				{
					var loginPage = new LoginPage(_authService);
					window = new Window(loginPage);
					System.Diagnostics.Debug.WriteLine("LoginPage window created successfully");
					Console.WriteLine("LoginPage window created successfully");
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"Error creating LoginPage: {ex.Message}");
					Console.WriteLine($"Error creating LoginPage: {ex.Message}");
					System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
					Console.WriteLine($"Stack trace: {ex.StackTrace}");
					// Fallback to main page
					window = CreateMainPageWindow();
				}
			}
			
#if IOS
			// Listen for notification from AppDelegate when callback is found
			NSNotificationCenter.DefaultCenter.AddObserver(
				new NSString("PendingOAuthCallbackFound"),
				async (notification) =>
				{
					System.Diagnostics.Debug.WriteLine("Received PendingOAuthCallbackFound notification - checking for callback");
					Console.WriteLine("Received PendingOAuthCallbackFound notification - checking for callback");
					await Task.Delay(500); // Small delay to ensure URL is fully stored
					await CheckAndProcessPendingCallback();
				});
			
			// Check for pending OAuth callback (in case app was terminated during sign-in)
			// Check immediately and then periodically
			_ = Task.Run(async () =>
			{
				try
				{
					// Check immediately (no delay)
					await CheckAndProcessPendingCallback();
					
					// Also check after a short delay (in case callback arrives slightly later)
					await Task.Delay(1000);
					await CheckAndProcessPendingCallback();
					
					// Check one more time after 3 seconds (for callbacks that arrive after app restart)
					await Task.Delay(2000);
					await CheckAndProcessPendingCallback();
					
					// Check again after 5 seconds total (for very delayed callbacks)
					await Task.Delay(2000);
					await CheckAndProcessPendingCallback();
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"Error checking for pending OAuth callback: {ex.Message}");
					Console.WriteLine($"Error checking for pending OAuth callback: {ex.Message}");
				}
			});
			
			// Also check when window resumes (in case callback arrives while app is in background)
			// This implements AuthStateListener pattern by checking auth state on resume
			window.Resumed += (sender, e) =>
			{
				_ = Task.Run(async () =>
				{
					System.Diagnostics.Debug.WriteLine("Window resumed - aggressively checking for pending callback and auth state");
					Console.WriteLine("Window resumed - aggressively checking for pending callback and auth state");
					
					// Aggressively check for pending OAuth callback multiple times
					// This handles callbacks that arrive after the app resumes
					for (int i = 0; i < 5; i++)
					{
						await Task.Delay(i * 500); // 0ms, 500ms, 1000ms, 1500ms, 2000ms
						await CheckAndProcessPendingCallback();
					}
					
					// Then verify authentication state (AuthStateListener pattern)
					if (_authService != null)
					{
						try
						{
							var (isAuthenticated, user) = await _authService.CheckAuthStateAsync();
							if (isAuthenticated && user != null)
							{
								System.Diagnostics.Debug.WriteLine($"AuthStateListener: User is authenticated ({user.Email})");
								Console.WriteLine($"AuthStateListener: User is authenticated ({user.Email})");
								
								// If we're on login page but user is authenticated, navigate to main app
								MainThread.BeginInvokeOnMainThread(async () =>
								{
									if (window.Page is LoginPage)
									{
										System.Diagnostics.Debug.WriteLine("AuthStateListener: Navigating from LoginPage to AppShell");
										Console.WriteLine("AuthStateListener: Navigating from LoginPage to AppShell");
										var appShell = new AppShell(_authService);
										window.Page = appShell;
									}
								});
							}
							else
							{
								System.Diagnostics.Debug.WriteLine("AuthStateListener: User is not authenticated");
								Console.WriteLine("AuthStateListener: User is not authenticated");
								
								// If we're on main app but user is not authenticated, navigate to login
								MainThread.BeginInvokeOnMainThread(async () =>
								{
									if (window.Page is AppShell)
									{
										System.Diagnostics.Debug.WriteLine("AuthStateListener: Navigating from AppShell to LoginPage");
										Console.WriteLine("AuthStateListener: Navigating from AppShell to LoginPage");
										var loginPage = new LoginPage(_authService);
										window.Page = loginPage;
									}
								});
							}
						}
						catch (Exception authEx)
						{
							System.Diagnostics.Debug.WriteLine($"Error checking auth state on resume: {authEx.Message}");
							Console.WriteLine($"Error checking auth state on resume: {authEx.Message}");
						}
					}
				});
			};
#endif
			
			// Configure window to ensure it's visible
			window.Title = "PhotoJobApp";
			window.Width = 1200;
			window.Height = 800;
			window.MinimumWidth = 800;
			window.MinimumHeight = 600;
			
			// Force window to be visible and not hidden - position it in the center of the screen
			var screenWidth = DeviceDisplay.Current.MainDisplayInfo.Width;
			var screenHeight = DeviceDisplay.Current.MainDisplayInfo.Height;
			window.X = (screenWidth - window.Width) / 2;
			window.Y = (screenHeight - window.Height) / 2;
			
			// Set window to be visible and not minimized
			window.MaximumWidth = double.PositiveInfinity;
			window.MaximumHeight = double.PositiveInfinity;
			
			// Ensure window is visible and focused
			window.Created += (sender, e) => {
				System.Diagnostics.Debug.WriteLine("Window Created event fired");
				Console.WriteLine("Window Created event fired");
				
				// Force the window to be visible on the main thread
				MainThread.BeginInvokeOnMainThread(async () => {
					System.Diagnostics.Debug.WriteLine("Forcing window to be visible on main thread");
					Console.WriteLine("Forcing window to be visible on main thread");
					
					// Try to bring window to front
					try
					{
						System.Diagnostics.Debug.WriteLine("Window should be visible now");
						Console.WriteLine("Window should be visible now");
						
						// Check if the page is loaded
						if (window.Page != null)
						{
							System.Diagnostics.Debug.WriteLine($"Window page type: {window.Page.GetType().Name}");
							Console.WriteLine($"Window page type: {window.Page.GetType().Name}");
						}
						else
						{
							System.Diagnostics.Debug.WriteLine("Window page is null");
							Console.WriteLine("Window page is null");
						}
						
						// Force window to be visible and bring to front
						// Note: Window.Show() doesn't exist in MAUI, window is automatically shown
						
						// Additional visibility checks
						System.Diagnostics.Debug.WriteLine($"Window Width: {window.Width}, Height: {window.Height}");
						Console.WriteLine($"Window Width: {window.Width}, Height: {window.Height}");
						System.Diagnostics.Debug.WriteLine($"Window X: {window.X}, Y: {window.Y}");
						Console.WriteLine($"Window X: {window.X}, Y: {window.Y}");
						
						// Wait a moment and try to bring window to front again
						await Task.Delay(100);
						// Note: Window.Show() doesn't exist in MAUI, window is automatically shown
						
						System.Diagnostics.Debug.WriteLine("Window positioning completed");
						Console.WriteLine("Window positioning completed");
						
						// Try to force window to be visible by changing its position slightly
						window.X += 1;
						window.Y += 1;
						await Task.Delay(50);
						window.X -= 1;
						window.Y -= 1;
						
						System.Diagnostics.Debug.WriteLine("Window repositioned to force visibility");
						Console.WriteLine("Window repositioned to force visibility");
					}
					catch (Exception ex)
					{
						System.Diagnostics.Debug.WriteLine($"Error in window visibility: {ex.Message}");
						Console.WriteLine($"Error in window visibility: {ex.Message}");
					}
				});
			};
			
			window.Activated += (sender, e) => {
				System.Diagnostics.Debug.WriteLine("Window Activated event fired");
				Console.WriteLine("Window Activated event fired");
			};
			
			// Note: window.Resumed handler is already set up above in the #if IOS block
			
			window.Stopped += (sender, e) => {
				System.Diagnostics.Debug.WriteLine("Window Stopped event fired");
				Console.WriteLine("Window Stopped event fired");
			};
			
			window.Destroying += (sender, e) => {
				System.Diagnostics.Debug.WriteLine("Window Destroying event fired");
				Console.WriteLine("Window Destroying event fired");
			};
			
			System.Diagnostics.Debug.WriteLine("Window configured and returning");
			Console.WriteLine("Window configured and returning");
			
			return window;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error in CreateWindow: {ex.Message}");
			Console.WriteLine($"Error in CreateWindow: {ex.Message}");
			System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
			Console.WriteLine($"Stack trace: {ex.StackTrace}");
			
			// Fallback to main page
			return CreateMainPageWindow();
		}
	}
	
	private Window CreateMainPageWindow()
	{
		System.Diagnostics.Debug.WriteLine("Creating fallback MainPage window");
		Console.WriteLine("Creating fallback MainPage window");
		
		try
		{
			var mainPage = new MainPage();
			var window = new Window(mainPage);
			
			// Configure fallback window
			window.Title = "PhotoJobApp - Main Page";
			window.Width = 1200;
			window.Height = 800;
			window.MinimumWidth = 800;
			window.MinimumHeight = 600;
			
			// Position in center of screen
			var screenWidth = DeviceDisplay.Current.MainDisplayInfo.Width;
			var screenHeight = DeviceDisplay.Current.MainDisplayInfo.Height;
			window.X = (screenWidth - window.Width) / 2;
			window.Y = (screenHeight - window.Height) / 2;
			
			// Force window to be visible
			window.Created += (sender, e) => {
				System.Diagnostics.Debug.WriteLine("Fallback window created");
				Console.WriteLine("Fallback window created");
				// Note: Window.Show() doesn't exist in MAUI, window is automatically shown
			};
			
			return window;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error creating MainPage: {ex.Message}");
			Console.WriteLine($"Error creating MainPage: {ex.Message}");
			
			// Final fallback - simple test page
			var testPage = new ContentPage
			{
				Title = "PhotoJobApp",
				Content = new StackLayout
				{
					Children = {
						new Label {
							Text = "PhotoJobApp is working!",
							HorizontalOptions = LayoutOptions.Center,
							VerticalOptions = LayoutOptions.Center,
							FontSize = 24
						},
						new Label {
							Text = "Some components failed to load, but the app is running.",
							HorizontalOptions = LayoutOptions.Center,
							VerticalOptions = LayoutOptions.Center,
							FontSize = 14,
							TextColor = Colors.Gray
						}
					}
				}
			};
			
			var window = new Window(testPage);
			window.Title = "PhotoJobApp - Test";
			window.Width = 800;
			window.Height = 600;
			
			// Position in center of screen
			var screenWidth = DeviceDisplay.Current.MainDisplayInfo.Width;
			var screenHeight = DeviceDisplay.Current.MainDisplayInfo.Height;
			window.X = (screenWidth - window.Width) / 2;
			window.Y = (screenHeight - window.Height) / 2;
			
			return window;
		}
	}
	
#if IOS
	private async Task CheckAndProcessPendingCallback()
	{
		try
		{
			// Check NSUserDefaults for pending callback (most reliable)
			var userDefaults = NSUserDefaults.StandardUserDefaults;
			userDefaults.Synchronize(); // Ensure we have latest data
			
			var pendingCallback = userDefaults.StringForKey("PendingOAuthCallback");
			var signInInProgress = userDefaults.BoolForKey("GoogleSignInInProgress");
			
			if (!string.IsNullOrEmpty(pendingCallback) && signInInProgress)
			{
				System.Diagnostics.Debug.WriteLine($"✓ Found pending OAuth callback: {pendingCallback.Substring(0, Math.Min(100, pendingCallback.Length))}...");
				Console.WriteLine($"✓ Found pending OAuth callback: {pendingCallback.Substring(0, Math.Min(100, pendingCallback.Length))}...");
				
				// Clear the pending callback flag immediately to prevent duplicate processing
				userDefaults.RemoveObject("PendingOAuthCallback");
				userDefaults.SetBool(false, "GoogleSignInInProgress");
				userDefaults.Synchronize();
				
				// Process the callback on the main thread
				MainThread.BeginInvokeOnMainThread(async () =>
				{
					try
					{
						// Parse the callback URL
						if (Uri.TryCreate(pendingCallback, UriKind.Absolute, out var callbackUri))
						{
							// Extract query parameters and fragment
							var fullUrl = callbackUri.ToString();
							var queryStart = fullUrl.IndexOf('?');
							var fragmentStart = fullUrl.IndexOf('#');
							
							// Parse query string manually (since System.Web might not be available)
							var queryParams = new Dictionary<string, string>();
							if (queryStart >= 0)
							{
								var queryEnd = fragmentStart >= 0 ? fragmentStart : fullUrl.Length;
								var queryString = fullUrl.Substring(queryStart + 1, queryEnd - queryStart - 1);
								foreach (var param in queryString.Split('&'))
								{
									var parts = param.Split('=');
									if (parts.Length == 2)
									{
										queryParams[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
									}
								}
							}
							
							// Parse fragment (tokens are often in the fragment)
							if (fragmentStart >= 0)
							{
								var fragment = fullUrl.Substring(fragmentStart + 1);
								foreach (var param in fragment.Split('&'))
								{
									var parts = param.Split('=');
									if (parts.Length == 2)
									{
										queryParams[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
									}
								}
							}
							
							// Check for id_token or other token parameters
							var idToken = queryParams.GetValueOrDefault("id_token") 
								?? queryParams.GetValueOrDefault("idToken") 
								?? queryParams.GetValueOrDefault("access_token");
							
							if (!string.IsNullOrEmpty(idToken))
							{
								System.Diagnostics.Debug.WriteLine("Processing OAuth callback with ID token...");
								Console.WriteLine("Processing OAuth callback with ID token...");
								
								if (_authService != null)
								{
									var result = await _authService.SignInWithGoogleIdTokenAsync(idToken);
									if (result.success && result.user != null)
									{
										// Save authentication state
										Preferences.Set("IsAuthenticated", true);
										Preferences.Set("UserId", result.user.Id);
										Preferences.Set("UserEmail", result.user.Email ?? "");
										
										// Navigate to main app
										if (Application.Current?.Windows != null && Application.Current.Windows.Count > 0)
										{
											var appShell = new AppShell(_authService);
											Application.Current.Windows[0].Page = appShell;
											System.Diagnostics.Debug.WriteLine("✓ Successfully completed Google Sign-In after app restart");
											Console.WriteLine("✓ Successfully completed Google Sign-In after app restart");
										}
									}
									else
									{
										System.Diagnostics.Debug.WriteLine($"Sign-in failed: {result.error}");
										Console.WriteLine($"Sign-in failed: {result.error}");
									}
								}
							}
							else
							{
								System.Diagnostics.Debug.WriteLine("No token found in callback URL - may need to retry sign-in");
								Console.WriteLine("No token found in callback URL - may need to retry sign-in");
								System.Diagnostics.Debug.WriteLine($"Full callback URL: {pendingCallback}");
								Console.WriteLine($"Full callback URL: {pendingCallback}");
							}
						}
					}
					catch (Exception ex)
					{
						System.Diagnostics.Debug.WriteLine($"Error processing pending OAuth callback: {ex.Message}");
						Console.WriteLine($"Error processing pending OAuth callback: {ex.Message}");
						System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
						Console.WriteLine($"Stack trace: {ex.StackTrace}");
					}
				});
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error checking for pending OAuth callback: {ex.Message}");
			Console.WriteLine($"Error checking for pending OAuth callback: {ex.Message}");
		}
	}
#endif
}