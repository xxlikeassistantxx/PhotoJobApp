using Android.App;
using Android.Content.PM;
using Android.OS;

namespace PhotoJobApp;

/// <summary>
/// Main activity for the Android app.
/// 
/// LaunchMode.SingleTop: Ensures that if the activity is already running, a new instance is not created.
/// This is important for OAuth flows where the app may be backgrounded during 2FA.
/// </summary>
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
}
