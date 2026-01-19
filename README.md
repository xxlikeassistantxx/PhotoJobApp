# PhotoJobApp

A cross-platform photo job management application built with .NET MAUI.

## Project Structure

```
PhotoJobApp/
├── Pages/              # All XAML page files (views)
├── Services/           # Business logic and service classes
├── Models/             # Data models
├── Converters/         # Value converters for data binding
├── Platforms/          # Platform-specific implementations
│   ├── Android/
│   ├── iOS/
│   ├── MacCatalyst/
│   └── Windows/
├── Resources/          # Images, fonts, styles, and other resources
├── Properties/         # Application properties and settings
├── docs/               # Documentation files
│   ├── SECURITY.md     # Security setup guide
│   ├── SETUP_GUIDE.md  # Setup instructions
│   └── TERMS_OF_SERVICE.md
└── appsettings.template.json  # Configuration template
```

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- Visual Studio 2022 or Visual Studio Code with C# extension
- Platform-specific SDKs:
  - Android: Android SDK (API 21+)
  - iOS: Xcode and iOS SDK (15.0+)
  - Windows: Windows 10 SDK (10.0.17763.0+)

### Configuration

1. **Copy the configuration template:**
   ```bash
   cp appsettings.template.json appsettings.json
   ```

2. **Configure Firebase:**
   - Download `google-services.json` from Firebase Console
   - Place it in `Platforms/Android/google-services.json`
   - Download `GoogleService-Info.plist` from Firebase Console
   - Place it in `Platforms/iOS/GoogleService-Info.plist`
   - Fill in your Firebase credentials in `appsettings.json`

3. **See [docs/SECURITY.md](docs/SECURITY.md) for detailed setup instructions**

### Building

```bash
# Build for Android
dotnet build -f net9.0-android

# Build for iOS
dotnet build -f net9.0-ios

# Build for Windows
dotnet build -f net9.0-windows10.0.19041.0
```

### Running

```bash
# Run on Android
dotnet build -t:Run -f net9.0-android

# Run on iOS (requires Mac)
dotnet build -t:Run -f net9.0-ios

# Run on Windows
dotnet build -t:Run -f net9.0-windows10.0.19041.0
```

## Features

- 📸 Photo job management
- 📊 Job analysis and statistics
- ☁️ Cloud synchronization with Firebase
- 🔐 Secure authentication
- 📱 Cross-platform support (Android, iOS, Windows, Mac)

## Documentation

- [Security Setup Guide](docs/SECURITY.md) - How to configure Firebase and secure your app
- [Setup Guide](docs/SETUP_GUIDE.md) - Detailed setup instructions
- [Terms of Service](docs/TERMS_OF_SERVICE.md)

## Security

⚠️ **Important:** This project is configured to keep sensitive data out of version control. Never commit:
- `appsettings.json`
- `Platforms/Android/google-services.json`
- `Platforms/iOS/GoogleService-Info.plist`

See [docs/SECURITY.md](docs/SECURITY.md) for more information.

## License

[Add your license here]

## Contributing

[Add contributing guidelines here]
