# Security Setup Guide

This document explains how to securely configure the PhotoJobApp project for development and deployment.

## ⚠️ Important Security Notes

**NEVER commit the following files to version control:**
- `appsettings.json` (contains Firebase API keys and credentials)
- `Platforms/Android/google-services.json` (contains Firebase configuration)
- `Platforms/iOS/GoogleService-Info.plist` (contains Firebase configuration)
- Any files containing API keys, passwords, or other sensitive credentials

These files are automatically excluded via `.gitignore`, but always double-check before committing.

## Initial Setup

### 1. Firebase Configuration Files

#### For Android:
1. Download `google-services.json` from your Firebase Console
2. Copy it to `Platforms/Android/google-services.json`
3. **DO NOT commit this file to git** (it's already in `.gitignore`)

#### For iOS:
1. Download `GoogleService-Info.plist` from your Firebase Console
2. Copy it to `Platforms/iOS/GoogleService-Info.plist`
3. **DO NOT commit this file to git** (it's already in `.gitignore`)

### 2. Application Settings

1. Copy `appsettings.template.json` to `appsettings.json`:
   ```bash
   cp appsettings.template.json appsettings.json
   ```

2. Edit `appsettings.json` and fill in your Firebase credentials:
   ```json
   {
     "Firebase": {
       "ApiKey": "YOUR_FIREBASE_API_KEY_HERE",
       "AuthDomain": "YOUR_FIREBASE_AUTH_DOMAIN_HERE",
       "ProjectId": "YOUR_FIREBASE_PROJECT_ID_HERE",
       "DatabaseUrl": "YOUR_FIREBASE_DATABASE_URL_HERE",
       "GoogleWebClientId": "YOUR_WEB_CLIENT_ID_HERE",
       "GoogleiOSClientId": "YOUR_IOS_CLIENT_ID_HERE",
       "GoogleAndroidClientId": "YOUR_ANDROID_CLIENT_ID_HERE",
       "GoogleReversedClientId": "YOUR_REVERSED_CLIENT_ID_HERE"
     },
     "AppSettings": {
       "Environment": "Development",
       "LogLevel": "Information"
     }
   }
   ```

3. **DO NOT commit `appsettings.json` to git** (it's already in `.gitignore`)

### 3. Where to Find Firebase Credentials

#### Firebase API Key:
- Firebase Console → Project Settings → General → Your apps → Web app
- Look for "API Key" in the config object

#### Firebase Project ID:
- Firebase Console → Project Settings → General
- Listed as "Project ID"

#### Firebase Auth Domain:
- Usually: `{ProjectId}.firebaseapp.com`
- Firebase Console → Authentication → Settings → Authorized domains

#### Firebase Database URL:
- Firebase Console → Realtime Database
- Format: `https://{ProjectId}-default-rtdb.firebaseio.com`

#### Google OAuth Client IDs:
- Firebase Console → Project Settings → Your apps → [Platform] app
- Or: Google Cloud Console → APIs & Services → Credentials
- Look for OAuth 2.0 Client IDs

## Configuration Priority

The application reads configuration in the following order (highest to lowest priority):

1. **appsettings.json** - Application-level configuration
2. **Platform-specific config files**:
   - Android: `google-services.json` (read by Firebase SDK)
   - iOS: `GoogleService-Info.plist` (read by Firebase SDK)
3. **Hardcoded defaults** - Only for non-sensitive values like collection names

## For Team Development

### Option 1: Shared Configuration (Recommended for small teams)
- Use a secure password manager or encrypted storage to share `appsettings.json`
- Each developer copies the file locally (never commit it)

### Option 2: Environment Variables (Recommended for CI/CD)
- Set environment variables for sensitive values
- Update `MauiProgram.cs` to read from environment variables as a fallback

### Option 3: Secure Configuration Service
- Use Azure Key Vault, AWS Secrets Manager, or similar
- Integrate with your CI/CD pipeline

## For Production Deployment

1. **Never use development credentials in production**
2. Create separate Firebase projects for development and production
3. Use different `appsettings.json` files for each environment
4. Store production credentials securely (Key Vault, Secrets Manager, etc.)
5. Use CI/CD pipeline secrets for automated deployments

## Verifying Your Setup

After configuration, verify that:
- ✅ `appsettings.json` exists and contains your credentials
- ✅ `Platforms/Android/google-services.json` exists (for Android builds)
- ✅ `Platforms/iOS/GoogleService-Info.plist` exists (for iOS builds)
- ✅ All sensitive files are listed in `.gitignore`
- ✅ No API keys or credentials are hardcoded in source files
- ✅ The application builds and runs successfully

## If You Accidentally Committed Sensitive Data

If you've accidentally committed sensitive data to git:

1. **Immediately rotate/revoke the exposed credentials** in Firebase Console
2. Remove the sensitive data from git history:
   ```bash
   git filter-branch --force --index-filter \
     "git rm --cached --ignore-unmatch appsettings.json Platforms/Android/google-services.json Platforms/iOS/GoogleService-Info.plist" \
     --prune-empty --tag-name-filter cat -- --all
   ```
3. Force push (coordinate with your team first):
   ```bash
   git push origin --force --all
   ```
4. Update all team members to pull the cleaned history

## Additional Security Best Practices

1. **Use Firebase Security Rules** - Configure proper read/write rules in Firebase Console
2. **Enable App Check** - Protect your backend resources from abuse
3. **Use Firebase Authentication** - Never store passwords in plain text
4. **Regular Audits** - Periodically review who has access to Firebase projects
5. **Monitor Usage** - Set up alerts for unusual activity in Firebase Console

## Support

If you encounter issues with configuration:
1. Check that all required files exist
2. Verify file formats (JSON/PLIST) are valid
3. Ensure Firebase project is properly set up
4. Review application logs for specific error messages
