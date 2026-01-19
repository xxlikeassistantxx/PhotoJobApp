# Firebase Realtime Database Rules Setup

## Current Issue: "Permission denied" Error

If you're getting a "Permission denied" error when pushing jobs, it means your Firebase Database Rules need to be updated.

## Quick Fix (Temporary - For Testing)

If you want to test quickly, you can temporarily use these permissive rules:

```json
{
  "rules": {
    ".read": true,
    ".write": true
  }
}
```

**⚠️ WARNING:** These rules allow anyone to read/write your entire database. Only use for testing!

## Secure Rules (Recommended)

Use these secure rules that I created in `FIREBASE_DATABASE_RULES.json`:

```json
{
  "rules": {
    "jobTypes": {
      "$uid": {
        ".read": "$uid === auth.uid",
        ".write": "$uid === auth.uid"
      }
    },
    "jobs": {
      "$uid": {
        ".read": "$uid === auth.uid",
        ".write": "$uid === auth.uid"
      }
    },
    "sharedJobs": {
      "$uid": {
        "jobs": {
          "$jobId": {
            ".read": "$uid === auth.uid || root.child('linkedAccounts').child(auth.uid).child('linkedUserIds').child($uid).exists()",
            ".write": "$uid === auth.uid"
          }
        }
      }
    },
    "sharedJobTypes": {
      "$uid": {
        "jobTypes": {
          "$jobTypeId": {
            ".read": "$uid === auth.uid || root.child('linkedAccounts').child(auth.uid).child('linkedUserIds').child($uid).exists()",
            ".write": "$uid === auth.uid"
          }
        }
      }
    },
    "linkedAccounts": {
      "$uid": {
        "linkedUserIds": {
          ".read": "$uid === auth.uid",
          ".write": "$uid === auth.uid"
        }
      }
    },
    "users": {
      "$userId": {
        ".read": "$userId === auth.uid || root.child('linkedAccounts').child(auth.uid).child('linkedUserIds').child($userId).exists()",
        ".write": "$userId === auth.uid"
      }
    }
  }
}
```

## How to Update Rules

1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Select your project: **photo-job-manager**
3. Click **Realtime Database** in the left sidebar
4. Click the **Rules** tab
5. Copy and paste the secure rules from above (or from `FIREBASE_DATABASE_RULES.json`)
6. Click **Publish**

## Important Notes

- The rules check `auth.uid` which comes from the Firebase ID token
- Make sure you're signed in when pushing jobs
- The `$uid` in the path must match the `auth.uid` from your ID token
- After updating rules, wait a few seconds for them to propagate

## Troubleshooting

If you still get "Permission denied" after updating rules:

1. **Check if rules were published**: Make sure you clicked "Publish" in Firebase Console
2. **Verify you're signed in**: The app needs a valid Firebase Auth session
3. **Check the user ID**: The user ID in the database path should match your Firebase Auth UID
4. **Wait a moment**: Rules can take a few seconds to propagate after publishing

