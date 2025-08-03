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
		
		// Check if user is authenticated
		var isAuthenticated = Preferences.Get("IsAuthenticated", false);
		System.Diagnostics.Debug.WriteLine($"IsAuthenticated: {isAuthenticated}");
		Console.WriteLine($"IsAuthenticated: {isAuthenticated}");
		
		// Create a new instance of FirebaseAuthService for now
		// We'll improve this later with proper DI
		_authService = new FirebaseAuthService();
		System.Diagnostics.Debug.WriteLine("FirebaseAuthService created");
		Console.WriteLine("FirebaseAuthService created");
		
		if (isAuthenticated)
		{
			System.Diagnostics.Debug.WriteLine("Creating AppShell");
			Console.WriteLine("Creating AppShell");
			return new Window(new AppShell(_authService));
		}
		else
		{
			System.Diagnostics.Debug.WriteLine("Creating LoginPage");
			Console.WriteLine("Creating LoginPage");
			return new Window(new LoginPage(_authService));
		}
	}
}