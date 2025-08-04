using PhotoJobApp.Services;

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
			// Check if user is authenticated and if they chose to be remembered
			var isAuthenticated = Preferences.Get("IsAuthenticated", false);
			var rememberMe = Preferences.Get("RememberMe", false);
			
			System.Diagnostics.Debug.WriteLine($"IsAuthenticated: {isAuthenticated}, RememberMe: {rememberMe}");
			Console.WriteLine($"IsAuthenticated: {isAuthenticated}, RememberMe: {rememberMe}");
			
			// Create a new instance of FirebaseAuthService
			_authService = new FirebaseAuthService();
			System.Diagnostics.Debug.WriteLine("FirebaseAuthService created");
			Console.WriteLine("FirebaseAuthService created");
			
			Window window;
			
			// Only auto-login if authenticated AND remember me is checked
			if (isAuthenticated && rememberMe)
			{
				System.Diagnostics.Debug.WriteLine("Creating AppShell for authenticated user with remember me");
				Console.WriteLine("Creating AppShell for authenticated user with remember me");
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
				System.Diagnostics.Debug.WriteLine("Creating LoginPage (not authenticated or remember me not checked)");
				Console.WriteLine("Creating LoginPage (not authenticated or remember me not checked)");
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
			
			window.Resumed += (sender, e) => {
				System.Diagnostics.Debug.WriteLine("Window Resumed event fired");
				Console.WriteLine("Window Resumed event fired");
			};
			
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
}