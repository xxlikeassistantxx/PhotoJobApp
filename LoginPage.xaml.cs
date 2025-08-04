using PhotoJobApp.Services;

namespace PhotoJobApp
{
    public partial class LoginPage : ContentPage
    {
        private readonly FirebaseAuthService _authService;

        public LoginPage(FirebaseAuthService authService)
        {
            System.Diagnostics.Debug.WriteLine("LoginPage constructor called");
            Console.WriteLine("LoginPage constructor called");
            
            try
            {
                InitializeComponent();
                System.Diagnostics.Debug.WriteLine("LoginPage InitializeComponent completed");
                Console.WriteLine("LoginPage InitializeComponent completed");
                
                _authService = authService;
                
                // Check if user had "Remember Me" enabled previously
                var rememberMe = Preferences.Get("RememberMe", false);
                RememberMeCheckBox.IsChecked = rememberMe;
                
                System.Diagnostics.Debug.WriteLine($"Remember Me preference: {rememberMe}");
                Console.WriteLine($"Remember Me preference: {rememberMe}");
                
                System.Diagnostics.Debug.WriteLine("LoginPage constructor completed");
                Console.WriteLine("LoginPage constructor completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LoginPage constructor: {ex.Message}");
                Console.WriteLine($"Error in LoginPage constructor: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            
            // Wire up button click events directly
            if (EmailEntry != null)
            {
                EmailEntry.TextChanged += (sender, e) => {
                    System.Diagnostics.Debug.WriteLine($"Email text changed: '{e.NewTextValue}'");
                    Console.WriteLine($"Email text changed: '{e.NewTextValue}'");
                };
            }
            
            if (PasswordEntry != null)
            {
                PasswordEntry.TextChanged += (sender, e) => {
                    System.Diagnostics.Debug.WriteLine($"Password text changed: length {e.NewTextValue?.Length ?? 0}");
                    Console.WriteLine($"Password text changed: length {e.NewTextValue?.Length ?? 0}");
                };
            }
        }

        private async void OnSignInClicked(object sender, EventArgs e)
        {
            var email = EmailEntry.Text?.Trim();
            var password = PasswordEntry.Text?.Trim();
            var rememberMe = RememberMeCheckBox.IsChecked;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Error", "Please enter both email and password", "OK");
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
                    // Check if email is verified
                    var isEmailVerified = await _authService.IsEmailVerifiedAsync(result.user.IdToken);
                    
                    if (!isEmailVerified)
                    {
                        // Show verification required message
                        VerificationStatusLayout.IsVisible = true;
                        VerificationStatusLabel.Text = $"Please verify your email address ({email}) before signing in.";
                        await DisplayAlert("Email Verification Required", 
                            "Please check your email and click the verification link before signing in.", "OK");
                        return;
                    }

                    System.Diagnostics.Debug.WriteLine("Authentication successful, attempting navigation...");
                    Console.WriteLine("Authentication successful, attempting navigation...");
                    
                    try
                    {
                        // Save authentication state
                        Preferences.Set("IsAuthenticated", true);
                        Preferences.Set("UserId", result.user.Id);
                        Preferences.Set("UserEmail", result.user.Email);
                        
                        // Save remember me preference
                        Preferences.Set("RememberMe", rememberMe);
                        
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
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error during navigation: {ex.Message}");
                        Console.WriteLine($"Error during navigation: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                        Console.WriteLine($"Stack trace: {ex.StackTrace}");
                        await DisplayAlert("Error", $"Navigation failed: {ex.Message}", "OK");
                    }
                }
                else
                {
                    var errorMessage = result.error ?? "Sign in failed. Please check your credentials.";
                    await DisplayAlert("Sign In Failed", errorMessage, "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during sign in: {ex.Message}");
                Console.WriteLine($"Error during sign in: {ex.Message}");
                await DisplayAlert("Error", $"Sign in failed: {ex.Message}", "OK");
            }
        }

        private async void OnResendVerificationClicked(object sender, EventArgs e)
        {
            try
            {
                var email = EmailEntry.Text?.Trim();
                if (string.IsNullOrEmpty(email))
                {
                    await DisplayAlert("Error", "Please enter your email address first", "OK");
                    return;
                }

                // Get the current user's ID token
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    await DisplayAlert("Error", "Please sign in first to resend verification email", "OK");
                    return;
                }

                var success = await _authService.SendEmailVerificationAsync(currentUser.IdToken);
                if (success)
                {
                    await DisplayAlert("Success", "Verification email sent! Please check your inbox.", "OK");
                    VerificationStatusLabel.Text = "Verification email sent! Please check your inbox.";
                }
                else
                {
                    await DisplayAlert("Error", "Failed to send verification email. Please try again.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to resend verification email: {ex.Message}", "OK");
            }
        }

        private async void OnSignUpClicked(object sender, EventArgs e)
        {
            var email = EmailEntry.Text?.Trim();
            var password = PasswordEntry.Text?.Trim();
            var rememberMe = RememberMeCheckBox.IsChecked;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Error", "Please enter both email and password", "OK");
                return;
            }

            if (password.Length < 6)
            {
                await DisplayAlert("Error", "Password must be at least 6 characters long", "OK");
                return;
            }

            try
            {
                var result = await _authService.SignUpAsync(email, password);
                
                if (result.success && result.user != null)
                {
                    // Show verification message
                    VerificationStatusLayout.IsVisible = true;
                    VerificationStatusLabel.Text = $"Account created! Please check your email ({email}) and verify your account.";
                    
                    await DisplayAlert("Account Created", 
                        "Your account has been created successfully! Please check your email and click the verification link before signing in.", "OK");
                    
                    // Clear password field
                    PasswordEntry.Text = "";
                }
                else
                {
                    var errorMessage = result.error ?? "Sign up failed. Please try again.";
                    await DisplayAlert("Sign Up Failed", errorMessage, "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Sign up failed: {ex.Message}", "OK");
            }
        }

        private async void OnGoogleSignInClicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("OnGoogleSignInClicked called!");
            Console.WriteLine("OnGoogleSignInClicked called!");
            await DisplayAlert("Coming Soon", "Google Sign-In will be available soon!", "OK");
        }

        private void OnCreateAccountClicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("OnCreateAccountClicked called!");
            Console.WriteLine("OnCreateAccountClicked called!");
            // For now, just call sign up directly
            OnSignUpClicked(sender, e);
        }

        private async void OnForgotPasswordClicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("OnForgotPasswordClicked called!");
            Console.WriteLine("OnForgotPasswordClicked called!");
            
            try
            {
                var forgotPasswordPage = new ForgotPasswordPage(_authService);
                await Navigation.PushModalAsync(forgotPasswordPage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error navigating to ForgotPasswordPage: {ex.Message}");
                Console.WriteLine($"Error navigating to ForgotPasswordPage: {ex.Message}");
                await DisplayAlert("Error", "Failed to open password reset page", "OK");
            }
        }
    }
} 