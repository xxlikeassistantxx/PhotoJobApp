# Google Sign-In and Hot Restart (iOS)

## The problem

When you run the app on a **physical iPhone from Windows** using **Hot Restart**, Google Sign-In and OAuth often fail with errors such as:

- **"The UIWindowScene for the returned window was not in the foreground active state"** (ASWebAuthenticationSession Code=3)
- **TaskCanceledException** or "Google Sign In failed"
- The OAuth redirect never reaches the app, or the app is terminated before the callback is handled

## Why it happens

**Hot Restart** uses a pre-built iOS runner (`Xamarin.PreBuilt.iOS`) that loads your .NET code from a content folder. In this mode:

- OAuth redirects (URL scheme callbacks) and `ASWebAuthenticationSession` / `WebAuthenticator` are **not fully supported**
- The pre-built app’s lifecycle and URL handling can differ from a normal iOS build
- Only one `ASWebAuthenticationSession` can be active; Hot Restart’s timing and window management make it easy to hit Code=3 or lost callbacks

## What works

To test **Google Sign-In on iOS** reliably:

1. **Full deploy from a Mac**  
   - Build and run the app on a device or simulator from **Visual Studio for Mac** or `dotnet build` / `dotnet run` on a Mac.  
   - Do **not** use Hot Restart.  
   - Example:  
     `dotnet build -f net9.0-ios -p:IsHotRestartBuild=false`  
     Then deploy/run from Xcode or VS Mac.

2. **Test on Android**  
   - Google Sign-In works with a normal Android build from Windows. Use an Android device or emulator to verify the flow.

## What the app does

- **Proactive warning (iOS + Hot Restart):** When you tap “Sign in with Google” and the app detects Hot Restart, it shows an alert explaining that sign-in may not work and offers “Try anyway” or “Cancel”.
- **Error messages:** If Google Sign-In fails on iOS, the error text includes a note that Hot Restart (deploy from Windows to iPhone) often causes this, and suggests a full deploy from a Mac.

## Summary

| Environment                          | Google Sign-In on iOS      |
|--------------------------------------|----------------------------|
| Windows → iPhone via **Hot Restart** | Unreliable, often fails     |
| **Mac** → iPhone (full deploy)       | Works                      |
| Windows → **Android**                | Works                      |

For iOS, use a **full deploy from a Mac** (or an Android build from Windows) to test Google Sign-In.
