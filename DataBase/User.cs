namespace TuneMates_Backend.DataBase
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string SpotifyId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime TokenExpiresAt { get; set; } = DateTime.UtcNow.AddSeconds(3600);
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}