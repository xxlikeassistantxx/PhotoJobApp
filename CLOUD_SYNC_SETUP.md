# Cloud Sync Setup Guide

## Firebase Configuration

To enable cloud syncing for job types, you need to set up Firebase Realtime Database:

### 1. Create a Firebase Project

1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Click "Create a project" or select an existing project
3. Follow the setup wizard

### 2. Enable Realtime Database

1. In your Firebase project, go to "Realtime Database" in the left sidebar
2. Click "Create Database"
3. Choose a location (pick the closest to your users)
4. Start in test mode (you can secure it later)

### 3. Update Configuration

1. Go to Project Settings (gear icon)
2. Copy your Project ID
3. Open `Services/FirebaseConfig.cs` in your project
4. Replace `"your-firebase-project-id"` with your actual Project ID

```csharp
public const string ProjectId = "your-actual-project-id";
```

### 4. Security Rules (Optional but Recommended)

In your Firebase Realtime Database, go to "Rules" and set up basic security:

```json
{
  "rules": {
    "jobTypes": {
      "$uid": {
        ".read": "$uid === auth.uid",
        ".write": "$uid === auth.uid"
      }
    }
  }
}
```

## How Cloud Sync Works

### Job Type Synchronization

- **Automatic Sync**: When you create, update, or delete job types locally, they are automatically synced to the cloud
- **Manual Sync**: Use the "🔄 Sync" button in the Job Type Management page to manually sync
- **Cross-Device**: Job types created on one device will appear on other devices when you sign in with the same account

### Data Structure

Job types are stored in Firebase under:
```
/jobTypes/{userId}/{jobTypeId}
```

Each job type contains:
- Name, Description, Color, Icon
- Feature flags (HasPhotos, HasLocation, etc.)
- Custom fields (JSON string)
- Status options
- Creation date

### Offline Support

- The app works offline with local SQLite storage
- Changes are queued and synced when connection is restored
- No data loss when offline

## Troubleshooting

### Sync Not Working

1. Check your internet connection
2. Verify Firebase project ID is correct
3. Ensure Firebase Realtime Database is enabled
4. Check the debug console for error messages

### Data Not Appearing

1. Make sure you're signed in with the same account on all devices
2. Try manual sync using the "🔄 Sync" button
3. Check if the job type was created locally first

### Firebase Errors

Common error messages and solutions:
- "Project not found": Check your Project ID
- "Permission denied": Check Firebase security rules
- "Network error": Check internet connection

## Future Enhancements

Planned features for cloud sync:
- Real-time sync with Firebase listeners
- Conflict resolution for simultaneous edits
- Background sync service
- Photo upload to Firebase Storage
- Selective job syncing (cloud vs local only) 