using PhotoJobApp.Services;

namespace PhotoJobApp
{
    public partial class LoginPage : ContentPage
    {
        private readonly FirebaseAuthService _authService;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public LoginPage(FirebaseAuthService authService)
        {
            InitializeComponent();
            _authService = authService;
            BindingContext = this;
        }

        private async void OnSignInClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                await DisplayAlert("Error", "Please enter both email and password.", "OK");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("Starting sign in process...");
                Console.WriteLine("Starting sign in process...");
                
                var result = await _authService.SignInAsync(Email, Password);
                System.Diagnostics.Debug.WriteLine($"Sign in result: success={result.success}, user={result.user != null}, error={result.error}");
                Console.WriteLine($"Sign in result: success={result.success}, user={result.user != null}, error={result.error}");
                
                if (result.success && result.user != null)
                {
                    System.Diagnostics.Debug.WriteLine("Authentication successful, attempting navigation...");
                    Console.WriteLine("Authentication successful, attempting navigation...");
                    
                    // Use the proper .NET MAUI navigation approach
                    if (Application.Current?.MainPage is AppShell currentShell)
                    {
                        // If we're already in a Shell, navigate within it
                        await Shell.Current.GoToAsync("//MainPage");
                    }
                    else
                    {
                        // Create a new AppShell and set it as the main page
                        var appShell = new AppShell(_authService);
                        Application.Current!.MainPage = appShell;
                    }
                    
                    System.Diagnostics.Debug.WriteLine("Navigation completed");
                    Console.WriteLine("Navigation completed");
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
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                await DisplayAlert("Error", "Please enter both email and password.", "OK");
                return;
            }

            if (Password.Length < 6)
            {
                await DisplayAlert("Error", "Password must be at least 6 characters long.", "OK");
                return;
            }

            try
            {
                var name = Email.Split('@')[0]; // Use email prefix as name
                var result = await _authService.SignUpAsync(Email, Password, name);
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
            await DisplayAlert("Coming Soon", "Google Sign-In will be available soon!", "OK");
        }

        private void OnCreateAccountClicked(object sender, EventArgs e)
        {
            // For now, just call sign up directly
            OnSignUpClicked(sender, e);
        }
    }
} 