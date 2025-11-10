namespace PhotoJobApp.Services
{
    /// <summary>
    /// Platform-specific interface for Google Sign-In.
    /// On iOS, this will use the native Google Sign-In SDK.
    /// On other platforms, this can fall back to WebAuthenticator.
    /// </summary>
    public interface IGoogleSignInService
    {
        /// <summary>
        /// Initiates Google Sign-In and returns the Google ID token.
        /// </summary>
        /// <returns>Google ID token if successful, null otherwise</returns>
        Task<string?> SignInAsync();
    }
}

