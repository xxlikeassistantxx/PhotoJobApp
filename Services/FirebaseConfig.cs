using Microsoft.Extensions.Configuration;
#if IOS
using Foundation;
#endif

namespace PhotoJobApp.Services
{
    public static class FirebaseConfig
    {
        private static IConfiguration? _configuration;
        
        // Collection names for Firestore (not sensitive, can remain as constants)
        public const string JobTypesCollection = "jobTypes";
        public const string JobsCollection = "jobs";
        public const string UsersCollection = "users";
        
        // Initialize configuration (should be called during app startup)
        public static void Initialize(IConfiguration? configuration)
        {
            _configuration = configuration;
        }
        
        // Firebase project ID - reads from configuration or falls back to default
        public static string ProjectId => 
            _configuration?["Firebase:ProjectId"] ?? 
            GetFromPlatformConfig("PROJECT_ID") ?? 
            "photo-job-manager";
        
        // Firebase Authentication domain
        public static string AuthDomain => 
            _configuration?["Firebase:AuthDomain"] ?? 
            $"{ProjectId}.firebaseapp.com";
        
        // Firebase API key - reads from configuration or platform-specific config
        public static string ApiKey => 
            _configuration?["Firebase:ApiKey"] ?? 
            GetFromPlatformConfig("API_KEY") ?? 
            throw new InvalidOperationException("Firebase API key not found. Please configure it in appsettings.json or platform-specific config files.");
        
        // Firebase Realtime Database URL
        public static string DatabaseUrl => 
            _configuration?["Firebase:DatabaseUrl"] ?? 
            GetFromPlatformConfig("DATABASE_URL") ?? 
            $"https://{ProjectId}-default-rtdb.firebaseio.com";
        
        // Google OAuth Web Client ID
        public static string GoogleWebClientId => 
            _configuration?["Firebase:GoogleWebClientId"] ?? 
            GetFromPlatformConfig("WEB_CLIENT_ID") ?? 
            string.Empty;
        
        // Helper method to read from platform-specific configuration files
        private static string? GetFromPlatformConfig(string key)
        {
#if ANDROID
            // Android reads from google-services.json automatically via Firebase SDK
            // For manual access, you'd need to parse the JSON file
            return null;
#elif IOS
            // iOS reads from GoogleService-Info.plist
            try
            {
                var path = NSBundle.MainBundle.PathForResource("GoogleService-Info", "plist");
                if (path != null)
                {
                    var plist = NSDictionary.FromFile(path);
                    return plist?[key]?.ToString();
                }
            }
            catch
            {
                // Fallback if plist not found
            }
            return null;
#else
            return null;
#endif
        }
    }
} 