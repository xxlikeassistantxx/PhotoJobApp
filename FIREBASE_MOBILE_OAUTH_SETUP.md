# Firebase Mobile OAuth Setup for iOS

> **Note:** As of November 9, 2025 the iOS build now uses Google's direct OAuth endpoint with the reversed client ID scheme. The steps below are kept for historical reference in case we need to revisit the Firebase mobile handler approach.

## What I Changed

Switched from **Google's direct OAuth** to **Firebase's Mobile OAuth handler**, which is specifically designed for iOS apps and handles redirects properly.

## Key Changes

### 1. **OAuth URL Format**

**Before (Direct Google OAuth):**
```
https://accounts.google.com/o/oauth2/v2/auth?
  client_id=1021759232753-mm6ns7f4r20aohg8ric4med68kqtul9e...&
  redirect_uri=com.googleusercontent.apps.1021759232753-mm6ns7f4r20aohg8ric4med68kqtul9e:&
  response_type=code&
  ...
```

**After (Firebase Mobile OAuth):**
```
https://photo-job-manager.firebaseapp.com/__/auth/handler?
  apiKey=AIzaSyDDNFUx-oGkgHCtGRQ-N8xw_JbLM6Cbu4g&
  appName=[DEFAULT]&
  authType=signInViaRedirect&
  providerId=google.com&
  redirectUrl=https://photo-job-manager.firebaseapp.com/__/auth/handler&
  continueUrl=com.pinebelttrophy.photojobapp2025://&
  ibi=com.pinebelttrophy.photojobapp2025&
  v=9.23.0
```

### 2. **Why This Should Work Better**

Firebase's mobile OAuth:
- ✅ Designed specifically for mobile apps
- ✅ Handles iOS custom URL scheme redirects properly
- ✅ Works with app termination during 2FA (if iOS app is registered)
- ✅ Uses the `ibi` (iOS Bundle Identifier) parameter to tell Firebase this is an iOS app
- ✅ Automatically manages OAuth state across app termination

### 3. **Critical Requirement: iOS App Must Be Registered in Firebase**

For this to work, your iOS app **MUST be registered** in Firebase Console.

**Check if registered:**

1. Go to: https://console.firebase.google.com/project/photo-job-manager/settings/general
2. Scroll down to **"Your apps"** section
3. Look for an **iOS app** with bundle ID: `com.pinebelttrophy.photojobapp2025`

**If you DON'T see an iOS app registered:**

### **Add iOS App to Firebase (CRITICAL)**

1. In Firebase Console, go to **Project Settings**
2. Scroll to **"Your apps"** section
3. Click **"Add app"** → Select **iOS** icon
4. Fill in:
   - **Apple bundle ID:** `com.pinebelttrophy.photojobapp2025`
   - **App nickname (optional):** Photo Job Manager iOS
   - **App Store ID (optional):** Leave empty
5. Click **Register app**
6. **Download GoogleService-Info.plist** (you already have this)
7. Click **Next** through the remaining steps
8. Click **Continue to console**

**This is critical!** Firebase won't redirect to your iOS app scheme unless the iOS app is registered in Firebase.

## How It Works

1. **User clicks "Sign in with Google"**
2. **App opens Firebase mobile OAuth URL** in Safari/WebAuthenticator
3. **Firebase redirects to Google** for authentication
4. **User signs in with Gmail/password** and completes 2FA
5. **Google redirects back to Firebase** auth handler
6. **Firebase completes the OAuth flow** and gets the tokens
7. **Firebase redirects to your iOS app** using `continueUrl` parameter: `com.pinebelttrophy.photojobapp2025://`
8. **iOS launches your app** and calls `AppDelegate.OpenUrl`
9. **AppDelegate stores the callback** in NSUserDefaults
10. **App processes the callback** and completes sign-in

## Parameters Explained

- `apiKey` - Your Firebase API key
- `appName` - Firebase app name (always `[DEFAULT]`)
- `authType` - Type of auth flow (`signInViaRedirect`)
- `providerId` - OAuth provider (`google.com`)
- `redirectUrl` - Where Google OAuth redirects (back to Firebase handler)
- `continueUrl` - Where Firebase redirects after OAuth completes (your iOS app scheme)
- `ibi` - iOS Bundle Identifier (tells Firebase this is an iOS app)
- `v` - Firebase SDK version

## What to Check

### ✅ **Check #1: iOS App Registered in Firebase**

Most important! Go to Firebase Console → Project Settings → Your apps

You should see:
- ✅ Web app (for Firebase web auth)
- ✅ **iOS app with bundle ID: com.pinebelttrophy.photojobapp2025**

### ✅ **Check #2: Google Sign-In Enabled**

Firebase Console → Authentication → Sign-in method

Make sure **Google** is enabled.

### ✅ **Check #3: Info.plist Has Custom Scheme**

`Platforms/iOS/Info.plist` should have:
```xml
<key>CFBundleURLSchemes</key>
<array>
    <string>com.pinebelttrophy.photojobapp2025</string>
    ...
</array>
```

✅ Already configured!

## Testing

After registering the iOS app in Firebase (if not already done):

1. **Rebuild and deploy** the app
2. **Tap "Sign in with Google"**
3. **Complete sign-in and 2FA**
4. **App should redirect back** and complete sign-in

## Expected Device Logs

If Firebase is properly configured, you should see:

```
⚠️ USING FIREBASE MOBILE OAUTH
Starting Firebase Mobile OAuth flow
  Firebase Handler: https://photo-job-manager.firebaseapp.com/__/auth/handler
  App Callback: com.pinebelttrophy.photojobapp2025://
  Bundle ID: com.pinebelttrophy.photojobapp2025

... [user completes 2FA] ...

🔵 AppDelegate.OpenUrl CALLED!
   Scheme: com.pinebelttrophy.photojobapp2025
   URL: com.pinebelttrophy.photojobapp2025://?id_token=...

✓✓✓ STORED OAuth callback in NSUserDefaults
✓✓✓ Found pending OAuth callback in LoginPage!
✓ Received ID token from Firebase, signing in...
✓✓✓ Google Sign-In successful! User: your.email@gmail.com
```

The key difference: **Firebase knows to redirect to iOS apps** when the iOS app is registered, whereas Google's direct OAuth doesn't.

## Files Modified

- `Services/FirebaseAuthService.cs` - Changed to use Firebase mobile OAuth handler
- `Platforms/iOS/Info.plist` - Already has correct URL schemes
- `Platforms/iOS/AppDelegate.cs` - Already handles custom URL scheme callbacks

## Next Step

**CRITICAL: Check if iOS app is registered in Firebase Console!**

If not registered → Register it now following the steps above.

Then test the Google Sign-In flow again.

