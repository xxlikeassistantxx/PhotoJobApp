# PhotoJobApp - Mac Setup Guide

## Prerequisites

### 1. Install Xcode
- Download and install Xcode from the Mac App Store (minimum version 15.0)
- Launch Xcode and accept the license agreements
- Install additional components when prompted

### 2. Install .NET 9
```bash
# Download and install .NET 9 SDK from Microsoft
# Visit: https://dotnet.microsoft.com/download/dotnet/9.0
# Or use Homebrew:
brew install --cask dotnet-sdk
```

### 3. Install .NET MAUI Workload
```bash
# Install MAUI workload
dotnet workload install maui

# Install iOS workload (for iOS development)
dotnet workload install ios

# Install Mac Catalyst workload (for Mac apps)
dotnet workload install maccatalyst
```

### 4. Install Git (if not already installed)
```bash
# Using Homebrew
brew install git

# Or download from: https://git-scm.com/download/mac
```

## Project Setup

### 1. Clone the Repository
```bash
# Navigate to your desired directory
cd ~/Documents/Projects

# Clone the repository (replace with your actual repository URL)
git clone https://github.com/yourusername/PhotoJobApp.git

# Navigate to the project directory
cd PhotoJobApp
```

### 2. Restore Dependencies
```bash
# Restore NuGet packages
dotnet restore

# Build the project to ensure everything is working
dotnet build
```

### 3. Configure iOS Development (Optional but Recommended)

#### Apple Developer Account Setup
1. Sign up for an Apple Developer account at https://developer.apple.com
2. Open Xcode and sign in with your Apple ID (Xcode > Preferences > Accounts)

#### Update Code Signing
Open `PhotoJobApp.csproj` and update the iOS code signing section:
```xml
<PropertyGroup Condition="'$(TargetFramework)'=='net9.0-ios'">
  <CodesignKey>Apple Development: Your Name (TEAM_ID)</CodesignKey>
  <CodesignProvision>Your Provisioning Profile</CodesignProvision>
</PropertyGroup>
```

### 4. Firebase Configuration (Required for Cloud Sync)

#### Create Firebase Project
1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Create a new project or use existing one
3. Enable Realtime Database
4. Set up authentication (optional)

#### Update Firebase Config
Edit `Services/FirebaseConfig.cs`:
```csharp
public const string ProjectId = "your-firebase-project-id";
public const string ApiKey = "your-firebase-api-key";
```

You can find these values in:
- Firebase Console > Project Settings > General
- Project ID is in the project info section
- Web API Key is in the web app configuration

## Development Environment Setup

### 1. Visual Studio for Mac (Recommended)
```bash
# Download Visual Studio for Mac from:
# https://visualstudio.microsoft.com/vs/mac/

# Or use Visual Studio Code with C# extension:
brew install --cask visual-studio-code
```

### 2. Install VS Code Extensions (if using VS Code)
- C# Dev Kit
- .NET MAUI Extension Pack
- GitLens

### 3. Simulator Setup
iOS Simulator is included with Xcode. You can also test on physical devices.

## Building and Running

### 1. Build for Mac Catalyst
```bash
# Build for Mac Catalyst
dotnet build -f net9.0-maccatalyst

# Run on Mac
dotnet run -f net9.0-maccatalyst
```

### 2. Build for iOS Simulator
```bash
# Build for iOS
dotnet build -f net9.0-ios

# Run on iOS Simulator (requires Xcode)
dotnet run -f net9.0-ios
```

### 3. Build for Android (requires Android SDK)
```bash
# Install Android SDK via Android Studio or command line tools
# Then build for Android
dotnet build -f net9.0-android
```

## Project Structure

```
PhotoJobApp/
├── Models/                 # Data models (JobType, PhotoJob)
├── Services/              # Business logic and data services
│   ├── AuthService.cs     # Authentication
│   ├── PhotoJobService.cs # Job management
│   ├── JobTypeService.cs  # Job type management
│   ├── CloudJobService.cs # Cloud sync for jobs
│   ├── CloudJobTypeService.cs # Cloud sync for job types
│   ├── FirebaseAuthService.cs # Firebase authentication
│   ├── FirebaseConfig.cs  # Firebase configuration
│   └── ThemeService.cs    # Theme management
├── Pages/                 # XAML pages and code-behind
├── Converters/           # Value converters for data binding
├── Resources/            # Images, fonts, styles
└── Platforms/            # Platform-specific code
```

## Key Features

### Local Database (SQLite)
- Offline-first approach
- Local storage for all job and job type data
- Automatic database creation and schema updates

### Cloud Synchronization
- Firebase Realtime Database integration
- User-based data isolation
- Automatic sync when online
- Manual sync option available

### Cross-Platform Support
- iOS (iPhone/iPad)
- Mac Catalyst (macOS apps)
- Android
- Windows (when developed on Windows)

## Development Tips

### 1. Hot Reload
Enable hot reload for faster development:
```bash
# Run with hot reload
dotnet watch run -f net9.0-maccatalyst
```

### 2. Debugging
- Use Visual Studio debugger for breakpoints
- Check debug output for Firebase and database operations
- Use iOS Simulator device logs for troubleshooting

### 3. Database Inspection
The SQLite database is stored locally. You can inspect it using:
- DB Browser for SQLite (https://sqlitebrowser.org/)
- Terminal sqlite3 command

### 4. Testing on Physical Devices
For iOS testing on physical devices:
1. Connect your iPhone/iPad via USB
2. Trust the computer on your device
3. Select your device in the deployment target
4. Build and deploy

## Troubleshooting

### Common Issues

#### 1. Build Errors
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

#### 2. MAUI Workload Issues
```bash
# Update workloads
dotnet workload update

# Reinstall if needed
dotnet workload uninstall maui
dotnet workload install maui
```

#### 3. iOS Signing Issues
- Ensure Apple Developer account is set up
- Check provisioning profiles in Xcode
- Verify bundle identifier is unique

#### 4. Firebase Connection Issues
- Verify Project ID and API Key
- Check internet connection
- Ensure Firebase Realtime Database is enabled

### Getting Help
- Check the debug console for error messages
- Use Firebase Console to monitor database activity
- Test with a clean database if data issues occur

## Next Steps

1. Clone and build the project
2. Configure Firebase (optional for basic functionality)
3. Set up iOS development certificates (for iOS deployment)
4. Start developing with hot reload enabled
5. Test on simulators and physical devices

The app will work offline by default using SQLite storage. Cloud sync is optional but recommended for multi-device usage.
