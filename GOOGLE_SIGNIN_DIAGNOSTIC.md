# Google Sign-In Diagnostic Guide

## Current Flow

### When User Taps "Sign in with Google":
1. ✅ OAuth URL is generated and stored in `NSUserDefaults` with timestamp
2. ✅ `WebAuthenticator` launches the OAuth flow
3. ✅ User enters credentials in browser
4. ⚠️ **User switches to authenticator app for 2FA** → App may be terminated

### When App Returns (After Termination):
1. `AppDelegate.OpenUrl` should receive the OAuth callback URL
2. Callback URL is stored in `NSUserDefaults` as `PendingOAuthCallback`
3. `LoginPage.OnAppearing` calls `CheckForPendingCallback()`
4. If callback found → `ProcessPendingOAuthCallback()` is called
5. OAuth tokens are extracted and exchanged with Firebase
6. User is signed in

## Common Issues & Solutions

### Issue 1: Callback Not Received
**Symptoms:**
- App restarts but no callback is found
- `CheckForPendingCallback` shows "No pending callback found"

**Possible Causes:**
- OAuth URL expired (should auto-create new one if > 5 minutes old)
- Callback URL scheme doesn't match `Info.plist`
- App terminated before callback arrived

**Check:**
- Look for `🔵 AppDelegate.OpenUrl CALLED!` in logs
- Verify `Info.plist` has correct URL scheme: `com.googleusercontent.apps.1021759232753-hhfhegcuq82cc9er9slf1r3iuqkkpbsh`

### Issue 2: Stored OAuth URL Too Old
**Symptoms:**
- App resumes with old OAuth URL (> 5 minutes)
- OAuth flow fails or times out

**Solution:**
- Code now checks timestamp and creates new URL if > 5 minutes old
- Check logs for: `⚠ Stored OAuth URL is too old`

### Issue 3: Callback Processed But Sign-In Fails
**Symptoms:**
- Callback is found and processed
- But Firebase sign-in fails

**Check:**
- Look for error messages in `ProcessPendingOAuthCallback`
- Verify Firebase configuration
- Check if `id_token` or `code` is present in callback

### Issue 4: WebAuthenticator Code=3 Error
**Symptoms:**
- Error: "The UIWindowScene for the returned window was not in the foreground active state"

**Solution:**
- This happens when trying to launch `WebAuthenticator` automatically
- Fixed: Only launch when user taps button (app is in foreground)

## Debugging Steps

1. **Check if callback is stored:**
   - Look for: `✓✓✓ STORED OAuth callback in NSUserDefaults`
   - Check `NSUserDefaults` key: `PendingOAuthCallback`

2. **Check if callback is found on restart:**
   - Look for: `✓✓✓ Found pending OAuth callback in LoginPage!`
   - If not found, callback wasn't stored or was cleared

3. **Check OAuth URL age:**
   - Look for: `AttemptResumeGoogleSignInAsync: Sign-in in progress for X.Xs`
   - If > 300 seconds (5 minutes), new URL should be created

4. **Check if OAuth URL is resumed:**
   - Look for: `✓ Resuming previous OAuth flow (stored X.Xs ago)`
   - Or: `⚠ Stored OAuth URL is too old`

## What to Check in Your Logs

1. When you tap "Sign in with Google":
   ```
   ✓ Stored OAuth state and URL for recovery: ...
   ```

2. When app returns after termination:
   ```
   🔵 AppDelegate.OpenUrl CALLED!
   ✓✓✓ STORED OAuth callback in NSUserDefaults
   ```

3. When LoginPage appears:
   ```
   LoginPage.CheckForPendingCallback - Checking for pending OAuth callback...
   ✓✓✓ Found pending OAuth callback in LoginPage!
   ```

4. When processing callback:
   ```
   ✓ Found id_token in callback, exchanging for Firebase token...
   ✓✓✓ Google Sign-In successful!
   ```

## If Still Not Working

Share these logs:
1. When you tap "Sign in with Google"
2. When app returns after 2FA
3. What happens when LoginPage appears
4. Any error messages
