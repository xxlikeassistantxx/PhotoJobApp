using PhotoJobApp.Services;

namespace PhotoJobApp
{
    public partial class ForgotPasswordPage : ContentPage
    {
        private readonly FirebaseAuthService _authService;

        public ForgotPasswordPage(FirebaseAuthService authService)
        {
            InitializeComponent();
            _authService = authService;
        }

        private async void OnSendResetLinkClicked(object sender, EventArgs e)
        {
            var email = EmailEntry.Text?.Trim();

            if (string.IsNullOrEmpty(email))
            {
                await DisplayAlert("Error", "Please enter your email address", "OK");
                return;
            }

            if (!IsValidEmail(email))
            {
                await DisplayAlert("Error", "Please enter a valid email address", "OK");
                return;
            }

            try
            {
                // Show loading state
                var button = sender as Button;
                if (button != null)
                {
                    button.IsEnabled = false;
                    button.Text = "Sending...";
                }

                var result = await _authService.SendPasswordResetEmailAsync(email);

                if (result.success)
                {
                    // Show success message
                    StatusLayout.IsVisible = true;
                    StatusLabel.Text = $"Password reset link sent to {email}";
                    StatusLabel.TextColor = Colors.Green;
                    
                    await DisplayAlert("Success", 
                        "Password reset link has been sent to your email address. Please check your inbox and follow the instructions in the email. The link will expire in 1 hour.", "OK");
                    
                    // Clear the email field
                    EmailEntry.Text = "";
                    
                    // Automatically dismiss the modal after a short delay
                    await Task.Delay(1000);
                    await Navigation.PopModalAsync();
                }
                else
                {
                    var errorMessage = result.error ?? "Failed to send password reset email. Please check your email address and try again.";
                    await DisplayAlert("Error", errorMessage, "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to send password reset email: {ex.Message}", "OK");
            }
            finally
            {
                // Reset button state
                var button = sender as Button;
                if (button != null)
                {
                    button.IsEnabled = true;
                    button.Text = "Send Reset Link";
                }
            }
        }

        private async void OnBackToLoginClicked(object sender, EventArgs e)
        {
            // Dismiss the modal and return to login page
            await Navigation.PopModalAsync();
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
} 