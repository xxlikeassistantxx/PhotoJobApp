using System.Text.Json;

namespace PhotoJobApp.Services
{
    public class AuthService
    {
        private const string UserKey = "CurrentUser";
        private const string AuthKey = "IsAuthenticated";

        public class User
        {
            public string Id { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public DateTime CreatedDate { get; set; } = DateTime.Now;
        }

        public Task<bool> SignInAsync(string email, string password)
        {
            // Simple authentication for demo purposes
            // In a real app, this would validate against Firebase or your backend
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                return Task.FromResult(false);

            // For demo: accept any valid email format
            if (!email.Contains("@"))
                return Task.FromResult(false);

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Email = email,
                Name = email.Split('@')[0],
                CreatedDate = DateTime.Now
            };

            // Save user data
            var userJson = JsonSerializer.Serialize(user);
            Preferences.Set(UserKey, userJson);
            Preferences.Set(AuthKey, true);

            return Task.FromResult(true);
        }

        public Task<bool> SignUpAsync(string email, string password, string name)
        {
            // Simple registration for demo purposes
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(name))
                return Task.FromResult(false);

            if (!email.Contains("@"))
                return Task.FromResult(false);

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Email = email,
                Name = name,
                CreatedDate = DateTime.Now
            };

            // Save user data
            var userJson = JsonSerializer.Serialize(user);
            Preferences.Set(UserKey, userJson);
            Preferences.Set(AuthKey, true);

            return Task.FromResult(true);
        }

        public void SignOut()
        {
            Preferences.Remove(UserKey);
            Preferences.Set(AuthKey, false);
        }

        public User? GetCurrentUser()
        {
            var userJson = Preferences.Get(UserKey, "");
            if (string.IsNullOrEmpty(userJson))
                return null;

            try
            {
                return JsonSerializer.Deserialize<User>(userJson);
            }
            catch
            {
                return null;
            }
        }

        public bool IsAuthenticated()
        {
            return Preferences.Get(AuthKey, false);
        }
    }
} 