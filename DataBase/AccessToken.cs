namespace TuneMates_Backend.DataBase
{
    public class AccessToken
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; } = 3600;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}