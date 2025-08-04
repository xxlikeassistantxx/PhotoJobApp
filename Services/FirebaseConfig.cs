namespace PhotoJobApp.Services
{
    public static class FirebaseConfig
    {
        // TODO: Replace with your actual Firebase project ID
        // You can find this in your Firebase Console under Project Settings
        public const string ProjectId = "photo-job-manager-default-rtdb";
        
        // TODO: Replace with your actual Firebase API key
        // You can find this in your Firebase Console under Project Settings > General > Web API Key
        public const string ApiKey = "AIzaSyDYCKj1mp7GrEftKYPMnoXYrt6EwNsje6c";
        
        // Collection names for Firestore
        public const string JobTypesCollection = "jobTypes";
        public const string JobsCollection = "jobs";
        public const string UsersCollection = "users";
    }
} 