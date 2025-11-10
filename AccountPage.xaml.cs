using PhotoJobApp.Services;

namespace PhotoJobApp
{
    public partial class AccountPage : ContentPage
    {
        private readonly FirebaseAuthService _authService;

        public AccountPage(FirebaseAuthService authService)
        {
            InitializeComponent();
            _authService = authService;
            LoadUserInformation();
        }

        private async void LoadUserInformation()
        {
            try
            {
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser != null)
                {
                    UserEmailLabel.Text = currentUser.Email;
                    MemberSinceLabel.Text = DateTime.Now.ToString("MMMM yyyy"); // You could store actual signup date
                }
                else
                {
                    UserEmailLabel.Text = "Not signed in";
                    MemberSinceLabel.Text = "N/A";
                }

                // Load saved preferences
                LoadPreferences();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading user information: {ex.Message}");
            }
        }

        private void LoadPreferences()
        {
            try
            {
                // Load theme preference
                var savedTheme = Preferences.Get("Theme", "Auto");
                ThemePicker.SelectedItem = savedTheme;

                // Load notification preference
                var notificationsEnabled = Preferences.Get("NotificationsEnabled", true);
                NotificationsSwitch.IsToggled = notificationsEnabled;

                // Load sync job types preference
                var syncJobTypesEnabled = Preferences.Get("SyncJobTypesEnabled", true);
                SyncJobTypesSwitch.IsToggled = syncJobTypesEnabled;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading preferences: {ex.Message}");
            }
        }

        private async void OnUpgradeClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Premium Upgrade", 
                "Premium features coming soon!\n\n• Unlimited job types\n• Advanced analytics\n• Priority support\n• Cloud backup", 
                "OK");
        }

        private async void OnExportDataClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Export Data", 
                "Data export feature coming soon!\n\nYou'll be able to export all your jobs and job types as a backup.", 
                "OK");
        }

        private async void OnSignOutClicked(object sender, EventArgs e)
        {
            var confirm = await DisplayAlert("Sign Out", 
                "Are you sure you want to sign out?", 
                "Yes", "No");
            
            if (confirm)
            {
                try
                {
                    await _authService.SignOutAsync();
                    
                    // Clear authentication state
                    Preferences.Set("IsAuthenticated", false);
                    Preferences.Remove("UserId");
                    Preferences.Remove("UserEmail");
                    Preferences.Remove("RememberMe");
                    Preferences.Remove("FirebaseUserId");
                    Preferences.Remove("FirebaseUserEmail");
                    Preferences.Remove("FirebaseUserDisplayName");
                    Preferences.Remove("FirebaseIdToken");
                    Preferences.Remove("FirebaseRefreshToken");
                    Preferences.Remove("FirebaseAuthData:v1");
                    
                    // Navigate to login page
                    var loginPage = new LoginPage(_authService);
                    if (Application.Current?.Windows?.Count > 0)
                    {
                        Application.Current.Windows[0].Page = loginPage;
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to sign out: {ex.Message}", "OK");
                }
            }
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            if (ThemePicker.SelectedItem is string selectedTheme)
            {
                Preferences.Set("Theme", selectedTheme);
                // Apply theme changes here when implemented
            }
        }

        private void OnNotificationsToggled(object sender, ToggledEventArgs e)
        {
            Preferences.Set("NotificationsEnabled", e.Value);
            // Apply notification changes here when implemented
        }

        private void OnSyncJobTypesToggled(object sender, ToggledEventArgs e)
        {
            Preferences.Set("SyncJobTypesEnabled", e.Value);
            // Apply sync job types changes here when implemented
            System.Diagnostics.Debug.WriteLine($"Sync Job Types setting changed to: {e.Value}");
        }

        private async void OnPrivacyPolicyClicked(object sender, EventArgs e)
        {
            try
            {
                var privacyPolicyPage = new PrivacyPolicyPage();
                await Navigation.PushAsync(privacyPolicyPage);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to open Privacy Policy: {ex.Message}", "OK");
            }
        }

        private async void OnTermsOfServiceClicked(object sender, EventArgs e)
        {
            try
            {
                var termsPage = new TermsOfServicePage();
                await Navigation.PushAsync(termsPage);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to open Terms of Service: {ex.Message}", "OK");
            }
        }
    }
} 