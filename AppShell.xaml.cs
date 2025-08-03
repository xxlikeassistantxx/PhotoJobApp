using PhotoJobApp.Services;

namespace PhotoJobApp;

public partial class AppShell : Shell
{
	private readonly FirebaseAuthService _authService;

	public AppShell(FirebaseAuthService authService)
	{
		InitializeComponent();
		_authService = authService;
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
			
			await _authService.SignOutAsync();
			await Shell.Current.GoToAsync("//LoginPage");
			
			System.Diagnostics.Debug.WriteLine("Sign out completed");
			Console.WriteLine("Sign out completed");
		}
	}
}
