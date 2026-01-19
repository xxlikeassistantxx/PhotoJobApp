using Android.App;
using Android.Content;
using Android.Content.PM;
using Microsoft.Maui.Authentication;

namespace PhotoJobApp;

/// <summary>
/// Handles OAuth callback URLs from Google Sign-In.
/// 
/// Key Configuration:
/// - LaunchMode.SingleTop: Prevents Android from creating a new activity instance when returning from
///   the authenticator app during 2FA. This ensures the OAuth callback is properly received.
/// - Exported = true: Required for Android 12+ (API 31+) to allow external apps (Google browser) to
///   redirect back to this activity.
/// - NoHistory = true: Removes this activity from the back stack after OAuth completes.
/// </summary>
[Activity(Exported = true, NoHistory = true, LaunchMode = LaunchMode.SingleTop)]
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryBrowsable, Intent.CategoryDefault },
    DataSchemes = new[] { "com.pinebelttrophy.photojobapp2025" },
    DataHosts = new[] { "oauth2redirect" })]
public class WebAuthenticatorCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
{
}

