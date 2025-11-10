using PhotoJobApp.Services;

namespace PhotoJobApp;

public partial class AppShell : Shell
{
	private readonly FirebaseAuthService _authService;

	public AppShell(FirebaseAuthService authService)
	{
		System.Diagnostics.Debug.WriteLine("AppShell constructor called");
		Console.WriteLine("AppShell constructor called");
		
		InitializeComponent();
		_authService = authService;
		
		System.Diagnostics.Debug.WriteLine("AppShell InitializeComponent completed");
		Console.WriteLine("AppShell InitializeComponent completed");
		
		// Register all routes for navigation
		RegisterRoutes();
		
		System.Diagnostics.Debug.WriteLine("AppShell constructor completed");
		Console.WriteLine("AppShell constructor completed");
	}
	
	private void RegisterRoutes()
	{
		// Register routes for pages that are not defined as ShellContent in XAML
		// MainPage and CloudManagementPage are already defined in AppShell.xaml
		Routing.RegisterRoute("AddEditJobPage", typeof(AddEditJobPage));
		Routing.RegisterRoute("JobDetailPage", typeof(JobDetailPage));
		Routing.RegisterRoute("JobTypeManagementPage", typeof(JobTypeManagementPage));
		Routing.RegisterRoute("AddEditJobTypePage", typeof(AddEditJobTypePage));
		Routing.RegisterRoute("JobTypeDetailPage", typeof(JobTypeDetailPage));
		Routing.RegisterRoute("PhotoGalleryPage", typeof(PhotoGalleryPage));
		Routing.RegisterRoute("LoginPage", typeof(LoginPage));
		Routing.RegisterRoute("LogViewerPage", typeof(LogViewerPage));
		Routing.RegisterRoute("AccountPage", typeof(AccountPage));
	}

	private async void OnViewLogsClicked(object sender, EventArgs e)
	{
		try
		{
			await Shell.Current.GoToAsync("LogViewerPage");
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", $"Failed to open log viewer: {ex.Message}", "OK");
			System.Diagnostics.Debug.WriteLine($"Error navigating to LogViewerPage: {ex.Message}");
			Console.WriteLine($"Error navigating to LogViewerPage: {ex.Message}");
		}
	}

	private async void OnSignOutClicked(object sender, EventArgs e)
	{
		System.Diagnostics.Debug.WriteLine("Sign out clicked");
		Console.WriteLine("Sign out clicked");
		
		var result = await DisplayAlert("Sign Out", "Are you sure you want to sign out?", "Yes", "No");
		if (result)
		{
			System.Diagnostics.Debug.WriteLine("User confirmed sign out");
			Console.WriteLine("User confirmed sign out");
			
			try
			{
			await _authService.SignOutAsync();
				
				// Clear authentication state
				Preferences.Set("IsAuthenticated", false);
				Preferences.Remove("UserId");
				Preferences.Remove("UserEmail");
				
				System.Diagnostics.Debug.WriteLine("Authentication state cleared");
				Console.WriteLine("Authentication state cleared");
			
			// Create a new LoginPage and set it as the window page
			var loginPage = new LoginPage(_authService);
			
			// Use the recommended approach to update the window page
			if (Application.Current.Windows.Count > 0)
			{
				Application.Current.Windows[0].Page = loginPage;
					System.Diagnostics.Debug.WriteLine("Window page updated to LoginPage");
					Console.WriteLine("Window page updated to LoginPage");
				}
				
				System.Diagnostics.Debug.WriteLine("Sign out completed successfully");
				Console.WriteLine("Sign out completed successfully");
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error during sign out: {ex.Message}");
				Console.WriteLine($"Error during sign out: {ex.Message}");
				
				// Even if sign out fails, try to navigate to login page
				try
				{
					var loginPage = new LoginPage(_authService);
			if (Application.Current.Windows.Count > 0)
			{
				Application.Current.Windows[0].Page = loginPage;
						System.Diagnostics.Debug.WriteLine("Fallback navigation to LoginPage completed");
						Console.WriteLine("Fallback navigation to LoginPage completed");
					}
				}
				catch (Exception navEx)
				{
					System.Diagnostics.Debug.WriteLine($"Fallback navigation failed: {navEx.Message}");
					Console.WriteLine($"Fallback navigation failed: {navEx.Message}");
					await DisplayAlert("Error", "Sign out completed but navigation failed. Please restart the app.", "OK");
				}
			}
		}
	}
}
